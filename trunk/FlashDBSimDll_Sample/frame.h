#ifndef _FRAME_H_
#define _FRAME_H_

#include <vector>
using namespace std;

struct Frame
{
	size_t Id;
	bool Dirty;
	vector<char> Data;

	Frame(size_t id, size_t size)
	: Id(id), Dirty(false),
	  Data(vector<char>(size, -1))
	{ }
};

#endif