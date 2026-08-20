using System;
using Unity.Mathematics;

namespace Ustas.RimAI.Communication.Memory.Utils
{

    // Non-obvious edge case — read carefully before changing. (summary RimWorld GC foreach summary)
    public class RimRingBuffer<T>
    {
        private readonly T[] _buffer;

        private readonly int _capacity;
        private readonly int _capacityMask;
        private int _head = 0;
        private int _count = 0;

        public int Count => _count;

        public RimRingBuffer(int newCapacity)
        {
            _capacity = math.ceilpow2(newCapacity);

            _buffer = new T[_capacity];
            _capacityMask = _capacity - 1;
        }

        public void Add(T item)
        {
            _buffer[(_head + _count) & _capacityMask] = item;

            if (_count < _capacity) _count++;
            else _head = (_head + 1) & _capacityMask;
        }

        // Non-obvious edge case — read carefully before changing. (summary List index index Count summary)
        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= _count)
                {
                    throw new IndexOutOfRangeException($"索引 {index} 超出边界 (Count: {_count})");
                }
                return _buffer[(_head + index) & _capacityMask];
            }
        }

        public void Clear()
        {
            _head = 0;
            _count = 0;
            Array.Clear(_buffer, 0, _buffer.Length);
        }
    }

}
