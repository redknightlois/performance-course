using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Workshop
{
    public class Program
    {
        static void Main(string[] args)
        {
            var p = new LZ4CompressionBenchmark();
            p.Setup();
            p.HighRepetition();
        }
    }
}
