#include "stdafx.h"
#include "ClrTrivalDeviceWrapper.h"
#pragma managed

#if INC_BUFFERS
using namespace Buffers::Devices;

ClrTrivalDeviceWrapper::
ClrTrivalDeviceWrapper()
: pdev(gcnew TrivalBlockDevice) { }

ClrTrivalDeviceWrapper::
ClrTrivalDeviceWrapper(Buffers::IBlockDevice^ pdevice)
: pdev(pdevice) { }


size_t ClrTrivalDeviceWrapper::
GetPageSize() const {
	return pdev->PageSize;
}

void ClrTrivalDeviceWrapper::
Read(size_t pageid, void *result) {
	pdev->Read(pageid, nullptr);
}

void ClrTrivalDeviceWrapper::
Write(size_t pageid, const void *data) {
	pdev->Write(pageid, nullptr);
}

int ClrTrivalDeviceWrapper::
GetReadCount() const {
	return pdev->ReadCount;
}

int ClrTrivalDeviceWrapper::
GetWriteCount() const {
	return pdev->WriteCount;
}

int ClrTrivalDeviceWrapper::
GetTotalCost() const {
	return Buffers::Utils::CalcTotalCost(pdev->ReadCount, pdev->WriteCount);
}

#endif INC_BUFFERS
