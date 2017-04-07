using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using BenchmarkDotNet.Attributes;

#if FULL
using BenchmarkDotNet.Diagnostics.Windows.Configs;
#endif


namespace Course
{
#if FULL
    [InliningDiagnoser]
#endif
    public class Inlining
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int Inlined(int i)
        {
            return i;
        }

        [Benchmark]
        public int SumInlined()
        {
            int result = 0;
            for (int i = 0; i < 1000; i++)
                result += Inlined(i);
            return result;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private int NonInlined(int i)
        {
            return i;
        }

        [Benchmark]
        public int SumNonInlined()
        {
            int result = 0;
            for (int i = 0; i < 1000; i++)
                result += NonInlined(i);
            return result;
        }
    }
}
