#include "stdafx.h"
#include "T8Manager.h"
#include "IBlockDevice.h"
#include "Frame.h"
using namespace std;
using namespace stdext;
using namespace std::tr1;

T8Manager::T8Manager(shared_ptr<IBlockDevice> pdev, size_t nPages)
: BufferManagerBase(pdev, nPages),
  qclean_(), qdirty_()
{ }

T8Manager::~T8Manager()
{
	QueueType::iterator it, itend = qdirty_.end();

	for (it = qdirty_.begin(); it != itend; ++it) {
		DataFrame& frame = **it;

		if (frame.IsResident())
			pdev_->Write(frame.Id, frame.Get());
	}
}

void T8Manager::DoRead(size_t pageid, void *result)
{
	QueueType::iterator it;

	if (FindInQueue_(pageid, qclean_, it)) {
		DataFrame& frame = **it;

		if (!frame.IsResident()) {
			frame.SetResident(true);
			pdev_->Read(pageid, frame.Get());
			//XXXXXX
		}
		memcpy(result, frame.Get(), pagesize_);
		AdjustQueue_(qclean_, it);

	} else if (FindInQueue_(pageid, qdirty_, it)) {
		DataFrame& frame = **it;

		if (!frame.IsResident()) {
			frame.SetResident(true);
			pdev_->Read(pageid, frame.Get());
			//XXXXXX
		}
		memcpy(result, frame.Get(), pagesize_);
		//AdjustQueue_(qdirty_, it);

	} else {

	}
	
}

void T8Manager::DoWrite(size_t pageid, const void *data)
{
}

void T8Manager::DoFlush()
{
}

bool T8Manager::FindInQueue_(size_t pageid, QueueType& queue, QueueType::iterator& out)
{
	QueueType::iterator it, itend = queue.end();

	for (it=queue.begin(); it!=itend; ++it) {
		if ((*it)->Id == pageid) {
			out = it;
			return true;
		}
	}

	return false;
}

void T8Manager::AdjustQueue_(QueueType& queue, QueueType::iterator iter)
{
	shared_ptr<DataFrame> pframe = *iter;
	queue.erase(iter);
	queue.push_front(pframe);
}
