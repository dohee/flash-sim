#pragma once
#include <memory.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <map>

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

using namespace std;


#ifdef _DEBUG
#include <assert.h>
#define ASSERT(booleanExpression)	assert(booleanExpression)
#else
#define ASSERT(booleanExpression)
#endif //_DEBUG

#define LBA_IS_INVALID 0;
#define LBA_IS_VALID 1;

typedef map<int,int>::value_type  valType;
typedef int PID;

#define DEFBUFSIZE 1536//缓冲区大小

#define FRAMESIZE 2048//缓冲区中帧大小

typedef struct bFrame {
	char field[FRAMESIZE];
} bFrame;

typedef struct NewPage {
	int page_id;
	int frame_id;

public:
	NewPage(){ page_id = -1; frame_id = -1;}
	~NewPage(){}
} NewPage;

/*缓冲区算法基类，所有的缓冲区算法都从该类继承*/
class BMgr
{
public:
    BMgr(void);
    ~BMgr(void);

	virtual void Init() = 0;
    virtual int FixPage(int/*page_id*/) = 0;
    virtual NewPage FixNewPage(int/*LBA*/) = 0;
	virtual int UnFixPage(int/*page_id*/) = 0;
	virtual void ReadFrame(int/*frame_id*/,char * /*buffer*/) = 0;
	virtual void WriteFrame(int/*frame_id*/,const char * /*buffer*/) = 0;
	virtual int WriteDirty(void) = 0;
	virtual double HitRatio(void) = 0;
	virtual void RWInfo() = 0;
	virtual int IsLBAValid(LBA/*lba*/) = 0;
	virtual int LBAToPID(LBA/*lba*/) = 0;
};