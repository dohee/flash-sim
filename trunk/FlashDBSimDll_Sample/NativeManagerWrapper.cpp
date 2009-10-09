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
using namespace std::tr1;
using namespace cli;
using namespace System;
using namespace System::Runtime::InteropServices;


namespace Buffers {
	namespace Managers {
		namespace FromNative {


public ref class WrapperBase abstract : public Buffers::IBufferManager
{
protected:
	Buffers::IBlockDevice^ pdev;
	size_t pagesize;
	unsigned char* buffer;
	::IBufferManager* pmgr;

public:
	WrapperBase(Buffers::IBlockDevice^ pdevice)
		: pdev(pdevice),
		pagesize(pdevice->PageSize),
		buffer(new unsigned char[pagesize==0 ? 1 : pagesize]),
		pmgr(NULL) { }

	virtual ~WrapperBase() {
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


#define PARTA(shortname)											\
public ref class shortname : public WrapperBase {					\
public:																\
	shortname(Buffers::IBlockDevice^ pdevice, size_t npages

#define PARTB(shortname)								)			\
		: WrapperBase(pdevice) {									\
		pmgr = new shortname##Manager(shared_ptr<::IBlockDevice>(	\
			new ::ClrDeviceWrapper(pdevice)), npages

#define PARTC									);					\
	}																\
}

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

#define WRAP0(s)		PARTA(s) PARTB(s) PARTC
#define WRAP1(s, ...)	PARTA(s),FDECL1(__VA_ARGS__) PARTB(s),FCAST1(__VA_ARGS__) PARTC
#define WRAP2(s, ...)	PARTA(s),FDECL2(__VA_ARGS__) PARTB(s),FCAST2(__VA_ARGS__) PARTC
#define WRAP3(s, ...)	PARTA(s),FDECL3(__VA_ARGS__) PARTB(s),FCAST3(__VA_ARGS__) PARTC
#define WRAP4(s, ...)	PARTA(s),FDECL4(__VA_ARGS__) PARTB(s),FCAST4(__VA_ARGS__) PARTC
#define WRAP5(s, ...)	PARTA(s),FDECL5(__VA_ARGS__) PARTB(s),FCAST5(__VA_ARGS__) PARTC
#define WRAP6(s, ...)	PARTA(s),FDECL6(__VA_ARGS__) PARTB(s),FCAST6(__VA_ARGS__) PARTC
#define WRAP7(s, ...)	PARTA(s),FDECL7(__VA_ARGS__) PARTB(s),FCAST7(__VA_ARGS__) PARTC
#define WRAP8(s, ...)	PARTA(s),FDECL8(__VA_ARGS__) PARTB(s),FCAST8(__VA_ARGS__) PARTC
*/

#define WRAP0(s)					PARTA(s)					PARTB(s)			PARTC
#define WRAP1(s,a,b)				PARTA(s),a b				PARTB(s),b			PARTC
#define WRAP2(s,a,b,c,d)			PARTA(s),a b,c d			PARTB(s),b,d		PARTC
#define WRAP3(s,a,b,c,d,e,f)		PARTA(s),a b,c d,e f		PARTB(s),b,d,f		PARTC
#define WRAP4(s,a,b,c,d,e,f,g,h)	PARTA(s),a b,c d,e f,g h	PARTB(s),b,d,f,h	PARTC


WRAP1(CFLRUD, size_t, initialWindowSize);
WRAP1(CFLRU, size_t, windowSize);
WRAP0(CMFT);
WRAP0(LRU);
WRAP1(LRUWSR, size_t, maxCold);
WRAP4(Tn, int, srLength, int, HowManyToKickWhenWriteInDR, bool, AdjustDRWhenReadInDR, bool, EnlargeCRWhenReadInDNR);

		};
	};
};
