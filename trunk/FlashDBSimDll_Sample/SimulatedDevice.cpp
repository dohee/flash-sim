#include "stdafx.h"
#include "SimulatedDevice.h"
using namespace std;
using namespace std::tr1;

#ifdef _DEBUG
#pragma comment(lib, "../Debug/FlashDBSimDll.lib")
#else
#pragma comment(lib, "../Release/FlashDBSimDll.lib")
#endif

#include "../FlashDBSimDll/flashdbsim_i.h"	/* 请根据具体工程修改文件相对路径 */
#define DLLIMPORT	extern "C" __declspec(dllimport)
DLLIMPORT RV f_initialize(const VFD_INFO&, const FTL_INFO&);
DLLIMPORT RV f_release(void);
DLLIMPORT RV f_alloc_page(int, LBA *);
DLLIMPORT RV f_release_page(LBA);
DLLIMPORT RV f_read_page(LBA, BYTE *, int, size_t);
DLLIMPORT RV f_write_page(LBA, const BYTE *, int, size_t);
DLLIMPORT const IFTL * f_get_ftl_module(void);
DLLIMPORT const IVFD * f_get_vfd_module(void);


SimulatedDevice::SimulatedDevice()
{
	VFD_INFO vfdInfo;
	vfdInfo.id = ID_NAND_DEVICE_03;
	vfdInfo.blockCount = 1024;
	vfdInfo.pageCountPerBlock = 64;
	vfdInfo.pageSize.size1 = 2048;
	vfdInfo.pageSize.size2 = 0;
	vfdInfo.eraseLimitation = 100000;
	vfdInfo.readTime.randomTime = 25;
	vfdInfo.readTime.serialTime = 0;
	vfdInfo.programTime = 200;
	vfdInfo.eraseTime = 1500;

	FTL_INFO ftlInfo;
	ftlInfo.id = ID_FTL_01;
	ftlInfo.mapListSize = 65536;
	ftlInfo.wearLevelingThreshold = 4;

	f_initialize(vfdInfo, ftlInfo);
}

shared_ptr<SimulatedDevice> SimulatedDevice::Singleton()
{
	static shared_ptr<SimulatedDevice> ptr(new SimulatedDevice);
	return ptr;
}

size_t SimulatedDevice::GetPageSize() const
{
	throw std::runtime_error("not implemented");
}

void SimulatedDevice::Read(size_t addr, void *result)
{
	throw std::runtime_error("not implemented");
}

void SimulatedDevice::Write(size_t addr, const void *data)
{
	throw std::runtime_error("not implemented");
}

int SimulatedDevice::GetReadCount() const
{
	throw std::runtime_error("not implemented");
}

int SimulatedDevice::GetWriteCount() const
{
	throw std::runtime_error("not implemented");
}

 int SimulatedDevice::GetTotalCost() const
{
	throw std::runtime_error("not implemented");
}
