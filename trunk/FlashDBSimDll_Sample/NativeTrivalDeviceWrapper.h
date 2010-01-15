#ifndef _NATIVE_TRIVAL_DEVICE_WRAPPER_H_
#define _NATIVE_TRIVAL_DEVICE_WRAPPER_H_

namespace Buffers {
	namespace Devices {
		namespace FromNative {


public ref class TrivalBlockDevice : Buffers::IBlockDevice
{
private:
	::TrivalBlockDevice* pdev;

public:
	TrivalBlockDevice(::TrivalBlockDevice* pdev);

	virtual property System::String^ Name { System::String^ get(); }
	virtual property System::String^ Description { System::String^ get(); }
	virtual property size_t PageSize { size_t get(); }
	virtual property int ReadCount { int get(); }
	virtual property int WriteCount { int get(); }

	virtual void Read(size_t pageid, cli::array<unsigned char>^ result);
	virtual void Write(size_t pageid, cli::array<unsigned char>^ data);
};


		};
	};
};

#endif
