#include <sys/mman.h>
#include <sys/stat.h>
#include <sys/syscall.h>
#include <sys/types.h>
#include <sys/uio.h>
#include <fcntl.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

enum Actions {
	ActionNone, ActionRead, ActionWrite, ActionCreate,
};


const int BUFSIZE = 1024*1024*128;
const char *g_progname = NULL;
char *g_buf = NULL;
//struct timeval g_startTime, g_endTime;
int g_reqcount = 0, g_wrapcount = 0;


void Usage()
{
	fprintf(stderr, "Usage: %s <File> read|write seq|rnd <TimePeriod> <ReqSize>\n"
					"       %s <File> create <Size>\n",
		g_progname, g_progname);

	exit(1);
}
void Error(int r, const char *msg)
{
	fprintf(stderr, "%s: %s. Return value: %d\n", g_progname, msg, r);
	exit(-r);
}

void OutputStats()
{
	printf("ReqCount: %d\nWrapCount: %d\n",
		g_reqcount, g_wrapcount);
}

void Init(char *argv[])
{
	g_progname = argv[0];

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
	int *timeperiod, long long *reqsize, long long *size)
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
		*reqsize = ParseSize(argv[5]);

		if (echo)
			printf("File = %s\nAction = %s\nRamdon = %d\nTimePeriod = %d sec\nReqSize = %lld bytes\n",
				*filename, (*action==ActionRead ? "Read":"Write"),
				*rand, *timeperiod, *reqsize);
	}

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

	if ((r = close(fd)) < 0)
		Error(r, "cannot close file");

	return 0;
}

int FileAccess(const char *filename, int isWrite, int random, int timeperiod, long long reqsize)
{
	int r;
	int fd;
	time_t startTime;
	struct stat statbuf;
	off_t ofsmax, curofs = 0;

	if ((r = open(filename, (isWrite ? O_WRONLY : O_RDONLY))) < 0)
		Error(r, "cannot open file");

	fd = r;

	if ((r = fstat(fd, &statbuf)) < 0)
		Error(r, "fstat failed");

	ofsmax = statbuf.st_size / reqsize;
	InitBuffer(reqsize);

	startTime = time(NULL);

	//if ((r = gettimeofday(&g_startTime, NULL)) < 0)
	//	Error(r, "gettimeofday failed");

	while (time(NULL) - startTime < timeperiod) {
		if (random) {
			off_t offset = (rand() % ofsmax) * reqsize;
			if ((r = lseek(fd, offset, SEEK_SET)) < 0)
				Error(r, "lseek failed");
		} else {
			if (curofs++ >= ofsmax) {
				curofs = 1;
				++g_wrapcount;
				if ((r = lseek(fd, 0, SEEK_SET)) < 0)
					Error(r, "lseek failed");
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

	//if ((r = gettimeofday(&g_endTime, NULL)) < 0)
	//	Error(r, "gettimeofday failed");

	if ((r = close(fd)) < 0)
		Error(r, "cannot close file");

	return 0;
}


int main(int argc, char *argv[])
{
	Init(argv);

	int r;
	const char *filename;
	enum Actions action;
	int rand;
	int timeperiod;
	long long reqsize, size;
	
	if ((r = ParseArguments(argc, argv, 1,
		&filename, &action, &rand, &timeperiod, &reqsize, &size)) < 0)
		Usage();

	if (action == ActionCreate) {
		return FileCreate(filename, size);
	} else if (action == ActionRead) {
		return FileAccess(filename, 0, rand, timeperiod, reqsize);
	} else if (action == ActionWrite) {
		return FileAccess(filename, 1, rand, timeperiod, reqsize);
	} else {
		Usage();
	}

	return 0;
}
