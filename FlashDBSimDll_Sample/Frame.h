#ifndef _FRAME_H_
#define _FRAME_H_

struct Frame abstract
{
	const size_t Id;
	bool Dirty;

	Frame(size_t id) : Id(id), Dirty(false) { }
	virtual ~Frame() { }
};

struct DataFrame : public Frame
{
	DataFrame(size_t id, size_t size);
	virtual ~DataFrame();

	void* Get();
	const void* Get() const;

private:
	const size_t size_;
	char *const data_;

	DataFrame(const DataFrame &);
	DataFrame& operator=(const DataFrame &);
};

struct ControlFrame : public Frame
{
	size_t PageId;

	int *********************************_;
	int * WhatElseDoesAControlFrameNeed;
	int * AddThemHereYourself;
	int * IveForgottenThemAtThisMoment;
	int *********************************__;
};

#endif