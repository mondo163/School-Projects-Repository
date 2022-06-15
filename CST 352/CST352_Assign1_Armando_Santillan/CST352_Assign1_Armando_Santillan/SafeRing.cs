using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

namespace CST352_Assign1_Armando_Santillan
{
    public class SafeRing
    {
        private int[] buffer;
        private int head;
        private int tail;   
        private int capacity;
        private int size;

        private Mutex mutex;
        private ManualResetEvent hasItems;
        private ManualResetEvent hasCapacity;
        private PerformanceCounter timeoutRateCounter;
        public SafeRing(int capacity = 0)
        {
            this.capacity = capacity;
            buffer = new int[capacity];
            head = 0;
            tail = 0;
            size = 0;
            mutex = new Mutex();
            hasItems = new ManualResetEvent(false);
            hasCapacity = new ManualResetEvent(true);

            timeoutRateCounter = new PerformanceCounter("CST352", "TimeoutRate", false); ;
        }

        public void Insert(int i, int timeout = -1)
        {
            //wait for mutex and capacity to exist
            if (!WaitHandle.WaitAll(new WaitHandle[] { mutex, hasCapacity }, timeout))
            {
                timeoutRateCounter.Increment();
                throw new TimeoutException();
            }

            //insert at the end and move tail
            buffer[tail] = i;
            tail = (tail +1) % capacity;
            size++;
            Console.WriteLine($"Inserted {i}");

            //signal that it has items to remove
            hasItems.Set();

            //signel when buffer is full
            if (size == capacity)
            {
                hasCapacity.Reset();
            }

            //release the mutex()
            mutex.ReleaseMutex();
        }
        public int Remove(int timeout = -1)
        {
            //waits for both the mutex and hasItems 
            if(!WaitHandle.WaitAll(new WaitHandle[] { mutex, hasItems }, timeout))
            {
                timeoutRateCounter.Increment();
                throw new TimeoutException();
            }

            //remove item at head
            int i = buffer[head];
            head = (head+1) % capacity;
            size--;
            Console.WriteLine($"Removed {i}");

            //signal we have capacity
            hasCapacity.Set();

            //signal out of items
            if (size == 0)
            {
                hasItems.Reset();
            }

            //unlocks mutex
            mutex.ReleaseMutex();

            return i;
        }
        public int Count()
        {
            //pauses to retrieve the accurate size and returns the size
            mutex.WaitOne();
            int count = size;
            mutex.ReleaseMutex();

            return count;
        }
    }
}
