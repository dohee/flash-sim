/*
This program implements the CFLRU algorithm for Flash memory proposed in 2006. This work is based on the LRUBufferManager developed by Xuexuan Chen
lyf  2009 9 13
*/
#include "stdafx.h"
#include <stdexcept>
#include "CFLRUBufferManager.h"
#include "IBlockDevice.h"
#include "frame.h"

using namespace std;
using namespace stdext;
using namespace std::tr1;


CFLRUBufferManager::CFLRUBufferManager(shared_ptr<IBlockDevice> pDevice, size_t nPages, size_t iwindowSize)
: BufferManagerBase(pDevice),
  pagesize_(pdev_->GetPageSize()), npages_(nPages), windowSize(iwindowSize),
  queue_(), map_()
{
	if (iwindowSize > nPages)
		throw std::runtime_error("WindowSize larger than NumOfPages");
}

CFLRUBufferManager::~CFLRUBufferManager()
{
	Flush();
}

void CFLRUBufferManager::DoRead(size_t pageid, void *result)
{
	shared_ptr<Frame> pframe = AccessFrame_(pageid);

	if (pframe.get() == NULL)
	{
		pframe = AcquireFrame_(pageid);
		pdev_->Read(pageid, pframe->Get());

	}

	memcpy(result, pframe->Get(), pagesize_);
}

void CFLRUBufferManager::DoWrite(size_t pageid, const void *data)
{
	shared_ptr<Frame> pframe = AccessFrame_(pageid);

	if (pframe.get() == NULL)
		pframe = AcquireFrame_(pageid);

	memcpy(pframe->Get(), data, pagesize_);
	pframe->Dirty = true;
}


shared_ptr<Frame> CFLRUBufferManager::AccessFrame_(size_t pageid)
{
	MapType::iterator iter = map_.find(pageid);

	if (iter == map_.end())
		return shared_ptr<Frame>();

	shared_ptr<Frame> pframe = *(iter->second);
	queue_.erase(iter->second);
	queue_.push_front(pframe);
	map_[pageid] = queue_.begin();
	return pframe;
}

shared_ptr<Frame> CFLRUBufferManager::AcquireFrame_(size_t pageid)
{
	AcquireSlot_();
	shared_ptr<Frame> pframe(new Frame(pageid, pagesize_));
	queue_.push_front(pframe);
	map_[pageid] = queue_.begin();
	return pframe;
}


void CFLRUBufferManager::AcquireSlot_()
{
	if (queue_.size() < npages_)
		return;

	QueueType::iterator it = queue_.end();
	--it;
	shared_ptr<Frame> pframe = *it;
	
	size_t i = 0;
	//Find the first clean page in window.
	for(i=0; i<windowSize; i++,--it)
	{
		pframe = *it;
		if(!(pframe ->Dirty))
		{
			break;
		}
	}

	//There is no clean page in window, get the lru dirty frame of the queue.
	if(i >= windowSize)
	{
		it = queue_.end();
		--it;
	}

	pframe = *it;
	WriteIfDirty(pframe);
	queue_.erase(it);
	map_.erase(pframe->Id);
}

void CFLRUBufferManager::WriteIfDirty(shared_ptr<Frame> pFrame)
{
	if (!pFrame->Dirty)
		return;

	pFrame->Dirty = false;
	pdev_->Write(pFrame->Id, pFrame->Get());
}

void CFLRUBufferManager::DoFlush()
{
	QueueType::iterator it, itend = queue_.end();

	for (it = queue_.begin(); it != itend; ++it)
		WriteIfDirty(*it);
}



