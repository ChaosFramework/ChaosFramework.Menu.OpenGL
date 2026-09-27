using ChaosFramework.Components;
using ChaosFramework.Core;
using ChaosFramework.Graphics.OpenGl.AssetContainers;
using ChaosFramework.Input;
using ChaosFramework.Math.Vectors;
using ChaosFramework.Platform;
using ChaosUtil.Debug;
using static ChaosFramework.Math.Clamping;

namespace ChaosFramework.Menu.OpenGl
{
    public class MenuContext : Disposable
    {
        float _ratio = float.NaN;
        public float ratio
        {
            get => float.IsNaN(_ratio) ? window.Ratio() : _ratio;
            set => _ratio = value;
        }

        public Collections.LinkedList<PresentationContext> presentationContexts = [];

        public LanguagePack language;
        public readonly FontContainer.Entry font;

        public readonly Graphics.OpenGl.Graphics graphics;
        public readonly TextureProvider texProvider;
        public readonly PresentationContext window;

        public readonly BaseGame game;
        public readonly InputContext input;

        public readonly System.Enum inputLayer;

        public readonly ShaderProvider shaders;

        public MenuContext(
            BaseGame game,
            Graphics.OpenGl.Graphics graphics,
            PresentationContext window,
            InputContext input,
            System.Enum inputLayer,
            LanguagePack language,
            FontContainer.Entry font,
            TextureProvider texProvider = null
            )
        {
            this.inputLayer = inputLayer;
            this.game = game;
            this.language = language;
            this.graphics = graphics;
            this.texProvider = texProvider ?? new TextureProvider(graphics);
            this.window = window;
            this.input = input;
            this.font = font;

            shaders = new ShaderProvider(this);
            input.UpdateDeviceList();
            keyboardLayout = KeyboardLayout.GetLayout(System.Globalization.CultureInfo.InstalledUICulture.KeyboardLayoutId);
            presentationContexts.Add(window);
        }

        public Vector2f cursorPosition;
        public KeyboardLayouts keyboardLayout = KeyboardLayouts.QWERTZ_GER;

        public void Update(Time _)
        {
            MeasurementLog.StartMeasure("Update MenuContext");
            {
                const float MOUSE_SENSITIVITY = 0.1f / 60;
                foreach (Mouse mouse in input.EnumerateDevices<Mouse>())
                {
                    cursorPosition.x += (mouse.x.value - mouse.x.oldValue) * MOUSE_SENSITIVITY;
                    cursorPosition.y -= (mouse.y.value - mouse.y.oldValue) * MOUSE_SENSITIVITY;
                }
                cursorPosition.x = Clamp(-ratio, ratio, cursorPosition.x);
                cursorPosition.y = Clamp(-1, 1, cursorPosition.y);
            }
            MeasurementLog.EndMeasure();
        }

        protected override void DoDispose()
        {
            base.DoDispose();
            shaders.Dispose();
        }
    }
}
