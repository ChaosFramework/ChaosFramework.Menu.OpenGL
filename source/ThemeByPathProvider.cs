using System;
using ChaosFramework.Core;
using ChaosFramework.Graphics.OpenGl.AssetContainers;
using Tex = ChaosFramework.Graphics.OpenGl.AssetContainers.TextureContainer.Entry;

namespace ChaosFramework.Menu.OpenGl
{
    using Graphics = Graphics.OpenGl.Graphics;

    public class ThemeByPathProvider : TextureProvider
    {
        readonly string themePath;

        public ThemeByPathProvider(Graphics graphics, string themePath, TextureContainer container)
            : base(graphics, container)
        {
            ArgumentNullException.ThrowIfNull(container);
            this.themePath = themePath;
        }

        public override Tex Load(string source, Disposable monitor1, params Disposable[] monitors)
            => @override.TryLoad($"{themePath}/{source}", out Tex texture, monitor1, monitors)
                ? texture
                : base.Load(source, monitor1, monitors);
    }
}
