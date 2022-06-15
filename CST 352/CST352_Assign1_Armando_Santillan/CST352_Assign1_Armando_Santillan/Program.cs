using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;
namespace CST352_Assign1_Armando_Santillan
{
    internal class Program
    {
        const int RING_CAPACITY = 15;
        const int PRODUCERS = 3;
        const int NUM_ITEMS_TO_PRODUCE = 1000;
        const int CONSUMERS = 3;
        const int TIMEOUT = 10;
        static void Main(string[] args)
        {
            PerformanceCounter timeoutRateCounter =
                new PerformanceCounter("CST352", "TimeoutRate", false);
            PerformanceCounter warningRateCounter =
                new PerformanceCounter("CST352", "WarningRate", false);
            PerformanceCounter errorRateCounter =
                new PerformanceCounter("CST352", "ErrorRate", false);
            PerformanceCounter producedRate =
                new PerformanceCounter("CST352", "ProducedRate", false);
            PerformanceCounter consumedRate =
                new PerformanceCounter("CST352", "ConsumedRate", false);
            timeoutRateCounter.RawValue = 0;
            warningRateCounter.RawValue = 0;
            errorRateCounter.RawValue = 0;

            Random random = new Random();
            SafeRing ring = new SafeRing(RING_CAPACITY);

            Producer[] producers = new Producer[PRODUCERS];
            WaitHandle[] completeEvents = new WaitHandle[PRODUCERS];
            //producers produce
            for (int i = 0; i < PRODUCERS; i++)
            {
                producers[i] = new Producer(random, ring, NUM_ITEMS_TO_PRODUCE, TIMEOUT);
                completeEvents[i] = producers[i].Complete;
                producers[i].Start();
            }

            //start consumers consuming
            
            Consumer[] consumer = new Consumer[CONSUMERS];
            for (int i = 0; i < CONSUMERS; i++)
            {
                consumer[i] = new Consumer(random, ring, TIMEOUT);
                consumer[i].Start();
            }

            //wait until all producers are complete
            WaitHandle.WaitAll(completeEvents);

            //stop consumers
            for (int i = 0; i < CONSUMERS; i++)
            {
                consumer[i].Stop();
            }

            
        }
    }
}
