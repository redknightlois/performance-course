using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    public unsafe class LZ4CompressionBenchmark
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                Add(new Job(RunMode.Dry)
                {
                    Env =
                    {
                        Runtime = Runtime.Core,
                        Platform = Platform.X64,
                        Jit = Jit.RyuJit
                    }
                });

                // Exporters for data
                Add(GetExporters().ToArray());
                // Generate plots using R if %R_HOME% is correctly set
                Add(RPlotExporter.Default);

                Add(StatisticColumn.AllStatistics);

                Add(BaselineValidator.FailOnError);
                Add(JitOptimizationsValidator.FailOnError);
                // TODO: Uncomment next line. See https://github.com/PerfDotNet/BenchmarkDotNet/issues/272
                //Add(ExecutionValidator.FailOnError);
                Add(EnvironmentAnalyser.Default);
            }
        }


        private byte[] lowBitRandomInput = new byte[Constants.Size.Megabyte];
        private byte[] lowBitRandomOutput = new byte[Constants.Size.Megabyte];
        private byte[] lowBitEncodedOutput;

        private byte[] highRepeatRandomInput = new byte[Constants.Size.Megabyte];
        private byte[] highRepeatRandomOutput = new byte[Constants.Size.Megabyte];
        private byte[] highRepeatEncodedOutput;

        [Setup]
        public void Setup()
        {
            {
                int threshold = 1 << 4;
                var rnd = new Random(1000);
                for (int i = 0; i < lowBitRandomInput.Length; i++)
                    lowBitRandomInput[i] = (byte)(rnd.Next() % threshold);

                lowBitEncodedOutput = new byte[LZ4.MaximumOutputLength(lowBitRandomInput.Length)];
            }

            {
                var main = new Random(1000);

                int i = 0;
                while (i < highRepeatRandomInput.Length)
                {
                    int sequenceNumber = main.Next(20);
                    int sequenceLength = Math.Min(main.Next(128), highRepeatRandomInput.Length - i);

                    var rnd = new Random(sequenceNumber);
                    for (int j = 0; j < sequenceLength; j++, i++)
                        highRepeatRandomInput[i] = (byte)(rnd.Next() % 255);                  
                }

                highRepeatEncodedOutput = new byte[LZ4.MaximumOutputLength(highRepeatRandomInput.Length)];
            }
        }

        [Benchmark]
        public void LowBitRandom()
        {
            fixed (byte* inputPtr = highRepeatRandomInput)
            fixed (byte* encodedOutputPtr = highRepeatEncodedOutput)
            fixed (byte* outputPtr = highRepeatRandomOutput)
            {
                int compressedSize = LZ4.Encode64(inputPtr, encodedOutputPtr, highRepeatRandomInput.Length, highRepeatEncodedOutput.Length);
                int uncompressedSize = LZ4.Decode64(encodedOutputPtr, compressedSize, outputPtr, highRepeatRandomInput.Length, true);
            }
        }

        [Benchmark]
        public void HighRepetition()
        {
            fixed (byte* inputPtr = lowBitRandomInput)
            fixed (byte* encodedOutputPtr = lowBitEncodedOutput)
            fixed (byte* outputPtr = lowBitRandomOutput)
            {
                int compressedSize = LZ4.Encode64(inputPtr, encodedOutputPtr, lowBitRandomInput.Length, lowBitEncodedOutput.Length);
                int uncompressedSize = LZ4.Decode64(encodedOutputPtr, compressedSize, outputPtr, lowBitRandomInput.Length, true);
            }
        }
    }
}
