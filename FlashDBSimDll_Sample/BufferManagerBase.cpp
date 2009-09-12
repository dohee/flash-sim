#include "stdafx.h"
#include "BufferManagerBase.h"

BufferManagerBase::BufferManagerBase()
: read_(0), write_(0)
{ }

void BufferManagerBase::Read(size_t addr, void *result)
{
	DoRead(addr, result);
	read_++;
}

void BufferManagerBase::Write(size_t addr, const void *data)
{
	DoWrite(addr, data);
	write_++;
}

void BufferManagerBase::Flush()
{
	DoFlush();
}
	
int BufferManagerBase::GetReadCount() const
{
	return read_;
}

int BufferManagerBase::GetWriteCount() const
{
	return write_;
}
