using System.Xml;
using ChaosFramework.Components;
using ChaosFramework.Graphics.OpenGl;
using ChaosFramework.Graphics.OpenGl.AssetContainers;
using ChaosFramework.Math;
using static ChaosFramework.Math.Clamping;

namespace ChaosFramework.Menu.OpenGl.Controls
{
    public partial class TabPage : Layout.Box, ControlNode
    {
        public delegate void BuildAction(TabPage @this);

        public Action onOpen;

        public object value;
        public TabPageButton btnOpen;
        public XmlNode myNode { get; set; }

        TextureContainer.Entry texBackground, texBtnOpenClosed, texBtnOpenOpened;
        ShaderContainer.Entry shader => scene.context.graphics.shaders.spriteEffect;

        public string textureSource
        {
            set => texBackground = string.IsNullOrWhiteSpace(value)
                ? null
                : scene.context.texProvider.Load(value, this);
        }

        public override string text
        {
            get => ((TabPageContainer)parent).displayString(value);
            set => this.value = value;
        }

        public override bool scrollRelevant => false;

        protected override void Create(CreateParameters cparams)
        {
            base.Create(cparams);
            texBackground = scene.context.texProvider.tabPage;
            texBtnOpenClosed = scene.context.texProvider.tabPageButton;
            texBtnOpenOpened = scene.context.texProvider.tabPageButtonActive;

            if (parent is TabPageContainer parentContainer)
            {
                btnOpen = parentContainer.btnOpenContainer.AddComponent<TabPageButton>();
                btnOpen.texture = texBtnOpenClosed;
                btnOpen.mouseDownLeft = Open;
                btnOpen.textMargin.horizontal = 0.1f;
            }

            doUpdate = doDraw = false;
        }

        public void DelayedOpen(BuildAction build)
        {
            void buildPage(Control _)
            {
                build(this);
                onOpen -= buildPage;
            }

            onOpen += buildPage;
        }

        public void Open(Control sender = null)
        {
            foreach (TabPageContainer.Line l in ((TabPageContainer)parent).rows)
                foreach (TabPage page in l.pages)
                {
                    page.doUpdate = page.doDraw = false;
                    page.btnOpen.texture = texBtnOpenClosed;
                }

            btnOpen.texture = texBtnOpenOpened;

            int targetIndex = btnOpen.line.pages.IndexOf(this);
            btnOpen.line.scroll = Clamp(
                Max(0, targetIndex - (btnOpen.line.pages.length - btnOpen.line.maxScroll) + 1),
                targetIndex,
                btnOpen.line.scroll
                );

            doUpdate = doDraw = true;
            SetUpdateCalls();
            onOpen?.Invoke(this);
        }

        public override void SetUpdateCalls()
        {
            base.SetUpdateCalls();
            scene.updateLayers[(int)GuiScene.UpdateLayers.HudActions].Add(UpdateText);
        }

        void UpdateText()
            => btnOpen.text = text;

        public override void Draw()
        {
            if (texBackground != null)
            {
                scene.view.SetValues(shader, Matrix.Scaling(size, 1) * Matrix.Translation(scrolledPosition));
                shader.SetValue(Sprite.textureHandle, texBackground);
                shader.BeginPass("Sprite");
                Sprite.DrawPositionTextured(scene.context.graphics);
                shader.EndPass();
            }
        }

        public override bool IsHovered()
            => false;

        void ControlNode.AddControl(Control child) { }

        void ControlNode.AddChild(ControlNode child) { }

        public override string ToString() => $"{nameof(TabPage)}[{text}]";

        protected override void DoDispose()
        {
            base.DoDispose();
            (parent as TabPageContainer)?.KillPage(this);
        }
    }
}
