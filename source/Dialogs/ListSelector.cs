using ChaosFramework.Graphics;
using ChaosFramework.Math.Vectors;
using static ChaosFramework.Math.Clamping;

namespace ChaosFramework.Menu.OpenGl.Dialogs
{
    using Controls;

    public class ListSelector
        : DialogScene
    {
        const float BTN_HEIGHT = 0.05f;
        const float MARGIN = 0.05f;

        public object value { get; private set; } = null;

        public ListSelector(
            MenuContext context,
            string prompt,
            object[] items,
            Vector2f size,
            Control.GetText getText = null,
            object selected = null
            ) : base(context, size)
        {
            getText ??= Control.ToString;

            Label lbl = panel.CreateControl<Label>(panel.topLeft - new Vector2f(0, 0.05f), 0.05f);
            lbl.textAlign = Align.TopLeft;
            lbl.text = prompt;

            ScrollBox scrollBox = panel.CreateControl<ScrollBox>(
                new Vector2f(panel.position.x, (lbl.bottom + (btnOk.top + MARGIN)) / 2),
                new Vector2f(panel.width / 2 - 2 * MARGIN, (lbl.bottom - (btnOk.top + MARGIN)) / 2)
                );

            int objIndex = 0;
            float top = scrollBox.top;
            foreach (object iteratorObj in items)
            {
                object obj = iteratorObj;
                if (obj == selected)
                    value = obj;

                bool ValueIsSelected()
                    => value != obj;

                void SelectValue(Control _)
                    => value = obj;

                Button btn = scrollBox.CreateControl<Button>(
                    new Vector2f(0, -MARGIN + scrollBox.top - BTN_HEIGHT * 2 * (++objIndex - 0.5f)),
                    new Vector2f(scrollBox.width / 2 - 2 * MARGIN, BTN_HEIGHT),
                    getText(obj)
                    );
                btn.textFit = Control.TextFit.Squish;
                btn.enabledFunction = ValueIsSelected;
                btn.mouseDownLeft = SelectValue;

                top = Min(top, btn.bottom);
            }

            panel.Layout();
        }
    }
}
