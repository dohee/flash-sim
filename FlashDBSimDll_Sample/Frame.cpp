#include "stdafx.h"
#include "Frame.h"
using namespace std;

static char c[1];

DataFrame::DataFrame(size_t id, size_t size)
: Frame(id), size_(size),
  data_(size_ > 0 ? new char[size_] : c)
{ }

DataFrame::~DataFrame()
{
	if (data_ != c)
		delete [] data_;
}

void* DataFrame::Get()
{
	return data_;
}

const void* DataFrame::Get() const
{
	return data_;
}
