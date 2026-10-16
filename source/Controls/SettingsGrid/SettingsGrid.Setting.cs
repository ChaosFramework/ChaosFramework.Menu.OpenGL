using System;
using System.Reflection;
using ChaosFramework.Collections;

namespace ChaosFramework.Menu.OpenGl.Controls
{
    public partial class SettingsGrid
    {
        public abstract class Setting(string settingName, string description, EnableFunction enableFunc)
        {
            public readonly string settingName = settingName;
            public readonly string description = description;

            public bool enableColorCodes;

            public readonly EnableFunction enableFunc = enableFunc ?? Linq.PredicateTrue;

            public abstract ValueControl GetControl(LayoutContainer target, float fontSize);
            public abstract Type settingType { get; }
        }

        public sealed class Setting<T> : Setting
        {
            public readonly Func<T> getValue;
            public readonly Action<T> setValue;
            public readonly ControlProvider<T> controlProvider;

            public override Type settingType => typeof(T);

            public Setting(
                string name,
                string descr,
                Func<T> getValue,
                Action<T> setValue,
                ControlProvider<T> controlProvider,
                EnableFunction enabledFunc
                ) : base(name, descr, enabledFunc)
            {
                this.getValue = getValue;
                this.setValue = setValue;
                this.controlProvider = controlProvider;
            }

            public Setting(
                string name,
                string descr,
                FieldInfo field,
                object target,
                ControlProvider<T> controlProvider,
                EnableFunction enabledFunc
                ) : base(name, descr, enabledFunc)
            {
                if (field.FieldType != typeof(T))
                    throw new ArgumentException(
                        "Field type must match setting type "
                        + $"(fieldType is {field.FieldType.FullName}, setting type is {typeof(T).FullName})."
                        );

                getValue = () => (T)field.GetValue(target);
                setValue = value => field.SetValue(target, value);
                this.controlProvider = controlProvider;
            }

            public Setting(
                string name,
                string descr,
                PropertyInfo property,
                object target,
                ControlProvider<T> controlProvider,
                EnableFunction enabledFunc
                ) : base(name, descr, enabledFunc)
            {
                if (property.PropertyType != typeof(T))
                    throw new ArgumentException(
                        "Property type must match setting type "
                        + $"(property type is {property.PropertyType.FullName}, setting type is {typeof(T).FullName})."
                        );

                getValue = () => (T)property.GetValue(target);
                setValue = value => property.SetValue(target, value);
                this.controlProvider = controlProvider;
            }

            public override sealed ValueControl GetControl(LayoutContainer target, float fontSize)
            {
                ValueControl ctrl = controlProvider.GetControl(target, fontSize);
                ((Control)ctrl).enabledFunction = enableFunc;
                return ctrl;
            }
        }
    }
}
