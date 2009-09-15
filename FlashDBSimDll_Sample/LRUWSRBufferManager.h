#ifndef _LRUWSR_BUFFER_MANAGER_H_
#define _LRUWSR_BUFFER_MANAGER_H_

#include <memory>
#include "BufferManagerBase.h"

class LRUWSRBufferManager : public BufferManagerBase
{
public:
	LRUWSRBufferManager(std::tr1::shared_ptr<class IBlockDevice> pDevice, size_t nPages, size_t maxCold);
	virtual ~LRUWSRBufferManager();

protected:
	virtual void DoRead(size_t addr, void *result);
	virtual void DoWrite(size_t addr, const void *data);
	virtual void DoFlush();

private:
	std::tr1::shared_ptr<class LRUWSRBufferManagerImpl> pImpl;
};

#endif