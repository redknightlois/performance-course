using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Workshop
{
    public class PageLocator
    {
        private readonly PageHandlePtr[] _cache;
        private int _current;

        public PageLocator(int cacheSize = 4)
        {
            _cache = new PageHandlePtr[cacheSize];
        }

        public MyPage GetReadOnlyPage(long pageNumber)
        {
            int position = _current;

            int itemsLeft = _cache.Length;
            while (itemsLeft > 0)
            {
                int i = position % _cache.Length;

                // If the page number is equal to the page number we are looking for (therefore it's valid)
                // Will not fail at PageNumber=0 because the accesor will handle that.
                if (_cache[i].PageNumber != pageNumber)
                {
                    itemsLeft--;
                    position++;

                    continue;
                }

                return _cache[i].Value;
            }

            _current = (_current + 1) % _cache.Length;
            _cache[_current] = new PageHandlePtr(LowLevelTransaction.GetPage(pageNumber), false);
            return _cache[_current].Value;
        }

        public MyPage GetWritablePage(long pageNumber)
        {
            int position = _current;

            int itemsLeft = _cache.Length;
            while (itemsLeft > 0)
            {
                int i = position % _cache.Length;

                // If the page number is equal to the page number we are looking for (therefore it's valid)
                // Will not fail at PageNumber=0 because the accesor will handle that.
                if (_cache[i].PageNumber != pageNumber)
                {
                    // we continue.
                    itemsLeft--;
                    position++;

                    continue;
                }

                if (!_cache[i].IsWritable)
                    _cache[i] = new PageHandlePtr(LowLevelTransaction.ModifyPage(pageNumber), true);

                return _cache[i].Value;
            }

            _current = (_current + 1) % _cache.Length;
            _cache[_current] = new PageHandlePtr(LowLevelTransaction.ModifyPage(pageNumber), true);
            return _cache[_current].Value;
        }

        public void Clear()
        {
            _current = 0;
            Array.Clear(_cache, 0, _cache.Length);
        }

        public void Reset(long pageNumber)
        {
            for (int i = 0; i < _cache.Length; i++)
            {
                if (_cache[i].PageNumber == pageNumber)
                {
                    _cache[i] = new PageHandlePtr();
                    return;
                }
            }
        }
    }


}
