using ChaosFramework.Core;
using ChaosFramework.Graphics.OpenGl;
using ChaosFramework.Graphics.OpenGl.AssetContainers;
using ChaosFramework.IO.Streams;
using ChaosFramework.IO.Streams.Sources;

namespace ChaosFramework.Menu.OpenGl
{
    public class ShaderProvider : Disposable
    {
        public static readonly StreamSource shaderResources
            = new PrefixedResourceStreamSource("FX_", null, Properties.Resources.ResourceManager);

        public readonly ShaderContainer.Entry button, frame, grid, darken, colorDialog;

        readonly MenuContext context;
        readonly ShaderCodeContainer shaderCode;
        readonly ShaderContainer shaders;

        public ShaderProvider(MenuContext context)
        {
            this.context = context;

            // TODO: Even more heavily (and even more carefully) consider, why we need another one of those.
            shaderCode = new ShaderCodeContainer(new StreamSourceCollection(StreamSources.shaderCode));
            shaders = new ShaderContainer(shaderResources, context.graphics, shaderCode);

            frame = shaders.Load("Frame", this);
            button = shaders.Load("Button", this);
            grid = shaders.Load("Grid", this);
            darken = shaders.Load("Darken", this);
            colorDialog = shaders.Load("ColorDialog", this);
        }

        protected override void DoDispose()
        {
            base.DoDispose();
            shaders.Dispose();
            shaderCode.Dispose();
        }
    }
}
