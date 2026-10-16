using ChaosFramework.Components;
using Xml = System.Xml;

namespace ChaosFramework.Menu.OpenGl.Controls.Layout
{
    public abstract class ExpanderBase : Box
    {
        public event Action Built;

        public bool expanded { get; private set; }

        protected Xml.XmlNode nodeExpanded, nodeCollapsed;

        protected Box panel;
        protected Box headerBox, contentPanel;
        protected Button btnExpand;

        protected bool building;

        protected abstract string headerContentNodeName { get; }

        protected override void Create(CreateParameters cparams)
        {
            base.Create(cparams);
            panel = CreateControl<Box>(0, 1);
        }

        protected abstract void ProcessXml();
        public abstract float ComputeHeight();

        public void Build(bool layout)
        {
            building = true;

            panel.DisposeChildren();

            ProcessXml();

            height = ComputeHeight();
            CreateMenuInControl(panel, expanded ? nodeExpanded : nodeCollapsed, layout);

            headerBox = panel.GetControlById("Expander_Header") as Box;
            contentPanel = panel.GetControlById("Expander_Content") as Box;
            btnExpand = panel.GetControlById("Expander_Expand") as Button;

            btnExpand.texture = expanded
                ? scene.context.texProvider.expanderButtonExpanded
                : scene.context.texProvider.expanderButtonCollapsed;

            Xml.XmlNode headerNode = controlProperties.SelectSingleNode(headerContentNodeName);
            if (headerNode != null)
                CreateMenuInControl(headerBox, headerNode, false);

            Xml.XmlNode contentNode = controlProperties.SelectSingleNode("Expander.Content");
            if (contentNode != null && contentPanel != null)
                CreateMenuInControl(contentPanel, contentNode, false);


            if (btnExpand != null)
                btnExpand.mouseDownLeft += ToggleExpand;

            if (layout)
                GetParent<LayoutContainer>()?.Layout();

            building = false;

            Built?.Invoke(this);
        }

        void ToggleExpand(Control _)
        {
            if (expanded)
                Collapse();
            else
                Expand();
        }

        public void Expand()
        {
            if (!expanded)
            {
                expanded = true;
                Build(true);
            }
        }

        public void Collapse()
        {
            if (expanded)
            {
                expanded = false;
                Build(true);
            }
        }

        protected override void PerformLayoutInternal()
        {
            if (!building && headerBox == null)
                Build(true);

            base.PerformLayoutInternal();
        }
    }
}
