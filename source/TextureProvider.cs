using System;
using System.IO;
using System.Reflection;
using ChaosFramework.Core;
using ChaosFramework.Graphics.Imaging.Formats;
using ChaosFramework.Graphics.OpenGl;
using ChaosFramework.Graphics.OpenGl.AssetContainers;
using ChaosFramework.IO.Streams;
using ChaosUtil.Primitives;
using SysCol = System.Collections.Generic;
using Tex = ChaosFramework.Graphics.OpenGl.AssetContainers.TextureContainer.Entry;

namespace ChaosFramework.Menu.OpenGl
{
    using Graphics = Graphics.OpenGl.Graphics;

    public class TextureProvider : Disposable
    {
        class EmptyStreamSource : StreamSource
        {
            public bool alive => true;
            public bool ContainsKey(string key) => false;
            public SysCol.IEnumerable<string> EnumerateKeys() => Array<string>.empty;
            public Stream OpenRead(string key) => throw new NotSupportedException();
        }

        public readonly Graphics graphics;
        readonly TextureContainer @default;
        protected readonly TextureContainer @override;

        public TextureProvider(Graphics graphics, TextureContainer @override = null)
        {
            const BindingFlags INTERNAL_STATIC = BindingFlags.NonPublic | BindingFlags.Static;

            this.@override = @override;
            @default = new TextureContainer(new EmptyStreamSource(), graphics.dispatcher, false);

            foreach (PropertyInfo iteratorField in typeof(Properties.Resources).GetProperties(INTERNAL_STATIC))
            {
                PropertyInfo field = iteratorField;

                if (typeof(UnmanagedMemoryStream).IsAssignableFrom(field.PropertyType))
                {
                    const int PREFIX_LENGTH = 4; // "Tex_".Length
                    System.Text.StringBuilder fieldName = new(field.Name.Length - PREFIX_LENGTH);
                    fieldName.Append(field.Name[PREFIX_LENGTH].ToString().ToLower());
                    fieldName.Append(field.Name[(PREFIX_LENGTH + 1)..]);
                    fieldName.Append(".png");
                    @default.AddFactory(fieldName.ToString(), _ =>
                    {
                        using (Stream res = Properties.Resources.ResourceManager.GetStream(field.Name))
                            return Texture.FromBitmap(graphics.dispatcher, Png.FromStream(res));
                    });
                }
            }
        }

        public virtual Tex button => Load($"{nameof(button)}.png");
        public virtual Tex buttonDark => Load($"{nameof(buttonDark)}.png");
        public virtual Tex dialog => Load($"{nameof(dialog)}.png");
        public virtual Tex dialogDark => Load($"{nameof(dialogDark)}.png");
        public virtual Tex cursor => Load($"{nameof(cursor)}.png");
        public virtual Tex frame => Load($"{nameof(frame)}.png");
        public virtual Tex tabPage => Load($"{nameof(tabPage)}.png");
        public virtual Tex tabPageButton => Load($"{nameof(tabPageButton)}.png");
        public virtual Tex tabPageButtonActive => Load($"{nameof(tabPageButtonActive)}.png");
        public virtual Tex slider => Load($"{nameof(slider)}.png");
        public virtual Tex sliderButton => Load($"{nameof(sliderButton)}.png");
        public virtual Tex colorSliderButton => Load($"{nameof(colorSliderButton)}.png");
        public virtual Tex numericActive => Load($"{nameof(numericActive)}.png");
        public virtual Tex gridCell => Load($"{nameof(gridCell)}.png");
        public virtual Tex expanderButtonExpanded => Load($"{nameof(expanderButtonExpanded)}.png");
        public virtual Tex expanderButtonCollapsed => Load($"{nameof(expanderButtonCollapsed)}.png");

        Tex Load(string source)
            => Load(source, this);

        public virtual Tex Load(string source, Disposable monitor1, params Disposable[] monitors)
            => TryLoad(source, out Tex tex, monitor1, monitors)
                ? tex
                : throw new KeyNotFoundException(source);

        public virtual bool TryLoad(string source, out Tex texture, Disposable monitor1, params Disposable[] monitors)
            => @override.TryLoad(source, out texture, monitor1, monitors)
            || @default.TryLoad(source, out texture, monitor1, monitors);

        protected override void DoDispose()
        {
            base.DoDispose();
            @default.Dispose();
        }
    }
}
