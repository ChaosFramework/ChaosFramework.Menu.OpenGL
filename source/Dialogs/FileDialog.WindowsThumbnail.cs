#if OS_WINDOWS

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using ChaosFramework.Graphics.Imaging;
using ChaosFramework.Graphics.OpenGl;
using ChaosFramework.Graphics.OpenGl.AssetContainers;
using ChaosUtil.Platform.Windows.Thumbnail;

namespace ChaosFramework.Menu.OpenGl.Dialogs
{
    partial class FileDialog
    {
        unsafe TextureContainer.Entry GetWindowsThumbnail(string path)
        {
            const uint SIZE = 32;
            const uint NUM_TRIES = 3;

            for (int i = 0; i < NUM_TRIES; i++)
            {
                Bitmap bm;
                try
                {
                    bm = WindowsThumbnailProvider.GetThumbnail(path, (int)SIZE, (int)SIZE, ThumbnailOptions.ResizeToFit);
                }
                catch (COMException ex) when (ex.HResult == unchecked((int)0x8000000A)) // "Resource is not available yet"
                {
                    continue;
                }
                using (bm)
                {
                    Rgba8Image rgba8 = new(SIZE, SIZE);
                    BitmapData data = bm.LockBits(new(0, 0, (int)SIZE, (int)SIZE), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                    try
                    {
                        for (uint y = 0; y < SIZE; y++)
                            for (uint x = 0; x < SIZE; x++)
                            {
                                IntPtr @base = data.Scan0 + (int)y * data.Stride + (int)x * 4;
                                byte b = *(byte*)@base++, g = *(byte*)@base++, r = *(byte*)@base++, a = *(byte*)@base++;
                                rgba8[x, SIZE - y - 1] = new(r, g, b, a);
                            }
                    }
                    finally
                    {
                        bm.UnlockBits(data);
                    }

                    Texture tex = Texture.FromBitmap(context.graphics.dispatcher, rgba8);
                    return TextureContainer.Entry.Mock((_, __) => tex, _ => { });
                }
            }

            return null;
        }
    }
}

#endif
