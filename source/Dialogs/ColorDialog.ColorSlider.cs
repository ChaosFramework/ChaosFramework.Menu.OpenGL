using ChaosFramework.Components;
using ChaosFramework.Graphics.OpenGl;
using ChaosFramework.Graphics.OpenGl.AssetContainers;
using ChaosFramework.Math;

namespace ChaosFramework.Menu.OpenGl.Dialogs
{
    using Controls;

    public partial class ColorDialog
    {
        public class ColorSlider : Slider
        {
            const float COLOR_BAR_HEIGHT = 0.8f;

            public string pass;

            ShaderContainer.Entry shader;

            ColorDialog dlg
                => (ColorDialog)scene;

            protected override void Create(CreateParameters cparams)
            {
                base.Create(cparams);
                shader = scene.context.shaders.colorDialog;
                btnSlide.texture = scene.context.texProvider.colorSliderButton;
            }

            public override void Draw()
            {
                if (!drawLine)
                    return;

                Matrix transform = Matrix.IDENTITY;
                transform.m00 = axis.x; transform.m01 = axis.y;
                transform.m10 = -axis.y; transform.m11 = axis.x;

                float sz = orientation == SliderOrientation.Vertical ? btnSlide.size.y : btnSlide.size.x;
                scene.view.SetValues(
                    shader,
                    Matrix.Scaling(size.x - sz, size.y * COLOR_BAR_HEIGHT, 1)
                    * transform
                    * Matrix.Translation(scrolledPosition)
                    );

                shader.SetValue("rgba", dlg.rgba);
                shader.SetValue("hsva", dlg.hsva);
                shader.SetValue("invScreenHeight", 1.0f / scene.context.window.height);
                shader.BeginPass(pass);
                Sprite.DrawPositionTextured(scene.context.graphics);
                shader.EndPass();
            }
        }
    }
}
