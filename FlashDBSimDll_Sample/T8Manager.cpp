#include "stdafx.h"
#include "T8Manager.h"
#include "IBlockDevice.h"
#include "Frame.h"
using namespace std;
using namespace stdext;
using namespace std::tr1;

T8Manager::T8Manager(shared_ptr<IBlockDevice> pdev, size_t nPages)
: BufferManagerBase(pdev, nPages)
{
	limits_[0] = 0;
	SetLimits_(nPages / 2);
}

T8Manager::~T8Manager()
{
	FlushDRQueue();
}

inline void T8Manager::SetLimits_(size_t cr)
{
	limits_[CR] = cr;
	limits_[DR] = npages_ - cr;
	limits_[CNR] = npages_ - cr;
	limits_[DNR] = cr;
}

void T8Manager::EnlargeCRLimit_(int rela)
{
	if (rela == 0)
		return;

	int newcr = (int)limits_[CR] + rela;
	newcr = max(newcr, 1);
	newcr = min(newcr, (int) (npages_ - 1));

	SetLimits_((size_t) newcr);

	while (q_[CR].size() > limits_[CR]) {
		q_[CR].back()->SetResident(false);
		PushIntoQueue_(CNR, q_[CR].back());
		q_[CR].pop_back();
	}

	while (q_[DR].size() > limits_[DR]) {
		DataFrame& frame = *q_[DR].back();
		if (frame.Dirty) {
			pdev_->Write(frame.Id, frame.Get());
			frame.Dirty = false;
		}

		frame.SetResident(false);
		PushIntoQueue_(CNR, q_[DR].back());
		q_[DR].pop_back();
	}
	
}


void T8Manager::DoRead(size_t pageid, void *result)
{
	QueueType::iterator it;
	int pageQueue = FindInQueues_(pageid, it);

	if (pageQueue == CR || pageQueue == DR) {
		DataFrame& frame = **it;
		memcpy(result, frame.Get(), pagesize_);
		
		if (pageQueue == CR)
			AdjustQueue_(CR, it);

	} else if (pageQueue == CNR || pageQueue == DNR) {
		shared_ptr<DataFrame> pframe = *it;
		pframe->SetResident(true);
		pdev_->Read(pageid, pframe->Get());
		memcpy(result, pframe->Get(), pagesize_);

		q_[pageQueue].erase(it);

		if (pageQueue == CNR)
			EnlargeCRLimit_(1);

		PushIntoQueues_(CR, pframe);

	} else {
		shared_ptr<DataFrame> pframe(new DataFrame(pageid, pagesize_, true));
		pdev_->Read(pageid, pframe->Get());
		memcpy(result, pframe->Get(), pagesize_);

		PushIntoQueues_(CR, pframe);
	}
}

void T8Manager::DoWrite(size_t pageid, const void *data)
{
	QueueType::iterator it;
	int pageQueue = FindInQueues_(pageid, it);

	if (pageQueue == DR) {
		DataFrame& frame = **it;
		memcpy(frame.Get(), data, pagesize_);
		
		AdjustQueue_(DR, it);

	} else if (pageQueue == DNR || pageQueue == CR || pageQueue == CNR) {
		shared_ptr<DataFrame> pframe = *it;
		pframe->SetResident(true);
		memcpy(pframe->Get(), data, pagesize_);
		pframe->Dirty = true;

		q_[pageQueue].erase(it);

		if (pageQueue == DNR)
			EnlargeCRLimit_(-3);

		PushIntoQueues_(DR, pframe);

	} else {
		shared_ptr<DataFrame> pframe(new DataFrame(pageid, pagesize_, true));
		memcpy(pframe->Get(), data, pagesize_);
		pframe->Dirty = true;

		PushIntoQueues_(DR, pframe);
	}
}

void T8Manager::DoFlush()
{
	FlushDRQueue();
	q_[CR].insert(q_[CR].begin(), q_[DR].begin(), q_[DR].end());
	q_[DR].clear();
}

void T8Manager::FlushDRQueue()
{
	QueueType::iterator it = q_[DR].begin(), itend = q_[DR].end();

	for (; it!=itend; ++it) {
		DataFrame& frame = **it;
		pdev_->Write(frame.Id, frame.Get());
		assert(frame.Dirty == true);
		frame.Dirty = false;
	}
}


T8Manager::QueueIndex T8Manager::FindInQueues_(
	size_t pageid, QueueType::iterator& out)
{
	struct {
		int id;
		bool operator()(QueueType::const_reference pframe) const {
			return pframe->Id == id;
		}
	} pred = { pageid };

	for (int i=1; i<COUNT; ++i) {
		QueueType::iterator it = find_if(q_[i].begin(), q_[i].end(), pred);

		if (it != q_[i].end()) {
			out = it;
			return (QueueIndex) i;
		}
	}

	return NONE;
}


inline void T8Manager::AdjustQueue_(QueueIndex index, QueueType::iterator iter)
{
	shared_ptr<DataFrame> pframe = *iter;
	q_[index].erase(iter);
	q_[index].push_front(pframe);
}

shared_ptr<DataFrame> T8Manager::PushIntoQueue_(
	QueueIndex queue, shared_ptr<DataFrame> pframe)
{
	q_[queue].push_front(pframe);
	
	if (q_[queue].size() <= limits_[queue])
		return shared_ptr<DataFrame>();

	shared_ptr<DataFrame> old = q_[queue].back();
	q_[queue].pop_back();
	
	return old;
}

shared_ptr<DataFrame> T8Manager::PushIntoQueues_(
	QueueIndex head, shared_ptr<DataFrame> pframe)
{
	if (head != CR && head != DR)
		throw ::invalid_argument("cannot push frame into CNR or DNR queue");

	shared_ptr<struct DataFrame> headtail = PushIntoQueue_(head, pframe);

	if (headtail.get() == NULL)
		return headtail;

	assert(headtail->IsResident() == true);

	if (headtail->Dirty) {
		pdev_->Write(headtail->Id, headtail->Get());
		headtail->Dirty = false;
	}

	headtail->SetResident(false);

	if (head == CR)
		return PushIntoQueue_(CNR, headtail);
	else
		return PushIntoQueue_(DNR, headtail);
}
