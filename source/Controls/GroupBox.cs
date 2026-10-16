using ChaosFramework.Components;
using ChaosFramework.Math.Vectors;
using ChaosUtil.Primitives;

namespace ChaosFramework.Menu.OpenGl.Controls
{
    public sealed class GroupBox
        : Image
    {
        const float BTN_SZ = 0.05f;

        static int SortGroupBoxPages(Component a, Component b)
            => a is GroupBoxPage ? -1 : 1;

        public GroupBoxPage[] pages = [];
        int selectedPageIndex = 0;
        public GroupBoxPage selectedPage => pages[selectedPageIndex];
        Button btnLeft, btnRight;
        internal Image imgPageIndex;

        protected override DisposablesList<Component> InitChildrenList()
            => new SortedDisposablesList<Component>(SortGroupBoxPages);

        protected override void Create(CreateParameters cparams)
        {
            base.Create(cparams);
            btnLeft = CreateControl<Button>(Vector2f.EMPTY, BTN_SZ, UnicodeChars.ArrowLeft_3D.GetUnicodeString());
            btnRight = CreateControl<Button>(Vector2f.EMPTY, BTN_SZ, UnicodeChars.ArrowRight_3D.GetUnicodeString());
            imgPageIndex = CreateControl<Image>(
                Vector2f.EMPTY,
                new Vector2f(2 * BTN_SZ, BTN_SZ),
                $"{selectedPageIndex + 1}/{pages.Length}"
                );
            btnLeft.mouseDownLeft = Decrement;
            btnRight.mouseDownLeft = Increment;
        }

        public void SetPages(int numPages)
        {
            foreach (GroupBoxPage p in pages)
                p.Dispose();

            pages = new GroupBoxPage[numPages];
            for (int i = 0; i < numPages; i++)
                pages[i] = CreateControl<GroupBoxPage>(position, size, string.Empty);

            Open();
        }

        public void Increment(Control sender)
        {
            ++selectedPageIndex;
            selectedPageIndex %= pages.Length;
            Open();
        }

        public void Decrement(Control sender)
        {
            --selectedPageIndex;
            if (selectedPageIndex < 0)
                selectedPageIndex += pages.Length;

            Open();
        }

        public void Open()
        {
            foreach (GroupBoxPage page in pages)
                page.doUpdate = page.doDraw = false;

            selectedPage.doDraw = selectedPage.doUpdate = true;
        }

        public override void Draw()
        {
            btnRight.position = position + size + new Vector2f(-1f * BTN_SZ, -BTN_SZ);
            btnLeft.position = position + size + new Vector2f(-7f * BTN_SZ, -BTN_SZ);
            imgPageIndex.position = position + size + new Vector2f(-4f * BTN_SZ, -BTN_SZ);
            imgPageIndex.text = $"{selectedPageIndex + 1}/{pages.Length}";
        }

        public override bool IsHovered()
            => false;
    }
}
