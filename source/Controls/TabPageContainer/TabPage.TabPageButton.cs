using ChaosFramework.Input;
using ChaosFramework.Input.InputEvents;
using ChaosFramework.Math.Vectors;
using static ChaosFramework.Math.Clamping;
using static ChaosFramework.Math.Exponentials;

namespace ChaosFramework.Menu.OpenGl.Controls
{
    partial class TabPage
    {
        public class TabPageButton : Button
        {
            public delegate void TabIndexChanged(int newIndex);

            TabPageContainer.Line _line;
            public TabPageContainer.Line line
            {
                get
                {
                    if (_line == null)
                        foreach (TabPageContainer.Line row in GetParent<TabPageContainer>().rows)
                            foreach (TabPage page in row.pages)
                                if (page.btnOpen == this)
                                    return _line = row;

                    return _line;
                }
            }

            public TabIndexChanged tabIndexChanged;
            public bool inSight = false;
            float disappearTimer = 0;
            public Vector2f targetPos;

            public override bool scrollRelevant
                => false;

            internal void Teleport(Vector2f position)
                => this.position = targetPos = position;

            public override void SetUpdateCalls()
            {
                base.SetUpdateCalls();
                scene.updateLayers[(int)GuiScene.UpdateLayers.HudActions].Add(Update);
                scene.context.input.AddHandler<InputChangeEvent<Mouse.Wheel>, Mouse.Wheel, InputChange>(
                    scene.context.inputLayer,
                    MouseWheel
                    );
            }

            bool MouseWheel(InputChangeEvent<Mouse.Wheel> e)
            {
                if (base.IsHovered())
                    switch (e.axis.dir)
                    {
                        case Mouse.WheelDirection.Scroll:
                            line.scroll = Clamp(0, line.maxScroll, line.scroll - (int)(e.data.newValue - e.data.oldValue));
                            return true;

                        default:
                            return false;
                    }

                return false;
            }

            void Update()
            {
                base.position += (targetPos - base.position) * EaseIn(ftime * 10);
                inSight &= (targetPos - base.position).LengthSq() < 0.00005f;
                disappearTimer -= ftime;
                topMost = mouseDownOnControl[0];
                if (mouseDownOnControl[0])
                {
                    int targetIndex = 0, currentIndex = 0;
                    int i = 0;
                    TabPage myPage = null;
                    foreach (TabPage page in line.pages)
                    {
                        if (page.btnOpen == this)
                        {
                            currentIndex = i;
                            myPage = page;
                        }

                        if (scene.context.cursorPosition.x + line.btnOpenWidth >= page.btnOpen.targetPos.x)
                            targetIndex = i;

                        i++;
                    }

                    if (currentIndex > targetIndex && (disappearTimer <= 0 || line.scroll <= targetIndex))
                    {
                        line.pages.RemoveAt(currentIndex);
                        line.pages.Insert(targetIndex, myPage);
                        line.scroll = System.Math.Min(targetIndex, line.scroll);
                        disappearTimer = 0.3f;
                        tabIndexChanged?.Invoke(targetIndex);
                    }
                    else
                    {
                        int scrolledTargetIndex = targetIndex - (line.pages.length - line.maxScroll) + 1;
                        if (targetIndex > currentIndex && (disappearTimer <= 0 || scrolledTargetIndex >= targetIndex))
                        {
                            line.pages.Insert(targetIndex + 1, myPage);
                            line.pages.RemoveAt(currentIndex);
                            line.scroll = System.Math.Max(scrolledTargetIndex, line.scroll);
                            disappearTimer = 0.3f;
                            tabIndexChanged?.Invoke(targetIndex);
                        }
                    }
                }
            }

            public override bool IsHovered()
                => inSight && base.IsHovered();
        }
    }
}
