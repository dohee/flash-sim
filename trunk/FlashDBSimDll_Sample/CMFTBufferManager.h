#ifndef _CMFT_BUFFER_MANAGER_H_
#define _CMFT_BUFFER_MANAGER_H_

#include <memory>
#include "BufferManagerBase.h"

class CMFTBufferManager : public BufferManagerBase
{
public:
	CMFTBufferManager(std::tr1::shared_ptr<class IBlockDevice> pDevice, size_t nPages);
	virtual ~CMFTBufferManager();

protected:
	virtual void DoRead(size_t pageid, void *result);
	virtual void DoWrite(size_t pageid, const void *data);
	virtual void DoFlush();

private:
	std::tr1::shared_ptr<class CMFTBufferManagerImpl> pImpl;
};

#endif