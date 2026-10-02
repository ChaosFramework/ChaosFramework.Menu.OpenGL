namespace ChaosFramework.Menu.OpenGl.Controls
{
    public interface ValueControl { }

    public interface ValueControl<T> : ValueControl
    {
        System.Action<ValueControl> valueChanged { get; set; }
        T GetValue();
        void SetValue(T value);
    }

    internal static class ValueControlExtensions
    {
        internal static void SetValueChecked<T>(this ValueControl<T> @this, System.Action actualSet)
        {
            T before = @this.GetValue();
            actualSet();
            if (!before.Equals(@this.GetValue()))
                @this.valueChanged?.Invoke(@this);
        }
    }
}
