using System;
using ChaosFramework.Collections;
using ChaosFramework.Components;
using ChaosFramework.Graphics;
using ChaosFramework.Graphics.Colors;
using ChaosFramework.Graphics.OpenGl;
using ChaosFramework.Graphics.OpenGl.AssetContainers;
using ChaosFramework.Input;
using ChaosFramework.Input.InputEvents;
using ChaosFramework.Math;
using ChaosFramework.Math.Vectors;
using ChaosFramework.Platform;
using OpenTK.Graphics.OpenGL;
using static ChaosFramework.Graphics.OpenGl.Graphics;
using static ChaosFramework.Math.Clamping;

namespace ChaosFramework.Menu.OpenGl
{
    using Controls;

    public abstract class GuiScene : Scene
    {
        public enum DrawLayers
        {
            Setup,
            Default,
            TopMost,
            TearDown
        }

        public enum UpdateLayers
        {
            Prepare,
            MoveCursor,
            SelectHoverControl,
            Interactions,
            HudActions,
            VisualUpdates
        }

        public const float CURSOR_SZ = 0.05f;
        const float DOUBLE_CLICK_TIME = 0.3f;

        public static readonly Bounds2i FULL_SCREEN_SCISSOR = new(
            (int)(-(1L << 30)),      // Note:
            (int)(-(1L << 30)),      //  NVidia cards don't like 32bit ints.
            (int)(+(1L << 30) - 1L), //  So here you've got a 31bit int.
            (int)(+(1L << 30) - 1L)  //  Because NVidia...
        );

        public readonly MenuContext context;

        public bool hideCursor = false;

        public Control hoveringControl, activeControl;
        public Camera view;
        public TextureContainer.Entry cursorTex, cursorCol;
        public Rgba textColor = Rgba.OPAQUE_BLACK;

        protected internal int[] mouseDown = new int[3];
        protected internal int[] oldMouseDown = new int[3];
        internal LinkedList<Bounds2i> clipRects = [];

        Control previousHoveringControl;
        bool didDepthTestBefore;
        bool scissoredBefore;

        float lastMouseClick = float.MinValue;
        int numClickRepetitions = 0;

        Graphics.OpenGl.ChaosShader.Shader cursorFx => context.graphics.shaders.spriteEffect;

        public GuiScene(MenuContext context)
            : base(context.game, typeof(UpdateLayers), typeof(DrawLayers))
        {
            this.context = context;
            cursorTex = context.texProvider.cursor;
            view = new Camera();
            view.Update(
                pos: new Vector3f(0, 0, -1),
                dir: new Vector3f(0, 0, 1),
                up: new Vector3f(0, 1, 0),
                nearClip: 0.05f,
                farClip: 100,
                screenRatio: context.window.Ratio()
                );
        }

        public int IsMouseDown(int mouseButton) => mouseDown[mouseButton];
        public bool WasMouseClicked(int button) => mouseDown[button] > 0 && oldMouseDown[button] <= 0;
        public bool WasMouseReleased(int button) => mouseDown[button] <= 0 && oldMouseDown[button] > 0;

        void PrepareUpdate()
        {
            for (int i = 0; i < mouseDown.Length; i++)
                mouseDown[i] = 0;

            foreach (Mouse mouse in context.input.EnumerateDevices<Mouse>())
                for (int i = 0; i < mouseDown.Length; i++)
                    if (mouse.buttons[i].value > 0.5f)
                        mouseDown[i]++;

            Vector4i newViewport = new(context.window.position, context.window.width, context.window.height);
            if (newViewport != view.viewPort)
            {
                view.viewPort = newViewport;
                view.Update();
                foreach (Component c in components)
                    (c as LayoutContainer)?.Layout();
            }

            previousHoveringControl = hoveringControl;
            hoveringControl = null;
        }

        void UpdateInteractions()
        {
            if (hoveringControl != previousHoveringControl)
            {
                if (previousHoveringControl != null && previousHoveringControl.mouseLeave != null)
                    previousHoveringControl.mouseLeave(previousHoveringControl);
                if (hoveringControl != null && hoveringControl.mouseEnter != null)
                    hoveringControl.mouseEnter(hoveringControl);
            }

            if (activeControl != null && activeControl.enabledFunction())
                activeControl.UpdateInteraction();
        }

        internal Bounds2i TransformToClipRect(Bounds2f rect)
        {
            Vector2i pos = context.window.position;
            int w = (int)context.window.width;
            int h = (int)context.window.height;
            float ratio = context.window.Ratio();

            return new(
                (int)(0.5 * rect.low.x * w / ratio) + w / 2 + pos.x,
                (int)(0.5 * rect.low.y * h) + h / 2 + pos.y,
                (int)(0.5 * rect.high.x * w / ratio) + w / 2 + pos.x,
                (int)(0.5 * rect.high.y * h) + h / 2 + pos.y
                );
        }

