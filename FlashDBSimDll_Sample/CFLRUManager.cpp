#pragma managed(off)
/*
This program implements the CFLRU algorithm for Flash memory proposed in 2006. This work is based on the LRUManager developed by Xuexuan Chen
lyf  2009 9 13
*/
#include "stdafx.h"
#include <stdexcept>
#include "CFLRUManager.h"
#include "IBlockDevice.h"
#include "Frame.h"

using namespace std;
using namespace stdext;
using namespace std::tr1;


CFLRUManager::CFLRUManager(shared_ptr<IBlockDevice> pDevice, size_t nPages, size_t iwindowSize)
: BufferManagerBase(pDevice, nPages),
  windowSize(iwindowSize),
  queue_(), map_()
{
	if (iwindowSize > nPages)
		throw std::runtime_error("WindowSize larger than NumOfPages");
}

CFLRUManager::~CFLRUManager()
{
	Flush();
}

void CFLRUManager::DoRead(size_t pageid, void *result)
{
	shared_ptr<DataFrame> pframe = AccessFrame_(pageid);

	if (pframe.get() == NULL)
	{
		pframe = AcquireFrame_(pageid);
		pdev_->Read(pageid, pframe->Get());

	}

	memcpy(result, pframe->Get(), pagesize_);
}

void CFLRUManager::DoWrite(size_t pageid, const void *data)
{
	shared_ptr<DataFrame> pframe = AccessFrame_(pageid);

	if (pframe.get() == NULL)
		pframe = AcquireFrame_(pageid);

	memcpy(pframe->Get(), data, pagesize_);
	pframe->Dirty = true;
}


shared_ptr<DataFrame> CFLRUManager::AccessFrame_(size_t pageid)
{
	MapType::iterator iter = map_.find(pageid);

	if (iter == map_.end())
		return shared_ptr<DataFrame>();

	shared_ptr<DataFrame> pframe = *(iter->second);
	queue_.erase(iter->second);
	queue_.push_front(pframe);
	map_[pageid] = queue_.begin();
	return pframe;
}

shared_ptr<DataFrame> CFLRUManager::AcquireFrame_(size_t pageid)
{
	AcquireSlot_();
	shared_ptr<DataFrame> pframe(new DataFrame(pageid, pagesize_));
	queue_.push_front(pframe);
	map_[pageid] = queue_.begin();
	return pframe;
}


void CFLRUManager::AcquireSlot_()
{
	if (queue_.size() < npages_)
		return;

	QueueType::iterator it = queue_.end();
	shared_ptr<DataFrame> pframe;
	
	size_t i = windowSize;

	//Find the first clean page in window.
	while (i--) {
		--it;
		pframe = *it;
		if (!(pframe->Dirty))
			break;
	}

	//There is no clean page in window, get the lru dirty DataFrame of the queue.
	if (pframe.get() == NULL) {
		it = queue_.end();
		--it;
		pframe = *it;
	}

	WriteIfDirty(pframe);
	queue_.erase(it);
	map_.erase(pframe->Id);
}

void CFLRUManager::WriteIfDirty(shared_ptr<DataFrame> pFrame)
{
	if (!pFrame->Dirty)
		return;

	pFrame->Dirty = false;
	pdev_->Write(pFrame->Id, pFrame->Get());
}

void CFLRUManager::DoFlush()
{
	QueueType::iterator it, itend = queue_.end();

	
	for (it = queue_.begin(); it != itend; ++it) {
		//cout << ((*it)->Dirty ? 1 : 0);
		WriteIfDirty(*it);
	}
	//cout << endl;
	
}
