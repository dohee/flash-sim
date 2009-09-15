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


class CFLRUBufferManagerImpl
{
public:
	CFLRUBufferManagerImpl(shared_ptr<IBlockDevice> pDevice, size_t nPages, size_t iwindowSize);
	void Read(size_t addr, void *result);
	void Write(size_t addr, const void *data);
	void Flush();
	void setwindowSize(size_t iwindowSize);
private:
	shared_ptr<Frame> AccessFrame_(size_t pageid);
	shared_ptr<Frame> AcquireFrame_(size_t pageid);
	void AcquireSlot_();
	void WriteIfDirty(shared_ptr<Frame> pFrame);

private:
	shared_ptr<IBlockDevice> pdev_;
	size_t pagesize_, npages_;
	
	typedef list<shared_ptr<Frame> > QueueType;
	typedef hash_map<size_t, QueueType::iterator> MapType;
	QueueType queue_;
	MapType map_;

	size_t windowSize;		//windowSize parameter of CFLRU
};

CFLRUBufferManagerImpl::CFLRUBufferManagerImpl(shared_ptr<IBlockDevice> pDevice, size_t nPages, size_t iwindowSize)
: pdev_(pDevice),
  pagesize_(pDevice->GetPageSize()), npages_(nPages), windowSize(iwindowSize),
  queue_(), map_()
{
	if (iwindowSize > nPages)
		throw std::runtime_error("WindowSize larger than NumOfPages");
}

void CFLRUBufferManagerImpl::Read(size_t addr, void *result)
{
	size_t pageid = addr;
	shared_ptr<Frame> pframe = AccessFrame_(pageid);

	if (pframe.get() == NULL)
	{
		pframe = AcquireFrame_(pageid);
		pdev_->Read(pageid * pagesize_, &(pframe->Data.front()));

	}

	memcpy(result, &(pframe->Data.front()), pagesize_);
}

void CFLRUBufferManagerImpl::Write(size_t addr, const void *data)
{
	size_t pageid = addr;
	shared_ptr<Frame> pframe = AccessFrame_(pageid);

	if (pframe.get() == NULL)
		pframe = AcquireFrame_(pageid);

	memcpy(&(pframe->Data.front()), data, pagesize_);
	pframe->Dirty = true;
}


shared_ptr<Frame> CFLRUBufferManagerImpl::AccessFrame_(size_t pageid)
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

shared_ptr<Frame> CFLRUBufferManagerImpl::AcquireFrame_(size_t pageid)
{
	AcquireSlot_();
	shared_ptr<Frame> pframe(new Frame(pageid, pagesize_));
	queue_.push_front(pframe);
	map_[pageid] = queue_.begin();
	return pframe;
}


void CFLRUBufferManagerImpl::AcquireSlot_()
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

void CFLRUBufferManagerImpl::WriteIfDirty(shared_ptr<Frame> pFrame)
{
	if (!pFrame->Dirty)
		return;

	pFrame->Dirty = false;
	pdev_->Write(pFrame->Id * pagesize_, &(pFrame->Data.front()));
}

void CFLRUBufferManagerImpl::Flush()
{
	QueueType::iterator it, itend = queue_.end();

	for (it = queue_.begin(); it != itend; ++it)
		WriteIfDirty(*it);
}



CFLRUBufferManager::CFLRUBufferManager(shared_ptr<IBlockDevice> pDevice, size_t nPages, size_t iwindowSize)
: pImpl(new CFLRUBufferManagerImpl(pDevice, nPages, iwindowSize)),
  read_(0), write_(0)
{ }

CFLRUBufferManager::~CFLRUBufferManager()
{
	Flush();
}

void CFLRUBufferManager::Read(size_t addr, void *result)
{
	pImpl->Read(addr, result);
	read_++;
}
void CFLRUBufferManager::Write(size_t addr, const void *data)
{
	pImpl->Write(addr, data);
	write_++;
}
void CFLRUBufferManager::Flush()
{
	pImpl->Flush();
}
int CFLRUBufferManager::GetReadCount() const
{
	return read_;
}
int CFLRUBufferManager::GetWriteCount() const
{
	return write_;
}
