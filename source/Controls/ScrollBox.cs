using System;
using System.Linq;
using ChaosFramework.Components;
using ChaosFramework.Graphics.OpenGl;
using ChaosFramework.Graphics.OpenGl.AssetContainers;
using ChaosFramework.Input;
using ChaosFramework.Input.InputEvents;
using ChaosFramework.Math;
using ChaosFramework.Math.Vectors;
using static ChaosFramework.Math.Clamping;
using Keys = ChaosFramework.Input.Keyboard.HidUsage;

//TODO: Hide scrollbar together with scrollbox (notably for scriptboxes)

namespace ChaosFramework.Menu.OpenGl.Controls
{
    public class ScrollBox
        : LayoutContainer
        , ScrollControl
    {
        public const float SCROLL_BAR_HEIGHT = 0.05f;
        const float SCROLL_BAR_MARGIN = 0.1f;

        static void RemoveCustomScrollControls(Control[] controls)
        {
            foreach (Control ctrl in controls)
            {
                if (ctrl.parent)
                    ctrl.parent.children.Remove(ctrl);

                if (ctrl.scene)
                    ctrl.scene.components.Remove(ctrl);
            }
        }

        static bool IsLeftShiftDown(Keyboard k)
            => k[Keys.ShiftLeft].down;

        public Action offsetChanged;
        public float scrollPerStop = 0.1f;

        Slider scrollSliderHorizontal, scrollSliderVertical;

        public ShaderContainer.Entry shader;
        public TextureContainer.Entry texture;
        bool scrollUpdateOnCreateControl = true;

        float marginX = SCROLL_BAR_MARGIN, marginY = SCROLL_BAR_MARGIN;
        float maxScrollX, maxScrollY;

        public bool enableScrollX { set => marginX = value ? SCROLL_BAR_MARGIN : float.NaN; }
        public bool enableScrollY { set => marginY = value ? SCROLL_BAR_MARGIN : float.NaN; }

        public Vector2f scrollMargin
        {
            get => new(marginX, marginY);
            set
            {
                marginX = value.x;
                marginY = value.y;
                UpdateScroll();
            }
        }

        public Bounds2f baseArea
            => new(position - size + scrollMargin * 0.5f, position + size - scrollMargin * 0.5f);

        public bool hasScrollX => !float.IsNaN(maxScrollX);
        public bool hasScrollY => !float.IsNaN(maxScrollY);

        public string textureSource
        {
            set => texture = string.IsNullOrWhiteSpace(value)
                ? null
                : scene.context.texProvider.Load(value, this);
        }

        public override Vector2f size
        {
            get => base.size;
            set
            {
                base.size = value;
                if (scrollSliderHorizontal != null)
                {
                    float szY = scrollSliderHorizontal.size.y;
                    scrollSliderHorizontal.position = new Vector2f(position.x, bottom + szY / 2);
                    scrollSliderHorizontal.size = new Vector2f(size.x, szY);
                }
                if (scrollSliderVertical != null)
                {
                    float szY = scrollSliderVertical.size.y;
                    scrollSliderVertical.position = new Vector2f(right - szY / 2, position.y);
                    scrollSliderVertical.size = new Vector2f(size.y, szY);
                }
            }
        }

        public Vector2f offset { get; set; }

        Control[] customControlsXLo = [],
                  customControlsXHi = [],
                  customControlsYLo = [],
                  customControlsYHi = [];

        public Vector2f scroll => new(scrollX, scrollY);
        public float scrollX => scrollSliderHorizontal == null ? 0 : scrollSliderHorizontal.value;
        public float scrollY => scrollSliderVertical == null ? 0 : 1 - scrollSliderVertical.value;

        protected override void Create(CreateParameters cparams)
        {
            base.Create(cparams);
            texture = scene.context.texProvider.tabPage;
            shader = scene.context.graphics.shaders.spriteEffect;
        }

        public void SetCustomScrollControls(
            Control[] horizontalLow,
            Control[] horizontalHigh,
            Control[] verticalLow,
            Control[] verticalHigh
            )
        {
            customControlsXLo = horizontalLow ?? [];
            customControlsXHi = horizontalHigh ?? [];
            customControlsYLo = verticalLow ?? [];
            customControlsYHi = verticalHigh ?? [];

            RemoveCustomScrollControls(true);
            RemoveCustomScrollControls(false);
            UpdateScroll();
        }

        public void SetScrollX(float value)
        {
            if (scrollSliderHorizontal)
                scrollSliderHorizontal.value = value;
        }

        public void SetScrollY(float value)
        {
            if (scrollSliderVertical)
                scrollSliderVertical.value = 1 - value;
        }

        public void Scroll(float x = float.NaN, float y = float.NaN)
        {
            if (scrollSliderHorizontal != null && !float.IsNaN(x))
                scrollSliderHorizontal.value = Clamp(0, 1, x);

            if (scrollSliderVertical != null && !float.IsNaN(y))
                scrollSliderVertical.value = Clamp(0, 1, 1 - y);
        }

        public override Control CreateControl(Type type, Vector2f position, Vector2f size, string text = null)
        {
            Control newControl = base.CreateControl(type, position, size, text);
            if (scrollUpdateOnCreateControl)
                UpdateScroll();

            return newControl;
        }

        public void UpdateScroll()
        {
            scrollUpdateOnCreateControl = false;

            Bounds2f rect = new();
            foreach (Control c in EnumerateChildren<Control>(true, ShouldScrollChild))
                rect.Expand(c.bounds);

            maxScrollX = (float.IsNaN(marginX) || rect.size.x == 2 * size.x)
                       ? float.NaN
                       : (rect.right == float.MinValue
                       ? 0
                       : (rect.right - right));
            maxScrollY = (float.IsNaN(marginY) || rect.size.y == 2 * size.y)
                       ? float.NaN
                       : (rect.bottom == float.MaxValue
                       ? 0
                       : (-rect.bottom + bottom));

            RemoveCustomScrollControls(true);
            scrollSliderHorizontal?.Dispose();
            scrollSliderHorizontal = null;

            RemoveCustomScrollControls(false);
            scrollSliderVertical?.Dispose();
            scrollSliderVertical = null;

            bool needsHorizontalSlider = !(float.IsNaN(maxScrollX) || maxScrollX <= 0);
            bool needsVerticalSlider = !(float.IsNaN(maxScrollY) || maxScrollY <= 0);

            if (needsHorizontalSlider)
            {
                maxScrollX += marginX;
                float lo = left;
                float hi = right;
                if (needsVerticalSlider)
                    hi -= SCROLL_BAR_HEIGHT;

                Vector2f sliderPos = new((lo + hi) / 2, bottom + SCROLL_BAR_HEIGHT / 2);
                scrollSliderHorizontal = BuildSlider(true, sliderPos, (hi - lo) / 2);
                scrollSliderHorizontal.orientation = Slider.SliderOrientation.Horizontal;
                scrollSliderHorizontal.buttonOrientation = Slider.SliderOrientation.Horizontal;
                scrollSliderHorizontal.valueChanged = HandleHorizontalScroll;
                scrollSliderHorizontal.value = -offset.x / maxScrollX;
            }
            else
            {
                offset = new Vector2f(0, offset.y);
                maxScrollX = float.NaN;
            }

            if (needsVerticalSlider)
            {
                maxScrollY += marginY;
                float lo = bottom;
                float hi = top;
                if (needsHorizontalSlider)
                    lo += SCROLL_BAR_HEIGHT;

                Vector2f sliderPos = new(right - SCROLL_BAR_HEIGHT / 2, (lo + hi) / 2);
                scrollSliderVertical = BuildSlider(false, sliderPos, (hi - lo) / 2);
                scrollSliderVertical.orientation = Slider.SliderOrientation.Vertical;
                scrollSliderVertical.buttonOrientation = Slider.SliderOrientation.Horizontal;
                scrollSliderVertical.valueChanged = HandleVerticalScroll;
                scrollSliderVertical.value = 1 - offset.y / maxScrollY;
            }
            else
            {
                offset = new Vector2f(offset.x, 0);
                maxScrollY = float.NaN;
            }

            scrollUpdateOnCreateControl = true;
        }

        void HandleHorizontalScroll(Control slider)
        {
            offset = new Vector2f(-((Slider)slider).value * maxScrollX, offset.y);
            offsetChanged?.Invoke(this);
        }

        void HandleVerticalScroll(Control slider)
        {
            offset = new Vector2f(offset.x, (1 - ((Slider)slider).value) * maxScrollY);
            offsetChanged?.Invoke(this);
        }

        bool ShouldScrollChild(Control child)
            => child.scrollRelevant && child.scrollingParent == this;

        void RemoveCustomScrollControls(bool horizontal)
        {
            if (horizontal)
            {
                RemoveCustomScrollControls(customControlsXLo);
                RemoveCustomScrollControls(customControlsXHi);
            }
            else
            {
                RemoveCustomScrollControls(customControlsYLo);
                RemoveCustomScrollControls(customControlsYHi);
            }
        }

        Slider BuildSlider(bool horizontal, Vector2f sliderPos, float sliderLength)
        {
            Control ctrlParent = GetParent<Control>();

            RemoveCustomScrollControls(horizontal);

            Vector2f sldPos, sldSz;

            if (horizontal)
            {
                float customControlWidthLo = 0, customControlWidthHi = 0;

                float ctrlCursor = sliderPos.x - sliderLength;
                foreach (Control ctrl in customControlsXLo)
                {
                    AddControl(ctrl, ctrlParent);
                    ctrl.position = new Vector2f(ctrlCursor + ctrl.width / 2, sliderPos.y);
                    ctrlCursor += ctrl.width;
                    customControlWidthLo += ctrl.width;
                }

                ctrlCursor = sliderPos.x + sliderLength;
                for (int i = customControlsXHi.Length - 1; i >= 0; i--)
                {
                    Control ctrl = customControlsXHi[i];
                    AddControl(ctrl, ctrlParent);
                    ctrl.position = new Vector2f(ctrlCursor - ctrl.width / 2, sliderPos.y);
                    ctrlCursor -= ctrl.width;
                    customControlWidthHi += ctrl.height;
                }

                float lo = sliderPos.x - sliderLength + customControlWidthLo;
                float hi = sliderPos.x + sliderLength - customControlWidthHi;
                sldPos = new Vector2f((lo + hi) / 2, sliderPos.y);
                sldSz = new Vector2f((hi - lo) / 2, SCROLL_BAR_HEIGHT / 2);
            }
            else
            {
                float customControlWidthLo = 0, customControlWidthHi = 0;

                float ctrlCursor = sliderPos.y - sliderLength;
                for (int i = customControlsYLo.Length - 1; i >= 0; i--)
                {
                    Control ctrl = customControlsYLo[i];
                    AddControl(ctrl, ctrlParent);
                    ctrl.position = new Vector2f(sliderPos.x, ctrlCursor + ctrl.height / 2);
                    ctrlCursor += ctrl.height;
                    customControlWidthLo += ctrl.height;
                }

                ctrlCursor = sliderPos.y + sliderLength;
                foreach (Control ctrl in customControlsYHi)
                {
                    AddControl(ctrl, ctrlParent);
                    ctrl.position = new Vector2f(sliderPos.x, ctrlCursor - ctrl.height / 2);
                    ctrlCursor -= ctrl.height;
                    customControlWidthHi += ctrl.height;
                }

                float lo = sliderPos.y - sliderLength + customControlWidthLo;
                float hi = sliderPos.y + sliderLength - customControlWidthHi;
                sldPos = new Vector2f(sliderPos.x, (lo + hi) / 2);
                sldSz = new Vector2f((hi - lo) / 2, SCROLL_BAR_HEIGHT / 2);
            }

            Slider slider = ctrlParent == null
                ? ((DialogScene)scene).CreateControl<Slider>(sldPos, sldSz)
                : ctrlParent.CreateControl<Slider>(sldPos, sldSz);

            slider.instanceScrollRelevant = false;
            return slider;
        }

        void AddControl(Control ctrl, Control ctrlParent)
        {
            ctrl.instanceScrollRelevant = false;
            ctrl.parent = ctrlParent;
            if (ctrlParent == null)
                ((DialogScene)scene).components.Add(ctrl);
            else
                ctrlParent.children.Add(ctrl);
        }

        public override void UpdateInteraction()
        {
            base.UpdateInteraction();
            Control active = this;
            while (active != null)
                if ((active = active.activeControl) is ScrollBox)
                    return;
        }

        public override void SetUpdateCalls()
        {
            if (IsHoveredRecursively())
                scene.context.input.AddHandler<InputChangeEvent<Mouse.Wheel>, Mouse.Wheel, InputChange>(
                    scene.context.inputLayer,
                    HandleWheel
                    );

            base.SetUpdateCalls();
        }

        bool HandleWheel(InputChangeEvent<Mouse.Wheel> e)
        {
            switch (e.axis.dir)
            {
                case Mouse.WheelDirection.Scroll:
                    {
                        float scrollDelta = (e.data.newValue - e.data.oldValue) * scrollPerStop;
                        if ((scrollSliderVertical == null || scene.context.input.EnumerateDevices<Keyboard>().Any(IsLeftShiftDown))
                            && scrollSliderHorizontal != null
                            )
                        {
                            scrollSliderHorizontal.value = Clamp(0, 1, scrollSliderHorizontal.value - scrollDelta / maxScrollX);
                            return true;
                        }
                        else if (scrollSliderVertical != null)
                        {
                            scrollSliderVertical.value = 1 - Clamp(0, 1, (1 - scrollSliderVertical.value) - scrollDelta / maxScrollY);
                            return true;
                        }

                        goto default;
                    }

                default:
                    return false;
            }
        }

        public void ClearChildren()
        {
            foreach (Control c in children.OfType<Control>())
                if (c != scrollSliderHorizontal && c != scrollSliderVertical)
                    children.Remove(c);
        }

        public override void Draw()
        {
            if (texture == null)
                return;

            scene.view.SetValues(shader, Matrix.Scaling(size, 1) * Matrix.Translation(scrolledPosition));
            shader.SetValue(Sprite.textureHandle, texture);
            Sprite.DrawPositionTextured(scene.context.graphics, shader);
        }

        public override bool IsHovered()
            => (topMost || scrollingParent == null || ((Control)scrollingParent).IsHovered())
            && System.Math.Abs(scene.context.cursorPosition.x - scrolledPosition.x) < size.x
            && System.Math.Abs(scene.context.cursorPosition.y - scrolledPosition.y) < size.y;

        protected override void PerformLayoutInternal()
        {
            foreach (LayoutContainer child in children.OfType<LayoutContainer>())
            {
                child.size = size - new Vector2f(
                    float.IsNaN(scrollMargin.y) ? 0 : SCROLL_BAR_HEIGHT * 0.5f,
                    float.IsNaN(scrollMargin.x) ? 0 : SCROLL_BAR_HEIGHT * 0.5f
                    );
                child.position = position + new Vector2f(
                    float.IsNaN(scrollMargin.y) ? 0 : -SCROLL_BAR_HEIGHT * 0.5f,
                    float.IsNaN(scrollMargin.x) ? 0 : SCROLL_BAR_HEIGHT * 0.5f
                    );
            }

            UpdateScroll();
        }

        protected override void DoDispose()
        {
            base.DoDispose();
            scrollSliderVertical?.Dispose();
            scrollSliderHorizontal?.Dispose();
        }
    }
}
