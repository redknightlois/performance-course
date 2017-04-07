using System;
using System.Collections.Generic;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;

namespace Course
{
    [HardwareCounters(HardwareCounter.InstructionRetired)]
    public class SwitchIf
    {
        private int x = 7, y = 7000;

        [Benchmark]
        public int If()
        {
            if (x == 0)
                return 0;
            if (x == 1)
                return 1;
            if (x == 2)
                return 2;
            if (x == 3)
                return 3;
            if (x == 4)
                return 4;
            if (x == 5)
                return 5;
            if (x == 6)
                return 6;
            if (x == 7)
                return 7;
            return -1;
        }

        [Benchmark]
        public int WithoutGaps()
        {
            switch (x)
            {
                case 0:
                    return 0;
                case 1:
                    return 1;
                case 2:
                    return 2;
                case 3:
                    return 3;
                case 4:
                    return 4;
                case 5:
                    return 5;
                case 6:
                    return 6;
                case 7:
                    return 7;
                default:
                    return -1;
            }
        }

        [Benchmark]
        public int WithGaps()
        {
            switch (y)
            {
                case 0:
                    return 0;
                case 1000:
                    return 1000;
                case 2000:
                    return 2000;
                case 3000:
                    return 3000;
                case 4000:
                    return 4000;
                case 5000:
                    return 5000;
                case 6000:
                    return 6000;
                case 7000:
                    return 7000;
                default:
                    return -1;
            }
        }
    }
}
