namespace ChaosFramework.Menu.OpenGl.Controls
{
    partial class SettingsGrid
    {
        [method: ChaosAnalyzers.ClassIntegrity.ExplicitConstructor]
        public abstract class ControlProvider<T>(object[] fieldTypeArgs)
        {
            public readonly object[] fieldTypeArgs = fieldTypeArgs;

            public abstract ValueControl<T> GetControl(LayoutContainer target, float fontSize);
        }
    }
}
