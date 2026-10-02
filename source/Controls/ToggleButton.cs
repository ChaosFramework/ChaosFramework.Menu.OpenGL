using System;

namespace ChaosFramework.Menu.OpenGl.Controls
{
    public class ToggleButton
        : Button
        , ValueControl<object>
    {
        public object[] values { get; private set; }
        public object value => values[selectedIndex];
        public int selectedIndex { get; private set; } = 0;

        public Action<ValueControl> valueChanged { get; set; }

        public void SetValues<T>(T[] values, int selectedIndex = 0)
        {
            this.values = new object[values.Length];
            for (int i = 0; i < values.Length; i++)
                this.values[i] = values[i];

            this.selectedIndex = selectedIndex;
            if (selectedIndex < 0 || selectedIndex >= values.Length)
                selectedIndex = 0;

            text = getText(value);
        }

        public void SetValues(object[] values, int selectedIndex = 0)
        {
            this.values = values;
            this.selectedIndex = selectedIndex;
            if (selectedIndex < 0 || selectedIndex >= values.Length)
                selectedIndex = 0;

            text = getText(value);
        }

        public void Increment()
        {
            selectedIndex++;
            selectedIndex %= values.Length;
            text = getText(value);
            valueChanged?.Invoke(this);
        }

        object ValueControl<object>.GetValue()
            => value;

        void ValueControl<object>.SetValue(object value)
        {
            int index;
            if ((index = Array.IndexOf(values, value)) < 0)
                throw new InvalidOperationException($"This {nameof(ToggleButton)} does not contain {value}.");

            if (selectedIndex != index)
            {
                selectedIndex = index;
                valueChanged?.Invoke(this);
            }
        }
    }
}
