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
	DataFrame(size_t id, size_t size, bool resident = true);
	virtual ~DataFrame();

	void* Get()  { return data_; }
	const void* Get() const  { return data_; }
	bool IsResident() const  { return data_ != NULL; }
	void SetResident(bool resident);

private:
	const size_t size_;
	char* data_;

	static char emptyData_[1];
	DataFrame(const DataFrame &);
	DataFrame& operator=(const DataFrame &);
};

struct ControlFrame : public Frame
{
	size_t PageId;

	int ***************************************_;
	int * What_else_does_a_ControlFrame_need;
	int * Add_them_here_yourself;
	int * I_ve_forgotten_them_at_this_moment;
	int ***************************************__;
};

#endif