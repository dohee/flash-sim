#include "stdafx.h"
#include "BufferManagerBase.h"
#include "IBlockDevice.h"
using namespace std::tr1;

BufferManagerBase::BufferManagerBase(shared_ptr<IBlockDevice> pdev)
: read_(0), write_(0), pdev_(pdev)
{ }

inline void BufferManagerBase::Read(size_t pageid, void *result)
{
	DoRead(pageid, result);
	read_++;
}
inline void BufferManagerBase::Write(size_t pageid, const void *data)
{
	DoWrite(pageid, data);
	write_++;
}
inline void BufferManagerBase::Flush()
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

size_t BufferManagerBase::GetPageSize() const
{
	return pdev_->GetPageSize();
}
void BufferManagerBase::DeviceRead(size_t pageid, void *result)
{
	pdev_->Read(pageid, result);
}
void BufferManagerBase::DeviceWrite(size_t pageid, const void *data)
{
	pdev_->Write(pageid, data);
}
