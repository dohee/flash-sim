/*
This program implements the LRUWSRWSR algorithm for Flash memory. This work is based on the LRUWSRManager developed by Xuexuan Chen
When a dirty DataFrame is selected to be victim the first time, it will only ++cold and be given another opportunity. The system will only evict clean 
and cold enough dirty page.
lyf  2009 9 13
*/
#include "stdafx.h"
#include "LRUWSRManager.h"
#include "IBlockDevice.h"
#include "Frame.h"
using namespace std;
using namespace stdext;
using namespace std::tr1;

struct LRUWSRFrame : public DataFrame
{
	size_t Cold;		//initially 0, when a dirty DataFrame is to be evicted then cold increase.

	LRUWSRFrame(size_t id, size_t size)
	: DataFrame(id, size), Cold(0)
	{ }
};


LRUWSRManager::LRUWSRManager(
	shared_ptr<IBlockDevice> pDevice, size_t nPages, size_t maxCold)
: BufferManagerBase(pDevice, nPages),
  maxcold_(maxCold),
  queue_(), map_()
{ }

LRUWSRManager::~LRUWSRManager()
{
	Flush();
}

void LRUWSRManager::DoRead(size_t pageid, void *result)
{
	shared_ptr<LRUWSRFrame> pframe = AccessFrame_(pageid);

	if (pframe.get() == NULL)
	{
		pframe = AcquireFrame_(pageid);
		pdev_->Read(pageid, pframe->Get());
	}

	memcpy(result, pframe->Get(), pagesize_);
}

void LRUWSRManager::DoWrite(size_t pageid, const void *data)
{
	shared_ptr<LRUWSRFrame> pframe = AccessFrame_(pageid);

	if (pframe.get() == NULL)
		pframe = AcquireFrame_(pageid);

	memcpy(pframe->Get(), data, pagesize_);
	pframe->Dirty = true;
}


shared_ptr<LRUWSRFrame> LRUWSRManager::AccessFrame_(size_t pageid)
{
	MapType::iterator iter = map_.find(pageid);

	if (iter == map_.end())
		return shared_ptr<LRUWSRFrame>();

	shared_ptr<LRUWSRFrame> pframe = *(iter->second);
	queue_.erase(iter->second);

	//accessed DataFrame is not cold.
	if(pframe->Cold>0)
	{
		pframe->Cold=0;
	}

	queue_.push_front(pframe);
	map_[pageid] = queue_.begin();
	return pframe;
}

shared_ptr<LRUWSRFrame> LRUWSRManager::AcquireFrame_(size_t pageid)
{
	AcquireSlot_();
	shared_ptr<LRUWSRFrame> pframe(new LRUWSRFrame(pageid, pagesize_));
	queue_.push_front(pframe);
	map_[pageid] = queue_.begin();
	return pframe;
}


void LRUWSRManager::AcquireSlot_()
{
	if (queue_.size() < npages_)
		return;

	shared_ptr<LRUWSRFrame> pframe;

	//find a clean DataFrame or cold enough dirty page
	while(true)
	{
		pframe = queue_.back();
		queue_.pop_back();
		map_.erase(pframe->Id);

		if(pframe->Dirty && pframe->Cold < maxcold_) {
			(pframe->Cold)++;
			queue_.push_front(pframe);
			map_[pframe->Id] = queue_.begin();
			continue;
		} else {
			break;
		}
	}
	
	WriteIfDirty(pframe);
}

void LRUWSRManager::WriteIfDirty(shared_ptr<LRUWSRFrame> pFrame)
{
	if (!pFrame->Dirty)
		return;

	pFrame->Dirty = false;
	pdev_->Write(pFrame->Id, pFrame->Get());
}

void LRUWSRManager::DoFlush()
{
	QueueType::iterator it, itend = queue_.end();

	for (it = queue_.begin(); it != itend; ++it)
		WriteIfDirty(*it);
}

