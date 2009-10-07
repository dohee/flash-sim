#include "stdafx.h"
#include "ClrManagerWrapper.h"
#include "ClrTrivalDeviceWrapper.h"
using namespace std::tr1;
using namespace cli;
using namespace System;
using namespace System::Runtime::InteropServices;

ClrManagerWrapper::
ClrManagerWrapper(Buffers::IBufferManager^ pmanager)
: pmgr(pmanager),
  pdev(new ClrTrivalDeviceWrapper(pmanager->AssociatedDevice)),
  pagesize(pmanager->PageSize),
  buffer(gcnew array<unsigned char>(pagesize)) { }


void ClrManagerWrapper::
Read(size_t pageid, void *result) {
	pmgr->Read(pageid, buffer);
	Marshal::Copy(buffer, 0, IntPtr(result), pagesize);
}

void ClrManagerWrapper::
Write(size_t pageid, const void *data) {
	Marshal::Copy(IntPtr(const_cast<void*>(data)), buffer, 0, pagesize);
	pmgr->Write(pageid, buffer);
}

void ClrManagerWrapper::
Flush() {
	pmgr->Flush();
}
	
int ClrManagerWrapper::
GetReadCount() const {
	return pmgr->ReadCount;
}

int ClrManagerWrapper::
GetWriteCount() const {
	return pmgr->WriteCount;
}

std::tr1::shared_ptr<IBlockDevice> ClrManagerWrapper::
GetDevice() {
	return pdev;
}

std::tr1::shared_ptr<const IBlockDevice> ClrManagerWrapper::
GetDevice() const {
	return pdev;
}
