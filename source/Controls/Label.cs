using ChaosFramework.Components;
using ChaosFramework.Graphics;
using ChaosFramework.Graphics.OpenGl;
using ChaosFramework.Graphics.Text;
using ChaosFramework.Math;
using static ChaosFramework.Math.Clamping;

namespace ChaosFramework.Menu.OpenGl.Controls
{
    public class Label
        : Control
    {
        public override float width
        {
            get => Max(0, right - left);
            set { }
        }

        public override float height
        {
            get => Max(0, top - bottom);
            set { }
        }

        public override float left
            => (textAlign & Align.Left) != 0
                ? position.x
                : (textAlign & Align.Right) != 0
                ? position.x - textGeometry.geo.textBounds.width * size.x
                : position.x - textGeometry.geo.textBounds.width * size.x * 0.5f;

        public override float right
            => (textAlign & Align.Left) != 0
                ? position.x + textGeometry.geo.textBounds.width * size.x
                : (textAlign & Align.Right) != 0
                ? position.x
                : position.x + textGeometry.geo.textBounds.width * size.x * 0.5f;

        public override float top
            => (textAlign & Align.Top) != 0
                ? position.y
                : (textAlign & Align.Bottom) != 0
                ? position.y + textGeometry.geo.textBounds.height * size.y
                : position.y + textGeometry.geo.textBounds.height * size.y * 0.5f;

        public override float bottom
            => (textAlign & Align.Top) != 0
                ? position.y - textGeometry.geo.textBounds.height * size.y
                : (textAlign & Align.Bottom) != 0
                ? position.y
                : position.y - textGeometry.geo.textBounds.height * size.y * 0.5f;

        public override string text
        {
            get => base.text;
            set
            {
                base.text = value;
                UpdateTextGeometry();
            }
        }

        public override Align textAlign
        {
            get => base.textAlign;
            set
            {
                base.textAlign = value;
                UpdateTextGeometry();
            }
        }

        public float maxLineWidth = float.PositiveInfinity;
        public bool enableColorCodes = false;

        protected override void Create(CreateParameters cparams)
        {
            base.Create(cparams);
            clipChildren = false;
        }

        public override void Draw()
        {
            scene.view.SetValues(fontShader, Matrix.Scaling(size, 1) * Matrix.Translation(scrolledPosition));
            font.content.SetValues(fontShader);
            fontShader.SetValue("color", textColor);
            UpdateTextGeometry();
            textGeometry.DrawText(fontShader, enableColorCodes ? "HUDColored" : "HUD");
        }

        public override void UpdateTextGeometry()
            => font.content.UpdateText(
                ref _textGeometry,
                getText(text),
                new LayoutInfo(
                    textAlign,
                    monoSpace,
                    enableColorCodes,
                    ignoreEscapeSequences,
                    letterDistance,
                    null,
                    maxLineWidth
                    ));

        public override bool IsHovered()
            => false;
    }
}
