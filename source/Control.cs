using System;
using ChaosFramework.Components;
using ChaosFramework.Math;
using ChaosFramework.Math.Vectors;
using ChaosUtil.Reflection;
using OpenTK.Graphics.OpenGL;
using static ChaosFramework.Graphics.OpenGl.Graphics;
using static ChaosFramework.Math.Clamping;

namespace ChaosFramework.Menu.OpenGl
{
    [AssemblyManager.ListSubTypes]
    public abstract partial class Control
        : Component<GuiScene>
    {
        public delegate void ClipRegion();

        public bool moveChildren = true;
        public bool clipChildren = true;
        public virtual Bounds2f clipRect
            => new(scrolledPosition - size, scrolledPosition + size);

        protected bool topMost = false;

        protected override void Create(CreateParameters args)
        {
            font = scene.context.font;
            fontShader = scene.context.graphics.shaders.text;
            UpdateHoverPriority();

            if (args is Controls.ControlCreateParameters creationParams)
            {
                position = creationParams.position;
                size = creationParams.size;
                text = creationParams.text;
                if (text != null)
                    UpdateTextGeometry();
            }

            SetUpdateCalls();
            textColor = GetParentTextColor();
        }

        public T CreateControl<T>(Vector2f pos, Vector2f sz, string text = null)
            where T : Control
            => (T)CreateControl(typeof(T), pos, sz, text);

        public virtual Control CreateControl(Type type, Vector2f position, Vector2f size, string text = null)
        {
            Control control = (Control)AddComponent(type, new Controls.ControlCreateParameters(position, size, text));
            control.UpdateTextGeometry();
            return control;
        }

        public override sealed void SetDrawCalls()
        {
            if (topMost)
                scene.drawLayers[(int)GuiScene.DrawLayers.TopMost].Add(DrawControl);
            else if (parent == null)
                scene.drawLayers[(int)GuiScene.DrawLayers.Default].Add(DrawControl);

            base.SetDrawCalls();
        }

        void DrawControl()
        {
            Draw();
            if (clipChildren)
            {
                Bounds2i sceneRect = scene.TransformToClipRect(clipRect);
                Bounds2i scissorRect = sceneRect.Intersect(scene.clipRects.last);
                scene.clipRects.Add(scissorRect);
                GL.Scissor(scissorRect.low.x, scissorRect.low.y, Max(0, scissorRect.width), Max(0, scissorRect.height));
                ThrowErrors();
            }

            foreach (Component child in children)
                if (child.doDraw && child is Control tmp && !tmp.topMost)
                    tmp.DrawControl();

            if (clipChildren)
            {
                scene.clipRects.RemoveAt(scene.clipRects.length - 1);
                Bounds2i a = scene.clipRects.last;
                GL.Scissor(a.low.x, a.low.y, Max(0, a.width), Max(0, a.height));
                ThrowErrors();
            }
        }

        public abstract void Draw();
    }
}
