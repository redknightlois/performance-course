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
    public class ConstantPropagation
    {

        const ulong m1 = 0x5555555555555555;
        const ulong m2 = 0x3333333333333333;
        const ulong m4 = 0x0f0f0f0f0f0f0f0f;
        const ulong h01 = 0x0101010101010101;

        //This uses fewer arithmetic operations than any other known  
        //implementation on machines with fast multiplication.
        //It uses 12 arithmetic operations, one of which is a multiply.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int PopCountInlined(ulong x)
        {
            x -= (x >> 1) & m1;             //put count of each 2 bits into those 2 bits
            x = (x & m2) + ((x >> 2) & m2); //put count of each 4 bits into those 4 bits 
            x = (x + (x >> 4)) & m4;        //put count of each 8 bits into those 8 bits 
            return (int)((x * h01) >> 56);  //returns left 8 bits of x + (x<<8) + (x<<16) + (x<<24) + ... 
        }

        //This uses fewer arithmetic operations than any other known  
        //implementation on machines with fast multiplication.
        //It uses 12 arithmetic operations, one of which is a multiply.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int PopCount(ulong x)
        {
            x -= (x >> 1) & m1;             //put count of each 2 bits into those 2 bits
            x = (x & m2) + ((x >> 2) & m2); //put count of each 4 bits into those 4 bits 
            x = (x + (x >> 4)) & m4;        //put count of each 8 bits into those 8 bits 
            return (int)((x * h01) >> 56);  //returns left 8 bits of x + (x<<8) + (x<<16) + (x<<24) + ... 
        }


        [Benchmark]
        public int SumInlined()
        {
            ulong x = 0x0f0f0f0f0f0f0f0f;
            int result = 0;
            for (int i = 0; i < 1000; i++)
                result += PopCountInlined(x);
            return result;
        }

        [Benchmark]
        public int SumNonInlined()
        {
            ulong x = 0x0f0f0f0f0f0f0f0f;
            int result = 0;
            for (int i = 0; i < 1000; i++)
                result += PopCount(x);
            return result;
        }
    }
}
