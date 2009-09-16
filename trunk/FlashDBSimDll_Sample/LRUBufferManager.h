#ifndef _LRU_BUFFER_MANAGER_H_
#define _LRU_BUFFER_MANAGER_H_

#include <memory>
#include <list>
#include <hash_map>
#include "FrameBasedBufferManager.h"

class LRUBufferManager : public FrameBasedBufferManager
{
public:
	LRUBufferManager(std::tr1::shared_ptr<class IBlockDevice> pDevice, size_t nPages);
	virtual ~LRUBufferManager();

protected:
	virtual void DoFlush();
	std::tr1::shared_ptr<struct Frame> FindFrame(size_t pageid);
	std::tr1::shared_ptr<struct Frame> AllocFrame(size_t pageid);

private:
	void AcquireSlot_();

private:
	typedef std::list<std::tr1::shared_ptr<struct Frame> > QueueType;
	typedef stdext::hash_map<size_t, QueueType::iterator> MapType;
	QueueType queue_;
	MapType map_;
};

#endif