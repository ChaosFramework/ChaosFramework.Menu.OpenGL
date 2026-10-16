using System;
using System.Linq;
using ChaosFramework.Components;
using ChaosFramework.Math.Vectors;
using ChaosUtil.Primitives;
using static ChaosFramework.Math.Clamping;

namespace ChaosFramework.Menu.OpenGl.Controls
{
    public partial class DropDownBox
        : Button, ValueControl<object>
    {
        public enum State { Collapsed, Expanded }

        public delegate bool EqualityComparer(object a, object b);

        static bool DefaultEquality(object a, object b)
            => !(a == null ^ b == null) && ((a == null && b == null) || a.Equals(b));

        public EqualityComparer equalityComparer = DefaultEquality;

        public bool enableMouse = true;
        public Action<ValueControl> valueChanged { get; set; }

        public State state { get; private set; } = State.Collapsed;

        public override string text
        {
            get => selectedIndex == -1 ? nullString : getText(selectedItem);
            set => nullString = value;
        }

        public int maxExpandedEntries => _maxRows * _maxColumns;

        int _maxRows = 13;
        public int maxRows { get => _maxRows; set => _maxRows = Max(5, value); }

        int _maxColumns = 1;
        public int maxColumns { get => _maxColumns; set => _maxColumns = Max(1, value); }

        public string nullString = "<null>";
        public Action selectedIndexChanged;
        DropDownButton[] buttons = [];
        Label lblTop, lblScrollIndicator;

        object[] items = Array<object>.empty;
        public int selectedIndex { get; private set; } = -1;
        int currentScroll = 0;

        public object selectedItem
        {
            get => (items == null || items.Length == 0 || selectedIndex < 0) ? null : items[selectedIndex];
            set => Select(value);

        }

        public int GetIndex(object value)
        {
            int index = -1;
            if (value != null)
                for (int i = 0; i < items.Length; i++)
                    if (equalityComparer(value, items[i]))
                    {
                        index = i;
                        break;
                    }

            return index;
        }

        protected override void Create(CreateParameters cparams)
        {
            base.Create(cparams);
            getText = ToString;
            mouseDownLeft = MouseDown;
        }

        void MouseDown(Control _)
        {
            if (enableMouse)
                Expand();
        }

        public void SetValues(object[] items, int selectedIndex = -1)
            => this.selectedIndex = selectedIndex >= (this.items = items).Length || selectedIndex < 0
                ? Array.IndexOf(items, selectedItem)
                : selectedIndex
                ;

        public void SetValues(object[] items, object selectedValue)
            => SetValues(items, Array.IndexOf(items, selectedValue));

        public void SetValues<T>(T[] items, int selectedIndex = -1)
            => SetValues(items.Cast<object>().ToArray(), selectedIndex);

        public override void SetUpdateCalls()
        {
            base.SetUpdateCalls();
            if (buttons != null)
            {
                if (state == State.Expanded)
                {
                    Control ctrl = parent as Control;
                    if (enableMouse && (ctrl != null && ctrl.activeControl != this || ctrl == null && scene.activeControl != this))
                        Collapse();
                }

                switch (state)
                {
                    case State.Expanded:
                        lblTop.doDraw = currentScroll > 0;
                        lblScrollIndicator.doDraw = items.Length - currentScroll > maxExpandedEntries;
                        break;

                    case State.Collapsed:
                        bool fullyCollapsed = true;
                        foreach (DropDownButton b in buttons)
                            if (!b.reachedTarget)
                            {
                                fullyCollapsed = false;
                                break;
                            }

                        if (fullyCollapsed)
                        {
                            DisposeChildren();
                            buttons = null;
                        }

                        break;
                }
            }
        }

        public void Expand()
        {
            if (items.Length == 0 || state != State.Collapsed)
                return;

            state = State.Expanded;
            Select(selectedItem);
            buttons = new DropDownButton[items.Length];
            for (int index = 0; index < items.Length; index++)
            {
                int i = index;
                buttons[i] = CreateControl<DropDownButton>(position, size, getText(items[i]));
                buttons[i].index = i;
            }

            lblTop = CreateControl<Label>(
                buttons[0].position - new Vector2f(0, 2 * (size.y * 1.05f) * -1),
                new Vector2f(0.05f, 0.05f), UnicodeChars.ArrowUp_3D.GetUnicodeString()
                );
            lblScrollIndicator = CreateControl<Label>(
                buttons[0].position - new Vector2f(0, 2 * (size.y * 1.05f) * maxExpandedEntries),
                new Vector2f(0.05f, 0.05f), UnicodeChars.ArrowDown_3D.GetUnicodeString()
                );
            lblTop.position += new Vector2f(0, (-lblTop.textGeometry.geo.geometryBounds.low.y - 1) * lblTop.size.y);
            lblScrollIndicator.position += new Vector2f(
                0,
                (-lblScrollIndicator.textGeometry.geo.geometryBounds.high.y + 1) * lblScrollIndicator.size.y
                );
            lblTop.doDraw = lblScrollIndicator.doDraw = false;
        }

        public void Collapse()
        {
            if (state != State.Expanded)
                return;

            currentScroll = 0;
            state = State.Collapsed;
            foreach (DropDownButton btn in buttons)
                btn.targetPos = position;

            if (selectedIndex >= 0)
            {
                children.Remove(buttons[selectedIndex]);
                children.Add(buttons[selectedIndex]);
            }

            lblTop.Dispose();
            lblScrollIndicator.Dispose();
            selectedIndexChanged?.Invoke(this);
        }

        public void Increment()
        {
            selectedIndex = Min(selectedIndex + 1, items.Length - 1);
            if (selectedIndex - currentScroll >= maxExpandedEntries)
                currentScroll++;
        }

        public void Decrement()
        {
            selectedIndex = Max(selectedIndex - 1, 0);
            if (selectedIndex - currentScroll < 0)
                currentScroll--;
        }

        void Select(object value)
        {
            selectedIndex = GetIndex(value);
            if (items.Length > 0 && selectedIndex == -1)
                selectedIndex = 0;

            currentScroll = Clamp(0, Max(0, items.Length - maxExpandedEntries), selectedIndex);
        }

        public override bool IsHovered()
            => enableMouse && base.IsHovered();

        public override void Draw()
        {
            if (state == State.Collapsed)
                base.Draw();
        }

        object ValueControl<object>.GetValue()
            => selectedItem;

        void ValueControl<object>.SetValue(object value)
        {
            int index = GetIndex(value);
            if (selectedIndex != index)
            {
                Collapse();
                selectedItem = value;
                valueChanged?.Invoke(this);
            }
        }
    }

    public class DropDownBox<T>
        : DropDownBox, ValueControl<T>
    {
        T ValueControl<T>.GetValue()
            => (T)((ValueControl<object>)this).GetValue();

        void ValueControl<T>.SetValue(T value)
            => ((ValueControl<object>)this).SetValue(value);

        public void SetValues(T[] items, int selectedIndex = -1)
            => base.SetValues(items, selectedIndex);
    }
}