        public override void SetDrawCalls()
        {
            base.SetDrawCalls();
            drawLayers[(int)DrawLayers.Setup].Add(Setup);
            drawLayers[(int)DrawLayers.TearDown].Add(TearDown);
        }

        void Setup()
        {
            GL.GetBoolean(GetPName.DepthTest, out didDepthTestBefore);
            ThrowErrors();
            GL.GetBoolean(GetPName.ScissorTest, out scissoredBefore);
            ThrowErrors();

            if (didDepthTestBefore)
            {
                GL.Disable(EnableCap.DepthTest);
                ThrowErrors();
            }

            if (!scissoredBefore)
            {
                GL.Enable(EnableCap.ScissorTest);
                ThrowErrors();
            }

            clipRects.Add(FULL_SCREEN_SCISSOR);
            GL.Viewport(0, 0, (int)this.context.window.width, (int)this.context.window.height);
        }

        void TearDown()
        {
            if (doUpdate && !hideCursor)
            {
                view.SetValues(cursorFx, Matrix.Scaling(CURSOR_SZ, CURSOR_SZ, 1) * Matrix.Translation(context.cursorPosition));
                cursorFx.SetValue(Sprite.textureHandle, cursorTex);
                Sprite.DrawPositionTextured(context.graphics, cursorFx);
            }
            if (didDepthTestBefore)
            {
                GL.Enable(EnableCap.DepthTest);
                ThrowErrors();
            }
            if (!scissoredBefore)
            {
                GL.Disable(EnableCap.ScissorTest);
                ThrowErrors();
            }
            if (clipRects.length != 1)
                throw new InvalidOperationException("Did you know that clip rects are a stack?");
            clipRects.Clear();
        }

        public T CreateControl<T>(Vector2f position, Vector2f size, string text = null)
            where T : Control
            => (T)CreateControl(typeof(T), position, size, text);

        public Control CreateControl(Type type, Vector2f position, Vector2f size, string text = null)
        {
            Control control = (Control)AddComponent(type, new ControlCreateParameters(position, size, text));
            control.UpdateTextGeometry();
            return control;
        }

        public override void SetUpdateCalls()
        {
            base.SetUpdateCalls();
            updateLayers[(int)UpdateLayers.Prepare].Add(PrepareUpdate);
            updateLayers[(int)UpdateLayers.Interactions].Add(UpdateInteractions);
            activeControl?.SetInputHandlers();
            context.input.AddHandler<InputPushEvent<Mouse.Button>, Mouse.Button, InputChange>(context.inputLayer, HandleMouseDown);
            context.input.AddHandler<InputReleaseEvent<Mouse.Button>, Mouse.Button, InputChange>(context.inputLayer, HandleMouseRelease);
        }

        bool HandleMouseDown(InputPushEvent<Mouse.Button> e)
        {
            switch (e.axis.button)
            {
                case Mouse.ButtonSemantic.Left:
                    mouseDown[0]++;
                    UpdateFocus();
                    hoveringControl?.MouseDown(0);
                    if (game.time.realTotalTime - lastMouseClick < DOUBLE_CLICK_TIME)
                    {
                        numClickRepetitions++;
                        hoveringControl?.GetMultiClick(numClickRepetitions + 1)?.Invoke(hoveringControl);
                    }
                    else
                        numClickRepetitions = 0;
                    lastMouseClick = game.time.realTotalTime;

                    return hoveringControl != null;

                case Mouse.ButtonSemantic.Right:
                    mouseDown[1]++;
                    UpdateFocus();
                    hoveringControl?.MouseDown(1);
                    return hoveringControl != null;

                case Mouse.ButtonSemantic.Middle:
                    mouseDown[2]++;
                    UpdateFocus();
                    hoveringControl?.MouseDown(2);
                    return hoveringControl != null;

                default:
                    return false;
            }
        }

        bool HandleMouseRelease(InputReleaseEvent<Mouse.Button> e)
        {
            switch (e.axis.button)
            {
                case Mouse.ButtonSemantic.Left:
                case Mouse.ButtonSemantic.Right:
                case Mouse.ButtonSemantic.Middle:
                    int param = e.axis.button - Mouse.ButtonSemantic.Left;
                    mouseDown[param] = Max(mouseDown[param] - 1, 0);
                    return hoveringControl != null;

                default:
                    return false;
            }
        }

        void UpdateFocus()
        {
            Control lastActive = activeControl;
            if (lastActive != null)
                while (lastActive.activeControl != null)
                    lastActive = lastActive.activeControl;

            if (hoveringControl == null)
                activeControl = null;
            else
            {
                Control parentControl = hoveringControl.parent as Control;
                Control currentControl = hoveringControl;
                while (parentControl != null)
                {
                    parentControl.activeControl = currentControl;
                    currentControl = parentControl;
                    parentControl = parentControl.parent as Control;
                }
                activeControl = currentControl;
            }

            if (lastActive != null && hoveringControl != lastActive && lastActive.lostFocus != null)
                lastActive.lostFocus(lastActive);
        }
    }
}
