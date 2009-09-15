#ifndef _LRUWSR_BUFFER_MANAGER_H_
#define _LRUWSR_BUFFER_MANAGER_H_

#include <memory>
#include <list>
#include <hash_map>
#include "BufferManagerBase.h"

class LRUWSRBufferManager : public BufferManagerBase
{
public:
	LRUWSRBufferManager(std::tr1::shared_ptr<class IBlockDevice> pDevice, size_t nPages, size_t maxCold);
	virtual ~LRUWSRBufferManager();

protected:
	virtual void DoRead(size_t pageid, void *result);
	virtual void DoWrite(size_t pageid, const void *data);
	virtual void DoFlush();

private:
	std::tr1::shared_ptr<struct LRUWSRFrame> AccessFrame_(size_t pageid);
	std::tr1::shared_ptr<struct LRUWSRFrame> AcquireFrame_(size_t pageid);
	void AcquireSlot_();
	void WriteIfDirty(std::tr1::shared_ptr<struct LRUWSRFrame> pFrame);

private:
	size_t pagesize_, npages_, maxcold_;
	
	typedef std::list<std::tr1::shared_ptr<struct LRUWSRFrame> > QueueType;
	typedef stdext::hash_map<size_t, QueueType::iterator> MapType;
	QueueType queue_;
	MapType map_;
};

#endif