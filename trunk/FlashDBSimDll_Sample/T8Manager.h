#ifndef _T8_MANAGER_H_
#define _T8_MANAGER_H_
#pragma managed(push, off)

#include <memory>
#include <list>
#include <hash_map>
#include "FrameBasedBufferManager.h"


class TnManager : public FrameBasedBufferManager
{
public:
	TnManager(std::tr1::shared_ptr<class IBlockDevice> pDevice, size_t nPages,int srLength, 
		int HowManyToKickWhenWriteInDR, bool AdjustDRWhenReadInDR = false, bool EnlargeCRWhenReadInDNR = false );

	virtual ~TnManager();

protected:
	virtual void DoFlush();
	std::tr1::shared_ptr<struct DataFrame> FindFrame(size_t pageid, bool isWrite);
	std::tr1::shared_ptr<struct DataFrame> AllocFrame(size_t pageid, bool isWrite);

private:
	class Queue {
	public:
		typedef std::list<std::tr1::shared_ptr<struct DataFrame> > QueueType;
		typedef std::tr1::shared_ptr<QueueType> QueueTypePtr;

		Queue(size_t limit = 0);
		size_t GetSize() const;
		size_t GetLimit() const;
		bool IsTooLong() const;
		void ChangeLimit(int value);

		QueueType::iterator begin();
		QueueType::iterator end();
		QueueType::reference back();
		QueueType::const_iterator begin() const;
		QueueType::const_iterator end() const;
		QueueType::const_reference back() const;

		QueueType::iterator Find(size_t id);

		void Enqueue(std::tr1::shared_ptr<struct DataFrame> pframe);
		std::tr1::shared_ptr<struct DataFrame> Dequeue();
		std::tr1::shared_ptr<struct DataFrame> Dequeue(QueueType::iterator iter);

	private:
		QueueType q_;
		size_t limit_;
	};


	std::tr1::shared_ptr<struct DataFrame> FindFrameOnRead(size_t pageid);
	std::tr1::shared_ptr<struct DataFrame> FindFrameOnWrite(size_t pageid);

	std::tr1::shared_ptr<struct DataFrame> MoveFrame_(
		Queue& dequeueFrom, Queue::QueueType::iterator which, Queue& enqueueTo);

	void EnlargeCRLimit(int relative);
	void SqueezeResidentQueue_(Queue& headqueue, Queue& tailqueue);
	void SqueezeQueues_();

private:
	Queue cr_, cnr_, dr_, dnr_, sr_;
	int kickn_;
	bool adjustDROnReadDR_, enlargeCROnReadDNR_;
};

#pragma managed(pop)
#endif