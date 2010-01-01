#pragma managed(off)
#include "stdafx.h"
#include "BufferManagerBase.h"
#include "IBlockDevice.h"
using namespace std::tr1;

BufferManagerBase::BufferManagerBase(shared_ptr<IBlockDevice> pdev, size_t npages)
: pdev_(pdev), pagesize_(pdev->GetPageSize()), npages_(npages),
  read_(0), write_(0)
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

std::tr1::shared_ptr<IBlockDevice> BufferManagerBase::GetDevice()
{
	return pdev_;
}
std::tr1::shared_ptr<const IBlockDevice> BufferManagerBase::GetDevice() const
{
	return pdev_;
}

int BufferManagerBase::GetReadCount() const
{
	return read_;
}
int BufferManagerBase::GetWriteCount() const
{
	return write_;
}
