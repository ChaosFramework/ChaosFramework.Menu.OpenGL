using ChaosFramework.Graphics;
using ChaosFramework.Graphics.Colors;
using ChaosFramework.Math.Vectors;
using static ChaosFramework.Math.Clamping;

namespace ChaosFramework.Menu.OpenGl.Dialogs
{
    using Controls;

    public class MessageBox
        : DialogScene
    {
        // TODO: Should this consistently use textbounds instead of geometry bounds?

        const Align DEFAULT_ALIGN = Align.Top | Align.Left;

        readonly string prompt;
        readonly bool autoLayout;
        readonly Align align;

        public Control.GetText getText;
        public int chosenIndex { get; protected set; }
        public object[] choices { get; protected set; }
        public object chosen => choices[chosenIndex];

        protected Button[] myButtons;
        protected RichTextBox txtPrompt { get; private set; }
        Vector2f size;

        public MessageBox(
            MenuContext context,
            string prompt,
            object[] btnMessages,
            Vector2f size,
            Align align = DEFAULT_ALIGN,
            Control.GetText getText = null,
            bool autoLayout = true
            ) : base(context, size, false)
        {
            this.prompt = prompt;
            choices = btnMessages;
            this.size = size;
            this.align = align;
            this.getText = getText ?? Control.ToString;
            this.autoLayout = autoLayout;

            panel.texture = context.texProvider.dialogDark;
            CreateControls();
        }

        public virtual void CreateControls()
        {
            const byte BUTTON_MARGIN_CHARS = 3;

            panel.DisposeChildren();
            panel.margin = 0.05f;
            txtPrompt = panel.CreateControl<RichTextBox>(Vector2f.EMPTY, 0.05f);
            txtPrompt.enableColorCodes = true;
            txtPrompt.multiLine = true;
            txtPrompt.textAlign = align;
            txtPrompt.isReadonly = true;
            txtPrompt.canSelect = false;
            txtPrompt.text = prompt;
            txtPrompt.textColor = Rgba.OPAQUE_WHITE;
            txtPrompt.texture = null;
            txtPrompt.allowScroll = false;

            if (choices == null || choices.Length == 0)
                choices = ["OK"];

            const float BORDER = 0.05f;
            const float BTN_HEIGHT = 0.05f;

            float btnWidth = (size.x - BORDER) / choices.Length;
            myButtons = new Button[choices.Length];
            float maxSzX = 0;
            for (int i = 0; i < choices.Length; i++)
            {
                int index = i;
                Button btn = myButtons[i] = autoLayout
                    ? panel.CreateControl<Button>(
                        Vector2f.EMPTY,
                        Vector2f.EMPTY,
                        getText(choices[i])
                        )
                    : panel.CreateControl<Button>(
                        panel.topLeft + new Vector2f(BORDER + (2 * i + BUTTON_MARGIN_CHARS) * btnWidth, -panel.size.y * 2 + 0.1f),
                        new Vector2f(btnWidth, BTN_HEIGHT),
                        getText(choices[i])
                        );
                btn.mouseDownLeft = _ => ButtonAction(index);
                btn.texture = context.texProvider.buttonDark;
                btn.textColor = Rgba.OPAQUE_WHITE;
                if (autoLayout)
                    maxSzX = Max(maxSzX, (btn.textGeometry.geo.geometryBounds.width + BUTTON_MARGIN_CHARS) * BTN_HEIGHT);
            }

            maxSzX /= 2;
            if (autoLayout)
            {
                txtPrompt.UpdateTextGeometry();
                panel.size = new Vector2f(
                        Max(txtPrompt.fontSize * txtPrompt.textGeometry.geo.geometryBounds.width * 0.5f, myButtons.Length * maxSzX),
                        txtPrompt.fontSize * txtPrompt.textGeometry.geo.geometryBounds.height * 0.5f + BTN_HEIGHT + BORDER
                    ) + BORDER;
                for (int i = 0; i < choices.Length; i++)
                {
                    myButtons[i].size = new Vector2f(maxSzX, BTN_HEIGHT);
                    myButtons[i].position = new Vector2f((i * 2 - myButtons.Length + 1) * maxSzX, panel.bottom + BTN_HEIGHT);
                }
            }

            txtPrompt.position = new Vector2f(panel.position.x, (panel.top + panel.bottom) / 2 + BTN_HEIGHT);
            txtPrompt.size = new Vector2f(panel.size.x, (panel.top - panel.bottom) / 2 - BTN_HEIGHT);
        }

        public Button GetButton(int i)
            => myButtons[i];

        protected virtual void ButtonAction(int buttonIndex)
        {
            chosenIndex = buttonIndex;
            KindlyRequestSuicide();
        }
    }
}
