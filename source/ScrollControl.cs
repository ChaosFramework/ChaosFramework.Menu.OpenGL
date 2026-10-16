using ChaosFramework.Math.Vectors;

namespace ChaosFramework.Menu.OpenGl
{
    public interface ScrollControl
    {
        Vector2f offset { get; set; }
        void UpdateScroll();
    }
}
