using ChaosFramework.Components;
using ChaosFramework.Math.Vectors;
using static ChaosFramework.Math.Exponentials;

namespace ChaosFramework.Menu.OpenGl.Controls
{
    using FlowDirection = Layout.FlowPanel.FlowDirection;

    public class DraggableButton : Image
    {
        Vector2f dragPosition;
        public System.Action parentChanged;

        protected override void Create(CreateParameters cparams)
        {
            base.Create(cparams);
            dragPosition = position;
        }

        public override void UpdateInteraction()
        {
            base.UpdateInteraction();
            if ((topMost || IsHovered()) && scene.mouseDown[0] > 0)
            {
                topMost = true;
                position = scene.context.cursorPosition - (scrolledPosition - position);
            }
        }

        public override void SetUpdateCalls()
        {
            base.SetUpdateCalls();
            scene.updateLayers[(int)GuiScene.UpdateLayers.HudActions].Add(Update);
        }

        void Update()
        {
            if (topMost && scene.mouseDown[0] <= 0)
                Release();

            dragPosition += (position - dragPosition) * EaseIn(ftime * 10);
        }

        void Release()
        {
            topMost = false;
            SetParent(scene.hoveringControl == null ? null : GetNewParent() ?? parent as LayoutContainer);
        }

        protected virtual LayoutContainer GetNewParent()
            => scene.hoveringControl.GetParent<Layout.FlowPanel>();

        public override bool IsHovered()
            => !topMost && base.IsHovered();

        public void SetParent(LayoutContainer target)
        {
            Vector2f oldScroll = scrolledPosition - position;
            parent.children.Remove(this);

            int validIndex = -1;
            Control tmp;
            Layout.FlowPanel flowParent = target as Layout.FlowPanel;
            if (flowParent != null)
            {
                int index = 0;
                validIndex = 0;
                float ttb = flowParent.direction == FlowDirection.ttb ? 1 : (flowParent.direction == FlowDirection.btt ? -1 : 0);
                float rtl = flowParent.direction == FlowDirection.rtl ? 1 : (flowParent.direction == FlowDirection.ltr ? -1 : 0);

                foreach (Component maybeBtn in target.children)
                {
                    if ((tmp = maybeBtn as Control) != null && (tmp.y - position.y) * ttb + (tmp.x - position.x) * rtl < 0)
                        break;
                    else if (tmp is DraggableButton)
                        validIndex = index + 1;

                    ++index;
                }
            }

            ((LayoutContainer)parent)?.Layout();
            GetParent<ScrollBox>()?.Layout();
            parent = target;
            if (validIndex != -1 && validIndex < flowParent.orderedChildren.length)
                flowParent.orderedChildren.Insert(validIndex, this);
            else
                parent.children.Add(this);

            parentChanged?.Invoke();
            target.Layout();
            UpdateHoverPriority();
            GetParent<ScrollBox>()?.Layout();
            dragPosition -= scrolledPosition - position - oldScroll;
        }

        public override void Draw()
        {
            Vector2f prev = position;
            ScrollControl prevScroll = scrollingParent;
            position = dragPosition;
            base.Draw();
            position = prev;
        }
    }
}
