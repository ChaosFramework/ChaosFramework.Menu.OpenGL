using ChaosFramework.Components;
using ChaosFramework.Graphics.OpenGl;
using ChaosFramework.Graphics.OpenGl.AssetContainers;
using ChaosFramework.Math;
using ChaosFramework.Math.Vectors;
using static ChaosFramework.Math.Clamping;
using static ChaosFramework.Math.Constants;

namespace ChaosFramework.Menu.OpenGl.Controls
{
    public class Slider
        : Control
    {
        public enum SliderOrientation
        {
            Horizontal,
            Vertical
        }

        SliderOrientation _orientation = SliderOrientation.Horizontal;
        public SliderOrientation orientation
        {
            get => _orientation;
            set
            {
                _orientation = value;
                UpdateSliderButton();
            }
        }

        SliderOrientation _buttonOrientation = SliderOrientation.Vertical;
        public SliderOrientation buttonOrientation
        {
            get => _buttonOrientation;
            set
            {
                _buttonOrientation = value;
                UpdateSliderButton();
            }
        }

        protected Vector2f axis
            => orientation == SliderOrientation.Vertical ? new Vector2f(0, 1) : new Vector2f(1, 0);

        public Action valueChanged;
        public Action valueChangedCompleted;
        public bool drawLine = true;
        public bool forceInvoke = false;

        protected Button btnSlide;
        ShaderContainer.Entry shader;
        TextureContainer.Entry texture;

        bool clicked = false;
        bool preMouseDown = false;

        public float sliderButtonRatio => btnSlide.texture.content.args.ratio;
        float _value = 1;
        public float value
        {
            get => _value;
            set
            {
                value = Clamp(0, 1, value);
                bool invokeValueChanged = forceInvoke || (_value != value);
                forceInvoke = false;
                _value = value;
                UpdateSliderButton();
                if (invokeValueChanged && valueChanged != null)
                    valueChanged(this);
            }
        }

        public override Vector2f position
        {
            get => base.position;
            set
            {
                base.position = value;
                UpdateSliderButton();
            }
        }

        public override Vector2f size
        {
            get => base.size;
            set
            {
                base.size = value;
                UpdateSliderButton();
            }
        }

        protected override void Create(CreateParameters cparams)
        {
            base.Create(cparams);
            btnSlide = AddComponent<Button>();
            btnSlide.texture = scene.context.texProvider.sliderButton;
            btnSlide.enabledFunction = Enabled;
            texture = scene.context.texProvider.slider;
            shader = scene.context.graphics.shaders.spriteEffect;
            clipChildren = false;
        }

        bool Enabled() => enabledFunction();

        void UpdateSliderButton()
        {
            if (btnSlide == null)
                return;

            switch (orientation)
            {
                case SliderOrientation.Horizontal:
                    switch (buttonOrientation)
                    {
                        case SliderOrientation.Horizontal:
                            btnSlide.rotation = PI_HALF;
                            btnSlide.size = new Vector2f(size.y / sliderButtonRatio, size.y);
                            break;

                        case SliderOrientation.Vertical:
                            btnSlide.rotation = 0;
                            btnSlide.size = new Vector2f(size.y * sliderButtonRatio, size.y);
                            break;
                    }
                    break;

                case SliderOrientation.Vertical:
                    switch (buttonOrientation)
                    {
                        case SliderOrientation.Horizontal:
                            btnSlide.rotation = 0;
                            btnSlide.size = new Vector2f(size.y, size.y / sliderButtonRatio);
                            break;

                        case SliderOrientation.Vertical:
                            btnSlide.rotation = PI_HALF;
                            btnSlide.size = new Vector2f(size.y, size.y * sliderButtonRatio);
                            break;
                    }
                    break;
            }

            float sz = orientation == SliderOrientation.Vertical ? btnSlide.size.y : btnSlide.size.x;
            float low = -size.x + sz;
            float high = +size.x - sz;
            btnSlide.position = position + (low + (high - low) * value) * axis;
        }

        public override bool IsHovered()
            => btnSlide.IsHovered();

        public override void UpdateInteraction()
        {
            if (!clicked && !IsHovered())
                return;

            clicked = false;
            base.UpdateInteraction();

            if (scene.mouseDown[0] > 0)
            {
                float sx = size.x - btnSlide.size.x / 2;
                float f = Vector2f.Dot(scene.context.cursorPosition - scrolledPosition, axis) / sx;
                f = Clamp(-1, 1, f);
                scene.context.cursorPosition = scrolledPosition + f * axis * sx;
                value = Clamp(0, 1, (f + 1) / 2);
                clicked = true;
            }
            else if (preMouseDown && valueChangedCompleted != null)
                valueChangedCompleted(this);

            preMouseDown = scene.mouseDown[0] > 0;
        }

        public override void SetUpdateCalls()
        {
            scene.updateLayers[(int)GuiScene.UpdateLayers.HudActions].Add(UpdateSliderButton);
            scene.updateLayers[(int)GuiScene.UpdateLayers.MoveCursor].Add(MoveCursor);
            base.SetUpdateCalls();
        }

        void MoveCursor()
        {
            if (clicked && scene.mouseDown[0] > 0)
            {
                float sx = size.x - btnSlide.size.x / 2;
                float f = Vector2f.Dot(scene.context.cursorPosition - scrolledPosition, axis) / sx;
                f = Clamp(-1, 1, f);
                scene.context.cursorPosition = scrolledPosition + f * axis * sx;
            }
        }

        public override void Draw()
        {
            if (!drawLine)
                return;

            Matrix transform = Matrix.IDENTITY;
            transform.m00 = axis.x;
            transform.m01 = axis.y;
            transform.m10 = -axis.y;
            transform.m11 = axis.x;

            float sz = orientation == SliderOrientation.Vertical ? btnSlide.size.y : btnSlide.size.x;
            scene.view.SetValues(
                shader,
                Matrix.Scaling(size.x - sz, size.y, 1)
                * transform
                * Matrix.Translation(scrolledPosition)
                );

            shader.SetValue(Sprite.textureHandle, texture);
            shader.BeginPass("Sprite");
            Sprite.DrawPositionTextured(scene.context.graphics);
            shader.EndPass();
        }
    }
}
