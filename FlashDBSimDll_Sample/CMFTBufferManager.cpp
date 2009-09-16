#include "stdafx.h"
#include "CMFTBufferManager.h"
#include "IBlockDevice.h"
#include "frame.h"
using namespace std;
using namespace stdext;
using namespace std::tr1;


struct CMFTFrame : public Frame
{
	//size_t TimeStamp;
	size_t Betweenness;

	CMFTFrame(size_t id, size_t size)
		: Frame(id, size), Betweenness(0)
	{ }
};

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
		WriteIfDirty(**it);
}

shared_ptr<Frame> CMFTBufferManager::FindFrame(size_t pageid)
{
	StackType::iterator it, itend = stack_.end();

	for (it = stack_.begin(); it != itend; ++it)
		if ((*it)->Id == pageid)
			break;

	if (it == itend)
		return shared_ptr<CMFTFrame>();

	shared_ptr<CMFTFrame> pframe = *it;
	pframe->Betweenness = stack_.end() - it - 1;
	stack_.erase(it);
	stack_.push_back(pframe);
	return pframe;
}

shared_ptr<Frame> CMFTBufferManager::AllocFrame(size_t pageid)
{
	if (stack_.size() >= npages_)
	{
		StackType::iterator it, itend = stack_.end(), itmax;
		size_t maxbetw = 0;

		for (it = stack_.begin(); it != itend; ++it)
		{
			size_t betw = (*it)->Betweenness;
			if (betw > maxbetw)
			{
				maxbetw = betw;
				itmax = it;
			}
		}

		WriteIfDirty(**itmax);
		stack_.erase(itmax);
	}

	shared_ptr<CMFTFrame> pframe(new CMFTFrame(pageid, pagesize_));
	stack_.push_back(pframe);
	return pframe;
}
