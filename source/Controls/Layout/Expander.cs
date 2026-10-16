using Xml = System.Xml;

namespace ChaosFramework.Menu.OpenGl.Controls.Layout
{
    [Control($"{nameof(Controls.Layout)}.{nameof(Expander)}")]
    public class Expander : ExpanderBase
    {
        public float expandedHeight = 1;
        public float collapsedHeight = 0.15f;

        protected override string headerContentNodeName
            => "Expander.Header";

        public override float ComputeHeight()
            => expanded ? expandedHeight : collapsedHeight;

        protected override void ProcessXml()
        {
            if (nodeExpanded == null)
            {
                Xml.XmlDocument doc = new();
                doc.LoadXml(Properties.Resources.Layout_Expander);
                LanguagePack.ProcessMacros([doc]);

                nodeExpanded = doc.SelectSingleNode("Root/Expander_Expanded");
                nodeCollapsed = doc.SelectSingleNode("Root/Expander_Collapsed");
            }
        }
    }
}
