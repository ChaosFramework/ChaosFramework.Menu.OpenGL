using System;

namespace ChaosFramework.Menu.OpenGl
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ControlAttribute(string controlPath)
        : Attribute
    {
        public readonly string controlPath = controlPath;
    }
}
