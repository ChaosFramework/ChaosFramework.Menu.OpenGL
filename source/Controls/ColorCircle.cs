using ChaosFramework.Components;
using ChaosFramework.Graphics.Colors;
using ChaosFramework.Graphics.OpenGl;
using ChaosFramework.Math;
using ChaosFramework.Math.Vectors;
using static ChaosFramework.Math.Clamping;
using static ChaosFramework.Math.Constants;
using static ChaosFramework.Math.Trigonometry;

namespace ChaosFramework.Menu.OpenGl.Controls
{
    class ColorCircle : Image, ValueControl<Hsva>
    {
        public System.Action<ValueControl> valueChanged { get; set; }

        Hsva hsva;

        protected override void Create(CreateParameters cparams)
        {
            base.Create(cparams);
            ratioMode = RatioMode.KeepRatio;
            shader = scene.context.shaders.colorDialog;
        }

        protected override void DrawImg(Texture tex)
        {
            Matrix controlTransform = Matrix.RotationZ(rotation)
                * Matrix.Scaling(size, 1)
                * Matrix.Translation(scrolledPosition);

            scene.view.SetValues(shader, controlTransform);
            shader.content.SetValue("hsva", hsva);
            Sprite.DrawPositionTextured(scene.context.graphics, shader, "Circle");

            float angle = -PI_HALF - hsva.h * PI_2;
            scene.view.SetValues(shader,
                Matrix.Scaling(0.05f)
                * Matrix.Translation(Cos(angle) * hsva.s, Sin(angle) * hsva.s)
                * controlTransform);

            shader.content.SetValue("hsva", new Hsva(0, 0, 1, 0));
            Sprite.DrawPositionTextured(scene.context.graphics, shader, "Circle");
        }

        public override void UpdateInteraction()
        {
            base.UpdateInteraction();
            if (mouseDownOnControl[0])
            {
                Vector2f dir = scene.context.cursorPosition - scrolledPosition;
                float sat = dir.Length();
                float hue = ACos(Vector2f.Dot(dir, new Vector2f(0, -1)) / sat) / PI_2;
                if (dir.x > 0)
                    hue = 1 - hue;

                SetValue(new Hsva(hue, Clamp(0, 1, sat / base.size.x), hsva.v, hsva.a));
            }
        }

        public Hsva GetValue()
            => hsva;

        public void SetValue(Hsva value)
        {
            bool changed = hsva != value;
            hsva = value;
            if (changed)
                valueChanged?.Invoke(this);
        }
    }
}
