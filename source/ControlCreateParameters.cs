using System.IO;
using ChaosFramework.Components;
using ChaosFramework.IO;
using ChaosFramework.Math.Vectors;

namespace ChaosFramework.Menu.OpenGl.Controls
{
    public class ControlCreateParameters
        : CreateParameters
    {
        public readonly Vector2f position, size;
        public readonly string text;

        public ControlCreateParameters(Vector2f position, Vector2f size, string text)
        {
            this.position = position;
            this.size = size;
            this.text = text;
        }

        public ControlCreateParameters(BinaryReader rd)
        {
            position = new Vector2f(rd.Read<float>(), rd.Read<float>());
            size = new Vector2f(rd.Read<float>(), rd.Read<float>());
            text = rd.Read<string>();
        }

        public override void Save(BinaryWriter writer)
        {
            writer.WriteAs(position.x);
            writer.WriteAs(position.y);
            writer.WriteAs(size.x);
            writer.WriteAs(size.y);
            writer.WriteAs(text);
        }
    }
}
