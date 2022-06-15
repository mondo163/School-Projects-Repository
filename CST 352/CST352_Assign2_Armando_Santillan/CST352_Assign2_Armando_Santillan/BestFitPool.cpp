#include "BestFitPool.h"
std::vector<MemoryPool::Chunk>::iterator BestFitPool::FindAvailableChunk(unsigned int nBytes) {
	//Find the smallest available chunk that is big enoug to fit nbytes
	auto iter = chunks.begin();
	auto smallest = chunks.end();
	while (iter != chunks.end())
	{
		//checks if the iterator is allocated and has enought space
		if (!iter->allocated && iter->size >= nBytes)
		{
			//candidate block
			if (smallest == chunks.end() || (iter->size < smallest->size))
				smallest = iter;
		}
		
		iter++;

	}
	//returns smallest section
	return smallest;
}
