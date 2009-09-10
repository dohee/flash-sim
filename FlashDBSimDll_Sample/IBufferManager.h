
class IBufferManager abstract
{
public:
	virtual void Read(size_t addr, char *result) = 0;
	virtual void Write(size_t addr, const char *data) = 0;
	virtual void Flush() = 0;
	
	virtual int GetReadCount() const = 0;
	virtual int GetWriteCount() const = 0;
};