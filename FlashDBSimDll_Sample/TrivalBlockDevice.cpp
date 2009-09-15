#include "stdafx.h"
#include "TrivalBlockDevice.h"

TrivalBlockDevice::TrivalBlockDevice(size_t pageSize)
: pageSize_(pageSize), read_(0), write_(0)
{ }

size_t TrivalBlockDevice::GetPageSize() const
{
	return pageSize_;
}

void TrivalBlockDevice::Read(size_t addr, void *result)
{
	read_++;
}

void TrivalBlockDevice::Write(size_t addr, const void *data)
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
