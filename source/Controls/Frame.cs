using ChaosFramework.Components;
using ChaosFramework.Graphics;
using ChaosFramework.Graphics.OpenGl;
using ChaosFramework.Math;
using OpenTK.Graphics.OpenGL;
using static ChaosFramework.Graphics.OpenGl.Graphics;

namespace ChaosFramework.Menu.OpenGl.Controls
{
    public class Frame
        : Image
    {
        public float borderSize = 0.02f;

        protected override void Create(CreateParameters cparams)
        {
            base.Create(cparams);
            shader = scene.context.shaders.frame;
            texture = scene.context.texProvider.frame;
            anchor = Align.Bottom | Align.Top | Align.Left | Align.Right;
        }

        public override void Draw()
        {
            shader.content.SetValue("tex", texture);
            shader.content.SetValue("scale", size);
            scene.view.SetValues(shader, Matrix.Translation(scrolledPosition));
            shader.content.SetValue("thickness", borderSize);
            shader.content.BeginPass("Frame");
            GL.BindVertexArray(scene.context.graphics.emptyVAO);
            ThrowErrors();
            GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 10);
            ThrowErrors();
            shader.content.EndPass();
        }

        public override bool IsHovered()
            => false;
    }
}
