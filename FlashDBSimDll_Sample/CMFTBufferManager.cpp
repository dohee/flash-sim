#include "stdafx.h"
#include "CMFTBufferManager.h"
#include "IBlockDevice.h"
#include "frame.h"
using namespace std;
using namespace stdext;
using namespace std::tr1;


CMFTBufferManager::CMFTBufferManager(shared_ptr<IBlockDevice> pDevice, size_t nPages)
: FrameBasedBufferManager(pDevice, nPages), time_(1)
{ }

CMFTBufferManager::~CMFTBufferManager()
{
	Flush();
}

void CMFTBufferManager::DoFlush()
{
	StackType::iterator it, itend = stack_.end();
	for (it = stack_.begin(); it != itend; ++it)
		WriteIfDirty(*it);
}

shared_ptr<Frame> CMFTBufferManager::FindFrame(size_t pageid)
{
	return shared_ptr<CMFTFrame>();
}

shared_ptr<Frame> CMFTBufferManager::AllocFrame(size_t pageid)
{
	return shared_ptr<CMFTFrame>();
}
