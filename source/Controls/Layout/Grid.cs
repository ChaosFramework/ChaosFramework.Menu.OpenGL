using System;
using System.Linq;
using System.Xml;
using ChaosFramework.Graphics;
using ChaosFramework.Math.Vectors;
using ChaosUtil.Serialization.Text;
using static ChaosFramework.Math.Clamping;
using SysCol = System.Collections.Generic;

namespace ChaosFramework.Menu.OpenGl.Controls.Layout
{
    [Control($"{nameof(Controls.Layout)}.{nameof(Grid)}")]
    public partial class Grid : LayoutContainer
    {

        static float[] GenerateSeparators(int num, XmlNodeList nodeList, string attributeName, float low, float high, bool invert)
        {
            SysCol.Dictionary<int, float> explicitWidths = [];
            SysCol.Dictionary<int, float> partitionedWidths = [];
            float explicitWidthSum = 0, totalParts = 0;
            float[] separators = new float[num + 1];
            foreach (XmlNode colNode in nodeList)
            {
                XmlAttribute indexAttr = colNode.Attributes["index"];
                if (indexAttr == null || !(int.TryParse(indexAttr.Value, out int idx) && idx < num && idx >= 0))
                    continue;

                XmlAttribute widthAttr = colNode.Attributes[attributeName];
                if (widthAttr == null)
                    continue;

                if (float.TryParse(widthAttr.Value, out float width))
                {
                    explicitWidths[idx] = width;
                    explicitWidthSum += width;
                }
                else if (float.TryParse(widthAttr.Value.TrimEnd('*'), out width))
                {
                    partitionedWidths[idx] = width;
                    totalParts += width;
                }
                else
                {
                    partitionedWidths[idx] = 1;
                    totalParts += 1;
                }
            }

            for (int i = 0; i < num; i++)
                if (!(partitionedWidths.ContainsKey(i) || explicitWidths.ContainsKey(i)))
                {
                    partitionedWidths[i] = 1;
                    totalParts += 1;
                }

            separators[num] = high;
            float leftoverWidth = System.Math.Abs(high - low) - explicitWidthSum;
            float x = 0;
            for (int i = 0; i < num; i++)
            {
                separators[i] = low + x;
                if (!explicitWidths.TryGetValue(i, out float dx))
                    dx = partitionedWidths[i] / totalParts * leftoverWidth;

                x += invert ? -dx : dx;
            }

            return separators;
        }

        int _rows = 1, _columns = 1;
        public int rows { get => _rows; set => _rows = Max(1, value); }
        public int columns { get => _columns; set => _columns = Max(1, value); }

#pragma warning disable IDE0044 // Add readonly modifier - can be set via xml
        Align align = Align.BottomTopLeftRight;
#pragma warning restore IDE0044 // Add readonly modifier

        float[] horizontalSeparators, verticalSeparators;
        public ReadOnlySpan<float> GetHorizontalSeparators() => horizontalSeparators;
        public ReadOnlySpan<float> GetVerticalSeparators() => verticalSeparators;

        public override void SetControlProperties(XmlNode myData)
        {
            base.SetControlProperties(myData);
            if (myData.TryGetAttribute("rows", out string attrText) && int.TryParse(attrText, out int attrValue))
                rows = attrValue;

            if (myData.TryGetAttribute("columns", out attrText) && int.TryParse(attrText, out attrValue))
                columns = attrValue;
        }

        protected override void PerformLayoutInternal()
        {
            horizontalSeparators = GenerateSeparators(columns, controlProperties.SelectNodes("Column"), "width", left, right, false);
            verticalSeparators = GenerateSeparators(rows, controlProperties.SelectNodes("Row"), "height", top, bottom, true);

            foreach (Control child in children.OfType<Control>())
            {
                if (!controlLayoutData.TryGetValue(child, out XmlNode childData))
                    continue;

                Margin margin = GetMargin(childData);

                XmlAttribute attrAnchor = childData.Attributes[$"{nameof(Grid)}.align"];
                if (attrAnchor == null || !Parse.GetParser<Align>().Invoke(attrAnchor.Value, out Align anchor))
                    anchor = align;

                XmlAttribute indexAttr = childData.Attributes[$"{nameof(Grid)}.index"];
                Vector2i index = new(0, 0);
                if (indexAttr != null && Parse.GetParser<Vector2f>()(indexAttr.Value, out Vector2f v))
                    index = (Vector2i)v;

                Align horizontalAlign = anchor & Align.LeftRight;
                Align verticalAlign = anchor & Align.BottomTop;

                if (index.x >= 0 && index.x < columns)
                {
                    float lowX = horizontalSeparators[index.x] + margin.left;
                    float highX = horizontalSeparators[index.x + 1] - margin.right;
                    if (horizontalAlign == Align.LeftRight)
                    {
                        child.x = (lowX + highX) * 0.5f;
                        child.width = System.Math.Max(0, highX - lowX);
                    }
                }

                if (index.y >= 0 && index.y < rows)
                {
                    float highY = verticalSeparators[index.y] - margin.bottom;
                    float lowY = verticalSeparators[index.y + 1] + margin.top;
                    if (verticalAlign == Align.BottomTop)
                    {
                        child.y = (highY + lowY) * 0.5f;
                        child.height = System.Math.Max(0, highY - lowY);
                    }
                }

                if (index.x >= 0 && index.x < columns)
                {
                    float lowX = horizontalSeparators[index.x] + margin.left;
                    float highX = horizontalSeparators[index.x + 1] - margin.right;
                    child.x = horizontalAlign switch
                    {
                        Align.Center => (lowX + highX) * 0.5f,
                        Align.Left => lowX - (child.bounds.left - child.x),
                        Align.Right => highX - (child.bounds.right - child.x),
                        _ => child.x
                    };
                }

                if (index.y >= 0 && index.y < rows)
                {
                    float high_y = verticalSeparators[index.y] - margin.bottom;
                    float low_y = verticalSeparators[index.y + 1] + margin.top;
                    child.y = verticalAlign switch
                    {
                        Align.Center => (high_y + low_y) / 2,
                        Align.Bottom => low_y - (child.bounds.bottom - child.y),
                        Align.Top => high_y - (child.bounds.top - child.y),
                        _ => child.y
                    };
                }
            }
        }
    }
}
