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
#include <time.h>

enum Actions {
	ActionNone, ActionRead, ActionWrite, ActionCreate,
};


const int BUFSIZE = 1024*1024*128;
const char *g_progname = NULL;
char *g_buf = NULL;
time_t g_startTime, g_endTime;
int g_reqcount = 0, g_rewindcount = 0, g_childOutputFD = -1, g_phase = 0;
FILE *g_statfile = NULL;


void Usage()
{
	fprintf(stderr, "Usage: %s <File> create <Size>\n"
					"       %s <File> read|write-sync|write-async seq|rnd\n"
					"           <TimePeriod> <ReqSize> [<ProcessCount>]\n",
		g_progname, g_progname, g_progname);

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
		int info[2] = { g_reqcount, g_rewindcount };
		write(g_childOutputFD, info, sizeof(info));
	} else {
		fprintf(g_statfile, "ReqCount/RewindCount: %d/%d  TimeElapsed: %d sec\n\n",
			g_reqcount, g_rewindcount, (int)(g_endTime-g_startTime));
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
	const char **filename, enum Actions *action, int *sync, int *rand,
	int *timeperiod, int *preperiod, int *postperiod, long long *size, int *nprocess)
{
	if (argc < 4)
		return -1;

	*filename = argv[1];
	*sync = 0;
	const char *actionstr = argv[2];

	if (strcasecmp(actionstr, "read") == 0) {
		*action = ActionRead;
	} else if (strcasecmp(actionstr, "write-sync") == 0) {
		*action = ActionWrite;
		*sync = 1;
	} else if (strcasecmp(actionstr, "write-async") == 0) {
		*action = ActionWrite;
		*sync = 0;
	} else if (strcasecmp(actionstr, "create") == 0) {
		*action = ActionCreate;
	} else {
		return -1;
	}

	char timestr[100]; time_t timet;
	timet = time(NULL);
	strftime(timestr, sizeof(timestr), "%Y-%m-%d %H:%M:%S", localtime(&timet));

	if (*action == ActionCreate) {
		*size = ParseSize(argv[3]);

		if (echo)
			printf("%s  Create  %lldBytes  '%s'\n", timestr, *size, *filename);

	} else {
		if (strcasecmp(argv[3], "seq") == 0)
			*rand = 0;
		else if (strcasecmp(argv[3], "rnd") == 0)
			*rand = 1;
		else
			return -1;

		char periodstr[100], *endstr;
		strcpy(periodstr, argv[4]);
		strcat(periodstr, "+0+0");
		*timeperiod = (int)strtol(periodstr, &endstr, 10);
		*preperiod = (int)strtol(endstr+1, &endstr, 10);
		*postperiod = (int)strtol(endstr+1, &endstr, 10);

		*size = ParseSize(argv[5]);
		*nprocess = (argc>=7 ? atoi(argv[6]) : 1);

		if (echo)
			printf("%s  %s  %s  %s  %d+%d+%dsec  %lldB/req  %dprocesses  '%s'\n",
				timestr,
				(*action==ActionRead ? "Read":"Write"),
				(*sync ? "Sync" : "Async"),
				(*rand ? "Rnd" : "Seq"),
				*timeperiod, *preperiod, *postperiod,
				*size, *nprocess, *filename);
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

	const int REQSIZE = 1024*1023 - 3;
	InitBuffer(REQSIZE);
	g_startTime = time(NULL);

	for (j=0; j<size; j+=REQSIZE, ++g_reqcount)
		if ((lr = write(fd, g_buf, REQSIZE)) != REQSIZE)
			Error((int)lr, "write failed");

	g_endTime = time(NULL);
	close(fd);
	return 0;
}

void ChildFileAccess(const char *filename, int isWrite, int sync, int random,
	long long reqsize, off_t curofs, off_t ofsmax)
{
	int r, fd=0;
	off_t ofr;

	if ((fd = open(filename, (isWrite?O_WRONLY:O_RDONLY) | (sync?O_SYNC|O_NDELAY:0))) < 0)
		Error(fd, "cannot open file");

	if ((ofr = lseek(fd, curofs*reqsize, SEEK_SET)) == (off_t)-1)
		Error((int)ofr, "lseek failed");

	while (g_phase < 3) {
		if (random) {
			off_t offset = (rand() % ofsmax) * reqsize;
			if ((ofr = lseek(fd, offset, SEEK_SET)) == (off_t)-1)
				Error((int)ofr, "lseek failed");
		} else {
			if (curofs++ >= ofsmax) {
				curofs = 1;
				++g_rewindcount;
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
	g_phase++;
}

void PrintDot(union sigval val)
{
	fprintf(stderr, ".");
}

int FileAccess(const char *filename, int isWrite, int sync, int random,
	int timeperiod, int preperiod, int postperiod, long long reqsize, int nprocess)
{
	int result=0, r, i, j, fd=0, fdchilds[100]={0}, childinfo[2];
	pid_t pidchilds[100] = {0};
	struct stat statbuf;
	struct sigevent sigev;
	struct itimerspec tmrspec;
	off_t ofsmax=0;

	sigev.sigev_notify = SIGEV_THREAD;
	sigev.sigev_notify_function = PrintDot;
	sigev.sigev_notify_attributes = NULL;
	timer_t tmrid;
	tmrspec.it_interval.tv_sec = 2;
	tmrspec.it_interval.tv_nsec = 0;
	tmrspec.it_value.tv_sec = 0;
	tmrspec.it_value.tv_nsec = 0;

	if ((r = timer_create(CLOCK_REALTIME, &sigev, &tmrid)) < 0)
		Error(r, "timer_create failed");
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
			ChildFileAccess(filename, isWrite, sync, random, reqsize, ofsmax*i/nprocess, ofsmax);
			exit(0);
		} else { /* we are the parent */
			close(pipes[1]);
			fdchilds[i] = pipes[0];
			pidchilds[i] = pid;
		}
	}


	if ((r = timer_settime(tmrid, 0, &tmrspec, NULL)) < 0)
		Error(r, "timer_settime failed");

	int sleeptime[3] = {preperiod, timeperiod, postperiod};

	for (j=0; j<3; ++j) {
		sleep(sleeptime[j]);
		fprintf(stderr, "|");

		for (i=0; i<nprocess; ++i)
			if ((r = kill(pidchilds[i], SIGALRM)) < 0)
				Error(r, "sending SIGALRM to Child.%d (pid=%d) failed", i, pidchilds[i]);
	}

	fprintf(stderr, "\n");
	printf("Children's ReqCount/RewindCount: ");

	for (i=0; i<nprocess; ++i) {
		if ((r = read(fdchilds[i], childinfo, sizeof(childinfo))) != sizeof(childinfo)) {
			ErrorNoExit(r, "read info from Child.%d failed", i);
			result = 1;
		} else {
			printf("%d/%d ", childinfo[0], childinfo[1]);
			g_reqcount += childinfo[0];
			g_rewindcount += childinfo[1];
		}
	}

	printf("\n");
	timer_delete(tmrid);
	g_endTime = time(NULL);
	return 0;
}


int main(int argc, char *argv[])
{
	Init(argv);

	int r;
	const char *filename;
	enum Actions action;
	int sync, rand, time1, time2, time3, nprocess;
	long long size;

	if ((r = ParseArguments(argc, argv, 1,
		&filename, &action, &sync, &rand, &time2, &time1, &time3, &size, &nprocess)) < 0)
		Usage();

	if (action == ActionCreate) {
		return FileCreate(filename, size);
	} else if (action == ActionRead) {
		return FileAccess(filename, 0, sync, rand, time2, time1, time3, size, nprocess);
	} else if (action == ActionWrite) {
		return FileAccess(filename, 1, sync, rand, time2, time1, time3, size, nprocess);
	} else {
		Usage();
	}

	return 0;
}
