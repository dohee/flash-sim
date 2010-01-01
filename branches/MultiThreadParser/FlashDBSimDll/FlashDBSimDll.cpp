// FlashDBSimDll.cpp : 定义 DLL 应用程序的导出函数。
//

#include "stdafx.h"

#include "interface.h"
#include "FlashDBSim.h"

extern "C" DLLEXPORT
RV f_initialize(const VFD_INFO& vfdInfo, const FTL_INFO& ftlInfo) {
	return FlashDBSim::Initialize(vfdInfo, ftlInfo);
}

extern "C" DLLEXPORT
RV f_release(void) {
	return FlashDBSim::Release();
}

extern "C" DLLEXPORT
int f_alloc_page(int count, LBA * lbas) {
	return FlashDBSim::AllocPage(count, lbas);
}

extern "C" DLLEXPORT
RV f_release_page(LBA lba) {
	return FlashDBSim::ReleasePage(lba);
}

extern "C" DLLEXPORT
RV f_read_page(LBA lba, BYTE * buffer, int offset, size_t size) {
	return FlashDBSim::ReadPage(lba, buffer, offset, size);
}

extern "C" DLLEXPORT
RV f_write_page(LBA lba, const BYTE * buffer, int offset, size_t size) {
	return FlashDBSim::WritePage(lba, buffer, offset, size);
}

extern "C" DLLEXPORT
const IFTL * f_get_ftl_module(void) {
	return FlashDBSim::GetFTLModule();
}

extern "C" DLLEXPORT
const IVFD * f_get_vfd_module(void) {
	IFTL * ftl = const_cast<IFTL *>(FlashDBSim::GetFTLModule());

	if (ftl) return ftl->GetFlashDevice();
	else return NULL;
}
