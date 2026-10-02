using System;
using System.Linq;
using ChaosFramework.Components;
using ChaosFramework.Graphics;
using ChaosFramework.Math;
using ChaosFramework.Math.Vectors;

namespace ChaosFramework.Menu.OpenGl
{
    public partial class Control
    {
        public enum RatioMode
        {
            None,
            KeepRatio
        }

        public Align anchor = Align.TopLeft;
        public RatioMode ratioMode = RatioMode.None;

        Vector2f _position;
        public virtual Vector2f position
        {
            get => _position;
            set
            {
                if (moveChildren)
                    foreach (Component comp in children)
                    {
                        if (comp is Control control)
                            control.position += value - _position;
                    }

                _position = value;
            }
        }

        Vector2f _size = 1f;
        public virtual Vector2f size
        {
            get => _size;
            set
            {
                if (!float.IsNaN(ratio))
                    switch (ratioMode)
                    {
                        case RatioMode.KeepRatio:
                            value = Ratio.FitAinB(value, new Vector2f(ratio, 1));
                            break;

                        case RatioMode.None:
                        default:
                            break;
                    }

                if (moveChildren)
                    foreach (Component c in children)
                    {
                        if (c is Control control)
                        {
                            Vector2f topRight = control.position + control.size,
                                     bottomLeft = control.position - control.size;

                            if ((control.anchor & Align.Top) > 0)
                            {
                                topRight.y += value.y - _size.y;
                                if ((control.anchor & Align.Bottom) == 0)
                                    bottomLeft.y += value.y - _size.y;
                            }
                            if ((control.anchor & Align.Right) > 0)
                            {
                                topRight.x += value.x - _size.x;
                                if ((control.anchor & Align.Left) == 0)
                                    bottomLeft.x += value.x - _size.x;
                            }

                            if ((control.anchor & Align.Bottom) > 0)
                            {
                                bottomLeft.y -= value.y - _size.y;
                                if ((control.anchor & Align.Top) == 0)
                                    topRight.y -= value.y - _size.y;
                            }
                            if ((control.anchor & Align.Left) > 0)
                            {
                                bottomLeft.x -= value.x - _size.x;
                                if ((control.anchor & Align.Right) == 0)
                                    topRight.x -= value.x - _size.x;
                            }

                            control.position = (topRight + bottomLeft) * 0.5f;
                            control.size = (topRight - bottomLeft) * 0.5f;
                        }
                    }

                _size = value;
            }
        }

        public Vector2f topLeft => new(left, top);
        public Vector2f topRight => new(right, top);
        public Vector2f bottomLeft => new(left, bottom);
        public Vector2f bottomRight => new(right, bottom);
        public Bounds2f bounds => new(bottomLeft, topRight);

        public virtual float ratio { get; set; } = float.NaN;
        public virtual float width { get => size.x * 2; set => size = new Vector2f(value / 2, size.y); }
        public virtual float height { get => size.y * 2; set => size = new Vector2f(size.x, value / 2); }
        public virtual float x { get => position.x; set => position = new Vector2f(value, position.y); }
        public virtual float y { get => position.y; set => position = new Vector2f(position.x, value); }

        public virtual float left => position.x - size.x;
        public virtual float right => position.x + size.x;
        public virtual float top => position.y + size.y;
        public virtual float bottom => position.y - size.y;

        public Bounds2f GetChildBounds(bool recursive = false, params Control[] ignore)
        {
            Bounds2f bounds = new();
            foreach (Control c in children.OfType<Control>())
                if (Array.IndexOf(ignore, c) == -1)
                {
                    bounds.Expand(c.bounds);
                    if (recursive)
                        bounds.Expand(c.GetChildBounds());
                }

            return bounds;
        }
    }
}
