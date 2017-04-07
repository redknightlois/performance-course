using System;
using System.Reflection;
using BenchmarkDotNet.Running;

namespace Course
{
    class Program
    {
        static void Main(string[] args)
        {
            //new Inlining().SumNonInlined();
            new RefAllocation().StackByValue();
            new RefAllocation().StackByRef();
            new RefAllocation().HeapByReuse();            

            BenchmarkSwitcher.FromAssembly(typeof(Program).GetTypeInfo().Assembly).Run(args);
            Console.WriteLine();
        }
    }
}