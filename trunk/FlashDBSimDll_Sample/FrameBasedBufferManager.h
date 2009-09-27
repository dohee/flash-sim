#ifndef _FRAME_BASED_MANAGER_H_
#define _FRAME_BASED_MANAGER_H_

#include <memory>
#include "BufferManagerBase.h"

class FrameBasedBufferManager abstract : public BufferManagerBase
{
public:
	FrameBasedBufferManager(std::tr1::shared_ptr<class IBlockDevice> pDevice, size_t nPages);

protected:
	void DoRead(size_t pageid, void *result);
	void DoWrite(size_t pageid, const void *data);
	void ReadFromDev(struct DataFrame& frame);
	void WriteIfDirty(struct DataFrame& frame);

	virtual void DoFlush() = 0;
	virtual std::tr1::shared_ptr<struct DataFrame> FindFrame(size_t pageid, bool isWrite) = 0;
	virtual std::tr1::shared_ptr<struct DataFrame> AllocFrame(size_t pageid, bool isWrite) = 0;
};

#endif