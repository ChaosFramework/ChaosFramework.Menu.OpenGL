using ChaosFramework.Collections;
using ChaosFramework.Core;
using ChaosFramework.Graphics.Colors;
using ChaosFramework.Math.Vectors;
using static ChaosFramework.Math.Clamping;

namespace ChaosFramework.Menu.OpenGl.Controls
{
    public partial class TabPageContainer
    {
        public class Line : Disposable
        {
            public float extraSpace;
            public int scroll, maxScroll;
            public Button btnScrollLeft, btnScrollRight;
            public AdvancedLinkedList<TabPage> pages = [];

            public float btnOpenWidth { get; private set; }

            public void DisableScroll()
            {
                scroll = maxScroll = 0;
                btnScrollLeft?.Dispose();
                btnScrollLeft = null;
                btnScrollRight?.Dispose();
                btnScrollRight = null;
            }

            public void Layout(TabPageContainer container, int row)
            {
                float btnOpenY = container.position.y + container.size.y - (2 * row + 1) * BTN_SIZE_Y;
                float registerWidth = container.width - extraSpace;
                int numDisplayedButtons = pages.length;
                btnOpenWidth = registerWidth / numDisplayedButtons * 0.5f;
                maxScroll = 0;
                if (pages.length > 1 && btnOpenWidth < container.minPageButtonWidth)
                {
                    if (btnScrollLeft == null)
                    {
                        btnScrollLeft = container.CreateControl<Button>(0, new Vector2f(BTN_SIZE_Y / 2, BTN_SIZE_Y), "<");
                        btnScrollLeft.mouseDownLeft = ScrollLeft;
                        btnScrollLeft.enabledFunction = CanScrollLeft;
                    }

                    if (btnScrollRight == null)
                    {
                        btnScrollRight = container.CreateControl<Button>(0, new Vector2f(BTN_SIZE_Y / 2, BTN_SIZE_Y), ">");
                        btnScrollRight.mouseDownLeft = ScrollRight;
                        btnScrollRight.enabledFunction = CanScrollRight;
                    }

                    btnScrollLeft.position = new Vector2f(container.left + BTN_SIZE_Y / 2, btnOpenY);
                    btnScrollRight.position = new Vector2f(container.right - extraSpace - BTN_SIZE_Y / 2, btnOpenY);
                    registerWidth -= BTN_SIZE_Y * 2;
                    do
                    {
                        maxScroll++;
                        btnOpenWidth = registerWidth / --numDisplayedButtons * 0.5f;
                    }
                    while (btnOpenWidth < container.minPageButtonWidth && numDisplayedButtons > 1);
                    scroll = Min(scroll, maxScroll);
                }
                else
                    DisableScroll();

                int i = 0;
                foreach (TabPage page in pages)
                {
                    page.btnOpen.text = container.displayString(page.text);
                    page.btnOpen.textColor = new Rgba(0, 1, 0, 1);
                    page.btnOpen.size = new Vector2f(btnOpenWidth, BTN_SIZE_Y);
                    page.btnOpen.Teleport(new Vector2f(
                        (maxScroll > 0 ? btnScrollLeft.right : container.left) + ((i++ - scroll) * 2 + 1) * btnOpenWidth,
                        btnOpenY
                        ));
                }
            }

            void ScrollLeft(Control sender)
                => scroll = Max(0, scroll - 1);

            void ScrollRight(Control sender)
                => scroll = Min(maxScroll, scroll + 1);

            bool CanScrollLeft()
                => scroll > 0;

            bool CanScrollRight()
                => scroll < maxScroll;

            protected override void DoDispose()
            {
                base.DoDispose();
                foreach (TabPage page in pages)
                {
                    page.Dispose();
                    page.btnOpen.Dispose();
                }

                btnScrollLeft?.Dispose();
                btnScrollRight?.Dispose();
            }
        }
    }
}
