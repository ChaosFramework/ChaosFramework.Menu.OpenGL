using System;
using System.Linq;
using System.Xml;
using ChaosFramework.Components;
using ChaosFramework.Math.Vectors;
using ChaosUtil.Serialization.Text;
using static ChaosFramework.Math.Clamping;

namespace ChaosFramework.Menu.OpenGl.Controls.Layout
{
    static file class FlowPanelExt
    {
        public static bool IsHorizontal(this FlowPanel.FlowDirection dir)
            => dir == FlowPanel.FlowDirection.ltr || dir == FlowPanel.FlowDirection.rtl;
    }

    [Control($"{nameof(Controls.Layout)}.{nameof(FlowPanel)}")]
    public class FlowPanel : LayoutContainer
    {
        public enum FlowDirection
        {
#pragma warning disable ChaosCC0102 // Bad Identifier Case, used in Xml layout
            LeftToRight = 0,
            ltr = LeftToRight,
            RightToLeft = 1,
            rtl = RightToLeft,
            BottomToTop = 2,
            btt = BottomToTop,
            TopToBottom = 3,
            ttb = TopToBottom
#pragma warning restore ChaosCC0102 // Bad Identifier Case
        }

        enum Align
        {
            None,
            Center,
            Left,
            Right,
            Top,
            Bottom
        }

        enum AutoScale
        {
            None,
            Stretch,
            Scale,
        }

        public readonly OrderedDisposablesList<Component> orderedChildren = [];

        public FlowDirection direction = FlowDirection.ltr;

#pragma warning disable IDE0044 // Add readonly modifier - can be set via xml
        Align align = Align.Center;
#pragma warning restore IDE0044 // Add readonly modifier

        protected override void Create(CreateParameters cparams)
        {
            base.Create(cparams);
            clipChildren = false;
        }

        protected override DisposablesList<Component> InitChildrenList()
            => orderedChildren;

        public void MoveUp(Control c)
        {
            foreach (Control compare in orderedChildren.OfType<Control>())
                if (c == compare)
                {
                    orderedChildren.ShiftCurrent(-1);
                    break;
                }
        }

        public void MoveDown(Control c)
        {
            foreach (Control compare in orderedChildren.OfType<Control>())
                if (c == compare)
                {
                    orderedChildren.ShiftCurrent(1);
                    break;
                }
        }

        protected override void PerformLayoutInternal()
        {
            float lastMargin = 0;
            float lastPos = direction switch
            {
                FlowDirection.ltr => left,
                FlowDirection.rtl => right,
                FlowDirection.ttb => top,
                FlowDirection.btt => bottom,
                _ => throw new InvalidOperationException($"Invalid {nameof(FlowDirection)}")
            };

            foreach (Control child in orderedChildren.OfType<Control>())
            {
                controlLayoutData.TryGetValue(child, out XmlNode childData);

                Margin margin = GetMargin(childData);
                XmlAttribute attrRelSz = null, attrRelSzMode = null;
                if (childData != null)
                {
                    attrRelSz = childData.Attributes[$"{nameof(FlowPanel)}.autoScale"];
                    attrRelSzMode = childData.Attributes[$"{nameof(FlowPanel)}.autoScaleMode"];
                }

                if (attrRelSzMode == null || !Enum.TryParse(attrRelSzMode.Value, out AutoScale scale))
                    scale = AutoScale.None;

                float scaleFactor = 1;
                if (attrRelSz == null || !Parse.GetParser<float>().Invoke(attrRelSz.Value, out float relSz))
                    relSz = 1;

                if (scale != AutoScale.None)
                {
                    if (direction.IsHorizontal())
                        scaleFactor = (height - margin.bottom - margin.top) * relSz / child.height;
                    else
                        scaleFactor = (width - margin.left - margin.right) * relSz / child.width;

                    if (scale == AutoScale.Scale)
                        child.size *= scaleFactor;
                    else if (scale == AutoScale.Stretch)
                        if (direction.IsHorizontal())
                            child.size = new Vector2f(child.size.x, child.size.y * scaleFactor);
                        else
                            child.size = new Vector2f(child.size.x * scaleFactor, child.size.y);
                }

                float alignedPosition = 0;
                if (direction.IsHorizontal())
                    switch (align)
                    {
                        case Align.None:
                        case Align.Left:
                        case Align.Right: alignedPosition = child.position.y; break;
                        case Align.Center: alignedPosition = position.y; break;
                        case Align.Bottom: alignedPosition = bottom + (child.position.y - child.bottom) + margin.bottom; break;
                        case Align.Top: alignedPosition = top - (child.top - child.position.y) - margin.top; break;
                    }
                else
                    switch (align)
                    {
                        case Align.None:
                        case Align.Top:
                        case Align.Bottom: alignedPosition = child.position.x; break;
                        case Align.Center: alignedPosition = position.x; break;
                        case Align.Left: alignedPosition = left + (child.position.x - child.left) + margin.left; break;
                        case Align.Right: alignedPosition = right - (child.right - child.position.x) - margin.right; break;
                    }

                switch (direction)
                {
                    case FlowDirection.LeftToRight:
                        child.position = new Vector2f(
                            lastPos + Max(lastMargin, margin.left) + (child.position.x - child.left),
                            alignedPosition
                            );
                        lastPos = child.right;
                        lastMargin = margin.right;
                        break;
                    case FlowDirection.RightToLeft:
                        child.position = new Vector2f(
                            lastPos - Max(lastMargin, margin.right) - (child.right - child.position.x),
                            alignedPosition
                            );
                        lastPos = child.left;
                        lastMargin = margin.left;
                        break;
                    case FlowDirection.BottomToTop:
                        child.position = new Vector2f(
                            alignedPosition,
                            lastPos + Max(lastMargin, margin.top) + (child.position.y - child.bottom)
                            );
                        lastPos = child.top;
                        lastMargin = margin.bottom;
                        break;
                    case FlowDirection.TopToBottom:
                        child.position = new Vector2f(
                            alignedPosition,
                            lastPos - Max(lastMargin, margin.bottom) - (child.top - child.position.y)
                            );
                        lastPos = child.bottom;
                        lastMargin = margin.top;
                        break;
                    default:
                        throw new InvalidOperationException($"Invalid {nameof(FlowDirection)}");
                }
            }
        }
    }
}
