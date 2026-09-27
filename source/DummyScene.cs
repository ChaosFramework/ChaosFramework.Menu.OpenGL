using ChaosFramework.Components;
using ChaosFramework.Graphics.OpenGl;
using ChaosFramework.Graphics.OpenGl.AssetContainers;
using ChaosFramework.Math.Vectors;
using OpenTK.Graphics.OpenGL;
using static ChaosFramework.Graphics.OpenGl.Graphics;
using SysCol = System.Collections.Generic;

namespace ChaosFramework.Menu.OpenGl
{
    sealed class DummyScene(MenuContext context, BaseGame game, SysCol.IEnumerable<Scene> source)
        : Scene(game)
    {
        public readonly MenuContext context = context;
        internal readonly SysCol.IEnumerable<Scene> source = source;

        Texture texture;
        Framebuffer framebuffer;

        public override void SetDrawCalls()
        {
            base.SetDrawCalls();
            drawLayers[0].Add(Render);
        }

        void Render()
        {
            if (texture == null)
            {
                texture = new Texture(
                    context.graphics.dispatcher,
                    new Texture.Parameters(
                        (int)context.window.width,
                        (int)context.window.height,
                        pixelType: PixelType.UnsignedByte,
                        internalFormat: PixelInternalFormat.Rgba8,
                        pixelFormat: PixelFormat.Rgba
                        )
                    );
                framebuffer = new Framebuffer(texture.args.width, texture.args.height, [texture]);
                Framebuffer old = context.graphics.stateTracker.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer);
                GL.ClearColor(0, 0, 0, 0);
                ThrowErrors();
                GL.Clear(ClearBufferMask.ColorBufferBit);
                ThrowErrors();
                foreach (Scene s in source)
                    s.Draw();
                context.graphics.stateTracker.BindFramebuffer(FramebufferTarget.Framebuffer, old);
            }

            context.shaders.darken.SetValue("color", new Vector4f(0.5f, 0.5f, 0.5f, 1));
            context.shaders.darken.SetValue("tex", texture);
            Sprite.DrawPositionTextured(context.graphics, context.shaders.darken, "Screen");
        }

        protected override void DoDispose()
        {
            base.DoDispose();
            texture?.Dispose();
            framebuffer?.Dispose();
        }
    }
}
