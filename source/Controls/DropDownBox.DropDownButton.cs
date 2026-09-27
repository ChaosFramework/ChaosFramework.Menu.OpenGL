using System;
using ChaosFramework.Components;
using ChaosFramework.Graphics.Colors;
using ChaosFramework.Math.Vectors;
using static ChaosFramework.Math.Clamping;
using static ChaosFramework.Math.Transforms;

namespace ChaosFramework.Menu.OpenGl.Controls
{
    public partial class DropDownBox
    {
        protected class DropDownButton : Button
        {
            const float TARGET_MARGIN_SQ = 0.0001f;
            const float BTN_SPEED = 25;

            public State state = State.Expanded;
            public Vector2f targetPos;
            public int index;

            int scrolledIndex => index - drop.currentScroll;
            public bool reachedTarget => (position - targetPos).LengthSq() < TARGET_MARGIN_SQ;
            DropDownBox drop => parent as DropDownBox;
            public bool selected => index == drop.selectedIndex;

            protected override void Create(CreateParameters cparams)
            {
                base.Create(cparams);
                mouseDownLeft = Click;
                topMost = true;
            }

            public override void SetUpdateCalls()
            {
                base.SetUpdateCalls();
                scene.updateLayers[(int)GuiScene.UpdateLayers.HudActions].Add(Update);
            }

            void Update()
            {
                int xOffset = 0, yOffset = 0;
                for (int i = 0; i < scrolledIndex; i++)
                {
                    ++yOffset;
                    if (i < drop.maxExpandedEntries - 1 && yOffset == drop.maxRows)
                    {
                        yOffset = 0;
                        ++xOffset;
                    }
                }

                targetPos = drop.position + new Vector2f(
                    (float)(2 * base.size.x * Clamp(0, drop.maxColumns, xOffset)),
                    -2 * base.size.y * Clamp(0, drop.maxExpandedEntries, yOffset)
                    );

                base.position = InterpolatePosition(base.position, targetPos, ftime * BTN_SPEED);
            }

            void Click(Control _)
            {
                if (drop.enableMouse)
                {
                    drop.selectedIndex = Array.IndexOf(drop.buttons, this);
                    drop.Collapse();
                    drop.valueChanged?.Invoke(drop);
                }
            }

            protected override void PrepareDraw()
                => shader.content.SetValue("color", selected
                    ? new Rgba(new Rgb(0.25f), 1)
                    : (IsHovered()
                        ? new Rgba(new Rgb(0.5f), 1)
                        : Rgba.OPAQUE_WHITE
                    ));

            public override void Draw()
            {
                if (scrolledIndex < 0 || scrolledIndex >= drop.maxExpandedEntries)
                    return;

                base.Draw();
            }

            public override bool IsHovered()
                => drop.enableMouse && base.IsHovered();
        }
    }
}
