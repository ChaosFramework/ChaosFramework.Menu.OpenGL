using ChaosFramework.Components;
using ChaosFramework.Graphics.OpenGl;
using ChaosFramework.Graphics.OpenGl.AssetContainers;
using ChaosFramework.Math;
using ChaosFramework.Math.Vectors;
using ChaosFramework.Platform;

namespace ChaosFramework.Menu.OpenGl
{
    public partial class LayoutContainer
    {
        public class MasterLayoutContainer
            : LayoutContainer
        {
            public TextureContainer.Entry texture;
            public bool fillContext = false;

            public virtual string textureSource
            {
                set
                {
                    texture = string.IsNullOrWhiteSpace(value)
                        ? null
                        : scene.context.texProvider.Load(value, this);
                }
            }

            protected override void Create(CreateParameters cparams)
            {
                base.Create(cparams);
                clipChildren = false;
            }

            protected override void PerformLayoutInternal()
            {
                if (fillContext)
                {
                    position = Vector2f.EMPTY;
                    size = new Vector2f(scene.context.window.Ratio(), 1);
                }
            }

            public override void SetUpdateCalls()
            {
                base.SetUpdateCalls();
                scene.updateLayers[(int)GuiScene.UpdateLayers.HudActions].Add(Update);
            }

            void Update()
                => Update(ftime);

            public override void Draw()
            {
                base.Draw();
                if (texture != null)
                {
                    ShaderContainer.Entry shader = scene.context.graphics.shaders.spriteEffect;
                    scene.view.SetValues(shader, Matrix.Scaling(size.x, size.y, 1) * Matrix.Translation(scrolledPosition));
                    shader.SetValue(Sprite.textureHandle, texture);
                    Sprite.DrawPositionTextured(scene.context.graphics, shader);
                }
            }
        }
    }
}
