#include "stdafx.h"
#include "ClrDeviceWrapper.h"
#pragma managed
using namespace cli;
using namespace System;
using namespace System::Runtime::InteropServices;


ClrDeviceWrapper::
ClrDeviceWrapper(Buffers::IBlockDevice^ pdevice)
: pdev(pdevice),
  pagesize(pdevice->PageSize),
  buffer(gcnew array<unsigned char>(pagesize)) { }


size_t ClrDeviceWrapper::
GetPageSize() const {
	return pdev->PageSize;
}

void ClrDeviceWrapper::
Read(size_t pageid, void *result) {
	pdev->Read(pageid, buffer);
	if (pagesize != 0)
		Marshal::Copy(buffer, 0, IntPtr(result), pagesize);
}

void ClrDeviceWrapper::
Write(size_t pageid, const void *data) {
	if (pagesize != 0)
		Marshal::Copy(IntPtr(const_cast<void*>(data)), buffer, 0, pagesize);
	pdev->Write(pageid, buffer);
}

int ClrDeviceWrapper::
GetReadCount() const {
	return pdev->ReadCount;
}

int ClrDeviceWrapper::
GetWriteCount() const {
	return pdev->WriteCount;
}

int ClrDeviceWrapper::
GetTotalCost() const {
	return pdev->ReadCount*READCOST+WRITECOST*pdev->WriteCount;
}

