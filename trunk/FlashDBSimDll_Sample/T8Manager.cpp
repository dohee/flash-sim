#include "stdafx.h"
#include "T8Manager.h"
#include "IBlockDevice.h"
#include "Frame.h"
using namespace std;
using namespace stdext;
using namespace std::tr1;


TnManager::Queue::Queue(size_t limit)
: limit_(limit) { }

inline size_t TnManager::Queue::
GetSize() const				{ return q_.size(); }
inline size_t TnManager::Queue::
GetLimit() const			{ return limit_; }
inline bool TnManager::Queue::
IsTooLong() const			{ return q_.size() > limit_; }
inline void TnManager::Queue::
ChangeLimit(int value)		{ limit_ = value; }

inline TnManager::Queue::QueueType::iterator TnManager::Queue::
begin()						{ return q_.begin(); }
inline TnManager::Queue::QueueType::iterator TnManager::Queue::
end()						{ return q_.end(); }
inline TnManager::Queue::QueueType::reference TnManager::Queue::
back()						{ return q_.back(); }
inline TnManager::Queue::QueueType::const_iterator TnManager::Queue::
begin() const				{ return q_.begin(); }
inline TnManager::Queue::QueueType::const_iterator TnManager::Queue::
end() const					{ return q_.end(); }
inline TnManager::Queue::QueueType::const_reference TnManager::Queue::
back() const				{ return q_.back(); }
inline void TnManager::Queue::
Enqueue(shared_ptr<DataFrame> pframe)	{q_.push_front(pframe);}

inline TnManager::Queue::QueueType::iterator TnManager::Queue::
Find(size_t id)
{
	struct {
		int id;
		bool operator()(QueueType::const_reference pframe) const {
			return pframe->Id == id;
		}
	} pred = { id };

	return find_if(q_.begin(), q_.end(), pred);
}

inline shared_ptr<DataFrame> TnManager::Queue::
Dequeue()
{
	shared_ptr<DataFrame> pframe = q_.back();
	q_.pop_back();
	return pframe;
}
inline shared_ptr<DataFrame> TnManager::Queue::
Dequeue(QueueType::iterator iter)
{
	shared_ptr<DataFrame> pframe = *iter;
	q_.erase(iter);
	return pframe;
}


//-----------------------------------------------------

TnManager::
TnManager(
	shared_ptr<IBlockDevice> pDevice, size_t nPages, int HowManyToKickWhenWriteInDR,
	bool AdjustDRWhenReadInDR, bool EnlargeCRWhenReadInDNR)
: FrameBasedBufferManager(pDevice, nPages),
  kickn_(HowManyToKickWhenWriteInDR), adjustDROnReadDR_(AdjustDRWhenReadInDR),
  enlargeCROnReadDNR_(EnlargeCRWhenReadInDNR)
{
	cr_.ChangeLimit(npages_ /2 / HowManyToKickWhenWriteInDR);
	dr_.ChangeLimit(npages_ /2 - cr_.GetLimit());
	cnr_.ChangeLimit(npages_ / 2);
	dnr_.ChangeLimit(npages_ / 2);
	sr_.ChangeLimit(npages_ / 2); //XXX: how to change it?
}

TnManager::
~TnManager() { Flush(); }

void TnManager::
EnlargeCRLimit(int relative)
{
	int crlimit = relative + (int) cr_.GetLimit();
	crlimit = max(crlimit, 1);
	crlimit = min(crlimit, (int)npages_ - 1);

	cr_.ChangeLimit(crlimit);
	dr_.ChangeLimit(npages_ - crlimit);
}


void TnManager::
DoFlush()
{
	struct {
		TnManager& mgr;
		void operator()(Queue::QueueType::const_reference pframe) {
			mgr.WriteIfDirty(*pframe);
		}
	} func = { *this };

	for_each(sr_.begin(), sr_.end(), func);
	for_each(dr_.begin(), dr_.end(), func);


	Queue::QueueType::iterator it, it2;

	for (it=dr_.begin(); it!=dr_.end(); ) {
		it2 = it;
		it++;
		cr_.Enqueue(dr_.Dequeue(it2));
	}
}


shared_ptr<DataFrame> TnManager::
FindFrame(size_t pageid, bool isWrite)
{
	shared_ptr<DataFrame> pframe = (isWrite ?
		FindFrameOnWrite(pageid) : FindFrameOnRead(pageid));

	if (pframe.get() == NULL) {
		Queue::QueueType::iterator it = sr_.Find(pageid);

		if (it != sr_.end()) {
			pframe = sr_.Dequeue(it);

			if (isWrite || pframe->Dirty)
				dr_.Enqueue(pframe);
			else
				cr_.Enqueue(pframe);
			
			SqueezeQueues_();
		}
	}

	return pframe;
}

