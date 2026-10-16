using System;
using System.IO;
using ChaosFramework.Components;
using ChaosFramework.Graphics;
using ChaosFramework.Graphics.Colors;
using ChaosFramework.Graphics.OpenGl.AssetContainers;
using ChaosFramework.Math.Vectors;
using static ChaosFramework.Math.Signs;

namespace ChaosFramework.Menu.OpenGl.Dialogs
{
    using Controls;

    partial class FileDialog
    {
        class Entry
            : Frame
        {
            // TODO: customize?
            static readonly Rgba SELECTED_COLOR = new(0.5f, 0.3f, 0, 1);

            Rgba defaultColor = Rgba.OPAQUE_BLACK;
            FileDialog dlg;
            TextureContainer.Entry thumbnail;

            public EntryType type { get; private set; }

#if OS_WINDOWS
            bool hasAccess
            {
                get => defaultColor == textColor;
                set
                {
                    if (value)
                        defaultColor = textColor;
                    else
                    {
                        Hsva v = Hsva.FromRgba(textColor);
                        v.s *= 0.5f;
                        v.v = 0.5f;
                        defaultColor = Rgba.FromHsva(v);
                    }
                }
            }
#endif

            Image img;
            Label label;

            System.Threading.Tasks.Task thumbnailLoader;

            string internalPath;
            public string target { get; private set; }

            protected override void Create(CreateParameters cparams)
            {
                base.Create(cparams);
                dlg = (FileDialog)scene;

                borderSize = 0.02f;
                target = internalPath = text;
                type = EntryType.Directory;
                if (internalPath.Length == 3 && internalPath.Substring(1, 2) == ":\\")
                    type = EntryType.Drive;
                else if (!Directory.Exists(internalPath))
                    if (!File.Exists(internalPath))
                        throw new FileNotFoundException();
#if OS_WINDOWS
                    // TODO: implement links in a platform agnostic way
                    else if (internalPath.EndsWith(".lnk"))
                    {
                        target = ChaosUtil.Platform.Windows.FileSystem.GetShortcutTargetFile(internalPath);
                        if (File.Exists(target))
                            type = EntryType.FileLink;
                        else if (Directory.Exists(target))
                            type = EntryType.DirectoryLink;
                    }
#endif
                    else
                        type = EntryType.File;

                img = CreateControl<Image>(new Vector2f(left + size.y, position.y), size.y * BUTTON_SCALE);
                thumbnailLoader = System.Threading.Tasks.Task.Run(GetThumbnail);

                label = CreateControl<Label>(new Vector2f(img.right + TEXT_MARGIN, position.y), size.y, GetLabelText());
                label.textAlign = Align.Left;
                if (type == EntryType.Directory || type == EntryType.DirectoryLink || type == EntryType.Drive)
                    CreateControl<Button>(new Vector2f(right - size.y, position.y), size.y * BUTTON_SCALE, ">")
                        .mouseDownLeft = FollowDirectory;
                else if (type == EntryType.FileLink)
                    CreateControl<Button>(new Vector2f(right - size.y, position.y), size.y * BUTTON_SCALE, ">")
                        .mouseDownLeft = FollowLink;

                mouseDownLeft += Click;
                label.mouseDownLeft += Click;
                img.mouseDownLeft += Click;
                doubleClickLeft += DoubleClick;
                label.doubleClickLeft += DoubleClick;
                img.doubleClickLeft += DoubleClick;
#if OS_WINDOWS
                hasAccess = ChaosUtil.Platform.Paths.FileSystem.HasAccess(text);
#endif
            }

            void FollowDirectory(Control _)
                => ((FileDialog)scene).CollectEntries(target);

            void FollowLink(Control _)
            {
                ((FileDialog)scene).CollectEntries(Path.GetDirectoryName(target));
                foreach (Entry fileEntry in ((FileDialog)scene).entries)
                    if (fileEntry.target == target)
                        ((FileDialog)scene).Select(fileEntry);
            }

            void GetThumbnail()
            {
                lock (img)
                {
                    thumbnail = null;

                    if (dlg.getThumbnail != null)
                        thumbnail = dlg.getThumbnail(internalPath);
                    // TODO: implement thumbnails from file system in a platform agnostic way if thumbnail == null

                    img.texture = thumbnail;
                }
            }

            public string GetLabelText() => type switch
            {
                EntryType.File => Path.GetFileName(internalPath),
                EntryType.Directory => new DirectoryInfo(internalPath).Name,
                EntryType.Drive => $"{internalPath} {new DriveInfo(internalPath).VolumeLabel}",
                EntryType.FileLink => Path.GetFileName(internalPath),
                EntryType.DirectoryLink => Path.GetFileName(internalPath),
                _ => throw new InvalidOperationException("Unknown file system entry type"),
            };

            public override bool IsHovered()
                => ((Control)parent).IsHovered() && Abs(scene.context.cursorPosition.x - scrolledPosition.x) < size.x
                && Abs(scene.context.cursorPosition.y - scrolledPosition.y) < size.y;

            public override void Draw()
            {
                label.textColor = ((FileDialog)scene).selectedEntry == this ? SELECTED_COLOR : defaultColor;
                base.Draw();
            }

            void Click(Control sender)
            {
                if (((FileDialog)scene).openFile
                    && (type == EntryType.Directory || type == EntryType.Drive || type == EntryType.DirectoryLink)
                   )
                    ((FileDialog)scene).CollectEntries(target);
                else
                    ((FileDialog)scene).Select(this);
            }

            void DoubleClick(Control sender)
            {
                switch (type)
                {
                    case EntryType.File:
                        ((FileDialog)scene).Ok();
                        break;

                    case EntryType.Directory:
                    case EntryType.Drive:
                    case EntryType.DirectoryLink:
                        ((FileDialog)scene).CollectEntries(target);
                        break;

                    case EntryType.FileLink:
                        ((FileDialog)scene).CollectEntries(Path.GetDirectoryName(target));
                        foreach (Entry fileEntry in ((FileDialog)scene).entries)
                            if (fileEntry.target == target)
                                ((FileDialog)scene).Select(fileEntry);
                        break;
                }
            }

            protected override void DoDispose()
            {
                thumbnailLoader.ContinueWith(DisposeThumbnail);
                base.DoDispose();
            }

            void DisposeThumbnail(System.Threading.Tasks.Task _)
            {
                if (thumbnail?.key == null)
                {
                    lock (img)
                        thumbnail?.content?.Dispose();
                }
            }
        }
    }
}
