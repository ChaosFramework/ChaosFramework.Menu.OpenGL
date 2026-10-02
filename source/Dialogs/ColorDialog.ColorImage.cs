using ChaosFramework.Components;
using ChaosFramework.Graphics.OpenGl;
using ChaosFramework.Math;

namespace ChaosFramework.Menu.OpenGl.Dialogs
{
    using Controls;

    public partial class ColorDialog
    {
        public class ColorImage : Image
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

                shader.content.SetValue("invScreenHeight", 1.0f / scene.context.window.height);
                shader.content.SetValue("rgba", dlg.rgba);
                Sprite.DrawPositionTextured(scene.context.graphics, shader, "Preview");
            }
        }
    }
}