shared_ptr<DataFrame> TnManager::
FindFrameOnRead(size_t pageid)
{
	Queue::QueueType::iterator it;
	
	if ((it = cr_.Find(pageid)) != cr_.end()) {
		return MoveFrame_(cr_, it, cr_);

	} else if ((it = cnr_.Find(pageid)) != cnr_.end()) {
		shared_ptr<DataFrame> pframe = MoveFrame_(cnr_, it, cr_);
		pframe->SetResident(true);
		ReadFromDev(*pframe);

		EnlargeCRLimit(1);
		SqueezeQueues_();
		return pframe;

	} else if ((it = dr_.Find(pageid)) != dr_.end()) {
		if (adjustDROnReadDR_)
			return MoveFrame_(dr_, it, dr_);
		else
			return *it;

	} else if ((it = dnr_.Find(pageid)) != dnr_.end()) {
		shared_ptr<DataFrame> pframe = MoveFrame_(dnr_, it, cr_);
		pframe->SetResident(true);

		if (enlargeCROnReadDNR_)
			EnlargeCRLimit(1);

		SqueezeQueues_();
		return pframe;

	} else {
		return shared_ptr<DataFrame>();
	}
}

shared_ptr<DataFrame> TnManager::
FindFrameOnWrite(size_t pageid)
{
	Queue::QueueType::iterator it;
	
	if ((it = dr_.Find(pageid)) != dr_.end()) {
		return MoveFrame_(dr_, it, dr_);

	} else if ((it = dnr_.Find(pageid)) != dnr_.end()) {
		shared_ptr<DataFrame> pframe = MoveFrame_(dnr_, it, dr_);
		pframe->SetResident(true);

		EnlargeCRLimit(-kickn_);
		SqueezeQueues_();
		return pframe;

	} else if ((it = cr_.Find(pageid)) != cr_.end()) {
		return MoveFrame_(cr_, it, dr_);

	} else if ((it = cnr_.Find(pageid)) != cnr_.end()) {
		shared_ptr<DataFrame> pframe = MoveFrame_(cnr_, it, dr_);
		pframe->SetResident(true);
		SqueezeQueues_();
		return pframe;

	} else {
		return shared_ptr<DataFrame>();
	}
}


inline shared_ptr<DataFrame> TnManager::
MoveFrame_(Queue& dequeueFrom, Queue::QueueType::iterator which, Queue& enqueueTo)
{
	shared_ptr<DataFrame> pframe = dequeueFrom.Dequeue(which);
	enqueueTo.Enqueue(pframe);

#ifdef _DEBUG
	if (&dequeueFrom == &cr_)
		assert(pframe->Dirty == false && pframe->IsResident() == true);
	else if (&dequeueFrom == &cnr_)
		assert(pframe->Dirty == false && pframe->IsResident() == false);
	else if (&dequeueFrom == &dr_)
		assert(pframe->Dirty == true && pframe->IsResident() == true);
	else if (&dequeueFrom == &dnr_)
		assert(pframe->Dirty == false && pframe->IsResident() == false);
	else
		assert(false);
#endif

	return pframe;
}


shared_ptr<DataFrame> TnManager::
AllocFrame(size_t pageid, bool isWrite)
{
	shared_ptr<DataFrame> pframe(new DataFrame(pageid, pagesize_, true));
	sr_.Enqueue(pframe);
	SqueezeQueues_();
	return pframe;
}

void TnManager::
SqueezeResidentQueue_(Queue& headqueue, Queue& tailqueue)
{
	shared_ptr<DataFrame> pframe = headqueue.Dequeue();
	assert(pframe->IsResident() == true);
	WriteIfDirty(*pframe);
	pframe->SetResident(false);
	tailqueue.Enqueue(pframe);
}

void TnManager::
SqueezeQueues_()
{
	int crOverRun = cr_.GetSize() - cr_.GetLimit();
	int drOverRun = dr_.GetSize() - dr_.GetLimit();

	while (crOverRun + drOverRun > 0) {
		if (crOverRun > drOverRun) {
			SqueezeResidentQueue_(cr_, cnr_);
			crOverRun--;
		} else {
			SqueezeResidentQueue_(dr_, dnr_);
			drOverRun--;
		}
	}

	while (cnr_.GetSize() > cnr_.GetLimit())
		cnr_.Dequeue();

	while (dnr_.GetSize() > dnr_.GetLimit())
		dnr_.Dequeue();

	while (sr_.GetSize() > sr_.GetLimit()) {
		WriteIfDirty(*sr_.back());
		sr_.Dequeue();
	}
}
