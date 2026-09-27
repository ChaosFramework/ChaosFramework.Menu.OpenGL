using System.Reflection;
using System.Xml;
using ChaosFramework.Collections;
using ChaosFramework.Components;
using ChaosFramework.Graphics.Colors;
using ChaosFramework.Graphics.Text.Formatting;
using static ChaosFramework.Math.Clamping;

namespace ChaosFramework.Menu.OpenGl.Controls
{
    public partial class SettingsGrid : Layout.Box
    {
        public readonly LinkedList<Setting> settings = [];

        public float fontSize = 0.0333f;
        public Rgba labelColor = Rgba.NAN;

        XmlDocument doc;
        Layout.Grid grid;
        float occupiedByNameAndSetting = 0;
        LinkedList<Line> lines;

        protected override void Create(CreateParameters cparams)
        {
            base.Create(cparams);
            doc = new XmlDocument();
            doc.LoadXml(Properties.Resources.Layout_SettingsGrid);
        }

        public void Build()
        {
            DisposeChildren();

            CreateMenuInControl(this, doc.SelectSingleNode("Root/SettingsGrid"), false);

            grid = GetControlById("settingsGrid") as Layout.Grid;
            XmlNode templateNode = doc.SelectSingleNode("Root/Templates");

            XmlNode gridNode = templateNode.SelectSingleNode("Grid");
            XmlAttribute marginAttr = (XmlAttribute)gridNode.Attributes.GetNamedItem("margin");
            marginAttr.Value = (fontSize / 2).ToString();

            CreateMenuInControl(grid, gridNode, false);

            XmlNode dummyRow = gridNode.SelectSingleNode("Row[@index='-1']");
            int row = 0;
            float totalHeight = 0;
            float[] maxWidth = new float[2];

            lines = [];

            bool hasDescription = false;
            foreach (Setting it in settings)
                if (it != null)
                {
                    Setting setting = it;

                    string settingName = setting.settingName;
                    if (settingName.EndsWith(Scope.ESCAPE_CODE.ToString()))
                    {
                        int i = settingName.Length - 1;
                        while (i >= 0 && settingName[i] == Scope.ESCAPE_CODE)
                            i -= 3;

                        settingName = settingName.Insert(i + 1, ":");
                    }
                    else
                        settingName += ":";

                    Label lblName = grid.CreateControl<Label>(0, fontSize, settingName);
                    lblName.instanceScrollRelevant = false;
                    lblName.enableColorCodes = setting.enableColorCodes;
                    if (!labelColor.IsNaN())
                        lblName.textColor = labelColor;

                    XmlNode nameNode = templateNode.SelectSingleNode("Name").Clone();
                    nameNode.Attributes["Grid.index"].Value = $"0,{row}";
                    grid.SetControlLayoutData(lblName, nameNode);
                    lblName.UpdateTextGeometry();
                    maxWidth[0] = Max(maxWidth[0], lblName.bounds.width);

                    ValueControl control = setting.GetControl(grid, fontSize);
                    ((Control)control).instanceScrollRelevant = false;
                    XmlNode controlNode = templateNode.SelectSingleNode("Value").Clone();
                    controlNode.Attributes["Grid.index"].Value = $"1,{row}";

                    grid.SetControlLayoutData((Control)control, controlNode);
                    maxWidth[1] = Max(maxWidth[1], ((Control)control).bounds.width);

                    System.Type valueType = setting.settingType;
                    System.Type settingType = typeof(Setting<>).MakeGenericType(valueType);
                    System.Type controlType = typeof(ValueControl<>).MakeGenericType(valueType);

                    FieldInfo settingValueSetterField = settingType.GetField(nameof(Setting<object>.setValue));
                    System.Delegate settingValueSetter = (System.Delegate)settingValueSetterField.GetValue(setting);

                    FieldInfo settingValueGetterField = settingType.GetField(nameof(Setting<object>.getValue));
                    System.Delegate settingValueGetter = (System.Delegate)settingValueGetterField.GetValue(setting);

                    MethodInfo controlValueSetterMethod = controlType.GetMethod(nameof(ValueControl<object>.SetValue));
                    MethodInfo controlValueGetterMethod = controlType.GetMethod(nameof(ValueControl<object>.GetValue));

                    void ownValueChanged(ValueControl _)
                        => settingValueSetter.DynamicInvoke(
                            controlValueGetterMethod.Invoke(control, ChaosUtil.Primitives.Array<object>.empty)
                            );

                    PropertyInfo valueChangedHandlerProp = control.GetType().GetProperty(nameof(ValueControl<object>.valueChanged));
                    valueChangedHandlerProp.SetValue(
                        control,
                        (System.Action<ValueControl>)valueChangedHandlerProp.GetValue(control) + ownValueChanged
                        );

                    controlValueSetterMethod.Invoke(control, [settingValueGetter.DynamicInvoke()]);

                    Label lblDescr = null;
                    if (setting.description != null && setting.description.Trim().Length > 0)
                    {
                        hasDescription = true;
                        lblDescr = grid.CreateControl<Label>(0, fontSize, setting.description);
                        lblDescr.instanceScrollRelevant = false;
                        lblDescr.enableColorCodes = setting.enableColorCodes;
                        if (!labelColor.IsNaN())
                            lblDescr.textColor = labelColor;

                        XmlNode descrNode = templateNode.SelectSingleNode("Descr").Clone();
                        descrNode.Attributes["Grid.index"].Value = $"2,{row}";
                        grid.SetControlLayoutData(lblDescr, descrNode);
                        lblDescr.UpdateTextGeometry();
                    }

                    XmlNode rowNode = dummyRow.Clone();
                    Line line = new(fontSize, lblName, control, lblDescr, rowNode);
                    lines.Add(line);
                    totalHeight += line.height;
                    rowNode.Attributes["index"].Value = row.ToString();
                    gridNode.AppendChild(rowNode);
                    row++;

                    line.Update();
                }

            height = totalHeight;

            if (row == 0)
                grid.Dispose();
            else
            {
                gridNode.Attributes["rows"].Value = row.ToString();
                gridNode.Attributes["columns"].Value = (hasDescription ? 3 : 2).ToString();
                for (int i = 0; i < maxWidth.Length; i++)
                {
                    maxWidth[i] += fontSize;
                    XmlNode column = gridNode.SelectSingleNode($"Column[@index='{i}']");
                    if (column != null)
                        column.Attributes["width"].Value = maxWidth[i].ToString();
                }

                occupiedByNameAndSetting = 0;
                foreach (float col in maxWidth)
                    occupiedByNameAndSetting += col;

                grid.SetControlProperties(gridNode);
            }

            Layout();
        }

        protected override void PerformLayoutInternal()
        {
            base.PerformLayoutInternal();
            if (grid != null)
            {
                float totalHeight = 0;
                float descrWidth = (width - occupiedByNameAndSetting) / fontSize - 1;
                foreach (Line line in lines)
                {
                    if (line.lblDescr != null)
                    {
                        line.lblDescr.maxLineWidth = descrWidth;
                        line.lblDescr.UpdateTextGeometry();
                    }

                    line.Update();
                    totalHeight += line.height;
                }

                grid.Layout();
                grid.width = width;
                height = grid.height = totalHeight;
                grid.Layout();
            }
        }
    }
}
