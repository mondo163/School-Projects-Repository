using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace SetupSolutionCounters
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (PerformanceCounterCategory.Exists("CST352"))
            {
                PerformanceCounterCategory.Delete("CST352");
            }
            CounterCreationDataCollection counterCollection =
                new CounterCreationDataCollection();
            counterCollection.Add(new CounterCreationData("TimeoutRate", "TimeoutRate",
                PerformanceCounterType.RateOfCountsPerSecond32));
            counterCollection.Add(new CounterCreationData("WarningRate", "WarningRate",
                PerformanceCounterType.RateOfCountsPerSecond32));
            counterCollection.Add(new CounterCreationData("ErrorRate", "ErrorRate",
                PerformanceCounterType.RateOfCountsPerSecond32));
            counterCollection.Add(new CounterCreationData("ProducedRate", "ProducedRate",
                PerformanceCounterType.RateOfCountsPerSecond32));
            counterCollection.Add(new CounterCreationData("ConsumedRate", "ConsumedRate",
                PerformanceCounterType.RateOfCountsPerSecond32));
            PerformanceCounterCategory.Create("CST352", "CST352",
                PerformanceCounterCategoryType.SingleInstance, counterCollection);
        }
    }
}
