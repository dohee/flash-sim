#include "stdafx.h"
#include "LRUManager.h"
#include "IBlockDevice.h"
#include "Frame.h"
using namespace std;
using namespace stdext;
using namespace std::tr1;


LRUManager::LRUManager(shared_ptr<IBlockDevice> pdev, size_t nPages)
: FrameBasedBufferManager(pdev, nPages),
  queue_(), map_()
{ }

LRUManager::~LRUManager()
{
	Flush();
}

void LRUManager::DoFlush()
{
	QueueType::iterator it, itend = queue_.end();

	for (it = queue_.begin(); it != itend; ++it)
		WriteIfDirty(**it);
}

shared_ptr<DataFrame> LRUManager::FindFrame(size_t pageid, bool isWrite)
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

shared_ptr<DataFrame> LRUManager::AllocFrame(size_t pageid, bool isWrite)
{
	AcquireSlot_();
	shared_ptr<DataFrame> pframe(new DataFrame(pageid, pagesize_));
	queue_.push_front(pframe);
	map_[pageid] = queue_.begin();
	return pframe;
}


void LRUManager::AcquireSlot_()
{
	if (queue_.size() < npages_)
		return;

	DataFrame& frame = *(queue_.back());
	WriteIfDirty(frame);
	map_.erase(frame.Id);
	queue_.pop_back();
}

