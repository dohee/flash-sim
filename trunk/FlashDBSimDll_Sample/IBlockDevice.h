
class IBlockDevice abstract
{
public:
	virtual void Read(size_t addr, char *result) = 0;
	virtual void Write(size_t addr, const char *data) = 0;
};
