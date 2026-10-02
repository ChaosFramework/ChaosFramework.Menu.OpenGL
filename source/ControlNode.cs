using System.Xml;
using ChaosFramework.Components;
using ChaosUtil.Reflection;

namespace ChaosFramework.Menu.OpenGl
{
    [AssemblyManager.ListSubTypes]
    public interface ControlNode
    {
        XmlNode myNode { get; set; }

        void Update(Time time);
        void AddControl(Control child);
        void AddChild(ControlNode child);
    }
}
