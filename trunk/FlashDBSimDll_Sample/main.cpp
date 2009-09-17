#include "stdafx.h"
#include <ctime>
#include "TrivalBlockDevice.h"
#include "TrivalBufferManager.h"
#include "LRUBufferManager.h"
#include "CFLRUBufferManager.h"
#include "LRUWSRBufferManager.h"
#include "CMFTBufferManager.h"
#include "BufferManagerGroup.h"
using namespace std;
using namespace std::tr1;


void main()
{
	int bufferSize = 1000;
	BufferManagerGroup group;

	group.Add(shared_ptr<BufferManagerBase>(new LRUBufferManager(
		shared_ptr<IBlockDevice>(new TrivalBlockDevice), bufferSize)));
	group.Add(shared_ptr<BufferManagerBase>(new CFLRUBufferManager(
		shared_ptr<IBlockDevice>(new TrivalBlockDevice), bufferSize, bufferSize/2)));
	group.Add(shared_ptr<BufferManagerBase>(new LRUWSRBufferManager(
		shared_ptr<IBlockDevice>(new TrivalBlockDevice), bufferSize, 1)));
	group.Add(shared_ptr<BufferManagerBase>(new CMFTBufferManager(
		shared_ptr<IBlockDevice>(new TrivalBlockDevice), bufferSize)));


	srand(clock());
	int fcount = 0;
	ifstream traceFile("trace.txt");

	if(!traceFile.is_open())
	{
		cout<<"file missing~~"<<endl;
		exit(1);
	}

	int count=0;

	while (!traceFile.eof())
	{
		if(++count % 2000 == 0)
		{
			cout<<count<<endl;
		}
		size_t pageid;
		int length;
		int rw;
		char buf[2048];

		traceFile >> pageid >> length >> rw;
		rw = rand() % 2;

		if (count < 0)
			continue;
		if (count > 100000)
			break;
		
		//cout<<pageid<<","<<rw<<endl;
		for (int i = 0; i<length; i++)
		{
			if (rw == 0)
				group.Read(pageid, buf);
			else
				group.Write(pageid, buf);

			pageid++;
		}
	}

	group.Flush();

	printf("Manager\tRead %d\tWrite %d\n",
		group.GetReadCount(), group.GetWriteCount());

	int iend = group.GetMgrCount();
	for (int i=0; i<iend; ++i)
	{
		printf("Dev %d\tRead %d\tWrite %d\tCost %d\n", i,
			group.GetDevReadCount(i), group.GetDevWriteCount(i),
			group.GetDevCost(i));
	}
}
