using ChaosFramework.Graphics;
using ChaosFramework.Graphics.Colors;
using ChaosFramework.Graphics.OpenGl;
using ChaosFramework.Graphics.OpenGl.AssetContainers;
using ChaosFramework.Graphics.OpenGl.ChaosShader;
using ChaosFramework.Graphics.OpenGl.Text;
using ChaosFramework.Graphics.Text;
using ChaosFramework.Math;
using ChaosFramework.Math.Vectors;
using static ChaosFramework.Math.Clamping;

namespace ChaosFramework.Menu.OpenGl
{
    public partial class Control
    {
        public enum TextFit
        {
            None,
            Squish,
            Scale
        }

        public delegate string GetText(object value);

        public static readonly Rgba DEFAULT_TEXT_COLOR = Rgba.OPAQUE_BLACK;

        public static string ToString(object value)
            => value?.ToString();

        public bool monoSpace, ignoreEscapeSequences;
        public Vector2f letterDistance = 1f;
        public TextFit textFit = TextFit.Scale;

        public GetText getText = ToString;
        public Shader fontShader { get; private set; }
        public virtual FontContainer.Entry font { get; set; }

        public Rgba hoverTextColor = Rgba.NAN;
        public virtual Rgba textColor { get; set; } = DEFAULT_TEXT_COLOR;

        public virtual Align textAlign { get; set; } = Align.Left;

        protected virtual float visualTextSize => Min(size.x, size.y);

        public Margin textMargin = 0;
        public float textMarginLeft { get => textMargin.left; set => textMargin.left = value; }
        public float textMarginRight { get => textMargin.right; set => textMargin.right = value; }
        public float textMarginTop { get => textMargin.top; set => textMargin.top = value; }
        public float textMarginBottom { get => textMargin.bottom; set => textMargin.bottom = value; }

        protected TextMesh _textGeometry;
        public virtual TextMesh textGeometry { get => _textGeometry; set => _textGeometry = value; }

        public virtual string text { get; set; } = string.Empty;
        public string textSource
        {
            set
            {
                string prevText = text;
                text = scene.context.language.GetText(value);
                if (prevText != text)
                    UpdateTextGeometry();
            }
        }

        public virtual void UpdateTextGeometry()
            => font.content.UpdateText(
                ref _textGeometry,
                getText(text),
                new LayoutInfo(textAlign, monoSpace, false, ignoreEscapeSequences, letterDistance)
                );

        public Rgba GetParentTextColor()
            => (parent as Control)?.textColor ?? scene?.textColor ?? DEFAULT_TEXT_COLOR;

        protected Matrix GetTextScale()
        {
            float relWidth = width - textMargin.totalHorizontal;
            float textSz = visualTextSize;

            if (textGeometry.geo.geometryBounds.width * textSz > relWidth)
                if (textFit == TextFit.Squish)
                    return Matrix.Scaling(
                        relWidth / textGeometry.geo.geometryBounds.width,
                        textSz,
                        1
                        );
                else if (textFit == TextFit.Scale)
                    return Matrix.Scaling(
                        relWidth / textGeometry.geo.geometryBounds.width,
                        relWidth / textGeometry.geo.geometryBounds.width,
                        1
                        );

            return Matrix.Scaling(textSz, textSz, 1);
        }

        protected void DrawText()
        {
            if (!string.IsNullOrEmpty(text))
            {
                UpdateTextGeometry();

                Vector2f scrollOffset = scrolledPosition - position;
                float x = (textAlign & Align.LeftRight) switch
                {
                    Align.Left => scrollOffset.x + left + textMargin.left,
                    Align.Right => scrollOffset.x + right - textMargin.right,
                    _ => textMargin.offset.x + scrolledPosition.x,
                };
                float y = (textAlign & Align.BottomTop) switch
                {
                    Align.Top => scrollOffset.y + top - textMargin.top,
                    Align.Bottom => scrollOffset.y + bottom + textMargin.bottom,
                    _ => textMargin.offset.y + scrolledPosition.y,
                };

                Matrix textScale = GetTextScale();
                scene.view.SetValues(fontShader, textScale * Matrix.Translation(x, y));
                font.content.SetValues(fontShader);
                fontShader.SetValue("color", !hoverTextColor.IsNaN() && visuallyHovered ? hoverTextColor : textColor);
                textGeometry.DrawText(fontShader, "HUD");
            }
        }
    }
}
