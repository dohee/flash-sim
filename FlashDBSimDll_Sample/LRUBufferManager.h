#ifndef _LRU_BUFFER_MANAGER_H_
#define _LRU_BUFFER_MANAGER_H_

#include <memory>
#include <list>
#include <hash_map>
#include "BufferManagerBase.h"

class LRUBufferManager : public BufferManagerBase
{
public:
	LRUBufferManager(std::tr1::shared_ptr<class IBlockDevice> pDevice, size_t nPages);
	virtual ~LRUBufferManager();

protected:
	virtual void DoRead(size_t pageid, void *result);
	virtual void DoWrite(size_t pageid, const void *data);
	virtual void DoFlush();

private:
	std::tr1::shared_ptr<struct Frame> AccessFrame_(size_t pageid);
	std::tr1::shared_ptr<struct Frame> AcquireFrame_(size_t pageid);
	void AcquireSlot_();
	void WriteIfDirty(std::tr1::shared_ptr<struct Frame> pFrame);

private:
	typedef std::list<std::tr1::shared_ptr<struct Frame> > QueueType;
	typedef stdext::hash_map<size_t, QueueType::iterator> MapType;
	QueueType queue_;
	MapType map_;
};

#endif