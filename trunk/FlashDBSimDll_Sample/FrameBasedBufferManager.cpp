#include "stdafx.h"
#include "FrameBasedBufferManager.h"
#include "IBlockDevice.h"
#include "Frame.h"
using namespace std::tr1;


FrameBasedBufferManager::FrameBasedBufferManager(shared_ptr<IBlockDevice> pdev, size_t nPages)
: BufferManagerBase(pdev, nPages)
{ }

void FrameBasedBufferManager::DoRead(size_t pageid, void *result)
{
	shared_ptr<DataFrame> pframe = FindFrame(pageid);

	if (pframe.get() == NULL)
	{
		pframe = AllocFrame(pageid);
		pdev_->Read(pageid, pframe->Get());
	}

	memcpy(result, pframe->Get(), pagesize_);
}

void FrameBasedBufferManager::DoWrite(size_t pageid, const void *data)
{
	shared_ptr<DataFrame> pframe = FindFrame(pageid);

	if (pframe.get() == NULL)
		pframe = AllocFrame(pageid);

	memcpy(pframe->Get(), data, pagesize_);
	pframe->Dirty = true;
}

void FrameBasedBufferManager::WriteIfDirty(DataFrame& DataFrame)
{
	if (!DataFrame.Dirty)
		return;

	DataFrame.Dirty = false;
	pdev_->Write(DataFrame.Id, DataFrame.Get());
}
