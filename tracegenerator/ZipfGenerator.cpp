#include "ZipfGenerator.h"


ZipfGenerator::ZipfGenerator(size_t iaddressNum, double iratio)
{
	ratio = iratio;
	if(ratio>=1 || ratio<0.5)
	{
		throw std::runtime_error("zipf ratio error");
	}

	addressNum = iaddressNum;
	pageList = new double[iaddressNum];

	initialize();
}

ZipfGenerator::~ZipfGenerator(void)
{
	delete []pageList;
}

void ZipfGenerator::initialize()
{
	double theta = 1 - log(ratio)/ log(1-ratio);
	
	double sum = 0;
	for(int i = 0; i<addressNum; i++)
	{
		sum += pow(i+1, -theta);
		pageList[i] = sum;
	}
	
	for(int i = 0; i<addressNum; i++)
	{
		pageList[i]/=sum;
	}

	srand(time(NULL));
}

size_t ZipfGenerator::get()
{
	double ran =(double)rand()/RAND_MAX;
	for(int i=0; i<addressNum; i++)
	{
		if(ran<=pageList[i])
		{
			return i+1;
		}
	}
	return 0;
}