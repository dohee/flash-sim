#pragma managed(off)
#include "stdafx.h"
#include "TrivalBufferManager.h"
#include "IBlockDevice.h"
using namespace std::tr1;


TrivalBufferManager::TrivalBufferManager(shared_ptr<IBlockDevice> pDevice)
: BufferManagerBase(pDevice, 0)
{ }

void TrivalBufferManager::DoRead(size_t pageid, void *result)
{
	pdev_->Read(pageid, result);
}

void TrivalBufferManager::DoWrite(size_t pageid, const void *data)
{
	pdev_->Write(pageid, data);
}

void TrivalBufferManager::DoFlush()
{
}
