#include <sys/mman.h>
#include <sys/stat.h>
#include <sys/syscall.h>
#include <sys/types.h>
#include <sys/uio.h>
#include <fcntl.h>
#include <signal.h>
#include <stdarg.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

enum Actions {
	ActionNone, ActionRead, ActionWrite, ActionCreate,
};


const int BUFSIZE = 1024*1024*128;
const char *g_progname = NULL;
char *g_buf = NULL;
time_t g_startTime, g_endTime;
int g_reqcount = 0, g_wrapcount = 0, g_childOutputFD = -1, g_stopnow = 0;
FILE *g_statfile = NULL;


void Usage()
{
	fprintf(stderr, "Usage: %s <File> read|write seq|rnd <TimePeriod> <ReqSize> [<ProcessCount>]\n"
					"       %s <File> create <Size>\n",
		g_progname, g_progname);

	exit(1);
}
void VError(int r, const char *fmt, va_list ap)
{
	fprintf(stderr, "%s: ", g_progname);
	vfprintf(stderr, fmt, ap);
	fprintf(stderr, ". Return value: %d\n", r);
}
void Error(int r, const char *fmt, ...)
{
	va_list ap;
	va_start(ap, fmt);
	VError(r, fmt, ap);
	va_end(ap);
	exit(-r);
}
void ErrorNoExit(int r, const char *fmt, ...)
{
	va_list ap;
	va_start(ap, fmt);
	VError(r, fmt, ap);
	va_end(ap);
}

void OutputStats()
{
	if (g_childOutputFD >= 0) {
		int info[2] = { g_reqcount, g_wrapcount };
		write(g_childOutputFD, info, sizeof(info));
	} else {
		fprintf(g_statfile, "ReqCount/WrapCount: %d/%d\nTimeElapsed: %d sec\n\n",
			g_reqcount, g_wrapcount, (int)(g_endTime-g_startTime));
	}
}

void Init(char *argv[])
{
	g_progname = argv[0];
	g_statfile = stdout;

	int r;
	if ((r = atexit(OutputStats)) < 0)
		Error(r, "atexit failed");
}
void InitBuffer(int size)
{
	if (size > BUFSIZE)
		size = BUFSIZE;

	g_buf = malloc(size);
	srand(time(NULL));
	int i;

	for (i=0; i<size; ++i)
		g_buf[i] = (char)rand();
}

long long ParseSize(const char *str)
{
	char* endstr;
	long long size = strtoll(str, &endstr, 10);

	switch (*endstr) {
		case 'k': case 'K': size *= 1024; break;
		case 'm': case 'M': size *= 1024*1024; break;
		case 'g': case 'G': size *= 1024*1024*1024; break;
	}

	return size;
}
int ParseArguments(int argc, char *argv[], int echo,
	const char **filename, enum Actions *action, int *rand,
	int *timeperiod, long long *size, int *nprocess)
{
	if (argc < 4)
		return -1;

	*filename = argv[1];
	const char *actionstr = argv[2];

	if (strcasecmp(actionstr, "read") == 0)
		*action = ActionRead;
	else if (strcasecmp(actionstr, "write") == 0)
		*action = ActionWrite;
	else if (strcasecmp(actionstr, "create") == 0)
		*action = ActionCreate;
	else
		return -1;

	if (*action == ActionCreate) {
		*size = ParseSize(argv[3]);

		if (echo)
			printf("File = %s\nAction = Create\nSize = %lld bytes\n",
				*filename, *size);

	} else {
		if (strcasecmp(argv[3], "seq") == 0)
			*rand = 0;
		else if (strcasecmp(argv[3], "rnd") == 0)
			*rand = 1;
		else
			return -1;

		*timeperiod = atoi(argv[4]);
		*size = ParseSize(argv[5]);
		*nprocess = (argc>=7 ? atoi(argv[6]) : 1);

		if (echo)
			printf("File = %s\nAction = %s\n"
				"Ramdon = %d\nTimePeriod = %d sec\n"
				"ReqSize = %lld bytes\nProcessCount = %d\n",
				*filename, (*action==ActionRead ? "Read":"Write"),
				*rand, *timeperiod, *size, *nprocess);
	}

	if (echo)
		fflush(stdout);

	return 0;
}


