#pragma managed(off)
#include "stdafx.h"
#include "TrivalBlockDevice.h"

const int WRITECOST = 200;
const int READCOST = 66;


TrivalBlockDevice::TrivalBlockDevice()
: read_(0), write_(0)
{ }

size_t TrivalBlockDevice::GetPageSize() const
{
	return 0; // always returns 0, indicating it's trival
}

void TrivalBlockDevice::Read(size_t pageid, void *result)
{
	read_++;
}

void TrivalBlockDevice::Write(size_t pageid, const void *data)
{
	write_++;
}

int TrivalBlockDevice::GetReadCount() const
{
	return read_;
}

int TrivalBlockDevice::GetWriteCount() const
{
	return write_;
}

int TrivalBlockDevice::GetTotalCost() const
{
	return read_*READCOST+WRITECOST*write_;
}
