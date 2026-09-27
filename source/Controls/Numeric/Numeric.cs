using System;
using ChaosFramework.Components;
using ChaosFramework.Graphics;
using ChaosFramework.Graphics.OpenGl;
using ChaosFramework.Graphics.Text;
using ChaosFramework.Math;
using ChaosFramework.Math.Vectors;
using static ChaosFramework.Math.Clamping;
using static ChaosFramework.Math.Exponentials;
using static ChaosFramework.Math.Signs;

namespace ChaosFramework.Menu.OpenGl.Controls
{
    public partial class Numeric : Control
    {
        // TODO: make customizable - bases greater than the number of configured digit keys wouldn't work trivially
        const int BASE = 10;

        const float DIGIT_WIDTH = 0.5f;

        DigitButton[] digits;

        public float textScale = 0.666f;
        public decimal min { get; private set; } = 1;
        public decimal max { get; private set; } = BASE * BASE;

        uint numDecimals = 0;
        float decimalSeparatorX = 0;

#pragma warning disable IDE0044 // Add readonly modifier - can be set via xml
        string decimalSeparator = ",";
#pragma warning restore IDE0044 // Add readonly modifier

        decimal _value;
        public decimal value
        {
            get => _value;
            set
            {
                int div = Pow(BASE, numDecimals);
                value = (decimal)(int)(value * div) / div;
                bool update = value != _value;
                _value = Clamp(min, max, value);
                GetNumDecimals();
                if (update && valueChanged != null)
                    valueChanged(this);
            }
        }

        public override float right => (digits == null || digits.Length == 0) ? position.x : digits[^1].right;
        public override float left => (digits == null || digits.Length == 0) ? position.x : digits[0].left;

        public override Align textAlign
        {
            get => base.textAlign;
            set
            {
                base.textAlign = value;
                UpdateAlign();
            }
        }

        protected override void Create(CreateParameters cparams)
        {
            base.Create(cparams);
            clipChildren = false;
        }

        void GetNumDecimals()
        {
            long remainder = (long)(value * Pow(BASE, numDecimals));
            if (digits != null)
                for (int i = digits.Length - 1; i >= 0; i--)
                {
                    digits[i].value = (int)Abs(remainder % BASE);
                    remainder /= BASE;
                }
        }

        void GetNumber()
        {
            decimal coefficient = 0;
            for (int i = 0; i < digits.Length; i++)
            {
                coefficient += digits[i].value;
                coefficient *= BASE;
            }
            value = coefficient / Pow(BASE, numDecimals + 1);
        }

        public void SetValues(decimal min, decimal max, uint numDecimals)
        {
            DisposeChildren();
            this.max = Max(min, max);
            this.min = min;
            this.numDecimals = numDecimals;
            decimal tmp = Max(System.Math.Abs(max), System.Math.Abs(min));
            int numPlaces = 1;
            while ((tmp /= BASE) >= 1)
                numPlaces++;

            digits = new DigitButton[numPlaces + numDecimals];
            for (int i = 0; i < digits.Length; i++)
            {
                digits[i] = CreateControl<DigitButton>(position, new Vector2f(size.y * DIGIT_WIDTH, size.y), "0");
                digits[i].enabledFunction = ChildEnable;
                digits[i].numberTyped += NumberTyped;
                if (i > 0)
                {
                    int neighbor = i - 1;
                    void outOfRange(DigitButton sender, bool overflow)
                    {
                        if (overflow)
                            digits[neighbor].Increment();
                        else
                            digits[neighbor].Decrement();
                    }
                    digits[i].outOfRange = outOfRange;
                }
                else
                    digits[i].outOfRange = MostSignificantOutOfRange;
            }
            GetNumDecimals();
            UpdateAlign();
        }

        bool ChildEnable()
            => enabledFunction();

        void MostSignificantOutOfRange(DigitButton sender, bool overflow)
            => value = overflow ? max : min;

        void NumberTyped(Control sender)
        {
            int newIndex = Array.IndexOf(digits, sender) + 1;
            activeControl = newIndex < digits.Length && newIndex > 0 ? digits[newIndex] : null;
            GetNumber();
            valueChanged?.Invoke(this);
        }

        void UpdateAlign()
        {
            float x = position.x;
            float dWidth = DIGIT_WIDTH * size.y;

            switch (textAlign)
            {
                case Align.Center:
                    x -= digits.Length * dWidth + dWidth * 0.1667f;
                    break;
                case Align.Right:
                    x -= digits.Length * dWidth * 2 + dWidth * 0.333f;
                    break;
            }

            for (int i = 0; i < digits.Length; i++)
            {
                if (i == digits.Length - numDecimals)
                    decimalSeparatorX = (x += dWidth * 0.777f) - position.x;

                x += dWidth;
                digits[i].position = new Vector2f(x, position.y);
                x += dWidth;
            }
        }

        public override bool IsHovered()
        {
            if (digits == null)
                return false;

            foreach (DigitButton btn in digits)
                if (btn.IsHovered())
                    return true;

            return false;
        }

        public override void UpdateTextGeometry()
            => font.content.UpdateText(ref _textGeometry, decimalSeparator, LayoutInfo.RIGHT);

        public override void Draw()
        {
            if (numDecimals <= 0)
                return;

            UpdateTextGeometry();
            Vector2f scrollOffset = scrolledPosition - position;

            float x = scrollOffset.x + left + decimalSeparatorX;
            float y = scrollOffset.y + position.y;

            scene.view.SetValues(fontShader, Matrix.Scaling(size.y) * Matrix.Translation(x, y));
            font.content.SetValues(fontShader);
            fontShader.SetValue("color", !hoverTextColor.IsNaN() && visuallyHovered ? hoverTextColor : textColor);
            textGeometry.DrawText(fontShader, "HUD");
        }
    }
}
