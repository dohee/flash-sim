#ifndef _FRAME_H_
#define _FRAME_H_

struct Frame
{
	size_t Id;
	bool Dirty;

	Frame(size_t id, size_t size);
	~Frame();

	void* Get();
	const void* Get() const;

private:
	size_t size_;
	char* data_;

	Frame(const Frame &);
	Frame& operator=(const Frame &);
};

#endif