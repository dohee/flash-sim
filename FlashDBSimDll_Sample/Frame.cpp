#include "stdafx.h"
#include "Frame.h"
using namespace std;

Frame::Frame(size_t id, size_t size)
: Id(id), Dirty(false),
  size_(size), data_(NULL)
{
	if (size_ > 0)
		data_ = new char[size_];
}

Frame::~Frame()
{
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
