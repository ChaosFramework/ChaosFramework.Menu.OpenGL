using System;
using ChaosFramework.Graphics;
using ChaosFramework.Math.Vectors;
using ChaosUtil.Reflection;
using static ChaosFramework.Math.Clamping;

namespace ChaosFramework.Menu.OpenGl.Controls
{
    public partial class SettingsGrid
    {
        public class DefaultControlProvider<T>(object[] fieldTypeArgs)
            : ControlProvider<T>(fieldTypeArgs)
        {
            public const float RATIO = 5;

            static decimal MakeDecimal(object obj)
                => Cast.ForceCast<decimal>(Cast.ForceCast<T>(obj));

            public override ValueControl<T> GetControl(LayoutContainer target, float fontSize)
                => (ValueControl<T>)GetValueControl(target, fontSize);

            ValueControl GetValueControl(LayoutContainer target, float fontSize)
            {
                if (typeof(T).IsEnum)
                {
                    if (fieldTypeArgs.Length != 0)
                        throw new InvalidOperationException("Invalid number of arguments for field type settings.");

                    DropDownBox box = target.CreateControl<DropDownBox<T>>(0, new Vector2f(fontSize * RATIO, fontSize));
                    box.SetValues((T[])Enum.GetValues(typeof(T)));
                    return box;
                }
                else
                    switch (typeof(T).Name)
                    {
                        case nameof(Boolean):
                            {
                                if (fieldTypeArgs.Length != 0)
                                    throw new InvalidOperationException("Invalid number of arguments for field type settings.");

                                return target.CreateControl<CheckBox>(0, fontSize);
                            }

                        case nameof(String):
                            {
                                if (fieldTypeArgs.Length > 2)
                                    throw new InvalidOperationException("Invalid number of arguments for field type settings.");

                                int numChars = fieldTypeArgs.Length >= 1 ? (int)fieldTypeArgs[0] : 10;
                                bool multiLine = fieldTypeArgs.Length >= 2 && (bool)fieldTypeArgs[1];

                                RichTextBox textBox = target.CreateControl<RichTextBox>(
                                    0,
                                    new Vector2f(fontSize * Min(numChars / 2, RATIO), fontSize)
                                    );
                                textBox.multiLine = multiLine;
                                textBox.maxLength = numChars;
                                textBox.numbersOnly = false;
                                textBox.textSize = fontSize;
                                textBox.scrollMargin = 0.01f * fontSize;
                                textBox.textAlign = multiLine ? Align.TopLeft : Align.Center;
                                return textBox;
                            }

                        case nameof(Byte): return CreateIntegerNumeric<byte>(target, fontSize);
                        case nameof(Int16): return CreateIntegerNumeric<short>(target, fontSize);
                        case nameof(UInt16): return CreateIntegerNumeric<ushort>(target, fontSize);
                        case nameof(Int32): return CreateIntegerNumeric<int>(target, fontSize);
                        case nameof(UInt32): return CreateIntegerNumeric<uint>(target, fontSize);
                        case nameof(Int64): return CreateIntegerNumeric<long>(target, fontSize);
                        case nameof(UInt64): return CreateIntegerNumeric<ulong>(target, fontSize);
                        case nameof(Single): return CreateFloatingPointNumeric<float>(target, fontSize);
                        case nameof(Double): return CreateFloatingPointNumeric<double>(target, fontSize);

                        default:
                            throw new NotSupportedException($"There is no default control provider for {typeof(T).Name}.");
                    }
            }

            ValueControl CreateFloatingPointNumeric<ValueType>(LayoutContainer target, float fontSize)
            {
                if (fieldTypeArgs.Length != 3)
                    throw new InvalidOperationException("Invalid number of arguments for field type settings.");

                Numeric numeric = target.CreateControl<Numeric>(0, fontSize);
                numeric.SetValues(
                    MakeDecimal(fieldTypeArgs[0]),
                    MakeDecimal(fieldTypeArgs[1]),
                    Cast.ForceCast<uint>(fieldTypeArgs[2])
                );
                return numeric;
            }

            ValueControl CreateIntegerNumeric<ValueType>(LayoutContainer target, float fontSize)
            {
                if (fieldTypeArgs.Length != 2)
                    throw new InvalidOperationException("Invalid number of arguments for field type settings.");

                Numeric numeric = target.CreateControl<Numeric>(0, fontSize);
                numeric.SetValues(
                    MakeDecimal(fieldTypeArgs[0]),
                    MakeDecimal(fieldTypeArgs[1]),
                    0
                );
                return numeric;
            }
        }
    }
}
