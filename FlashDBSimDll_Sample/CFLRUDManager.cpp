#include "stdafx.h"
#include <stdexcept>
#include "IBlockDevice.h"
#include "CFLRUDManager.h"
#include "Frame.h"

using namespace std;
using namespace stdext;
using namespace std::tr1;


CFLRUDManager::CFLRUDManager(shared_ptr<IBlockDevice> pDevice, size_t nPages, size_t iwindowSize_)
: BufferManagerBase(pDevice, nPages),
  windowSize_(iwindowSize_),
  queue_(), map_()
{
	if (iwindowSize_ > nPages)
		throw std::runtime_error("windowSize_ larger than NumOfPages");
	observingNum_ = npages_/2;
	totalCost_ = 0;
}

CFLRUDManager::~CFLRUDManager()
{
	Flush();
}

void CFLRUDManager::DoRead(size_t pageid, void *result)
{
	shared_ptr<DataFrame> pframe = AccessFrame_(pageid);

	//add for dynamic

	observingList_.push_back(0);
	totalCost_ += READCOST;
	if(observingList_.size() > observingNum_)
	{
		if(observingList_.front()==1)
		{
			totalCost_ -= WRITECOST;
		}
		else{
			totalCost_ -= READCOST;
		}
		observingList_.pop_front();
	}
	double averageCost = (double)totalCost_/observingList_.size();
	
	windowSize_ = (int)(queue_.size() * ((averageCost-READCOST)/(WRITECOST-READCOST)));


	/////////////////

	if (pframe.get() == NULL)
	{
		pframe = AcquireFrame_(pageid);
		pdev_->Read(pageid, pframe->Get());

	}

	memcpy(result, pframe->Get(), pagesize_);
}

void CFLRUDManager::DoWrite(size_t pageid, const void *data)
{
	shared_ptr<DataFrame> pframe = AccessFrame_(pageid);

	//add for dynamic

	observingList_.push_back(1);
	totalCost_ += WRITECOST;
	if(observingList_.size() > observingNum_)
	{
		if(observingList_.front()==1)
		{
			totalCost_ -= WRITECOST;
		}
		else{
			totalCost_ -= READCOST;
		}
		observingList_.pop_front();
	}
	double averageCost = (double)totalCost_/observingList_.size();
	
	windowSize_ = (int)(queue_.size() * ((averageCost-READCOST)/(WRITECOST-READCOST)));


	/////////////////

	if (pframe.get() == NULL)
		pframe = AcquireFrame_(pageid);

	memcpy(pframe->Get(), data, pagesize_);
	pframe->Dirty = true;
}


shared_ptr<DataFrame> CFLRUDManager::AccessFrame_(size_t pageid)
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

shared_ptr<DataFrame> CFLRUDManager::AcquireFrame_(size_t pageid)
{
	AcquireSlot_();
	shared_ptr<DataFrame> pframe(new DataFrame(pageid, pagesize_));
	queue_.push_front(pframe);
	map_[pageid] = queue_.begin();
	return pframe;
}


void CFLRUDManager::AcquireSlot_()
{
	if (queue_.size() < npages_)
		return;

	QueueType::iterator it = queue_.end();
	--it;
	shared_ptr<DataFrame> pframe = *it;
	
	size_t i = 0;
	//Find the first clean page in window.
	for(i=0; i<windowSize_; i++,--it)
	{
		pframe = *it;
		if(!(pframe ->Dirty))
		{
			break;
		}
	}

	//There is no clean page in window, get the lru dirty DataFrame of the queue.
	if(i >= windowSize_)
	{
		it = queue_.end();
		--it;
	}

	pframe = *it;
	WriteIfDirty(pframe);
	queue_.erase(it);
	map_.erase(pframe->Id);
}

void CFLRUDManager::WriteIfDirty(shared_ptr<DataFrame> pFrame)
{
	if (!pFrame->Dirty)
		return;

	pFrame->Dirty = false;
	pdev_->Write(pFrame->Id, pFrame->Get());
}

void CFLRUDManager::DoFlush()
{
	QueueType::iterator it, itend = queue_.end();

	/*
	for (it = queue_.begin(); it != itend; ++it) {
		cout << ((*it)->Dirty ? 1 : 0);
		WriteIfDirty(*it);
	}
	cout << endl;
	*/
}
