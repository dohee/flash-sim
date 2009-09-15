#include "stdafx.h"
#include "TrivalBufferManager.h"
#include "IBlockDevice.h"
using namespace std::tr1;


TrivalBufferManager::TrivalBufferManager(shared_ptr<IBlockDevice> pDevice)
: BufferManagerBase(pDevice)
{ }

void TrivalBufferManager::DoRead(size_t pageid, void *result)
{
	DeviceRead(pageid, result);
}

void TrivalBufferManager::DoWrite(size_t pageid, const void *data)
{
	DeviceWrite(pageid, data);
}

void TrivalBufferManager::DoFlush()
{
}
