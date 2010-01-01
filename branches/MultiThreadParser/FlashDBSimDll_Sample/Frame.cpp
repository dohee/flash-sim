#pragma managed(off)
#include "stdafx.h"
#include "Frame.h"
using namespace std;

char DataFrame::emptyData_[1] = {0};


DataFrame::DataFrame(size_t id, size_t size, bool resident)
: Frame(id), size_(size), data_(NULL)
{
	SetResident(resident);
}

DataFrame::~DataFrame()
{
	if (data_ != emptyData_)
		delete [] data_;
}

void DataFrame::SetResident(bool resident)
{
	if (resident == IsResident())
		return;

	if (resident) {
		data_ = (size_ > 0 ? new char[size_] : emptyData_);

	} else {
		if (Dirty)
			throw ::runtime_error("cannot throw away dirty data");

		if (data_ != emptyData_)
			delete [] data_;

		data_ = NULL;
	}
}