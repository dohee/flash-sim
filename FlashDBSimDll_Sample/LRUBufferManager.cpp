#include "stdafx.h"
#include "LRUBufferManager.h"
#include "IBlockDevice.h"
#include "frame.h"
using namespace std;
using namespace stdext;
using namespace std::tr1;


LRUBufferManager::LRUBufferManager(shared_ptr<IBlockDevice> pdev, size_t nPages)
: BufferManagerBase(pdev),
  pagesize_(GetPageSize()), npages_(nPages),
  queue_(), map_()
{ }

LRUBufferManager::~LRUBufferManager()
{
	Flush();
}

void LRUBufferManager::DoRead(size_t pageid, void *result)
{
	shared_ptr<Frame> pframe = AccessFrame_(pageid);

	if (pframe.get() == NULL)
	{
		pframe = AcquireFrame_(pageid);
		DeviceRead(pageid, pframe->Get());
	}

	memcpy(result, pframe->Get(), pagesize_);
}

void LRUBufferManager::DoWrite(size_t pageid, const void *data)
{
	shared_ptr<Frame> pframe = AccessFrame_(pageid);

	if (pframe.get() == NULL)
		pframe = AcquireFrame_(pageid);

	memcpy(pframe->Get(), data, pagesize_);
	pframe->Dirty = true;
}


shared_ptr<Frame> LRUBufferManager::AccessFrame_(size_t pageid)
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

shared_ptr<Frame> LRUBufferManager::AcquireFrame_(size_t pageid)
{
	AcquireSlot_();
	shared_ptr<Frame> pframe(new Frame(pageid, pagesize_));
	queue_.push_front(pframe);
	map_[pageid] = queue_.begin();
	return pframe;
}


void LRUBufferManager::AcquireSlot_()
{
	if (queue_.size() < npages_)
		return;

	QueueType::iterator it = queue_.end();
	--it;
	shared_ptr<Frame> pframe = *it;
	WriteIfDirty(pframe);
	queue_.erase(it);
	map_.erase(pframe->Id);
}

void LRUBufferManager::WriteIfDirty(shared_ptr<Frame> pFrame)
{
	if (!pFrame->Dirty)
		return;

	pFrame->Dirty = false;
	DeviceWrite(pFrame->Id, pFrame->Get());
}

void LRUBufferManager::DoFlush()
{
	QueueType::iterator it, itend = queue_.end();

	for (it = queue_.begin(); it != itend; ++it)
		WriteIfDirty(*it);
}
