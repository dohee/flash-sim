#ifndef _LRU_BUFFER_MANAGER_H_
#define _LRU_BUFFER_MANAGER_H_

#include <memory>
#include "IBufferManager.h"


class LRUBufferManager : public IBufferManager
{
public:
	LRUBufferManager(std::tr1::shared_ptr<class IBlockDevice> pDevice, size_t nPages);
	virtual ~LRUBufferManager();

	virtual void Read(size_t addr, void *result);
	virtual void Write(size_t addr, const void *data);
	virtual void Flush();
	
	virtual int GetReadCount() const;
	virtual int GetWriteCount() const;

private:
	std::tr1::shared_ptr<class LRUBufferManagerImpl> pImpl;
	int read_, write_;
};

#endif