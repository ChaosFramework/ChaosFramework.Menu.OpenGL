using ChaosFramework.Math.Vectors;
using ChaosUtil.Serialization.Text;

namespace ChaosFramework.Menu.OpenGl
{
    public struct Margin
    {
        static Margin()
            => Parse.AddParser<Margin>(ParseMargin);

        static bool ParseMargin(string str, out Margin margin)
        {
            string[] values = str.Split([',']);

            switch (values.Length)
            {
                case 1:
                    if (Parse.TryParse(values[0], out margin.left))
                    {
                        margin = new Margin(margin.left);
                        return true;
                    }
                    break;

                case 2:
                    if (Parse.TryParse(values[0], out margin.left))
                        if (Parse.TryParse(values[1], out margin.bottom))
                        {
                            margin = new Margin(margin.left, margin.bottom);
                            return true;
                        }
                    break;

                case 4:
                    if (Parse.TryParse(values[0], out margin.left))
                        if (Parse.TryParse(values[1], out margin.right))
                            if (Parse.TryParse(values[2], out margin.bottom))
                                if (Parse.TryParse(values[3], out margin.top))
                                    return true;
                    break;

            }

            margin = new Margin();
            return false;
        }

        public float left, right, bottom, top;

        public float horizontal { set => left = right = value; }
        public float vertical { set => top = bottom = value; }

        public readonly float totalHorizontal => left + right;
        public readonly float totalVertical => top + bottom;

        public readonly Vector2f offset => new(left - right, bottom - top);

        public Margin(float all)
        {
            left = right = top = bottom = all;
        }

        public Margin(float horizontal, float vertical)
        {
            left = right = horizontal;
            top = bottom = vertical;
        }

        public Margin(float l, float r, float t, float b)
        {
            left = l;
            right = r;
            top = t;
            bottom = b;
        }

        public static implicit operator Margin(float margin) => new(margin);
        public static implicit operator Margin(Vector2f margin) => new(margin.x, margin.y);
    }
}
