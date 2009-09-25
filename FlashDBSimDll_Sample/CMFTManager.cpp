#include "stdafx.h"
#include <climits>
#include "CMFTManager.h"
#include "IBlockDevice.h"
#include "Frame.h"
using namespace std;
using namespace stdext;
using namespace std::tr1;


struct CMFTFrame : public DataFrame
{
	size_t TimeStamp;
	size_t InterreferenceRecency;

	CMFTFrame(size_t id, size_t size, size_t timestamp)
	: DataFrame(id, size), TimeStamp(timestamp),
	  InterreferenceRecency(UINT_MAX)
	{ }
};

CMFTManager::CMFTManager(shared_ptr<IBlockDevice> pDevice, size_t nPages)
: FrameBasedBufferManager(pDevice, nPages), time_(0)
{ }

CMFTManager::~CMFTManager()
{
	Flush();
}

void CMFTManager::DoFlush()
{
	StackType::iterator it, itend = stack_.end();

	for (it = stack_.begin(); it != itend; ++it)
		WriteIfDirty(**it);
}

shared_ptr<DataFrame> CMFTManager::FindFrame(size_t pageid, bool isWrite)
{
	time_++;
	StackType::iterator it, itend = stack_.end();

	for (it = stack_.begin(); it != itend; ++it)
		if ((*it)->Id == pageid)
			break;

	if (it == itend)
		return shared_ptr<CMFTFrame>();

	shared_ptr<CMFTFrame> pframe = *it;
	pframe->InterreferenceRecency = stack_.end() - it - 1;
	pframe->TimeStamp = time_;
	stack_.erase(it);
	stack_.push_back(pframe);
	return pframe;
}

shared_ptr<DataFrame> CMFTManager::AllocFrame(size_t pageid)
{
	if (stack_.size() >= npages_) {
		StackType::iterator it, itend = stack_.end();
		StackType::iterator itIrrMax = itend, itTimeMin = itend;
		size_t maxIrr = 0, minTime = UINT_MAX;

		for (it = stack_.begin(); it != itend; ++it) {
			size_t irr = (*it)->InterreferenceRecency;

			if (irr == UINT_MAX) {
				size_t time = (*it)->TimeStamp;
				if (time < minTime) {
					minTime = time;
					itTimeMin = it;
				}
			} else if (irr > maxIrr) {
				maxIrr = irr;
				itIrrMax = it;
			}
		}

		if (itIrrMax == itend)   //说明 irrmax 没找到
			it = itTimeMin;
		else
			it = itIrrMax;

		WriteIfDirty(**it);
		stack_.erase(it);
	}

	shared_ptr<CMFTFrame> pframe(new CMFTFrame(pageid, pagesize_, time_));
	stack_.push_back(pframe);
	return pframe;
}
