#ifndef _LRU_BUFFER_MANAGER_H_
#define _LRU_BUFFER_MANAGER_H_

#include <memory>
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
	std::tr1::shared_ptr<class LRUBufferManagerImpl> pImpl;
};

#endif