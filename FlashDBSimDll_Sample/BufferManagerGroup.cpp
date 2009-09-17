#include "StdAfx.h"
#include "BufferManagerGroup.h"
#include "bufferManagerBase.h"
#include "IBlockDevice.h"
using namespace std;
using namespace std::tr1;


void BufferManagerGroup::Add(shared_ptr<BufferManagerBase> pbuf)
{
	mgrs_.push_back(pbuf);
}

void BufferManagerGroup::Read(size_t pageid, void *result)
{
	MgrsType::iterator it, itend = mgrs_.end();

	for (it=mgrs_.begin(); it!=itend; ++it)
		(*it)->Read(pageid, result);
}
void BufferManagerGroup::Write(size_t pageid, const void *data)
{
	MgrsType::iterator it, itend = mgrs_.end();

	for (it=mgrs_.begin(); it!=itend; ++it)
		(*it)->Write(pageid, data);
}
void BufferManagerGroup::Flush()
{
	MgrsType::iterator it, itend = mgrs_.end();

	for (it=mgrs_.begin(); it!=itend; ++it)
		(*it)->Flush();
}

int BufferManagerGroup::GetMgrCount() const
{
	return mgrs_.size();
}
int BufferManagerGroup::GetReadCount() const
{
	return mgrs_[0]->GetReadCount();
}
int BufferManagerGroup::GetWriteCount() const
{
	return mgrs_[0]->GetWriteCount();
}

int BufferManagerGroup::GetDevReadCount(size_t index) const
{
	return mgrs_[index]->GetDevice()->GetReadCount();
}
int BufferManagerGroup::GetDevWriteCount(size_t index) const
{
	return mgrs_[index]->GetDevice()->GetWriteCount();
}
int BufferManagerGroup::GetDevCost(size_t index) const
{
	return mgrs_[index]->GetDevice()->GetTotalCost();
}
