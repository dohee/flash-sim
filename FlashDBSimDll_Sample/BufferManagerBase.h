#ifndef _BUFFER_MANAGER_H_
#define _BUFFER_MANAGER_H_

#include <memory>
#include "IBufferManager.h"

class BufferManagerBase abstract : public IBufferManager
{
public:
	BufferManagerBase(std::tr1::shared_ptr<class IBlockDevice> pdev);

	void Read(size_t pageid, void *result);
	void Write(size_t pageid, const void *data);
	void Flush();
	
	int GetReadCount() const;
	int GetWriteCount() const;

protected:
	virtual void DoRead(size_t pageid, void *result) = 0;
	virtual void DoWrite(size_t pageid, const void *data) = 0;
	virtual void DoFlush() = 0;

	size_t GetPageSize() const;
	void DeviceRead(size_t pageid, void *result);
	void DeviceWrite(size_t pageid, const void *data);

private:
	std::tr1::shared_ptr<class IBlockDevice> pdev_;
	int read_, write_;
};

#endif