#ifndef _BUFFER_MANAGER_H_
#define _BUFFER_MANAGER_H_

#include <memory>
#include "IBufferManager.h"

class BufferManagerBase abstract : public IBufferManager
{
public:
	BufferManagerBase(std::tr1::shared_ptr<class IBlockDevice> pDevice, size_t nPages);

	void Read(size_t pageid, void *result);
	void Write(size_t pageid, const void *data);
	void Flush();
	
	std::tr1::shared_ptr<class IBlockDevice> GetDevice();
	std::tr1::shared_ptr<const class IBlockDevice> GetDevice() const;
	int GetReadCount() const;
	int GetWriteCount() const;

protected:
	virtual void DoRead(size_t pageid, void *result) = 0;
	virtual void DoWrite(size_t pageid, const void *data) = 0;
	virtual void DoFlush() = 0;

protected:
	const std::tr1::shared_ptr<class IBlockDevice> pdev_;
	const size_t pagesize_, npages_;

private:
	int read_, write_;
};

#endif