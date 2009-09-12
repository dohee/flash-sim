#ifndef _BUFFER_MANAGER_H_
#define _BUFFER_MANAGER_H_

#include "IBufferManager.h"

class BufferManagerBase abstract : public IBufferManager
{
public:
	BufferManagerBase();

	void Read(size_t addr, void *result);
	void Write(size_t addr, const void *data);
	void Flush();
	
	int GetReadCount() const;
	int GetWriteCount() const;

protected:
	virtual void DoRead(size_t addr, void *result) = 0;
	virtual void DoWrite(size_t addr, const void *data) = 0;
	virtual void DoFlush() = 0;

private:
	int read_, write_;
};

#endif