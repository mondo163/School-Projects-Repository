using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace CST352_Assign1_Armando_Santillan
{
    public class Consumer
    {
        private Random rand;
        private SafeRing theRing;
        Thread thread;
        private bool stop;
        private int timeout;

        private PerformanceCounter warningRateCounter;
        private PerformanceCounter consumedRate;
        public Consumer(Random r, SafeRing ring, int timeout = -1)
        {
            rand = r;
            theRing = ring;
            stop = false;
            this.timeout = timeout;

            warningRateCounter = new PerformanceCounter("CST352", "WarningRate", false);
            consumedRate = new PerformanceCounter("CST352", "ConsumedRate", false);
        }

        public void Start()
        {
            //create and start new thread
            thread = new Thread(ThreadProc);
            thread.Start(this);
        }
        private static void ThreadProc(Object param)
        {
            //new thread starts producing
            (param as Consumer).Consume();
        }
        public void Consume()
        {
            //consume until told to stop
            while (!stop)
            {
                try
                {
                    ConsumeOneItem();
                    
                }
                catch (ThreadInterruptedException)
                {
                    Console.WriteLine("Consuming thread interrupted!");
                }
                
            }

        }
        public void ConsumeOneItem()
        {
            try
            {
                //remove item from queue
                int item = theRing.Remove(timeout);

                //produce random number
                int i = rand.Next(1, 1000);

                //sleep thread for i mseconds
                Thread.Sleep(i);

                consumedRate.Increment();
            }
            catch (TimeoutException te)
            {
                warningRateCounter.Increment();
                Console.WriteLine("Consumer timeout while removing.");
            }
        }
        public void Stop()
        {
            //tell consumer to stop consuming
            stop = true;

            //wake the consumer thread if it's sleeping
            thread.Interrupt();
        }
    }
}
