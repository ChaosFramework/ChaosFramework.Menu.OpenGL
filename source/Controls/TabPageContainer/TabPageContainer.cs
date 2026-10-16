using System.Collections;
using System.Linq;
using System.Xml;
using ChaosFramework.Collections;
using ChaosFramework.Collections.Immutable;
using ChaosFramework.Components;
using ChaosFramework.Math.Vectors;
using SysCol = System.Collections.Generic;

namespace ChaosFramework.Menu.OpenGl.Controls
{
    public partial class TabPageContainer
        : LayoutContainer
        , SysCol.IEnumerable<TabPage>
    {
        class BtnOpenContainer : Control
        {
            public override void Draw() { }

            public override bool IsHovered()
                => false;
        }

        public delegate string DisplayString(object value);

        public const float BTN_SIZE_Y = 0.05f;

        public DisplayString displayString = ToString;

        public Line[] rows = [new()];
        public float minPageButtonWidth { get; set; } = 0.2f;
        internal Control btnOpenContainer;

        public readonly OrderedDisposablesList<Component> orderedChildren = [];

        public override Vector2f size
        {
            get => base.size;
            set
            {
                base.size = value;
                PerformLayoutInternal();
            }
        }

        public TabPage this[string caption]
        {
            get
            {
                foreach (Line row in rows)
                    foreach (TabPage page in row.pages)
                        if (page.text == caption)
                            return page;

                return null;
            }
        }

        public TabPage currentlyOpened
        {
            get
            {
                foreach (Line row in rows)
                    foreach (TabPage page in row.pages)
                        if (page.doUpdate)
                            return page;

                return null;
            }
        }

        protected override void Create(CreateParameters cparams)
        {
            btnOpenContainer = CreateControl<BtnOpenContainer>(Vector2f.EMPTY, Vector2f.EMPTY);
            base.Create(cparams);
            clipChildren = false;
        }

        protected override DisposablesList<Component> InitChildrenList()
            => orderedChildren;

        public override Control CreateControl(System.Type type, Vector2f position, Vector2f size, string text = null)
        {
            Control c = base.CreateControl(type, position, size, text);
            if (c is TabPage page)
                AddPage(0, page);

            return c;
        }

        public void SetPages(object[] values)
            => SetPages([values]);

        public void SetPages(object[][] values)
        {
            childContainers.Clear();
            foreach (Line row in rows)
                row.Dispose();

            rows = new Line[values.Length];
            for (int row = 0; row < values.Length; row++)
            {
                rows[row] = new Line();
                for (int col = 0; col < values[row].Length; col++)
                {
                    TabPage page = CreateControl<TabPage>(position, size);
                    if (row != 0)
                    {
                        rows[0].pages.Remove(page);
                        rows[row].pages.Add(page);
                    }

                    page.value = values[row][col];
                }
            }

            PerformLayoutInternal();
        }

        internal void KillPage(TabPage page)
        {
            if (!alive)
                return;

            TabPage lastPage = null;
            for (int row = 0; row < rows.Length; row++)
                foreach (TabPage refPage in rows[row].pages)
                    if (refPage == page)
                    {
                        rows[row].pages.Remove(refPage);
                        page.Dispose();
                        page.btnOpen.Dispose();
                        lastPage?.Open(lastPage.btnOpen);
                        PerformLayoutInternal();
                        return;
                    }
                    else
                        lastPage = page;
        }

        void RemovePageNoKill(TabPage toBeRemoved)
        {
            foreach (Line line in rows)
                line.pages.Remove(toBeRemoved);
        }

        void AddPage(int row, TabPage page, bool layout = true)
        {
            RemovePageNoKill(page);
            rows[row].pages.Add(page);
            Update(ftime);
            if (layout)
                Layout();
        }

        public void InsertPage(int row, int column, TabPage page, bool layout = true)
        {
            RemovePageNoKill(page);
            rows[row].pages.Insert(column, page);
            Update(ftime);
            if (layout)
                Layout();
        }

        public ImmutableArray<TabPage> GetPages(int row)
            => rows[row].pages.ToArray();

        public override void SetUpdateCalls()
        {
            scene.updateLayers[(int)GuiScene.UpdateLayers.HudActions].Add(Update);
            base.SetUpdateCalls();
        }

        void Update()
        {
            for (int rowIndex = 0; rowIndex < rows.Length; rowIndex++)
            {
                Line row = rows[rowIndex];
                float btnOpenY = position.y + size.y - (2 * rowIndex + 1) * BTN_SIZE_Y;
                int i = 0;
                foreach (TabPage page in row.pages)
                {
                    page.btnOpen.inSight = i >= row.scroll && i < row.scroll + (row.pages.length - row.maxScroll);
                    page.btnOpen.targetPos = new Vector2f(
                        (row.maxScroll > 0 ? row.btnScrollLeft.right : left) + ((i++ - row.scroll) * 2 + 1) * row.btnOpenWidth,
                        btnOpenY
                        );
                }
            }

            btnOpenContainer.position = position + new Vector2f(0, size.y - BTN_SIZE_Y * rows.Length);
            btnOpenContainer.size = new Vector2f(size.x, BTN_SIZE_Y * rows.Length);
        }

        public override void Draw() { }

        public override bool IsHovered()
            => false;

        protected override void PerformLayoutInternal()
        {
            btnOpenContainer.position = position + new Vector2f(0, size.y - BTN_SIZE_Y * rows.Length);
            btnOpenContainer.size = new Vector2f(size.x, BTN_SIZE_Y * rows.Length);
            bool hasOpenPage = false;
            foreach (LayoutContainer child in childContainers)
            {
                if (child is not TabPage page || page.myNode == null)
                    continue;

                XmlAttribute attr = page.myNode.Attributes["row"];
                if (attr == null || !int.TryParse(attr.Value, out int row))
                    row = 0;

                rows[row].pages.AddUnique(page);
            }

            for (int i = 0; i < rows.Length; i++)
                rows[i].extraSpace = 0;

            LinkedList<Control> nonPageControls = [];
            foreach (Button button in children.OfType<Button>())
                nonPageControls.Add(button);

            foreach (Line row in rows)
            {
                foreach (TabPage page in row.pages)
                {
                    nonPageControls.Remove(page.btnOpen);
                    hasOpenPage |= page.doUpdate;
                }

                nonPageControls.Remove(row.btnScrollLeft);
                nonPageControls.Remove(row.btnScrollRight);
            }

            foreach (Control c in nonPageControls)
            {
                if (c.width == width - (margin.left + margin.right))
                    c.width = BTN_SIZE_Y * 2;

                int row = 0;
                if (controlLayoutData.TryGetValue(c, out XmlNode childNode))
                {
                    XmlAttribute rowAttr = childNode.Attributes[nameof(TabPageContainer) + ".row"];
                    if (rowAttr != null)
                        int.TryParse(rowAttr.Value, out row);
                }

                float last = rows[row].extraSpace;
                rows[row].extraSpace += c.width;
                c.x = right - (last + rows[row].extraSpace) * 0.5f;
                c.height = BTN_SIZE_Y * 2;
                c.y = position.y + size.y - (2 * row + 1) * BTN_SIZE_Y;
            }

            for (int i = 0; i < rows.Length; i++)
                rows[i].Layout(this, i);

            if (!hasOpenPage)
                foreach (Line row in rows)
                    foreach (TabPage page in row.pages)
                    {
                        page.Open(page.btnOpen);
                        goto break2;
                    }

        break2:
            foreach (TabPage page in EnumerateChildren<TabPage>(false))
            {
                Vector2f pageAreaBottomLeft = bottomLeft;
                Vector2f pageAreaTopRight = topRight - new Vector2f(0, btnOpenContainer.height);
                page.position = (pageAreaBottomLeft + pageAreaTopRight) * 0.5f;
                page.size = (pageAreaTopRight - pageAreaBottomLeft) * 0.5f;
            }
        }

        protected override void DoDispose()
        {
            base.DoDispose();
            foreach (Line row in rows)
                row.Dispose();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public SysCol.IEnumerator<TabPage> GetEnumerator()
        {
            foreach (Line row in rows)
                foreach (TabPage page in row.pages)
                    yield return page;
        }
    }
}
