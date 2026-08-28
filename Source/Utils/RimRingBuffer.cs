using System;
using Ustas.RimAI.Communication.Memory.Policy;
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
            MemoryColonyCapacityPolicy.AfterAdd(_head, _count, _capacity, out int writeIndex, out int newHead, out int newCount);
            _buffer[writeIndex] = item;
            _head = newHead;
            _count = newCount;
        }

        // Non-obvious edge case — read carefully before changing. (summary List index index Count summary)
        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= _count)
                {
                    throw new IndexOutOfRangeException($"Індекс {index} поза межами (Count: {_count})");
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
