using ChaosFramework.Components;
using ChaosUtil.Primitives;

namespace ChaosFramework.Menu.OpenGl.Controls
{
    public class CheckBox : Button, ValueControl<bool>
    {
        static void ChangeValue(Control sender)
        {
            CheckBox cb = (CheckBox)sender;
            cb.isChecked = !cb.isChecked;
        }

        bool _isChecked;
        public bool isChecked
        {
            get => _isChecked;
            set
            {
                bool needsCallback = value != isChecked;
                _isChecked = value;
                text = value ? UnicodeChars.CheckMark.GetUnicodeString() : string.Empty;
                if (needsCallback)
                    valueChanged?.Invoke(this);
            }
        }

        public System.Action<ValueControl> valueChanged { get; set; }

        protected override void Create(CreateParameters cparams)
        {
            base.Create(cparams);
            texture = scene.context.texProvider.button;
            mouseDownLeft += ChangeValue;
            isChecked = false;
        }

        bool ValueControl<bool>.GetValue()
            => isChecked;

        void ValueControl<bool>.SetValue(bool value)
            => this.SetValueChecked(() => isChecked = value);
    }
}
