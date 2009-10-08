#ifndef _CLR_MANAGER_WRAPPER_H_
#define _CLR_MANAGER_WRAPPER_H_

#pragma managed(push, off)
#include "IBufferManager.h"
#pragma managed(on)
#include "ClrReference.h"

#if INC_BUFFERS
#include <vcclr.h>

class ClrManagerWrapper : public IBufferManager
{
public:
	ClrManagerWrapper(Buffers::IBufferManager^ pmanager);

	void Read(size_t pageid, void *result);
	void Write(size_t pageid, const void *data);
	void Flush();
	
	int GetReadCount() const;
	int GetWriteCount() const;
	std::tr1::shared_ptr<class IBlockDevice> GetDevice();
	std::tr1::shared_ptr<const class IBlockDevice> GetDevice() const;

protected:
	gcroot<Buffers::IBufferManager^> pmgr;
	std::tr1::shared_ptr<class IBlockDevice> pdev;
	size_t pagesize;
	gcroot<array<unsigned char>^> buffer;
};

#endif

#pragma managed(pop)
#endif