#pragma once
#include "MemoryPool.h"
class BestFitPool :
    public MemoryPool
{
public:
    //instantiates the base class with the poolsize
    BestFitPool(unsigned int poolSize) :MemoryPool(poolSize) {}
protected:
    virtual std::vector<Chunk>::iterator FindAvailableChunk(unsigned int nBytes);
};

