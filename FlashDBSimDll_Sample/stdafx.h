#ifdef _DEBUG
#pragma comment(lib, "../Debug/FlashDBSimDll.lib")
#else
#pragma comment(lib, "../Release/FlashDBSimDll.lib")
#endif

#define _CRT_SECURE_NO_WARNINGS 1

#include <memory>
#include <string>
#include <cstdlib>
#include <cstdio>
#include <iostream>
#include <vector>
#include <map>

#include "../FlashDBSimDll/flashdbsim_i.h"	/* 请根据具体工程修改文件相对路径 */

#define DLLIMPORT	extern "C" __declspec(dllimport)

DLLIMPORT
RV f_initialize(const VFD_INFO&, const FTL_INFO&);
DLLIMPORT
RV f_release(void);
DLLIMPORT
RV f_alloc_page(int, LBA *);
DLLIMPORT
RV f_release_page(LBA);
DLLIMPORT
RV f_read_page(LBA, BYTE *, int, size_t);
DLLIMPORT
RV f_write_page(LBA, const BYTE *, int, size_t);
DLLIMPORT
const IFTL * f_get_ftl_module(void);
DLLIMPORT
const IVFD * f_get_vfd_module(void);
