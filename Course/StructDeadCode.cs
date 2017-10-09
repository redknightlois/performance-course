using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using BenchmarkDotNet.Attributes;

namespace Course
{
    public class StructDeadCode
    {
        public interface IMarker { }

        public struct AsStruct : IMarker { }
        public class AsClass : IMarker { }

        public StructDeadCode()
        {
            var rnd = new Random();
            value = rnd.Next();
            value2 = rnd.Next();
        }

        public static class Generic
        {
            public static int Method<T>(int i, int j) where T : IMarker
            {
                // This will be eliminated when T is an struct.
                if (typeof(T) == typeof(AsClass))
                {
                    return i;
                }

                return j;
            }
        }

        private readonly int value;
        private readonly int value2;

        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public int WithStruct()
        {
            return Generic.Method<AsStruct>(value, value2);
        }

        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public int WithClass()
        {
            return Generic.Method<AsClass>(value, value2);
        }
    }
}
