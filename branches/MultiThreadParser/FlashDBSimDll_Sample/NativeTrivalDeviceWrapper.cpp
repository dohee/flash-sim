#include "stdafx.h"
#pragma unmanaged
#include "TrivalBlockDevice.h"

#pragma managed
#include "ClrDeviceWrapper.h"
#include "NativeTrivalDeviceWrapper.h"
using namespace cli;
using namespace System;
using namespace System::Runtime::InteropServices;


namespace Buffers {
	namespace Devices {
		namespace FromNative {

TrivalBlockDevice::
TrivalBlockDevice(::TrivalBlockDevice* p): pdev(p) { }

size_t TrivalBlockDevice::
PageSize::get() { return pdev->GetPageSize(); }

int TrivalBlockDevice::
ReadCount::get() { return pdev->GetReadCount(); }

int TrivalBlockDevice::
WriteCount::get() { return pdev->GetWriteCount(); }

void TrivalBlockDevice::
Read(size_t pageid, array<unsigned char>^ result) {
	pdev->Read(pageid, NULL);
}

void TrivalBlockDevice::
Write(size_t pageid, array<unsigned char>^ data) {
	pdev->Write(pageid, NULL);
}

		};
	};
};