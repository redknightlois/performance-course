using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Validators;

namespace Workshop
{
    [Config(typeof(Config))]
    public class PlRandomWrite
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                Add(new Job
                {
                    Env =
                    {
                        Runtime = Runtime.Core,
                        Platform = Platform.X64,
                        Jit = Jit.RyuJit
                    },
                    // TODO: Next line is just for testing. Fine tune parameters.
                    //Mode = Mode.SingleRun,
                    //LaunchCount = 1,
                    //WarmupCount = 2,
                    //TargetCount = 40,
                });

                // Exporters for data
                Add(GetExporters().ToArray());
                // Generate plots using R if %R_HOME% is correctly set
                Add(RPlotExporter.Default);

                Add(StatisticColumn.AllStatistics);

                Add(BaselineValidator.FailOnError);
                Add(JitOptimizationsValidator.FailOnError);
                Add(EnvironmentAnalyser.Default);
            }
        }

        private const int NumberOfOperations = 10000;

        [Params(8, 16, 32, 64, 128, 256, 512)]
        public int CacheSize { get; set; }

        [Params(5)]
        public int RandomSeed { get; set; }

        private List<long> _pageNumbers;

        private PageLocator _cacheV1;

        [Setup]
        public void Setup()
        {
            _cacheV1 = new PageLocator(CacheSize);

            var generator = new Random(RandomSeed);

            _pageNumbers = new List<long>();
            for (int i = 0; i < NumberOfOperations; i++)
            {
                long valueBuffer = generator.Next();
                valueBuffer += (long)generator.Next() << 32;
                valueBuffer += (long)generator.Next() << 64;
                valueBuffer += (long)generator.Next() << 96;

                _pageNumbers.Add(valueBuffer);
            }
        }

        [Benchmark(OperationsPerInvoke = NumberOfOperations)]
        public void Basic()
        {
            foreach (var pageNumber in _pageNumbers)
            {
                _cacheV1.GetWritablePage(pageNumber);
            }
        }
    }
}
