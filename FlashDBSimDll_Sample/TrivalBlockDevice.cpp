#include "stdafx.h"
#include "TrivalBlockDevice.h"

TrivalBlockDevice::TrivalBlockDevice(size_t pageSize)
: pageSize_(pageSize)
{ }

void TrivalBlockDevice::Read(size_t addr, char *result)
{
	memset(result, 0, pageSize_);
}

void TrivalBlockDevice::Write(size_t addr, const char *data)
{
}