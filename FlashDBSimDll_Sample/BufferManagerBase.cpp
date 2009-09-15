#include "stdafx.h"
#include "BufferManagerBase.h"

BufferManagerBase::BufferManagerBase()
: read_(0), write_(0)
{ }

void BufferManagerBase::Read(size_t pageid, void *result)
{
	DoRead(pageid, result);
	read_++;
}

void BufferManagerBase::Write(size_t pageid, const void *data)
{
	DoWrite(pageid, data);
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
