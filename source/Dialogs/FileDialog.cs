using System;
using System.IO;
using System.Text.RegularExpressions;
using ChaosFramework.Collections;
using ChaosFramework.Collections.Immutable;
using ChaosFramework.Components;
using ChaosFramework.Graphics;
using ChaosFramework.Graphics.OpenGl.AssetContainers;
using ChaosFramework.Math.Vectors;
using ChaosUtil.Platform.Paths;
using ChaosUtil.Primitives;
using SysCol = System.Collections.Generic;

namespace ChaosFramework.Menu.OpenGl.Dialogs
{
    using Controls;

    public partial class FileDialog
        : DialogScene
    {
        [Flags]
        public enum Mode
        {
            GetDirectory = 0,
            GetFile = 1,
            AllowNew = 2,
            GetFileAndAllowNew = 3,
        }

        public enum EntryType
        {
            File,
            Directory,
            Drive,
            FileLink,
            DirectoryLink
        }

        public delegate TextureContainer.Entry GetThumbnail(string fullpath);

        const int MAX_PATH_ELEMENT_LENGTH = 33;
        const float TEXT_MARGIN = 0.01f;
        const float BUTTON_SCALE = 0.666f;
        static readonly Vector2f BTN_RESULT_SIZE = new(0.2f, .045f);
        public static readonly Vector2f DEFAULT_SIZE = new(1.3f, 0.5f);

        static readonly ImmutableHashSet<char> invalidChars = (SysCol.HashSet<char>)[
            .. Path.GetInvalidFileNameChars(),
            .. Path.GetInvalidPathChars(),
            '\\',
            '/'
            ];

        static void ReducePathString(Label targetLabel, float maxWidth)
        {
            while (targetLabel.textGeometry.geo.geometryBounds.width * targetLabel.size.x > maxWidth)
            {
                const string DOTS = "...";

                string root = Path.GetPathRoot(targetLabel.text);
                string restOfPath = targetLabel.text[root.Length..];
                if (restOfPath.StartsWith(DOTS))
                    restOfPath = restOfPath[(DOTS.Length + 1)..];

                string[] split = restOfPath.Split(['/'], 2);
                if (split.Length < 2)
                    return;

                string restOfPathWithoutFirstEntry = split[1];
                targetLabel.text = $"{root}{DOTS}/{restOfPathWithoutFirstEntry}";
            }
        }

        static int Compare(Entry a, Entry b)
            => a.target.CompareTo(b.target);

        public GetThumbnail getThumbnail = null;

        readonly string filter;
        readonly string extension;
        readonly Mode mode;
        readonly ScrollBox entryContainer;
        readonly Label lblCurrentDir;
        readonly LinkedList<Entry> entries = [];
        readonly StopScene childBaseStopMode;
        string currentDirectory;
        string newFileEntry = null;
        Entry selectedEntry = null;

        bool openFile
            => mode.HasFlag(Mode.GetFile);

        public string selectedValue
            => selectedEntry?.target ?? newFileEntry ?? currentDirectory;

        public FileDialog(
            MenuContext context,
            StopScene childBaseStopMode,
            Vector2f pos,
            Vector2f sz,
            Mode mode,
            string prompt,
            string baseDirectory,
            string filter = "*"
            ) : base(context, pos, sz, false)
        {
            this.childBaseStopMode = childBaseStopMode;

            if (!Directory.Exists(currentDirectory = baseDirectory))
                Directory.CreateDirectory(baseDirectory);

            this.mode = mode;
            this.filter = filter;

            string[] split;
            if ((split = filter.Split('|')).Length == 1 && (split = split[0].Split('.')).Length == 2)
                extension = "." + split[1];

            panel.texture = context.texProvider.dialog;

            currentDirectory = Path.GetFullPath(currentDirectory);
            Label lblPrompt = panel.CreateControl<Label>(
                panel.topLeft + new Vector2f(.05f, -.05f),
                .05f,
                prompt
                );

            lblPrompt.textAlign = Align.Left;
            Button btnDirUp = panel.CreateControl<Button>(
                new Vector2f(panel.left + 0.1f, lblPrompt.bottom - 0.05f),
                new Vector2f(0.05f, 0.05f) * BUTTON_SCALE,
                UnicodeChars.ArrowUp_3D.GetUnicodeString()
                );
            lblCurrentDir = panel.CreateControl<Label>(
                new Vector2f(btnDirUp.right + TEXT_MARGIN, btnDirUp.position.y),
                new Vector2f(.045f, .05f),
                "<null>"
                );

            const float MARGIN = 0.025f;
            float bottomOfEntryContainer = panel.bottom + 0.1f;

            entryContainer = panel.CreateControl<ScrollBox>(
                new Vector2f(panel.position.x, (btnDirUp.bottom - MARGIN + bottomOfEntryContainer) / 2),
                new Vector2f(panel.size.x - 0.05f, (btnDirUp.bottom - MARGIN - bottomOfEntryContainer) / 2)
                );
            entryContainer.enableScrollX = false;
            Button btnOk = panel.CreateControl<Button>(
                new Vector2f(entryContainer.right - BTN_RESULT_SIZE.x, (panel.bottom + bottomOfEntryContainer) / 2),
                BTN_RESULT_SIZE,
                "ok"
                );

            btnOk.enabledFunction = OkEnabled;
            btnOk.mouseDownLeft = Ok;
            Button btnCancel = panel.CreateControl<Button>(
                new Vector2f(btnOk.left - BTN_RESULT_SIZE.x - MARGIN, btnOk.position.y),
                BTN_RESULT_SIZE,
                "cancel"
                );
            btnCancel.mouseDownLeft = Cancel;

            const float BTN_CREATE_DIR_WIDTH_FACTOR = 1.5f;
            Button btnCreateNewDirectory = CreateControl<Button>(
                new Vector2f(entryContainer.left + BTN_RESULT_SIZE.x * BTN_CREATE_DIR_WIDTH_FACTOR, btnOk.position.y),
                new Vector2f(BTN_RESULT_SIZE.x * BTN_CREATE_DIR_WIDTH_FACTOR, BTN_RESULT_SIZE.y),
                "New Directory"
                );
            btnCreateNewDirectory.mouseDownLeft = HandleDirectorySelect;

            if (mode == Mode.GetFileAndAllowNew)
            {
                Button btnCreateNewFile = CreateControl<Button>(
                    new Vector2f(btnCreateNewDirectory.right + BTN_RESULT_SIZE.x * BTN_CREATE_DIR_WIDTH_FACTOR + MARGIN, btnOk.position.y),
                    new Vector2f(BTN_RESULT_SIZE.x * BTN_CREATE_DIR_WIDTH_FACTOR, BTN_RESULT_SIZE.y),
                    "New File"
                    );

                void CreateNewFile(Control _)
                {
                    Regex regex = new(GlobRegex.ConvertGlobToRegex(filter), RegexOptions.Compiled | RegexOptions.IgnoreCase);
                    InputBox dlgNewFile = new(context, 1, 0.25f);
                    dlgNewFile.textbox.invalidCharacters = invalidChars;

                    void CreateFileCallback()
                    {
                        if (dlgNewFile.canceled)
                            return;

                        if (!regex.IsMatch(dlgNewFile.text))
                        {
                            string oldText = dlgNewFile.text;
                            string plusExt = oldText + extension;
                            if (regex.IsMatch(plusExt))
                            {
                                newFileEntry = $"{currentDirectory}/{plusExt}";
                                KindlyRequestSuicide();
                                return;
                            }
                            else
                            {
                                dlgNewFile = new InputBox(context, 1, 0.25f);
                                dlgNewFile.textbox.invalidCharacters = invalidChars;
                                AskForFileName("Invalid file name", oldText);
                                return;
                            }
                        }

                        foreach (char c in invalidChars)
                            if (dlgNewFile.text.Contains(c.ToString()))
                            {
                                //just in case...
                                string oldText = dlgNewFile.text;
                                dlgNewFile = new InputBox(context, 1, 0.25f);
                                dlgNewFile.textbox.invalidCharacters = invalidChars;
                                AskForFileName("Invalid file name", oldText);
                                return;
                            }

                        string path = $"{currentDirectory}/{dlgNewFile.text}";
                        if (File.Exists(path))
                        {
                            string oldText = dlgNewFile.text;
                            dlgNewFile = new InputBox(context, 1, 0.25f);
                            dlgNewFile.textbox.invalidCharacters = invalidChars;
                            AskForFileName("File already exists", oldText);
                            return;
                        }
                        else
                        {
                            newFileEntry = path;
                            KindlyRequestSuicide();
                            return;
                        }
                    }

                    void AskForFileName(string prompt, string folderName)
                        => dlgNewFile.ShowDialog(GetStopMode, prompt, folderName, MAX_PATH_ELEMENT_LENGTH, CreateFileCallback);

                    AskForFileName("Enter a name.", "New File");
                }

                btnCreateNewFile.mouseDownLeft = CreateNewFile;
            }

            btnDirUp.mouseDownLeft = DirectoryUp;
        }

        void DirectoryUp(Control _)
        {
            string basePath = Path.GetDirectoryName(Path.GetFullPath(currentDirectory));
            CollectEntries(basePath);
            activeControl = entryContainer;
        }

        void Cancel(Control _)
        {
            canceled = true;
            KindlyRequestSuicide();
        }

        void HandleDirectorySelect(Control _)
            => HandleDirectorySelect(context);

        void HandleDirectorySelect(MenuContext context)
        {
            InputBox dlgNewDir = new(context, 1, 0.25f);
            dlgNewDir.textbox.invalidCharacters = invalidChars;

            void NewDirectoryCallback()
            {
                if (dlgNewDir.canceled)
                    return;

                foreach (char c in invalidChars)
                    if (dlgNewDir.text.Contains(c.ToString()))
                    {
                        //just in case...
                        string oldText = dlgNewDir.text;
                        dlgNewDir = new InputBox(context, 1, 0.25f);
                        dlgNewDir.textbox.invalidCharacters = invalidChars;
                        AskForDirectoryName("Invalid directory name", oldText);
                        return;
                    }

                string path = $"{currentDirectory}/{dlgNewDir.text}";
                if (Directory.Exists(path))
                {
                    string oldText = dlgNewDir.text;
                    dlgNewDir = new InputBox(context, 1, 0.25f);
                    dlgNewDir.textbox.invalidCharacters = invalidChars;
                    AskForDirectoryName("Directory already exists", oldText);
                    return;
                }

                Directory.CreateDirectory(path);
                CollectEntries(currentDirectory);
            }

            void AskForDirectoryName(string prompt, string folderName)
                => dlgNewDir.ShowDialog(GetStopMode, prompt, folderName, MAX_PATH_ELEMENT_LENGTH, NewDirectoryCallback);

            AskForDirectoryName("Enter a name.", "New Folder");
        }

        void Ok(Control _)
            => Ok();

        bool OkEnabled()
            => openFile
             ? (selectedEntry != null && selectedEntry.type == EntryType.File)
             : (selectedEntry == null || selectedEntry.type != EntryType.File);

        StopMode GetStopMode(Scene s)
            => s == this ? StopMode.UseDummy : childBaseStopMode(s);

        public override void ShowDialog(StopScene stopScenes, Action dialogCallBackFunction)
        {
            CollectEntries(currentDirectory);
            base.ShowDialog(stopScenes, dialogCallBackFunction);
        }

        void CollectEntries(string baseDir)
        {
            Vector2f sz = new(entryContainer.size.x - .05f, 0.05f);
            if (baseDir == null)
            {
                entryContainer.DisposeChildren();
                entries.Clear();

                LinkedList<string> drives = [];
                foreach (string drive in Directory.GetLogicalDrives())
                    if (new DriveInfo(drive).IsReady)
                        drives.AddSorted(drive);

                foreach (string drive in drives)
                    entries.Add(entryContainer.CreateControl<Entry>(Vector2f.EMPTY, sz, drive));

                Layout();
                lblCurrentDir.text = "Computer";
            }
            else
            {
                if (!FileSystem.HasAccess(baseDir))
                    return;

                entryContainer.DisposeChildren();
                entries.Clear();

                LinkedList<Entry> dirs = [], files = [], dirLinks = [], fileLinks = [];
                foreach (string dir in Directory.EnumerateDirectories(baseDir, "*", SearchOption.TopDirectoryOnly))
                    if ((new DirectoryInfo(dir).Attributes & FileAttributes.System) == 0)
                        dirs.Add(entryContainer.CreateControl<Entry>(Vector2f.EMPTY, sz, dir));

                if (openFile)
                    foreach (string file in Directory.EnumerateFiles(baseDir, filter, SearchOption.TopDirectoryOnly))
                        if ((new FileInfo(file).Attributes & FileAttributes.System) == 0)
                            files.Add(entryContainer.CreateControl<Entry>(Vector2f.EMPTY, sz, file));

#if OS_WINDOWS
                // TODO: verify that this works / make it work again
                foreach (string link in Directory.EnumerateFiles(baseDir, "*.lnk"))
                {
                    string target = ChaosUtil.Platform.Windows.FileSystem.GetShortcutTargetFile(link);
                    if (File.Exists(target))
                        fileLinks.Add(entryContainer.CreateControl<Entry>(Vector2f.EMPTY, sz, link));
                    else if (Directory.Exists(target))
                        dirLinks.Add(entryContainer.CreateControl<Entry>(Vector2f.EMPTY, sz, link));
                }
#endif

                dirs.Sort(Compare);
                files.Sort(Compare);
                dirLinks.Sort(Compare);
                fileLinks.Sort(Compare);
                entries.Add(dirs);
                entries.Add(dirLinks);
                entries.Add(files);
                entries.Add(fileLinks);

                if (entries.empty)
                {
                    Label lbl = entryContainer.CreateControl<Label>(0, 0.05f, "Empty");
                    lbl.textAlign = Align.Center;
                }

                Layout();
                lblCurrentDir.text = currentDirectory = baseDir;
                ReducePathString(lblCurrentDir, entryContainer.right - lblCurrentDir.left);
            }
        }

        void Layout()
        {
            float y = entryContainer.top;
            foreach (Entry entry in entries)
            {
                entry.position = new Vector2f(entryContainer.left + entry.size.x, y - entry.size.y);
                y = entry.bottom;
            }

            panel.Layout();
        }

        void Select(Entry entry)
            => selectedEntry = entry;

        void Ok()
        {
            if ((selectedEntry != null || !openFile)
                && (selectedEntry.type != EntryType.File || openFile)
                && (selectedEntry.type == EntryType.File || !openFile)
                )
                KindlyRequestSuicide();
        }

        public override void SetUpdateCalls()
        {
            base.SetUpdateCalls();
            updateLayers[(int)UpdateLayers.VisualUpdates].Add(CullInvisibleEntries);
        }

        void CullInvisibleEntries()
        {
            float upperBound = entryContainer.scrolledPosition.y + entryContainer.size.y;
            float lowerBound = entryContainer.scrolledPosition.y - entryContainer.size.y;
            foreach (Entry entry in entries)
                entry.doDraw = entry.doUpdate = (entry.scrolledPosition.y - entry.size.y < upperBound) &&
                    (entry.scrolledPosition.y + entry.size.y > lowerBound);
        }
    }
}
