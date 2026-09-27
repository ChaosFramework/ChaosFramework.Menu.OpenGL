using ChaosFramework.Components;
using ChaosFramework.Graphics;
using ChaosFramework.Graphics.Colors;
using ChaosFramework.Graphics.OpenGl.AssetContainers;

namespace ChaosFramework.Menu.OpenGl.Controls
{
    public class Button : Image
    {
        protected Rgba renderColor { get; private set; }

        public string hoverText;
        public string hoverTextSource
        {
            set
            {
                string prevText = hoverText;
                hoverText = scene.context.language.GetText(value);
                if (prevText != hoverText && IsHovered())
                    UpdateTextGeometry();
            }
        }

        TextureContainer.Entry _hoverTexture;
        public TextureContainer.Entry hoverTexture => _hoverTexture ?? texture;
        public string hoverTextureSource { set => scene.context.texProvider.TryLoad(value, out _hoverTexture, this); }

        bool enabled { get; set; } = true;
        bool isEnabled => enabled && (enabledFunction == null || enabledFunction());

        protected override void Create(CreateParameters cparams)
        {
            base.Create(cparams);
            getText = GetText;
            texture = scene.context.texProvider.button;
            shader = scene.context.shaders.button;
            textAlign = Align.Center;
        }

        string GetText(object _)
            => IsHovered() ? hoverText ?? text : text;

        public override void SetUpdateCalls()
        {
            scene.updateLayers[(int)GuiScene.UpdateLayers.HudActions].Add(Update);
            base.SetUpdateCalls();
        }

        void Update()
        {
            bool mouseDownOnControl = false;
            for (int i = 0; i < base.mouseDownOnControl.Length; i++)
                mouseDownOnControl |= base.mouseDownOnControl[i];

            renderColor = isEnabled
                        ? (scene.hoveringControl == this
                        ? (mouseDownOnControl
                        ? new Rgba(0.25f, 0.25f, 0.25f, 1)
                        : new Rgba(0.5f, 0.5f, 0.5f, 1))
                        : Rgba.OPAQUE_WHITE)
                        : new Rgba(0.3f, 0.3f, 0.3f, 1)
                        ;
        }

        public override void Draw()
        {
            PrepareDraw();
            DrawImg(visuallyHovered ? hoverTexture : texture);
            DrawText();
        }

        protected virtual void PrepareDraw()
            => shader.SetValue("color", renderColor);

        public override bool IsHovered()
            => isEnabled && base.IsHovered();
    }
}
