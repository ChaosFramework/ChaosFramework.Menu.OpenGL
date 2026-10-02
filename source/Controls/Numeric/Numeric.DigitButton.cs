using System;
using ChaosFramework.Components;
using ChaosFramework.Graphics.OpenGl.AssetContainers;
using ChaosFramework.Graphics.Text;
using ChaosFramework.Input;
using ChaosFramework.Input.InputEvents;
using ChaosFramework.Math;
using ChaosUtil.Primitives;
using static ChaosFramework.Math.Clamping;
using static ChaosFramework.Math.Signs;
using Keys = ChaosFramework.Input.Keyboard.HidUsage;

namespace ChaosFramework.Menu.OpenGl.Controls
{
    public partial class Numeric : Control
    {
        class DigitButton : Button
        {
            public delegate void Overflow(DigitButton sender, bool overflow);

            const float INCR_THRESHOLD = 0.2f;

            static readonly Keys[] dKeys = [
                Keys.D0,
                Keys.D1,
                Keys.D2,
                Keys.D3,
                Keys.D4,
                Keys.D5,
                Keys.D6,
                Keys.D7,
                Keys.D8,
                Keys.D9
                ];

            static readonly Keys[] numKeys = [
                Keys.Keypad0,
                Keys.Keypad1,
                Keys.Keypad2,
                Keys.Keypad3,
                Keys.Keypad4,
                Keys.Keypad5,
                Keys.Keypad6,
                Keys.Keypad7,
                Keys.Keypad8,
                Keys.Keypad9
                ];

            static readonly UnicodeChars[] digitChars = "0123456789".GetUnicodeChars();

            public int value;
            public Overflow outOfRange;
            public Action numberTyped;

            bool clicked;
            float incr;
            Numeric numeric;
            TextureContainer.Entry texActive, texDefault;

            float _visualTextSize;
            protected override float visualTextSize => _visualTextSize;

            public override string text
            {
                get => value.ToString();
                set
                {
                    // TODO: don't parse, use digitChars
                    if (!int.TryParse(value, out int digit) || digit >= BASE)
                        digit = 0;

                    this.value = digit;
                    base.text = digit.ToString();
                }
            }

            protected override void Create(CreateParameters cparams)
            {
                numeric = parent as Numeric ?? throw new InvalidOperationException($"Parent must be {nameof(Numeric)}.");
                base.Create(cparams);
                enabledFunction = Enabled;
                texDefault = texture;
                texActive = scene.context.texProvider.numericActive;
                clipChildren = false;
                textFit = TextFit.Squish;
            }

            bool Enabled() => !clicked;

            public override void UpdateInteraction()
            {
                texture = texActive;
                if (!clicked && !IsHovered())
                {
                    scene.hideCursor = false;
                    return;
                }

                base.UpdateInteraction();
                clicked = scene.mouseDown[0] > 0;
                scene.hideCursor = true;
                if (!clicked)
                {
                    scene.hideCursor = false;
                    incr = 0;
                }
            }

            public void Increment()
            {
                value += 1;
                if (value >= BASE)
                {
                    value = 0;
                    outOfRange?.Invoke(this, true);
                }
            }

            public void Decrement()
            {
                value -= 1;
                if (value < 0)
                {
                    value = BASE - 1;
                    outOfRange?.Invoke(this, false);
                }
            }

            public override void SetUpdateCalls()
            {
                base.SetUpdateCalls();
                texture = texDefault;
                scene.updateLayers[(int)GuiScene.UpdateLayers.MoveCursor].Add(MouseMove);
            }

            void MouseMove()
            {
                if (scene.mouseDown[0] <= 0)
                    clicked = false;

                if (clicked)
                {
                    incr += numeric.value < 0
                        ? scrolledPosition.y - scene.context.cursorPosition.y
                        : scene.context.cursorPosition.y - scrolledPosition.y;

                    if (incr > INCR_THRESHOLD)
                        do
                        {
                            incr -= INCR_THRESHOLD;
                            Increment();
                        } while (incr > INCR_THRESHOLD);
                    else if (incr < -INCR_THRESHOLD)
                        do
                        {
                            incr += INCR_THRESHOLD;
                            Decrement();
                        } while (incr < -INCR_THRESHOLD);
                    else
                        goto no_update;

                    numeric.GetNumber();

                no_update:
                    scene.context.cursorPosition = scrolledPosition;
                }
            }

            protected internal override void SetInputHandlers()
            {
                scene.context.input.AddHandler<InputPushEvent<Keyboard.Key>, Keyboard.Key, InputChange>(scene.context.inputLayer, KeyDown);
                base.SetInputHandlers();
            }

            bool KeyDown(InputPushEvent<Keyboard.Key> e)
            {
                for (int i = 0; i < BASE; i++)
                    if (e.axis.hidKey == dKeys[i] || e.axis.hidKey == numKeys[i])
                    {
                        value = i;
                        clicked = false;
                        scene.hideCursor = false;
                        numberTyped?.Invoke(this);
                        return true;
                    }

                return false;
            }

            public override void UpdateTextGeometry()
            {
                base.UpdateTextGeometry();
                Bounds2f bounds = new();

                for (uint i = 0; i < BASE; i++)
                {
                    GlyphDimensions charDescr = font.content.GetGlyph(digitChars[i]);
                    bounds.Expand(charDescr.charBounds);
                }

                _visualTextSize = numeric.textScale * Min(
                    Abs(size.x / bounds.left),
                    Abs(size.y / bounds.top),
                    Abs(size.x / bounds.right),
                    Abs(size.y / bounds.bottom)
                );
            }
        }
    }
}
