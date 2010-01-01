#ifndef _TRIVAL_BUFFER_MANAGER_H_
#define _TRIVAL_BUFFER_MANAGER_H_
#pragma managed(push, off)

#include <memory>
#include "BufferManagerBase.h"


class TrivalBufferManager : public BufferManagerBase
{
public:
	TrivalBufferManager(std::tr1::shared_ptr<class IBlockDevice> pDevice);

protected:
	void DoRead(size_t pageid, void *result);
	void DoWrite(size_t pageid, const void *data);
	void DoFlush();

};

#pragma managed(pop)
#endif
