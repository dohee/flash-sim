#ifndef _I_BUFFER_MANAGER_H_
#define _I_BUFFER_MANAGER_H_
#pragma managed(push, off)

#include <memory>

class IBufferManager abstract
{
public:
	virtual void Read(size_t pageid, void *result) = 0;
	virtual void Write(size_t pageid, const void *data) = 0;
	virtual void Flush() = 0;
	
	virtual int GetReadCount() const = 0;
	virtual int GetWriteCount() const = 0;
	virtual std::tr1::shared_ptr<class IBlockDevice> GetDevice() = 0;
	virtual std::tr1::shared_ptr<const class IBlockDevice> GetDevice() const = 0;
};

#pragma managed(pop)
#endif
