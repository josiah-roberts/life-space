using System;

namespace LifeSpace
{
    record BoundedValue
    {
        private int Value { get; init; }
        public BoundedValue(int Value, int min, int max)
        {
            if (Value > max || Value < min)
            {
                throw new ArgumentException("Argument out-of-bounds", nameof(Value));
            }
            this.Value = Value;
        }
        public static implicit operator int(BoundedValue p) => p.Value;

        public override string ToString() => Value.ToString();
    }

    record Pleasure(int Value) : BoundedValue(Value, -100, 100)
    {

        public override string ToString() => base.ToString();
    }
    record Urgency(int Value) : BoundedValue(Value, 0, 100)
    {

        public override string ToString() => base.ToString();
    }
    record Importance(int Value) : BoundedValue(Value, 0, 100)
    {
        public override string ToString() => base.ToString();
    }
    record Effort(int Value) : BoundedValue(Value, 0, MaxValue)
    {
        public const int MaxValue = 20;
        public override string ToString() => base.ToString();
    }
}