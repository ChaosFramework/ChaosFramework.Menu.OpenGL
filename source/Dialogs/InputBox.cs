using System;
using ChaosFramework.Graphics;
using ChaosFramework.Math.Vectors;

namespace ChaosFramework.Menu.OpenGl.Dialogs
{
    using Controls;

    public class InputBox
        : DialogScene
    {
        public RichTextBox textbox;
        readonly Label prompt;

        public string text => textbox.text;

        public InputBox(MenuContext context, float width, float height = 0.5f)
            : base(context, new Vector2f(width, height), true)
        {
            prompt = panel.CreateControl<Label>(
                new Vector2f(panel.position.x, panel.top - 0.1f),
                0.05f,
                "Enter text"
                );

            prompt.textAlign = Align.Center;
            textbox = panel.CreateControl<RichTextBox>(
                panel.position,
                new Vector2f(panel.size.x - 0.05f, 0.1f),
                string.Empty
                );
            textbox.fontSize = 0.07f;
            textbox.textAlign = Align.Left;

            btnOk.position = new Vector2f(btnOk.position.x + btnOk.size.x, btnOk.position.y);
            Button btnCancel = CreateControl<Button>(
                new Vector2f(btnOk.position.x - btnOk.size.x * 2, btnOk.position.y),
                btnOk.size,
                "Cancel"
                );
            btnCancel.mouseDownLeft += Cancel;

            panel.texture = base.context.texProvider.dialog;
        }

        void Cancel(Control _)
        {
            canceled = true;
            KindlyRequestSuicide();
        }

        public void ShowDialog(StopScene stopScenes, string prompt, string defaultText, int maxLength, Action callBack)
        {
            textbox.maxLength = maxLength;
            textbox.multiLine = false;
            textbox.text = defaultText;
            this.prompt.text = prompt;
            ShowDialog(stopScenes, callBack);
        }
    }
}
