using ChaosFramework.Components;

namespace ChaosFramework.Menu.OpenGl.Controls.Layout
{
    public partial class Grid
    {
        public abstract class Visualizer : Image
        {
            protected Grid myGrid { get; private set; }

            protected override void Create(CreateParameters cparams)
            {
                base.Create(cparams);
                myGrid = parent as Grid;
            }

            public sealed override bool IsHovered() => false;
        }
    }
}
