using ChaosFramework.Components;
using ChaosFramework.Graphics.OpenGl;
using ChaosFramework.Math;

namespace ChaosFramework.Menu.OpenGl.Dialogs
{
    using Controls;

    public partial class ColorDialog
    {
        public class ColorButton : Button
        {
            ColorDialog dlg
                => (ColorDialog)scene;

            protected override void Create(CreateParameters cparams)
            {
                base.Create(cparams);
                shader = scene.context.shaders.colorDialog;
            }

            protected override void DrawImg(Texture tex)
            {
                if (tex == null)
                    return;

                scene.view.SetValues(
                    shader,
                    Matrix.RotationZ(rotation)
                    * Matrix.Scaling(size, 1)
                    * Matrix.Translation(scrolledPosition)
                    );

                shader.content.SetValue("buttonTex", tex);
                shader.content.SetValue("rgba", dlg.rgba);
                shader.content.SetValue("buttonCol", renderColor);
                Sprite.DrawPositionTextured(scene.context.graphics, shader, "Button");
            }
        }
    }
}
