namespace ChaosFramework.Menu.OpenGl.Controls
{
    public partial class Numeric
        : ValueControl<float>
        , ValueControl<double>
        , ValueControl<decimal>
        , ValueControl<sbyte>
        , ValueControl<byte>
        , ValueControl<short>
        , ValueControl<ushort>
        , ValueControl<int>
        , ValueControl<uint>
        , ValueControl<long>
        , ValueControl<ulong>
    {
        public System.Action<ValueControl> valueChanged { get; set; }

        float ValueControl<float>.GetValue() => (float)value;
        void ValueControl<float>.SetValue(float value) => ((ValueControl<float>)this).SetValueChecked(() => this.value = (decimal)value);

        double ValueControl<double>.GetValue() => (double)value;
        void ValueControl<double>.SetValue(double value) => ((ValueControl<double>)this).SetValueChecked(() => this.value = (decimal)value);

        decimal ValueControl<decimal>.GetValue() => value;
        void ValueControl<decimal>.SetValue(decimal value) => ((ValueControl<decimal>)this).SetValueChecked(() => this.value = value);

        sbyte ValueControl<sbyte>.GetValue() => (sbyte)value;
        void ValueControl<sbyte>.SetValue(sbyte value) => ((ValueControl<sbyte>)this).SetValueChecked(() => this.value = value);

        byte ValueControl<byte>.GetValue() => (byte)value;
        void ValueControl<byte>.SetValue(byte value) => ((ValueControl<byte>)this).SetValueChecked(() => this.value = value);

        short ValueControl<short>.GetValue() => (short)value;
        void ValueControl<short>.SetValue(short value) => ((ValueControl<short>)this).SetValueChecked(() => this.value = value);

        ushort ValueControl<ushort>.GetValue() => (ushort)value;
        void ValueControl<ushort>.SetValue(ushort value) => ((ValueControl<ushort>)this).SetValueChecked(() => this.value = value);

        int ValueControl<int>.GetValue() => (int)value;
        void ValueControl<int>.SetValue(int value) => ((ValueControl<int>)this).SetValueChecked(() => this.value = value);

        uint ValueControl<uint>.GetValue() => (uint)value;
        void ValueControl<uint>.SetValue(uint value) => ((ValueControl<uint>)this).SetValueChecked(() => this.value = value);

        long ValueControl<long>.GetValue() => (long)value;
        void ValueControl<long>.SetValue(long value) => ((ValueControl<long>)this).SetValueChecked(() => this.value = value);

        ulong ValueControl<ulong>.GetValue() => (ulong)value;
        void ValueControl<ulong>.SetValue(ulong value) => ((ValueControl<ulong>)this).SetValueChecked(() => this.value = value);
    }
}
