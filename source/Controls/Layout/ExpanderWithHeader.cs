using System.Linq;
using ChaosUtil.Reflection;
using SysCol = System.Collections.Generic;
using Xml = System.Xml;

namespace ChaosFramework.Menu.OpenGl.Controls.Layout
{
    [Control($"{nameof(Controls.Layout)}.{nameof(ExpanderWithHeader)}")]
    public class ExpanderWithHeader : ExpanderBase
    {
        [System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Property)]
        class MacroArg : System.Attribute;

        [MacroArg()]
        public float expandedHeaderHeight = 0.15f;

        [MacroArg()]
        public float collapsedHeaderHeight = 0.15f;

        [MacroArg()]
        public float headerToContentMargin = 0.01f;

        [MacroArg()]
        public float contentHeight = 1f;

        [MacroArg()]
        public float collapseButtonSize => collapsedHeaderHeight / 3;

        readonly SysCol.Dictionary<string, string> currentArgs = [];

        protected override string headerContentNodeName
            => expanded ? "Expander.ExpandedHeader" : "Expander.CollapsedHeader";

        public override float ComputeHeight()
            => expanded
                ? expandedHeaderHeight + headerToContentMargin + contentHeight
                : collapsedHeaderHeight;

        protected override void ProcessXml()
        {
            if (NeedsXmlUpdate())
            {
                currentArgs.Clear();

                Xml.XmlDocument doc = new();
                doc.LoadXml(Properties.Resources.Layout_SpecialExpander);

                Xml.XmlNode rootNode = doc.SelectSingleNode("Root");

                Xml.XmlNode nodeMacro = doc.CreateNode(Xml.XmlNodeType.Element, "Macro", doc.NamespaceURI);
                rootNode.AppendChild(nodeMacro);

                Xml.XmlAttribute attrId = doc.CreateAttribute("id");
                attrId.Value = "Expander";
                nodeMacro.Attributes.Append(attrId);

                Xml.XmlNode nodeArgs = doc.CreateNode(Xml.XmlNodeType.Element, "Args", doc.NamespaceURI);
                foreach (SysCol.KeyValuePair<string, string> arg in EnumerateMacroArgs())
                {
                    Xml.XmlAttribute attrArg = doc.CreateAttribute(arg.Key);
                    attrArg.Value = arg.Value;
                    nodeArgs.Attributes.Append(attrArg);
                    currentArgs[arg.Key] = arg.Value;
                }
                nodeMacro.AppendChild(nodeArgs);

                LanguagePack.ProcessMacros([doc]);

                nodeExpanded = doc.SelectSingleNode("Root/Expander_Expanded");
                nodeCollapsed = doc.SelectSingleNode("Root/Expander_Collapsed");
            }
        }

        bool NeedsXmlUpdate(SysCol.KeyValuePair<string, string> arg)
            => !currentArgs.TryGetValue(arg.Key, out string tmp) || tmp != arg.Value;

        bool NeedsXmlUpdate()
            => EnumerateMacroArgs().Any(NeedsXmlUpdate);

        SysCol.IEnumerable<SysCol.KeyValuePair<string, string>> EnumerateMacroArgs()
        {
            foreach (System.Reflection.FieldInfo field in typeof(ExpanderWithHeader).GetFields())
                if (field.GetAttributes<MacroArg>().Length > 0)
                    yield return new SysCol.KeyValuePair<string, string>(field.Name, field.GetValue(this).ToString());

            foreach (System.Reflection.PropertyInfo property in typeof(ExpanderWithHeader).GetProperties())
                if (property.CanRead && property.GetAttributes<MacroArg>().Length > 0)
                    yield return new SysCol.KeyValuePair<string, string>(property.Name, property.GetValue(this).ToString());
        }
    }
}
