using System;
using ChaosFramework.Components;
using ChaosFramework.Graphics.OpenGl;
using ChaosFramework.Graphics.OpenGl.AssetContainers;
using ChaosFramework.Graphics.OpenGl.Instancing;
using ChaosFramework.Math;
using ChaosFramework.Math.Vectors;
using static ChaosFramework.Math.Signs;

namespace ChaosFramework.Menu.OpenGl.Controls.Layout
{
    [Control($"{nameof(Layout)}.{nameof(GridCellVisualizer)}")]
    public class GridCellVisualizer : Grid.Visualizer
    {
        public enum Mode
        {
            CheckerBoard,
            Rows,
            Columns
        }

        public Vector2f textureScale = new(1, 1);
        public Vector4f color1 = 1, color2 = new(0.7f, 0.7f, 0.7f, 1);
        public bool textureRepeat = false;
        public Mode mode = Mode.CheckerBoard;

        MatrixInstancer instancer;
        ShaderContainer.Entry shader;

        protected override void Create(CreateParameters cparams)
        {
            base.Create(cparams);
            instancer = new MatrixInstancer(scene.context.graphics, ["INSTANCE_COLOR"], 0);
            shader = scene.context.shaders.grid;
            texture = scene.context.texProvider.gridCell;
        }

        public override void Draw()
        {
            instancer.Reset();
            scene.view.SetValues(shader, Matrix.IDENTITY, Matrix.IDENTITY);
            shader.SetValue("screenSize", new Vector2f(scene.context.window.width, scene.context.window.height));
            shader.SetValue("textureScale", textureScale);
            shader.SetValue("tex", texture);

            ReadOnlySpan<float> h = myGrid.GetHorizontalSeparators();
            ReadOnlySpan<float> v = myGrid.GetVerticalSeparators();
            for (int y = 0; y < myGrid.rows; y++)
            {
                float posY = (v[y] + v[y + 1]) * 0.5f;
                float szY = Abs(v[y + 1] - v[y]);
                for (int x = 0; x < myGrid.columns; x++)
                    instancer.AddInstance(
                        Matrix.Scaling(Abs(h[x + 1] - h[x]) * 0.5f, szY * 0.5f, 1)
                        * Matrix.Translation(
                            (h[x] + h[x + 1]) * 0.5f + scrolledPosition.x - position.x,
                            posY + scrolledPosition.y - position.y
                            ),
                        GetColor(x, y)
                        );
            }
            Sprite.DrawPositionInstanced(scene.context.graphics, shader, instancer, textureRepeat ? "Repeat" : "Stretch");
        }

        Vector4f GetColor(int x, int y)
            => mode switch
            {
                Mode.Columns => (x % 2) == 0 ? color1 : color2,
                Mode.Rows => (y % 2) == 0 ? color1 : color2,
                _ => (x % 2) == (y % 2) ? color1 : color2,
            };

        protected override void DoDispose()
        {
            base.DoDispose();
            instancer.Dispose();
        }
    }
}
