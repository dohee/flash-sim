#include "stdafx.h"
#include "Frame.h"
using namespace std;

static char c[1];

Frame::Frame(size_t id, size_t size)
: Id(id), Dirty(false), size_(size),
  data_(size_ > 0 ? new char[size_] : c)
{ }

Frame::~Frame()
{
	if (data_ != c)
		delete [] data_;
}

void* Frame::Get()
{
	return data_;
}

const void* Frame::Get() const
{
	return data_;
}
