using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Workshop
{
    public class LowLevelTransaction
    {
        // TODO: implement register shuffling here.
        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        public static MyPage ModifyPage(long pageNumber)
        {
            unsafe
            {
                return new MyPage { PageNumber = pageNumber };
            }
        }

        // TODO: implement register shuffling here.
        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        public static MyPage GetPage(long pageNumber)
        {
            unsafe
            {
                return new MyPage { PageNumber = pageNumber };
            }
        }
    }

    public class MyPage
    {
        public long PageNumber;
    }

    public struct PageHandlePtr
    {
        public readonly MyPage Value;
        public readonly bool IsWritable;

        private const int Invalid = -1;

        public PageHandlePtr(MyPage value, bool isWritable)
        {
            this.Value = value;
            this.IsWritable = isWritable;
        }

        public long PageNumber
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return IsValid ? Value.PageNumber : Invalid; }
        }

        public bool IsValid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return Value != null; }
        }
    }
}
