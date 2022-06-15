#pragma once
#include "MemoryPool.h"
class FirstFitPool :
    public MemoryPool
{
public:
    //instantiate the base class constructor with the poolSize
	FirstFitPool(unsigned int poolSize) : MemoryPool(poolSize) {}

protected:
    //findavailable chunk function implementation. 
    virtual std::vector<MemoryPool::Chunk>::iterator FindAvailableChunk(unsigned int nBytes);

};

