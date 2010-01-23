#ifndef _ZIPF_GENERATOR_
#define _ZIPF_GENERATOR_

#pragma once
//Generate address conforming to zipf distribution.
#include <stdexcept>
#include <cmath>
#include "Generator.h"
#include <ctime>

class ZipfGenerator :public Generator
{
public:
	ZipfGenerator(size_t iaddressNum, double iratio);
	~ZipfGenerator(void);
	size_t get();
	void initialize();
	double* pageList;

	double ratio;			//ratio is the zipf
};

#endif