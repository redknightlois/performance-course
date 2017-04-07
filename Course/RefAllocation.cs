using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Jobs;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Jobs;

#if FULL
using BenchmarkDotNet.Diagnostics.Windows.Configs;
#endif
namespace Course
{
    [Config(typeof(Config))]
    [MemoryDiagnoser]
#if FULL
    [HardwareCounters(HardwareCounter.InstructionRetired)]
#endif
    public class RefAllocation
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                // The same, using the .With() factory methods:
                Add(
                    Job.Default
                        .With(Platform.X64)
                        .With(Jit.RyuJit)
                );                                 
            }
        }

        public struct Value
        {
            public long A;
            public long B;
            public long C;
            public long D;
        }

        public class Reference
        {
            public long A;
            public long B;
            public long C;
            public long D;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void WorkByRef(long i, ref Value output)
        {
            output.A = i;
            output.B = i;
            output.C = i;
            output.D = i;
        }

        [Benchmark]
        public long StackByRef()
        {
            long result = 0;
            Value output = default(Value);
            for (long i = 0; i < 100000; i++)
            {
                WorkByRef(i, ref output);
                result = output.A;
            }
            return result;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private Value WorkByValue(int i)
        {
            return new Value { A = i, B = i, C = i, D = i };
        }

        [Benchmark]
        public long StackByValue()
        {
            long result = 0;
            for (int i = 0; i < 100000; i++)
            {
                result += WorkByValue(i).A;
            }
            return result;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private Reference WorkByHeapConstruction(int i)
        {
            return new Reference { A = i, B = i, C = i, D = i };
        }

        [Benchmark]
        public long HeapByConstruction()
        {
            long result = 0;
            for (int i = 0; i < 100000; i++)
            {
                result += WorkByHeapConstruction(i).A;
            }
            return result;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void WorkByHeapReuse(long i, Reference output)
        {
            output.A = i;
            output.B = i;
            output.C = i;
            output.D = i;
        }

        [Benchmark]
        public long HeapByReuse()
        {
            long result = 0;
            var output = new Reference();
            for (int i = 0; i < 100000; i++)
            {
                WorkByHeapReuse(i, output);
                result += output.A;
            }
            return result;
        }
    }
}