int FileCreate(const char *filename, long long size)
{
	int r; long lr; long long j;
	int fd;

	if ((r = open(filename, O_WRONLY|O_CREAT|O_TRUNC, 0666)) < 0)
		Error(r, "cannot open file");

	fd = r;

	const int REQSIZE = 1024*1024;
	InitBuffer(REQSIZE);

	for (j=0; j<size; j+=REQSIZE, ++g_reqcount)
		if ((lr = write(fd, g_buf, REQSIZE)) != REQSIZE)
			Error((int)lr, "write failed");

	close(fd);
	return 0;
}

void ChildFileAccess(const char *filename, int isWrite, int random,
	long long reqsize, off_t curofs, off_t ofsmax)
{
	int r, fd=0;
	off_t ofr;

	if ((fd = open(filename, (isWrite ? O_WRONLY : O_RDONLY))) < 0)
		Error(fd, "cannot open file");

	while (!g_stopnow) {
		if (random) {
			off_t offset = (rand() % ofsmax) * reqsize;
			if ((ofr = lseek(fd, offset, SEEK_SET)) == (off_t)-1)
				Error((int)ofr, "lseek failed");
		} else {
			if (curofs++ >= ofsmax) {
				curofs = 1;
				++g_wrapcount;
				if ((ofr = lseek(fd, 0, SEEK_SET)) == (off_t)-1)
					Error((int)ofr, "lseek failed");
			}
		}

		if (isWrite) {
			if ((r = write(fd, g_buf, reqsize)) != reqsize)
				Error(r, "write failed");
		} else {
			if ((r = read(fd, g_buf, reqsize)) != reqsize)
				Error(r, "read failed");
		}

		++g_reqcount;
	}
}
static void ChildAlarmHdlr(int signo)
{
	g_stopnow = 1;
}

int FileAccess(const char *filename, int isWrite, int random,
	int timeperiod, long long reqsize, int nprocess)
{
	int result=0, r, i, fd=0, fdchilds[100]={0}, childinfo[2];
	struct stat statbuf;
	off_t ofsmax=0;

	if ((r = access(filename, (isWrite ? W_OK : R_OK))) < 0)
		Error(r, "access failed");

	if ((r = stat(filename, &statbuf)) < 0)
		Error(r, "stat failed");

	ofsmax = statbuf.st_size / reqsize;

	InitBuffer(reqsize);
	g_startTime = time(NULL);

	for (i=0; i<nprocess; ++i) {
		int pipes[2] = { i*2+10, i*2+11 };

		if ((r = pipe(pipes)) < 0)
			Error(r, "pipe failed");

		srand(rand());
		pid_t pid = fork();

		if (pid < 0) {
			Error((int)pid, "fork failed");
		} else if (pid == 0) { /* we are the child */
			close(pipes[0]);
			g_childOutputFD = pipes[1];
			signal(SIGALRM, ChildAlarmHdlr);
			alarm(timeperiod);
			ChildFileAccess(filename, isWrite, random, reqsize, ofsmax*i/nprocess, ofsmax);
			exit(0);
		} else { /* we are the parent */
			close(pipes[1]);
			fdchilds[i] = pipes[0];
		}
	}

	for (i=0; i<nprocess; ++i) {
		if ((r = read(fdchilds[i], childinfo, sizeof(childinfo))) != sizeof(childinfo)) {
			ErrorNoExit(r, "read info from Child.%d failed", i);
			result = 1;
		} else {
			printf("ReqCount/WrapCount of ChildNo.%d: %d/%d\n", i, childinfo[0], childinfo[1]);
			g_reqcount += childinfo[0];
			g_wrapcount += childinfo[1];
		}
	}

	g_endTime = time(NULL);
	return 0;
}


int main(int argc, char *argv[])
{
	Init(argv);

	int r;
	const char *filename;
	enum Actions action;
	int rand, timeperiod, nprocess;
	long long size;

	if ((r = ParseArguments(argc, argv, 1,
		&filename, &action, &rand, &timeperiod, &size, &nprocess)) < 0)
		Usage();

	if (action == ActionCreate) {
		return FileCreate(filename, size);
	} else if (action == ActionRead) {
		return FileAccess(filename, 0, rand, timeperiod, size, nprocess);
	} else if (action == ActionWrite) {
		return FileAccess(filename, 1, rand, timeperiod, size, nprocess);
	} else {
		Usage();
	}

	return 0;
}
