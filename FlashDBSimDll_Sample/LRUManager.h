#ifndef _LRU_BUFFER_MANAGER_H_
#define _LRU_BUFFER_MANAGER_H_

#include <memory>
#include <list>
#include <hash_map>
#include "FrameBasedBufferManager.h"

class LRUManager : public FrameBasedBufferManager
{
public:
	LRUManager(std::tr1::shared_ptr<class IBlockDevice> pDevice, size_t nPages);
	virtual ~LRUManager();

protected:
	virtual void DoFlush();
	std::tr1::shared_ptr<struct DataFrame> FindFrame(size_t pageid, bool isWrite);
	std::tr1::shared_ptr<struct DataFrame> AllocFrame(size_t pageid, bool isWrite);

private:
	void AcquireSlot_();

private:
	typedef std::list<std::tr1::shared_ptr<struct DataFrame> > QueueType;
	typedef stdext::hash_map<size_t, QueueType::iterator> MapType;
	QueueType queue_;
	MapType map_;
};

#endif