using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
namespace CST352_Assign1_Armando_Santillan
{
    public class Producer
    {
        private int numItems;
        private Random rand;
        private SafeRing theRing;
        private ManualResetEvent complete;
        Thread thread;
        private int timeout;

        private PerformanceCounter warningRateCounter;
        private PerformanceCounter errorRateCounter;
        private PerformanceCounter producedRate;
        public Producer(Random r, SafeRing ring, int n, int timeout = -1)
        {
            rand = r;
            theRing = ring;
            numItems = n;
            this.timeout = timeout;

            complete = new ManualResetEvent(false);

            warningRateCounter = new PerformanceCounter("CST352", "WarningRate", false);
            errorRateCounter = new PerformanceCounter("CST352", "ErrorRate", false);
            producedRate = new PerformanceCounter("CST352", "ProducedRate", false);
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
            (param as Producer).Produce();
        }
        public void Produce()
        {
            for (int i = 0; i < numItems; i++)
            {
                //produces single item
                ProduceOneItem();
            }

            //Signal when process is complete
            complete.Set();
        }
        public void ProduceOneItem()
        {
            //produce random number
            int i = rand.Next(1, 1000);

            bool done = false;
            int tries = 0;
            while (!done && tries < 5)
            {
                try
                {
                    //insert number into queue
                    theRing.Insert(i, timeout);

                    //sleep thread for i mseconds
                    Thread.Sleep(i);

                    done = true;
                    producedRate.Increment();
                }
                catch (TimeoutException te)
                {
                    warningRateCounter.Increment();
                    tries++;
                    Console.WriteLine($"Producer timeout while inserting, tries = {tries}");
                }
            }

            if (!done)
            {
                errorRateCounter.Increment();
                Console.WriteLine("Producer timeout while inserting ");
            }
        }
        public WaitHandle Complete { get { return complete; } }
        
    }
}
