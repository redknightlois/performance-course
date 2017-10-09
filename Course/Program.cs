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
            //new RefAllocation().StackByValue();
            //new RefAllocation().StackByRef();
            //new RefAllocation().HeapByReuse();            
            //new ConstantPropagation().SumNonInlined();
            //new ConstantPropagation().SumInlined();            
            //new SwitchIf().If();
            //new SwitchIf().WithGaps();
            //new SwitchIf().WithoutGaps();
            //new VirtualCall().NonVirtual();
            //new VirtualCall().VirtualThis();
            //new VirtualCall().VirtualDerived();
            //new VirtualCall().VirtualInterface();
            //new StructDeadCode().WithClass();
            //new StructDeadCode().WithStruct();
            new ObjectPoolBenchmark().UsingFactory();
            new ObjectPoolBenchmark().UsingGenericNew();
            new ObjectPoolBenchmark().UsingSpecificNew();

            BenchmarkSwitcher.FromAssembly(typeof(Program).GetTypeInfo().Assembly).Run(args);
            Console.WriteLine();
        }
    }
}