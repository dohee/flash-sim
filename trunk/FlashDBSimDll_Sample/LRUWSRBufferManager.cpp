/*
This program implements the LRUWSRWSR algorithm for Flash memory. This work is based on the LRUWSRBufferManager developed by Xuexuan Chen
When a dirty frame is selected to be victim the first time, it will only ++cold and be given another opportunity. The system will only evict clean 
and cold enough dirty page.
lyf  2009 9 13
*/
#include "stdafx.h"
#include "LRUWSRBufferManager.h"
#include "IBlockDevice.h"
#include "frame.h"
using namespace std;
using namespace stdext;
using namespace std::tr1;

struct LRUWSRFrame : public Frame
{
	size_t Cold;		//initially 0, when a dirty frame is to be evicted then cold increase.

	LRUWSRFrame(size_t id, size_t size)
	: Frame(id, size), Cold(0)
	{ }
};


class LRUWSRBufferManagerImpl
{
public:
	LRUWSRBufferManagerImpl(shared_ptr<IBlockDevice> pDevice, size_t nPages, size_t maxCold);
	void Read(size_t addr, void *result);
	void Write(size_t addr, const void *data);
	void Flush();
private:
	shared_ptr<LRUWSRFrame> AccessFrame_(size_t pageid);
	shared_ptr<LRUWSRFrame> AcquireFrame_(size_t pageid);
	void AcquireSlot_();
	void WriteIfDirty(shared_ptr<LRUWSRFrame> pFrame);

private:
	shared_ptr<IBlockDevice> pdev_;
	size_t pagesize_, npages_, maxcold_;
	
	typedef list<shared_ptr<LRUWSRFrame> > QueueType;
	typedef hash_map<size_t, QueueType::iterator> MapType;
	QueueType queue_;
	MapType map_;
};

LRUWSRBufferManagerImpl::LRUWSRBufferManagerImpl(
	shared_ptr<IBlockDevice> pDevice, size_t nPages, size_t maxCold)
: pdev_(pDevice),
  pagesize_(pDevice->GetPageSize()), npages_(nPages),
  maxcold_(maxCold),
  queue_(), map_()
{ }

void LRUWSRBufferManagerImpl::Read(size_t addr, void *result)
{
	size_t pageid = addr;
	shared_ptr<LRUWSRFrame> pframe = AccessFrame_(pageid);

	if (pframe.get() == NULL)
	{
		pframe = AcquireFrame_(pageid);
		pdev_->Read(pageid * pagesize_, &(pframe->Data.front()));
	}

	memcpy(result, &(pframe->Data.front()), pagesize_);
}

void LRUWSRBufferManagerImpl::Write(size_t addr, const void *data)
{
	size_t pageid = addr;
	shared_ptr<LRUWSRFrame> pframe = AccessFrame_(pageid);

	if (pframe.get() == NULL)
		pframe = AcquireFrame_(pageid);

	memcpy(&(pframe->Data.front()), data, pagesize_);
	pframe->Dirty = true;
}


shared_ptr<LRUWSRFrame> LRUWSRBufferManagerImpl::AccessFrame_(size_t pageid)
{
	MapType::iterator iter = map_.find(pageid);

	if (iter == map_.end())
		return shared_ptr<LRUWSRFrame>();

	shared_ptr<LRUWSRFrame> pframe = *(iter->second);
	queue_.erase(iter->second);

	//accessed frame is not cold.
	if(pframe->Cold>0)
	{
		pframe->Cold=0;
	}

	queue_.push_front(pframe);
	map_[pageid] = queue_.begin();
	return pframe;
}

shared_ptr<LRUWSRFrame> LRUWSRBufferManagerImpl::AcquireFrame_(size_t pageid)
{
	AcquireSlot_();
	shared_ptr<LRUWSRFrame> pframe(new LRUWSRFrame(pageid, pagesize_));
	queue_.push_front(pframe);
	map_[pageid] = queue_.begin();
	return pframe;
}


void LRUWSRBufferManagerImpl::AcquireSlot_()
{
	if (queue_.size() < npages_)
		return;

	shared_ptr<LRUWSRFrame> pframe;

	//find a clean frame or cold enough dirty page
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

void LRUWSRBufferManagerImpl::WriteIfDirty(shared_ptr<LRUWSRFrame> pFrame)
{
	if (!pFrame->Dirty)
		return;

	pFrame->Dirty = false;
	pdev_->Write(pFrame->Id * pagesize_, &(pFrame->Data.front()));
}

void LRUWSRBufferManagerImpl::Flush()
{
	QueueType::iterator it, itend = queue_.end();

	for (it = queue_.begin(); it != itend; ++it)
		WriteIfDirty(*it);
}



LRUWSRBufferManager::LRUWSRBufferManager(
	shared_ptr<IBlockDevice> pDevice, size_t nPages, size_t maxCold)
: pImpl(new LRUWSRBufferManagerImpl(pDevice, nPages, maxCold))
{ }

LRUWSRBufferManager::~LRUWSRBufferManager()
{
	Flush();
}

void LRUWSRBufferManager::DoRead(size_t addr, void *result)
{
	pImpl->Read(addr, result);
}
void LRUWSRBufferManager::DoWrite(size_t addr, const void *data)
{
	pImpl->Write(addr, data);
}
void LRUWSRBufferManager::DoFlush()
{
	pImpl->Flush();
}
