#include "stdafx.h"
#pragma unmanaged
#include <memory>
#include "LRUManager.h"
#include "IBlockDevice.h"
#pragma managed
#include "ClrDeviceWrapper.h"
using namespace std::tr1;
using namespace cli;
using namespace System;
using namespace System::Runtime::InteropServices;


namespace Buffers {
	namespace Managers {
		namespace FromNative {


public ref class LRU : public Buffers::IBufferManager
{
private:
	Buffers::IBlockDevice^ pdev;
	::LRUManager* pmgr;
	size_t pagesize;
	unsigned char* buffer;

public:
	LRU(Buffers::IBlockDevice^ pdevice, size_t npages)
		: pdev(pdevice),
		pmgr(new ::LRUManager(shared_ptr<::IBlockDevice>(new ::ClrDeviceWrapper(pdevice)), npages)),
		pagesize(pdev->PageSize),
		buffer(new unsigned char[pagesize==0 ? 1 : pagesize]) { }

	~LRU() {
		delete [] buffer;
		delete pmgr;
	}


	virtual property size_t PageSize { size_t get() { return pagesize; } }
	virtual property int ReadCount { int get() { return pmgr->GetReadCount(); } }
	virtual property int WriteCount { int get() { return pmgr->GetWriteCount(); } }
	virtual property int FlushCount { int get() { return -1; } }

	virtual property Buffers::IBlockDevice^ AssociatedDevice {
		Buffers::IBlockDevice^ get() { return pdev; }
	}

	virtual void Read(size_t pageid, array<unsigned char>^ result) {
		pmgr->Read(pageid, buffer);
		if (pagesize != 0)
			Marshal::Copy(IntPtr(buffer), result, 0, pagesize);
	}

	virtual void Write(size_t pageid, array<unsigned char>^ data) {
		if (pagesize != 0)
			Marshal::Copy(data, 0, IntPtr(buffer), pagesize);
		pmgr->Write(pageid, buffer);
	}

	virtual void Flush() { pmgr->Flush(); }

};


		};
	};
};
