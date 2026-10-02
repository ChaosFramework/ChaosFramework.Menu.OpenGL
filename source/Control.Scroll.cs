using ChaosFramework.Math.Vectors;

namespace ChaosFramework.Menu.OpenGl
{
    public partial class Control
    {
        public bool? instanceScrollRelevant = null;

        public ScrollControl scrollingParent
            => GetParent<ScrollControl>();

        public Vector2f scrolledPosition
        {
            get
            {
                Control c = this;
                Vector2f off = Vector2f.EMPTY;
                while (c != null)
                {
                    if (c.scrollingParent != null)
                        off += c.scrollingParent.offset;

                    c = c.scrollingParent as Control;
                    if (c == this)
                        break;
                }

                return position + off;
            }
        }

        public virtual bool scrollRelevant
            => instanceScrollRelevant ?? true;
    }
}
