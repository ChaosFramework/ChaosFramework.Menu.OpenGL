using System;
using System.Reflection;
using System.Xml;
using ChaosFramework.Collections;
using ChaosFramework.Components;
using ChaosFramework.Math.Vectors;
using ChaosUtil.Reflection;
using ChaosUtil.Serialization.Text;
using SysCol = System.Collections.Generic;

namespace ChaosFramework.Menu.OpenGl
{
    public abstract partial class LayoutContainer
        : Control
    {
        public delegate bool NeedsLayout(LayoutContainer sender);
        public delegate void UpdateLayoutConstraints(LayoutContainer sender);

        public const float DEFAULT_MARGIN = 0.025f;

        static readonly SysCol.Dictionary<string, Type> controlTypes = [];
        static LayoutContainer currentContainer = null;
        static void NoConstraints(LayoutContainer _) { }

        static LayoutContainer()
        {
            foreach (Type t in AssemblyManager.SubTypesOf(typeof(Control)))
                CreateControlTypePath(t);

            foreach (Type t in AssemblyManager.SubTypesOf(typeof(ControlNode)))
                CreateControlTypePath(t);

            Parse.AddParser<ControlNode>(ParseControlNode);
            Parse.AddParser<ScrollControl>(ParseScrollControl);
        }

        static void CreateControlTypePath(Type t)
        {
            ControlAttribute attr = t.GetCustomAttribute<ControlAttribute>();
            string path = attr?.controlPath ?? t.Name;

            if (controlTypes.TryGetValue(path, out Type compareType))
            {
                if (compareType != t)
                    throw new ArgumentException($"Control Type Name Conflict '{t.FullName}' vs '{compareType.FullName}'", nameof(t));
            }
            else
                controlTypes[path] = t;
        }

        static bool ParseControlNode(string str, out ControlNode node)
        {
            LayoutContainer searchingContainer = currentContainer;
            while (searchingContainer != null)
            {
                foreach (SysCol.KeyValuePair<ControlNode, string> kvp in searchingContainer.controlNodes)
                    if (kvp.Value == str)
                    {
                        node = kvp.Key;
                        return true;
                    }

                searchingContainer = searchingContainer.parent as LayoutContainer;
            }

            node = null;
            return false;
        }

        static bool ParseScrollControl(string str, out ScrollControl scroll)
        {
            LayoutContainer searchingContainer = currentContainer;
            while (searchingContainer != null)
            {
                foreach (SysCol.KeyValuePair<Control, XmlNode> kvp in searchingContainer.controlLayoutData)
                    if (typeof(ScrollControl).IsAssignableFrom(kvp.Key.GetType()) && kvp.Value.Attributes["id"]?.Value == str)
                    {
                        scroll = (ScrollControl)kvp.Key;
                        return true;
                    }

                searchingContainer = searchingContainer.parent as LayoutContainer;
            }

            scroll = null;
            return false;
        }

        public static LinkedList<Control> CreateMenuInControl(LayoutContainer parent, XmlNode masterNode, bool layout = true)
        {
            LinkedList<Control> output = [];
            if (masterNode == null)
                return output;

            parent.SetControlProperties(masterNode);
            ApplyControlProperties(parent, parent.controlProperties);

            foreach (XmlNode childNode in masterNode.ChildNodes)
                if (controlTypes.TryGetValue(childNode.Name, out Type nodeType))
                {
                    object potentialControlNode;
                    if (typeof(ControlNode).IsAssignableFrom(nodeType) && !typeof(Control).IsAssignableFrom(nodeType))
                        potentialControlNode = DefaultActivator.CreateInstance(nodeType);
                    else
                    {
                        Control c = parent.CreateControl(nodeType, parent.position, parent.size);
                        output.Add(c);
                        c.width = parent.size.x * 2;
                        c.height = parent.size.y * 2;
                        c.x = parent.position.x;
                        c.y = parent.position.y;
                        potentialControlNode = c;
                        ApplyControlProperties(c, childNode);
                        parent.controlLayoutData[c] = childNode;

                        XmlAttribute idAttribute = childNode.Attributes["id"];
                        if (idAttribute != null)
                            parent.AddChildControl(idAttribute.Value, c);

                        if (c is LayoutContainer nestedLayout)
                        {
                            LayoutContainer previousContainer = currentContainer;
                            currentContainer = nestedLayout;
                            CreateMenuInControl(nestedLayout, childNode, layout);
                            currentContainer = previousContainer;
                        }
                    }

                    if (potentialControlNode is ControlNode ctrlNode)
                    {
                        ctrlNode.myNode = childNode;
                        XmlAttribute idAttr = childNode.Attributes["id"];
                        if (idAttr != null)
                            parent.controlNodes[ctrlNode] = idAttr.Value;
                    }
                }

            if (layout)
                parent.Layout();

            return output;
        }

        static void ApplyControlProperties(Control c, XmlNode controlProperties)
        {
            const BindingFlags ALL_INSTANCE_MEMBERS = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            Type t = c.GetType();
            Delegate parser;
            foreach (XmlAttribute attr in controlProperties.Attributes)
            {
                FieldInfo field = t.GetField(attr.Name, ALL_INSTANCE_MEMBERS);
                if (field != null)
                {
                    if (!Parse.TryGetParser(field.FieldType, out parser))
                        throw new MalformedLanguageException($"No parser found for {field.FieldType.FullName}");

                    object[] @params = [attr.Value, null];
                    parser.DynamicInvoke(@params);
                    field.SetValue(c, @params[1]);
                }

                PropertyInfo props = t.GetProperty(attr.Name, ALL_INSTANCE_MEMBERS);
                if (props != null && props.CanWrite)
                {
                    if (!Parse.TryGetParser(props.PropertyType, out parser))
                        throw new MalformedLanguageException($"No parser found for {props.PropertyType.FullName}");

                    object[] @params = [attr.Value, null];
                    parser.DynamicInvoke(@params);
                    props.SetValue(c, @params[1], null);
                }
            }
        }

        public Margin margin = DEFAULT_MARGIN;
        public float marginLeft { set => margin.left = value; get => margin.left; }
        public float marginRight { set => margin.right = value; get => margin.right; }
        public float marginBottom { set => margin.bottom = value; get => margin.bottom; }
        public float marginTop { set => margin.top = value; get => margin.top; }

        NeedsLayout needsLayout = Linq.PredicateTrue;
        UpdateLayoutConstraints updateLayoutConstraints = NoConstraints;

        internal readonly SysCol.Dictionary<Control, XmlNode> controlLayoutData = [];
        protected readonly LinkedList<LayoutContainer> childContainers = [];
        protected XmlNode controlProperties { get; private set; }
        readonly SysCol.Dictionary<string, LinkedList<Control>> controlByName = [];
        readonly SysCol.Dictionary<ControlNode, string> controlNodes = [];

        public void SetLayoutConstraints(NeedsLayout needsLayout, UpdateLayoutConstraints updateLayoutConstraints)
        {
            this.needsLayout = needsLayout ?? Linq.PredicateTrue;
            this.updateLayoutConstraints = updateLayoutConstraints ?? NoConstraints;
        }

        public virtual void SetControlProperties(XmlNode controlProperties)
        {
            this.controlProperties = controlProperties;
            ApplyControlProperties(this, controlProperties);
        }

        public Control GetControlById(params string[] path)
            => GetControlById(path, 0);

        Control GetControlById(string[] path, int i)
        {
            if (!controlByName.TryGetValue(path[i], out LinkedList<Control> c))
                return null;
            else if (i < path.Length - 1)
                if (c.length != 1)
                    return null;
                else if (c.first is not LayoutContainer container)
                    return null;
                else
                    return container.GetControlById(path, i + 1);
            else if (c.length == 1)
                return c.first;
            else
                return null;
        }

        public override Control CreateControl(Type type, Vector2f position, Vector2f size, string text = null)
        {
            Control output = base.CreateControl(type, position, size, text);
            if (output is LayoutContainer container)
                childContainers.Add(container);

            return output;
        }

        public void SetControlLayoutData(Control c, XmlNode controlProperties)
        {
            void cleanup()
            {
                controlLayoutData.Remove(c);
                c.RemoveOnDispose(cleanup);
            }

            controlLayoutData[c] = controlProperties;
            c.AddOnDispose(cleanup);
        }

        public XmlNode GetControlLayoutData(Control c)
            => controlLayoutData[c];

        public void Update(Time time)
        {
            foreach (SysCol.KeyValuePair<ControlNode, string> node in controlNodes)
                node.Key.Update(time);

            foreach (LayoutContainer child in childContainers)
                if (child.doUpdate)
                    child.Update(time);
        }

        public void Layout()
        {
            if (!needsLayout(this))
                return;

            PerformLayoutInternal();
            foreach (LayoutContainer child in childContainers)
                child.Layout();

            GetParent<ScrollControl>()?.UpdateScroll();
            (this as ScrollControl)?.UpdateScroll();

            updateLayoutConstraints(this);
        }

        void AddChildControl(string name, Control c)
        {
            LinkedList<Control> controls = controlByName.GetOrCreateValue(name);

            void cleanup()
            {
                controls.Remove(c);
                c.RemoveOnDispose(cleanup);
            }

            c.AddOnDispose(cleanup);
            controls.AddUnique(c);
            (parent as LayoutContainer)?.AddChildControl(name, c);
        }

        protected abstract void PerformLayoutInternal();

        public override void Draw() { }

        public override bool IsHovered() => false;

        protected override void OnDisposeChildren()
        {
            controlLayoutData.Clear();
            controlByName.Clear();
            childContainers.Clear();
        }

        protected Margin GetMargin(XmlNode childProperties)
        {
            XmlAttribute attrMargin = null,
                         attrX = null,
                         attrY = null,
                         attrLeft = null,
                         attrRight = null,
                         attrBottom = null,
                         attrTop = null;

            if (childProperties != null)
            {
                string typeName = GetType().Name;
                attrMargin = childProperties.Attributes[$"{typeName}.margin"];
                attrX = childProperties.Attributes[$"{typeName}.marginX"];
                attrY = childProperties.Attributes[$"{typeName}.marginY"];
                attrLeft = childProperties.Attributes[$"{typeName}.marginLeft"];
                attrRight = childProperties.Attributes[$"{typeName}.marginRight"];
                attrBottom = childProperties.Attributes[$"{typeName}.marginBottom"];
                attrTop = childProperties.Attributes[$"{typeName}.marginTop"];
            }

            if (attrMargin == null || !Parse.GetParser<Margin>().Invoke(attrMargin.Value, out Margin output))
                output = margin;

            if (attrX != null && Parse.GetParser<float>().Invoke(attrX.Value, out float tmp))
                margin.left = margin.right = tmp;
            if (attrY != null && Parse.GetParser<float>().Invoke(attrY.Value, out tmp))
                margin.bottom = margin.top = tmp;

            if (attrLeft != null && Parse.GetParser<float>().Invoke(attrLeft.Value, out tmp))
                margin.left = tmp;
            if (attrRight != null && Parse.GetParser<float>().Invoke(attrRight.Value, out tmp))
                margin.right = tmp;
            if (attrTop != null && Parse.GetParser<float>().Invoke(attrTop.Value, out tmp))
                margin.top = tmp;
            if (attrBottom != null && Parse.GetParser<float>().Invoke(attrBottom.Value, out tmp))
                margin.bottom = tmp;

            return margin;
        }
    }
}
