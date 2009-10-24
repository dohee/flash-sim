#include "stdafx.h"
#pragma unmanaged
#include <memory>
#include "IBlockDevice.h"
#include "IBufferManager.h"

#include "CFLRUDManager.h"
#include "CFLRUManager.h"
#include "LRUManager.h"
#include "CMFTManager.h"
#include "LRUWSRManager.h"
#include "T8Manager.h"

#pragma managed
#include "ClrDeviceWrapper.h"
#include "NativeTrivalDeviceWrapper.h"
using namespace std::tr1;
using namespace cli;
using namespace System;
using namespace System::Runtime::InteropServices;


namespace Buffers {
	namespace Managers {


public ref class Wrapper
{
private:
	Wrapper() { }

	ref class WrapperInner : public Buffers::IBufferManager
	{
	private:
		String^ name;
		Buffers::IBlockDevice^ pdev;
		size_t pagesize;
		unsigned char* buffer;
		::IBufferManager* pmgr;

	public:
		WrapperInner(const char* mgrname, Buffers::IBlockDevice^ pdevice, ::IBufferManager* pmanager)
			: name(Marshal::PtrToStringAnsi(IntPtr(const_cast<char*>(mgrname)))),
			pdev(pdevice),
			pagesize(pdevice->PageSize),
			buffer(new unsigned char[pagesize==0 ? 1 : pagesize]),
			pmgr(pmanager) { }

		WrapperInner(const char* mgrname, ::TrivalBlockDevice* pdevice, ::IBufferManager* pmanager)
			: name(Marshal::PtrToStringAnsi(IntPtr(const_cast<char*>(mgrname)))),
			pdev(gcnew Buffers::Devices::FromNative::TrivalBlockDevice(pdevice)),
			pagesize(pdev->PageSize),
			buffer(new unsigned char[pagesize==0 ? 1 : pagesize]),
			pmgr(pmanager) { }

		virtual ~WrapperInner() {
			delete [] buffer;
			delete pmgr;
		}

		virtual property String^ Name { String^ get() { return name; } }
		virtual property String^ Description { String^ get() { return nullptr; } }
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


public:

/* 万恶的 VS 竟然不完全支持下面的语法
#define FDECL1(t1, a1)	t1 a1
#define FDECL2(t2, a2, ...) t2 a2, FDECL1(__VA_ARGS__)
#define FDECL3(t3, a3, ...) t3 a3, FDECL2(__VA_ARGS__)
#define FDECL4(t4, a4, ...) t4 a4, FDECL3(__VA_ARGS__)
#define FDECL5(t5, a5, ...) t5 a5, FDECL4(__VA_ARGS__)
#define FDECL6(t6, a6, ...) t6 a6, FDECL5(__VA_ARGS__)
#define FDECL7(t7, a7, ...) t7 a7, FDECL6(__VA_ARGS__)
#define FDECL8(t8, a8, ...) t8 a8, FDECL7(__VA_ARGS__)

#define FCAST1(t1, a1)	(t1) a1
#define FCAST2(t2, a2, ...) (t2) a2, FCAST1(__VA_ARGS__)
#define FCAST3(t3, a3, ...) (t3) a3, FCAST2(__VA_ARGS__)
#define FCAST4(t4, a4, ...) (t3) a4, FCAST3(__VA_ARGS__)
#define FCAST5(t5, a5, ...) (t3) a5, FCAST4(__VA_ARGS__)
#define FCAST6(t6, a6, ...) (t3) a6, FCAST5(__VA_ARGS__)
#define FCAST7(t7, a7, ...) (t3) a7, FCAST6(__VA_ARGS__)
#define FCAST8(t8, a8, ...) (t3) a8, FCAST7(__VA_ARGS__)
*/


#define PART_A(s)														\
	static Buffers::IBufferManager^										\
	Create##s(Buffers::IBlockDevice^ pdevice, size_t npages

#define PART_B(s)											) {			\
		return gcnew WrapperInner(										\
		#s, pdevice, new s##Manager(shared_ptr<::IBlockDevice>(			\
			new ::ClrDeviceWrapper(pdevice)), npages

#define PART_C(s)									));					\
	}																	\
	static Buffers::IBufferManager^ Create##s(size_t npages

#define PART_D(s)											) {			\
		shared_ptr<::TrivalBlockDevice> pdev(new ::TrivalBlockDevice);	\
		return gcnew WrapperInner("Wrap<" #s ">", pdev.get(),			\
			new s##Manager(pdev, npages

#define PART_E							));								\
	}


#define WRAP0(s)		\
	PART_A(s) PART_B(s)	\
	PART_C(s) PART_D(s) PART_E

#define WRAP1(s,a,b)			\
	PART_A(s),a b PART_B(s),b	\
	PART_C(s),a b PART_D(s),b PART_E

#define WRAP2(s,a,b,c,d)				\
	PART_A(s),a b,c d	PART_B(s),b,d	\
	PART_C(s),a b,c d	PART_D(s),b,d PART_E

#define WRAP3(s,a,b,c,d,e,f)				\
	PART_A(s),a b,c d,e f	PART_B(s),b,d,f	\
	PART_C(s),a b,c d,e f	PART_D(s),b,d,f PART_E

#define WRAP4(s,a,b,c,d,e,f,g,h)					\
	PART_A(s),a b,c d,e f,g h	PART_B(s),b,d,f,h	\
	PART_C(s),a b,c d,e f,g h	PART_D(s),b,d,f,h PART_E


WRAP0(CFLRUD);
WRAP1(CFLRUD, size_t, initialWindowSize);
WRAP1(CFLRU, size_t, windowSize);
WRAP0(CMFT);
WRAP0(LRU);
WRAP0(LRUWSR);
WRAP1(LRUWSR, size_t, maxCold);
WRAP2(Tn, int, srLength, int, HowManyToKickWhenWriteInDR);
WRAP4(Tn, int, srLength, int, HowManyToKickWhenWriteInDR, bool, AdjustDRWhenReadInDR, bool, EnlargeCRWhenReadInDNR);
};


/* 旧的 wrapper 实现
#define WRAP0(s)					PART_A(s)					PART_B(s)			PART_C
#define WRAP1(s,a,b)				PART_A(s),a b				PART_B(s),b			PART_C
#define WRAP2(s,a,b,c,d)			PART_A(s),a b,c d			PART_B(s),b,d		PART_C
#define WRAP3(s,a,b,c,d,e,f)		PART_A(s),a b,c d,e f		PART_B(s),b,d,f		PART_C
#define WRAP4(s,a,b,c,d,e,f,g,h)	PART_A(s),a b,c d,e f,g h	PART_B(s),b,d,f,h	PART_C

#define PART_A(shortname)											\
public ref class shortname : public Wrapper {						\
public:																\
	shortname(Buffers::IBlockDevice^ pdevice, size_t npages

#define PART_B(shortname)								)			\
		: Wrapper(pdevice) {										\
		pmgr = new shortname##Manager(shared_ptr<::IBlockDevice>(	\
			new ::ClrDeviceWrapper(pdevice)), npages

#define PART_C									);					\
	}																\
}

WRAP1(CFLRUD, size_t, initialWindowSize);
WRAP1(CFLRU, size_t, windowSize);
WRAP0(CMFT);
WRAP0(LRU);
WRAP1(LRUWSR, size_t, maxCold);
WRAP4(Tn, int, srLength, int, HowManyToKickWhenWriteInDR, bool, AdjustDRWhenReadInDR, bool, EnlargeCRWhenReadInDNR);
*/

	};
};
