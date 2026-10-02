using System.Linq;

namespace ChaosFramework.Menu.OpenGl.Controls.Layout
{
    [Control($"{nameof(Controls.Layout)}.{nameof(Box)}")]
    public class Box : LayoutContainer
    {
        protected override void PerformLayoutInternal()
        {
            foreach (Control control in children.OfType<Control>())
            {
                control.position = position;
                control.size = size;
            }
        }
    }
}
