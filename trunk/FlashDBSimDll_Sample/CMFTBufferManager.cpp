#include "stdafx.h"
#include "CMFTBufferManager.h"
#include "IBlockDevice.h"
#include "frame.h"
using namespace std;
using namespace stdext;
using namespace std::tr1;



CMFTBufferManager::CMFTBufferManager(shared_ptr<IBlockDevice> pDevice, size_t nPages)
: BufferManagerBase(pDevice, nPages)
{ }

CMFTBufferManager::~CMFTBufferManager()
{
	Flush();
}

void CMFTBufferManager::DoRead(size_t pageid, void *result)
{
}
void CMFTBufferManager::DoWrite(size_t pageid, const void *data)
{
}
void CMFTBufferManager::DoFlush()
{
}
