all: tags io

io: io.c
	gcc -O2 -o io io.c

tags: io.c
	ctags -R .
