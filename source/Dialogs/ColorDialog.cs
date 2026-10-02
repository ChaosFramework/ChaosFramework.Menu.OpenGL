using ChaosFramework.Graphics.Colors;
using ChaosFramework.Math.Vectors;
using Xml = System.Xml;

namespace ChaosFramework.Menu.OpenGl.Dialogs
{
    using ChaosFramework.Collections;
    using Controls;

    public partial class ColorDialog
        : DialogScene
    {
        static string X2(float f)
            => ((int)(255 * f)).ToString("X2");

        object valueChanger = null;

        readonly Slider h, s, v, r, g, b, a;
        readonly RichTextBox txtHex;
        readonly ColorCircle colCircle;

        public Hsva hsva
            => new(h.value, s.value, v.value, a.value);

        public Rgba rgba
        {
            get => new(r.value, g.value, b.value, a.value);
            set
            {
                if (!allowAlpha)
                    value.a = 1;

                Hsva hsv = Hsva.FromRgba(value);

                if (valueChanger != r) r.value = value.r;
                if (valueChanger != g) g.value = value.g;
                if (valueChanger != b) b.value = value.b;
                if (valueChanger != a) a.value = value.a;

                bool rgbGray = rgba.r == rgba.g && rgba.g == rgba.b;
                bool rgbBlack = rgba.r == 0 && rgba.g == 0 && rgba.b == 0;

                if (valueChanger != h && s.value != 0 && v.value != 0 && !rgbGray)
                    h.value = hsv.h;
                if (valueChanger != s && v.value != 0 && !rgbBlack)
                    s.value = hsv.s;
                if (valueChanger != v)
                    v.value = hsv.v;

                if (valueChanger != txtHex)
                    txtHex.text = $"{X2(value.r)}{X2(value.g)}{X2(value.b)}{X2(value.a)}";

                if (valueChanger != colCircle)
                    colCircle.SetValue(hsva);
            }
        }

        public bool allowAlpha
        {
            get => a.enabledFunction();
            set
            {
                a.enabledFunction = value
                                  ? Linq.PredicateTrue
                                  : Linq.PredicateFalse;

                if (!value)
                    a.value = 1;
            }
        }

        public ColorDialog(MenuContext context, float size)
            : base(context, new Vector2f(2, 1) * size, false)
        {
            panel.DisposeChildren();
            Xml.XmlDocument doc = new();
            doc.LoadXml(Properties.Resources.Layout_ColorDialog);
            LanguagePack.ProcessMacros([doc]);
            Xml.XmlNode node = doc.SelectSingleNode("ColorDialog");
            LayoutContainer.CreateMenuInControl(panel, node, true);

            (h = (Slider)panel.GetControlById("sldH")).valueChanged = HsvChanged;
            (s = (Slider)panel.GetControlById("sldS")).valueChanged = HsvChanged;
            (v = (Slider)panel.GetControlById("sldV")).valueChanged = HsvChanged;
            (r = (Slider)panel.GetControlById("sldR")).valueChanged = RgbaChanged;
            (g = (Slider)panel.GetControlById("sldG")).valueChanged = RgbaChanged;
            (b = (Slider)panel.GetControlById("sldB")).valueChanged = RgbaChanged;
            (a = (Slider)panel.GetControlById("sldA")).valueChanged = RgbaChanged;

            txtHex = (RichTextBox)panel.GetControlById("txtHex");
            txtHex.valueChanged = TextChanged;
            txtHex.textSize *= 2;

            colCircle = (ColorCircle)panel.GetControlById("colorCircle");
            colCircle.valueChanged = CircleChanged;

            ColorButton btnOk = (ColorButton)panel.GetControlById("btnOk");
            btnOk.mouseDown[0] = KindlyRequestSuicide;
        }

        void TextChanged(ValueControl c)
        {
            if (valueChanger == null)
            {
                valueChanger = c;
                if (Rgba.TryParseHex(txtHex.text.Trim(), out Rgba col))
                    rgba = col;

                valueChanger = null;
            }
        }

        void HsvChanged(Control c)
        {
            if (valueChanger == null)
            {
                valueChanger = c;
                rgba = Rgba.FromHsva(new Hsva(h.value, s.value, v.value, a.value));
                valueChanger = null;
            }
        }

        void RgbaChanged(Control c)
        {
            if (valueChanger == null)
            {
                valueChanger = c;
                rgba = new Rgba(r.value, g.value, b.value, a.value);
                valueChanger = null;
            }
        }

        void CircleChanged(ValueControl c)
        {
            if (valueChanger == null)
            {
                valueChanger = c;
                Hsva hsv = colCircle.GetValue();
                rgba = hsv.ToRgba();
                valueChanger = null;
            }
        }
    }
}
