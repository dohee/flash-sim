#ifndef _CFLRU_BUFFER_MANAGER_H_
#define _CFLRU_BUFFER_MANAGER_H_

#include <memory>
#include "IBufferManager.h"


class CFLRUBufferManager : public IBufferManager
{
public:
	CFLRUBufferManager(std::tr1::shared_ptr<class IBlockDevice> pDevice, size_t nPages, size_t iwindowSize);
	virtual ~CFLRUBufferManager();

	virtual void Read(size_t pageid, void *result);
	virtual void Write(size_t pageid, const void *data);
	virtual void Flush();
	
	virtual int GetReadCount() const;
	virtual int GetWriteCount() const;

private:
	std::tr1::shared_ptr<class CFLRUBufferManagerImpl> pImpl;
	int read_, write_;
};

#endif