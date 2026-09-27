using SysCol = System.Collections.Generic;

namespace ChaosFramework.Menu.OpenGl
{
    public partial class Control
    {
        public delegate void Action(Control sender);
        public delegate bool EnableFunction();

        protected bool visuallyHovered = false;
        public int hoverPriority { get; private set; }
        public Control activeControl { get; internal set; }

        public bool[] mouseDownOnControl { get; private set; } = [false, false, false];

        EnableFunction _enabledFunction = null;
        public EnableFunction enabledFunction
        {
            get => _enabledFunction ?? Collections.Linq.PredicateTrue;
            set => _enabledFunction = value;
        }

        public Action[] mouseDown = new Action[3];
        public Action mouseEnter, mouseLeave, lostFocus;
        public Action mouseDownLeft { get => mouseDown[0]; set => mouseDown[0] = value; }

        readonly SysCol.Dictionary<int, Action> multiClick = [];
        public Action doubleClickLeft { get => GetMultiClick(2); set => AddMultiClick(2, value); }

        public void MouseDown(int i = 0)
        {
            mouseDownOnControl[i] = true;
            mouseDown[i]?.Invoke(this);
        }

        public void AddMultiClick(int numClicks, Action handler)
            => multiClick[numClicks] = multiClick.TryGetValue(numClicks, out Action registeredHandler)
                ? registeredHandler + handler
                : handler;

        public Action GetMultiClick(int numClicks)
            => multiClick.TryGetValue(numClicks, out Action handler)
                ? handler
                : null;

        public virtual void UpdateInteraction()
        {
            if (activeControl?.enabledFunction() ?? false)
                activeControl.UpdateInteraction();
        }

        protected internal virtual void SetInputHandlers()
            => activeControl?.SetInputHandlers();

        public override void SetUpdateCalls()
        {
            base.SetUpdateCalls();
            scene.updateLayers[(int)GuiScene.UpdateLayers.SelectHoverControl].Add(SelectHoverControl);
            scene.updateLayers[(int)GuiScene.UpdateLayers.VisualUpdates].Add(UpdateHover);
        }

        public abstract bool IsHovered();

        public bool IsHoveredRecursively()
        {
            if (scene.hoveringControl == this)
                return true;

            foreach (Control child in EnumerateChildren<Control>(false))
                if (child.IsHoveredRecursively())
                    return true;

            return false;
        }

        internal void UpdateHoverPriority()
        {
            GetRootComponent(out int tmp);
            hoverPriority = tmp;
        }

        void UpdateHover()
            => visuallyHovered = IsHovered();

        void SelectHoverControl()
        {
            for (int i = 0; i < mouseDownOnControl.Length; i++)
                mouseDownOnControl[i] &= scene.mouseDown[i] > 0;

            if ((scene.hoveringControl == null || hoverPriority > scene.hoveringControl.hoverPriority) && IsHovered())
                scene.hoveringControl = this;
        }
    }
}
