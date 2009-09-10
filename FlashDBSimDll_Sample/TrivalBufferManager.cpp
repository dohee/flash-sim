#include "stdafx.h"
#include "TrivalBufferManager.h"

TrivalBufferManager::TrivalBufferManager(std::tr1::shared_ptr<IBlockDevice> pDevice)
: pdev_(pDevice), read_(0), write_(0)
{ }

void TrivalBufferManager::Read(size_t addr, char *result)
{
	read_++;
	pdev_->Read(addr, result);
}

void TrivalBufferManager::Write(size_t addr, const char *data)
{
	write_++;
	pdev_->Write(addr, data);
}

void TrivalBufferManager::Flush()
{
}

int TrivalBufferManager::GetReadCount() const
{
	return read_;
}

int TrivalBufferManager::GetWriteCount() const
{
	return write_;
}

