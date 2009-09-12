#include "stdafx.h"
#include "TrivalBufferManager.h"
#include "IBlockDevice.h"
using namespace std::tr1;


TrivalBufferManager::TrivalBufferManager(shared_ptr<IBlockDevice> pDevice)
: pdev_(pDevice)
{ }

void TrivalBufferManager::DoRead(size_t addr, void *result)
{
	pdev_->Read(addr, result);
}

void TrivalBufferManager::DoWrite(size_t addr, const void *data)
{
	pdev_->Write(addr, data);
}

void TrivalBufferManager::DoFlush()
{
}
