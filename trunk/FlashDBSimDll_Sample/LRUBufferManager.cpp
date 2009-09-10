#include "stdafx.h"
#include "LRUBufferManager.h"
#include "IBlockDevice.h"
using namespace std;
using namespace stdext;
using namespace std::tr1;

struct Frame
{
	size_t Id;
	bool Dirty;
	vector<char> Data;

	Frame(size_t id, size_t size)
	: Id(id), Dirty(false),
	  Data(vector<char>(size, -1))
	{ }
};


class LRUBufferManagerImpl
{
public:
	LRUBufferManagerImpl(shared_ptr<IBlockDevice> pDevice, size_t nPages);
	void Read(size_t addr, void *result);
	void Write(size_t addr, const void *data);
	void Flush();
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
};

LRUBufferManagerImpl::LRUBufferManagerImpl(shared_ptr<IBlockDevice> pDevice, size_t nPages)
: pdev_(pDevice),
  pagesize_(pDevice->GetPageSize()), npages_(nPages),
  queue_(), map_()
{ }

void LRUBufferManagerImpl::Read(size_t addr, void *result)
{
	size_t pageid = addr / pagesize_;
	shared_ptr<Frame> pframe = AccessFrame_(pageid);

	if (pframe.get() == NULL)
	{
		pframe = AcquireFrame_(pageid);
		pdev_->Read(pageid * pagesize_, &(pframe->Data.front()));
	}

	memcpy(result, &(pframe->Data.front()), pagesize_);
}

void LRUBufferManagerImpl::Write(size_t addr, const void *data)
{
	size_t pageid = addr / pagesize_;
	shared_ptr<Frame> pframe = AccessFrame_(pageid);

	if (pframe.get() == NULL)
		pframe = AcquireFrame_(pageid);

	memcpy(&(pframe->Data.front()), data, pagesize_);
	pframe->Dirty = true;
}


shared_ptr<Frame> LRUBufferManagerImpl::AccessFrame_(size_t pageid)
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

shared_ptr<Frame> LRUBufferManagerImpl::AcquireFrame_(size_t pageid)
{
	AcquireSlot_();
	shared_ptr<Frame> pframe(new Frame(pageid, pagesize_));
	queue_.push_front(pframe);
	map_[pageid] = queue_.begin();
	return pframe;
}


void LRUBufferManagerImpl::AcquireSlot_()
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

void LRUBufferManagerImpl::WriteIfDirty(shared_ptr<Frame> pFrame)
{
	if (!pFrame->Dirty)
		return;

	pFrame->Dirty = false;
	pdev_->Write(pFrame->Id * pagesize_, &(pFrame->Data.front()));
}

void LRUBufferManagerImpl::Flush()
{
	QueueType::iterator it, itend = queue_.end();

	for (it = queue_.begin(); it != itend; ++it)
		WriteIfDirty(*it);
}



LRUBufferManager::LRUBufferManager(shared_ptr<IBlockDevice> pDevice, size_t nPages)
: pImpl(new LRUBufferManagerImpl(pDevice, nPages)),
  read_(0), write_(0)
{ }

LRUBufferManager::~LRUBufferManager()
{
	Flush();
}

void LRUBufferManager::Read(size_t addr, void *result)
{
	read_++;
	pImpl->Read(addr, result);
}
void LRUBufferManager::Write(size_t addr, const void *data)
{
	write_++;
	pImpl->Write(addr, data);
}
void LRUBufferManager::Flush()
{
	pImpl->Flush();
}
int LRUBufferManager::GetReadCount() const
{
	return read_;
}
int LRUBufferManager::GetWriteCount() const
{
	return write_;
}
