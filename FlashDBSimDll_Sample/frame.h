#ifndef _FRAME_H_
#define _FRAME_H_

#include <vector>
using namespace std;

struct Frame
{
	size_t Id;
	bool Dirty;
	int Cold;		//initially 0, when a dirty frame is to be evicted then cold increase.
	vector<char> Data;

	Frame(size_t id, size_t size)
	: Id(id), Dirty(false), Cold(0),
	  Data(vector<char>(size, -1))
	{ }
};

#endif