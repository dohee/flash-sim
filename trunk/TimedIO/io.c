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

const char *g_progname;

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
			printf("# File = %s\n# Action = Create\n# Size = %lld bytes\n",
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
			printf("# File = %s\n# Action = %s\n# Ramdon = %d\n# TimePeriod = %d sec\n# ReqSize = %lld bytes\n",
				*filename, (*action==ActionRead ? "Read":"Write"),
				*rand, *timeperiod, *reqsize);
	}

	return 0;
}

void Usage()
{
	fprintf(stderr, "Usage: %s <File> read|write seq|rnd <TimePeriod> <ReqSize>\n"
					"       %s <File> create <Size>\n",
		g_progname, g_progname);

	exit(1);
}

void Error(int r, const char *msg)
{
	fprintf(stderr, "%s: %s\n", g_progname, msg);
	exit(-r);
}


int FileCreate(const char *filename, long long size)
{
	int r, i; long lr; long long j;
	int fd;

	if ((r = open(filename, O_WRONLY|O_CREAT|O_TRUNC, 0666)) < 0)
		Error(r, "cannot open file");

	fd = r;

	const int BUFSIZE = 1024*1024;
	char *buf = malloc(BUFSIZE);
	srand(time());
	
	for (i=0; i<BUFSIZE; ++i)
		buf[i] = (char)rand();

	for (j=0; j<size; j+=BUFSIZE)
		if ((lr = write(fd, buf, BUFSIZE)) < 0)
			Error((int)lr, "write error");

	free(buf);

	if ((r = close(fd)) < 0)
		Error(r, "cannot close file");

	return 0;
}

int main(int argc, char *argv[])
{
	g_progname = argv[0];

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
	}	

	return 0;
}
