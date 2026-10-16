using ChaosFramework.Components;
using ChaosFramework.Graphics;
using ChaosFramework.Graphics.Colors;
using ChaosFramework.Graphics.OpenGl;
using ChaosFramework.Graphics.OpenGl.Model;
using ChaosFramework.Graphics.OpenGl.Text;
using ChaosFramework.Graphics.Text;
using ChaosFramework.Math;
using ChaosFramework.Math.Vectors;
using ChaosFramework.Shapes;
using ChaosUtil.Primitives;
using static ChaosFramework.Math.Clamping;

namespace ChaosFramework.Menu.OpenGl.Controls
{
    public class RichLabel : Label
    {
        public delegate string ColorText(string text);

        static readonly LayoutInfo CURSOR_LAYOUT = new(Align.Center, true);

        static string UncoloredText(string text)
            => text;

        public ColorText colorText = UncoloredText;

        public bool canSelect = true;
        public int selectionCursor;

        Mesh selectionMesh;
        MeshBuffers selectionMeshBuffers;
        int lastBuiltSelectLow = -1, lastBuiltSelectHi = -1;
        string lastBuiltSelectText;

        string _coloredText;
        public override string text
        {
            get => base.text;
            set
            {
                _coloredText = colorText(value);
                base.text = value;
            }
        }

        internal bool showCursor = false;
        int cursorIndex;
        Vector2f cursor;
        readonly TextMesh[] cursorGeo = new TextMesh[2];

        protected override void Create(CreateParameters cparams)
        {
            base.Create(cparams);
            text = string.Empty;

            MeshData nothing = new(Array<Vector3f>.empty, Array<Vector3f>.empty, Array<Vector2f>.empty, Array<uint>.empty);
            selectionMeshBuffers = new MeshBuffers(scene.context.graphics.dispatcher, nothing);
            selectionMesh = new Mesh(nothing, selectionMeshBuffers);
        }

        public void SetCursor(int cursorIndex)
        {
            Vector2f cursorInText = textGeometry.geo.GetCursorPosFromIndex(this.cursorIndex = cursorIndex);
            cursor = cursorInText;
            cursor.x *= size.x;
            cursor.y *= size.y;
            cursor += scrolledPosition;
        }

        public override void UpdateTextGeometry()
        {
            LayoutInfo layout = new(
                textAlign,
                false,
                enableColorCodes,
                false,
                letterDistance,
                new[] { 2.5f },
                maxLineWidth
                );
            font.content.UpdateText(ref _textGeometry, enableColorCodes ? _coloredText : text, layout);
            SetCursor(cursorIndex);
        }

        public override void Draw()
        {
            UpdateTextGeometry();
            Matrix myTransform = Matrix.Scaling(size, 1) * Matrix.Translation(scrolledPosition);
            scene.view.SetValues(fontShader, myTransform);

            int selectionStart = Clamp(0, text.Length, selectionCursor);
            if (canSelect && selectionStart != cursorIndex)
            {
                int selectLow = System.Math.Min(selectionStart, cursorIndex);
                int selectHigh = System.Math.Max(selectionStart, cursorIndex);

                if (lastBuiltSelectLow != selectLow || lastBuiltSelectHi != selectHigh || text != lastBuiltSelectText)
                {
                    int lineIndexLow = textGeometry.geo.lineStartIndices.Length - 1;
                    int lineIndexHigh = lineIndexLow;
                    for (int i = 0; i < textGeometry.geo.lineStartIndices.Length; i++)
                        if (textGeometry.geo.lineStartIndices[i] > selectLow)
                        {
                            lineIndexLow = i - 1;
                            for (int l = lineIndexLow; l < textGeometry.geo.lineStartIndices.Length; l++)
                                if (textGeometry.geo.lineStartIndices[l] > selectHigh)
                                {
                                    lineIndexHigh = l - 1;
                                    break;
                                }

                            break;
                        }

                    Vector3f[] verts = new Vector3f[4 * (lineIndexHigh - lineIndexLow + 1)];
                    int vertCounter = 0;
                    TextGeometry geo = textGeometry.geo;
                    for (int i = lineIndexLow; i <= lineIndexHigh; i++)
                    {
                        Vector2f start = geo.GetCursorPosFromIndex(Max(selectLow, geo.lineStartIndices[i]));
                        Vector2f end = geo.GetCursorPosFromIndex(Min(selectHigh, geo.lineStartIndices[i] + geo.lines[i].Length));
                        verts[vertCounter++] = new Vector3f(start.x, start.y + 0.5f, 0);
                        verts[vertCounter++] = new Vector3f(end.x, end.y + 0.5f, 0);
                        verts[vertCounter++] = new Vector3f(start.x, start.y - 0.5f, 0);
                        verts[vertCounter++] = new Vector3f(end.x, end.y - 0.5f, 0);
                    }

                    selectionMesh = new Mesh(
                        new MeshData(
                            verts,
                            null,
                            Sprite.CreateQuadIndices(verts.Length / 4)
                            ),
                        selectionMeshBuffers
                        );

                    lastBuiltSelectLow = selectLow;
                    lastBuiltSelectHi = selectHigh;
                    lastBuiltSelectText = text;
                }

                fontShader.SetValue("color", new Rgba(0.5f));
                selectionMesh.Draw(fontShader, "Selection");
            }

            font.content.SetValues(fontShader);
            fontShader.SetValue("color", textColor);
            textGeometry.DrawText(fontShader, enableColorCodes ? "HUDColored" : "HUD");
            if (showCursor)
            {
                font.content.UpdateText(ref cursorGeo[0], UnicodeChars.WriteCursorA.GetUnicodeString(), CURSOR_LAYOUT);
                font.content.UpdateText(ref cursorGeo[1], UnicodeChars.WriteCursorB.GetUnicodeString(), CURSOR_LAYOUT);
                scene.view.SetValues(fontShader, Matrix.Scaling(size, 1) * Matrix.Translation(cursor));
                bool cursorType = (ftime.totalTime % (RichTextBox.CURSOR_BLINK_INTERVAL * 2)) > RichTextBox.CURSOR_BLINK_INTERVAL;
                cursorGeo[cursorType ? 0 : 1].DrawText(fontShader, "HUD");
            }
        }

        public override bool IsHovered()
            => false;

        protected override void DoDispose()
        {
            base.DoDispose();
            selectionMesh?.Dispose();
            selectionMeshBuffers?.Dispose();
        }
    }
}
