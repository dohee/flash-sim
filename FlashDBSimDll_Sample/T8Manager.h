#ifndef _T8_MANAGER_H_
#define _T8_MANAGER_H_

#include <memory>
#include <list>
#include <hash_map>
#include "FrameBasedBufferManager.h"

class T8Manager : public BufferManagerBase
{
public:
	T8Manager(std::tr1::shared_ptr<class IBlockDevice> pDevice, size_t nPages);
	virtual ~T8Manager();

protected:
	virtual void DoRead(size_t pageid, void *result);
	virtual void DoWrite(size_t pageid, const void *data);
	virtual void DoFlush();

private:
	typedef std::list<std::tr1::shared_ptr<struct DataFrame> > QueueType;
	enum QueueIndex { NONE = 0, CR, CNR, DR, DNR, COUNT };

	void SetLimits_(size_t cleanResidentSize);
	void EnlargeCRLimit_(int relativeCRSize);

	QueueIndex FindInQueues_(size_t pageid, QueueType::iterator& out);
	void AdjustQueue_(QueueIndex queue, QueueType::iterator iter);
	void FlushDRQueue();
	std::tr1::shared_ptr<struct DataFrame> PushIntoQueue_(QueueIndex queue, std::tr1::shared_ptr<struct DataFrame> pframe);
	std::tr1::shared_ptr<struct DataFrame> PushIntoQueues_(QueueIndex headqueue, std::tr1::shared_ptr<struct DataFrame> pframe);

private:
	QueueType q_[COUNT];
	size_t limits_[COUNT];
};




class TnManager : public FrameBasedBufferManager
{
public:
	TnManager(std::tr1::shared_ptr<class IBlockDevice> pDevice, size_t nPages,
		int HowManyToKickWhenWriteInDR, bool AdjustDRWhenReadInDR = false, bool EnlargeCRWhenReadInDNR = false);

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
	Queue cr_, cnr_, dr_, dnr_;
	int kickn_;
	bool adjustDROnReadDR_, enlargeCROnReadDNR_;
};

#endif