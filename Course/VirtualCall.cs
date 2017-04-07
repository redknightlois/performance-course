using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using BenchmarkDotNet.Attributes;

namespace Course
{
    public interface IInterface
    {
        int Virtual(int i);
    }

    public class Parent : IInterface
    {
        public int NonVirtual(int i)
        {
            return i;
        }

        public virtual int Virtual(int i)
        {
            return i;
        }
    }

    public class Child : Parent
    {
        public override int Virtual(int i)
        {
            return i;
        }
    }

    public class VirtualCall
    {
        private readonly Parent child = new Child();
        private readonly Parent parent = new Parent();
        private readonly IInterface @interface = new Child();

        [Benchmark]
        public int NonVirtual()
        {
            int result = 0;
            for (int i = 0; i < 1000; i++)
                result += parent.NonVirtual(i);
            return result;
        }

        [Benchmark]
        public int VirtualThis()
        {
            int result = 0;
            for (int i = 0; i < 1000; i++)
                result += parent.Virtual(i);
            return result;
        }

        [Benchmark]
        public int VirtualDerived()
        {
            int result = 0;
            for (int i = 0; i < 1000; i++)
                result += child.Virtual(i);
            return result;
        }

        [Benchmark]
        public int VirtualInterface()
        {
            int result = 0;
            for (int i = 0; i < 1000; i++)
                result += @interface.Virtual(i);
            return result;
        }
    }
}
