#include "stdafx.h"
#include <ctime>
#include "TrivalBlockDevice.h"
#include "TrivalBufferManager.h"
#include "LRUManager.h"
#include "CFLRUManager.h"
#include "CFLRUDManager.h"
#include "LRUWSRManager.h"
#include "CMFTManager.h"
#include "T8Manager.h"
#include "BufferManagerGroup.h"
using namespace std;
using namespace std::tr1;


void main()
{
	int bufferSize = 500;
	BufferManagerGroup group;

	group.Add(shared_ptr<BufferManagerBase>(new LRUManager(
		shared_ptr<IBlockDevice>(new TrivalBlockDevice), bufferSize)));

	/*
	group.Add(shared_ptr<BufferManagerBase>(new CFLRUManager(
		shared_ptr<IBlockDevice>(new TrivalBlockDevice), bufferSize, bufferSize*1000/1000)));
	group.Add(shared_ptr<BufferManagerBase>(new CFLRUManager(
		shared_ptr<IBlockDevice>(new TrivalBlockDevice), bufferSize, bufferSize*500/1000)));
	group.Add(shared_ptr<BufferManagerBase>(new CFLRUManager(
		shared_ptr<IBlockDevice>(new TrivalBlockDevice), bufferSize, bufferSize*800/1000)));
	
	group.Add(shared_ptr<BufferManagerBase>(new LRUWSRManager(
		shared_ptr<IBlockDevice>(new TrivalBlockDevice), bufferSize, 1)));*/
	
	group.Add(shared_ptr<BufferManagerBase>(new TnManager(
		shared_ptr<IBlockDevice>(new TrivalBlockDevice), bufferSize, WRITECOST/READCOST,false,false)));


	srand(clock());
	int fcount = 0;
	ifstream traceFile("trace.txt");

	if(!traceFile.is_open())
	{
		cout<<"file missing~~"<<endl;
		exit(1);
	}

	int count = 0;
	size_t pageid, length, rw;
	char buf[2048];

	while (traceFile >> pageid >> length >> rw) {

		if (++count % 2000 == 0)
			cout<<count<<endl;

#ifdef _DEBUG
		if (count >= 20000)
			break;
#endif

		if (rw == 0)
			while (length--)
				group.Read(pageid++, buf);
		else
			while (length--)
				group.Write(pageid++, buf);
	}

	group.Flush();

	printf("Manager\tRead %d\tWrite %d\n",
		group.GetReadCount(), group.GetWriteCount());

	int iend = group.GetMgrCount();

	for (int i=0; i<iend; ++i) {
		printf("Dev %d\tRead %d\tWrite %d\tCost %d\n", i,
			group.GetDevReadCount(i), group.GetDevWriteCount(i),
			group.GetDevCost(i));
	}

	//system("pause");
}
