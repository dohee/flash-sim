#pragma managed(off)
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
	shared_ptr<DataFrame> pframe = FindFrame(pageid, false);

	if (pframe.get() == NULL) {
		pframe = AllocFrame(pageid, false);
		ReadFromDev(*pframe);
	}

	memcpy(result, pframe->Get(), pagesize_);
}

void FrameBasedBufferManager::DoWrite(size_t pageid, const void *data)
{
	shared_ptr<DataFrame> pframe = FindFrame(pageid, true);

	if (pframe.get() == NULL)
		pframe = AllocFrame(pageid, true);

	memcpy(pframe->Get(), data, pagesize_);
	pframe->Dirty = true;
}

void FrameBasedBufferManager::ReadFromDev(DataFrame& frame)
{
	pdev_->Read(frame.Id, frame.Get());
}

void FrameBasedBufferManager::WriteIfDirty(DataFrame& frame)
{
	if (frame.Dirty) {
		pdev_->Write(frame.Id, frame.Get());
		frame.Dirty = false;
	}
}

