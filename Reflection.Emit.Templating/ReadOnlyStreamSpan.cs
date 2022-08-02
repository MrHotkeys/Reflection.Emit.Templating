using System;

namespace MrHotkeys.Reflection.Emit.Templating
{
    public ref struct ReadOnlyStreamSpan<T>
    {
        private ReadOnlySpan<T> Items { get; }

        public int Position => PositionLocal + SourcePosition;

        public int PositionLocal { get; private set; }

        private int SourcePosition { get; }

        public int Length => Items.Length - PositionLocal;

        public ReadOnlyStreamSpan(ReadOnlySpan<T> items)
            : this(items, 0)
        { }

        public ReadOnlyStreamSpan(ReadOnlySpan<T> items, int sourcePosition)
        {
            Items = items;

            if (sourcePosition < 0)
                throw new ArgumentOutOfRangeException(nameof(sourcePosition), $"Must be >= 0!");
            SourcePosition = sourcePosition;

            PositionLocal = 0;
        }

        public T this[int index]
        {
            get
            {
                var indexOffset = PositionLocal + index;

                if (indexOffset < 0)
                    throw new ArgumentOutOfRangeException(nameof(index), $"Must be >= zero minus the current local position (-{PositionLocal})!");
                if (indexOffset >= Items.Length)
                    throw new ArgumentOutOfRangeException(nameof(index), $"Must be < the total number of items ({Items.Length})!");

                return Items[indexOffset];
            }
        }

        public T this[Index index] =>
            this[index.IsFromEnd ? Length - index.Value : index.Value];

        public ReadOnlyStreamSpan<T> this[Range range] =>
            Slice(range);

        public void Move(int count)
        {
            var newPositionLocal = PositionLocal + count;

            if (newPositionLocal < 0)
                throw new ArgumentOutOfRangeException(nameof(count), $"Must be >= zero minus the current local position (-{PositionLocal})!");
            if (count > Length)
                throw new ArgumentOutOfRangeException(nameof(count), $"Must be <= the total number of remaining items ({Length})!");

            PositionLocal = newPositionLocal;
        }

        public T Take()
        {
            if (Length == 0)
                throw new InvalidOperationException("No items remaining!");

            var item = this[0];
            PositionLocal++;
            return item;
        }

        public ReadOnlySpan<T> Take(int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Must be >= 0!");
            if (count > Length)
                throw new ArgumentOutOfRangeException(nameof(count), $"Must be <= the total number of remaining items ({Length})!");

            var span = Items.Slice(PositionLocal, count);
            PositionLocal += count;
            return span;
        }

        public ReadOnlyStreamSpan<T> Slice(int start) =>
            Slice(start, Length - start);

        public ReadOnlyStreamSpan<T> Slice(int start, int length)
        {
            var startOffset = PositionLocal + start;

            if (startOffset < 0)
                throw new ArgumentOutOfRangeException(nameof(start), $"Must be >= zero minus the current local position (-{PositionLocal})!");
            if (start >= Length)
                throw new ArgumentOutOfRangeException(nameof(start), $"Must be < number of items remaining ({Length})!");

            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), $"Must be >= 0!");
            if (start + length > Length)
                throw new ArgumentOutOfRangeException(nameof(length), $"Insufficent items to slice from {start} (requested {length}, remaining {Length})!");

            var slice = Items.Slice(startOffset, length);
            return new ReadOnlyStreamSpan<T>(slice, SourcePosition + startOffset);
        }

        public ReadOnlyStreamSpan<T> Slice(Range range)
        {
            var start = range.Start.IsFromEnd ?
                Length - range.Start.Value :
                range.Start.Value;
            var startOffset = PositionLocal + start;

            if (startOffset < 0)
                throw new ArgumentOutOfRangeException(nameof(range), $"Start must be >= zero minus the current position (-{PositionLocal})!");
            if (start >= Length)
                throw new ArgumentOutOfRangeException(nameof(range), $"Start must be < the number of items remaining ({Length})!");

            var end = range.End.IsFromEnd ?
                Length - range.End.Value :
                range.End.Value;
            var endOffset = PositionLocal + end;

            if (endOffset < 0)
                throw new ArgumentOutOfRangeException(nameof(range), $"End must be >= zero minus the current position (-{PositionLocal})!");
            if (endOffset < startOffset)
                throw new ArgumentOutOfRangeException(nameof(range), $"End must be >= Start!");
            if (end > Length)
                throw new ArgumentOutOfRangeException(nameof(range), $"Start must be <= the number of items remaining ({Length})!");

            return Slice(startOffset, endOffset - startOffset);
        }
    }
}