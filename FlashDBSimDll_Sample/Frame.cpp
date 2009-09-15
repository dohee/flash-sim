#include "stdafx.h"
#include "Frame.h"
using namespace std;

Frame::Frame(size_t id, size_t size)
: Id(id), Dirty(false),
  size_(size), inited_(false), data_(NULL)
{ }

Frame::~Frame()
{
	delete [] data_;
}

void* Frame::Get()
{
	return const_cast<void *>(
		static_cast<const Frame &>(*this).Get()
		);
}

const void* Frame::Get() const
{
	if (!inited_) {
		if (size_ > 0)
			data_ = new char[size_];
		inited_ = true;
	}

	return data_;
}
