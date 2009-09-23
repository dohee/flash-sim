#include "stdafx.h"
#include "LRUBufferManager.h"
#include "IBlockDevice.h"
#include "Frame.h"
using namespace std;
using namespace stdext;
using namespace std::tr1;


LRUBufferManager::LRUBufferManager(shared_ptr<IBlockDevice> pdev, size_t nPages)
: FrameBasedBufferManager(pdev, nPages),
  queue_(), map_()
{ }

LRUBufferManager::~LRUBufferManager()
{
	Flush();
}

void LRUBufferManager::DoFlush()
{
	QueueType::iterator it, itend = queue_.end();

	for (it = queue_.begin(); it != itend; ++it)
		WriteIfDirty(**it);
}

shared_ptr<DataFrame> LRUBufferManager::FindFrame(size_t pageid)
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

shared_ptr<DataFrame> LRUBufferManager::AllocFrame(size_t pageid)
{
	AcquireSlot_();
	shared_ptr<DataFrame> pframe(new DataFrame(pageid, pagesize_));
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
	shared_ptr<DataFrame> pframe = *it;
	WriteIfDirty(*pframe);
	queue_.erase(it);
	map_.erase(pframe->Id);
}

