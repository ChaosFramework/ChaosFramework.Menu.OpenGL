using ChaosFramework.Components;
using ChaosFramework.Graphics;
using ChaosFramework.Graphics.OpenGl;
using ChaosFramework.Graphics.OpenGl.AssetContainers;
using ChaosFramework.Math;
using static ChaosFramework.Math.Signs;

namespace ChaosFramework.Menu.OpenGl.Controls
{
    public class Image
        : Control
    {
        public float rotation = 0;

        protected ShaderContainer.Entry shader;

        TextureContainer.Entry _texture;
        public TextureContainer.Entry texture
        {
            get => _texture;
            set => ratio = ((_texture = value) == null || value.content == null) ? float.NaN : _texture.content.args.ratio;
        }

        public virtual string textureSource
        {
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    texture = null;
                else
                    texture = scene.context.texProvider.Load(value, this);
            }
        }

        protected override void Create(CreateParameters cparams)
        {
            base.Create(cparams);
            textAlign = Align.Center;
            shader = scene.context.graphics.shaders.spriteEffect;
            texture = scene.context.texProvider.button;
        }

        protected virtual void DrawImg(Texture tex)
        {
            if (tex == null)
                return;

            scene.view.SetValues(
                shader,
                Matrix.RotationZ(rotation)
                * Matrix.Scaling(size, 1)
                * Matrix.Translation(scrolledPosition)
                );

            shader.SetValue(Sprite.textureHandle, tex);
            Sprite.DrawPositionTextured(scene.context.graphics, shader, "Sprite");
        }

        public override void Draw()
        {
            DrawImg(texture);
            DrawText();
        }

        public override bool IsHovered()
            => (topMost || scrollingParent == null || ((Control)scrollingParent).IsHovered())
            && Abs(scene.context.cursorPosition.x - scrolledPosition.x) < size.x
            && Abs(scene.context.cursorPosition.y - scrolledPosition.y) < size.y;
    }
}
