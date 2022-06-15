#include "FirstFitPool.h"

std::vector<MemoryPool::Chunk>::iterator FirstFitPool::FindAvailableChunk(unsigned int nBytes) {
	//find the chunk allocated for the block
	//loop through chunkc, find the allocated one and starting index == block-pool
	auto iter = chunks.begin();
	while (iter != chunks.end() && (iter->size < nBytes || iter->allocated))
	{
		//keep looking.. isn't the one
		iter++;
	}

	return iter;
}