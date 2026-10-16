using System.Xml;
using static ChaosFramework.Math.Clamping;

namespace ChaosFramework.Menu.OpenGl.Controls
{
    public partial class SettingsGrid
    {
        class Line(float fontSize, Label lblName, ValueControl ctrl, Label lblDescr, XmlNode rowNode)
        {
            public readonly float fontSize = fontSize;
            public readonly Label lblName = lblName;
            public readonly ValueControl ctrl = ctrl;
            public readonly Label lblDescr = lblDescr;
            public readonly XmlNode rowNode = rowNode;

            public float height
            {
                get
                {
                    float height = Max(
                        lblName.bounds.height,
                        ((Control)ctrl).bounds.height
                        );

                    if (lblDescr != null && lblDescr.height > height)
                        height = lblDescr.bounds.height;

                    height += fontSize;
                    return height;
                }
            }

            public void Update()
                => rowNode.Attributes["height"].Value = height.ToString();
        }
    }
}
