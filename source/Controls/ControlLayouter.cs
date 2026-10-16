using System;
using System.Linq;
using ChaosFramework.Collections;
using ChaosFramework.Components;
using ChaosFramework.Graphics;
using ChaosFramework.Math;
using ChaosFramework.Math.Vectors;
using StringBuilder = System.Text.StringBuilder;

namespace ChaosFramework.Menu.OpenGl.Controls
{
    public class ControlLayouter : Control
    {
        public const string CONTROL_SEQUENCE = "ctrl";

        Control[] allControls;

        Control[] _publicControls = [];
        public Control[] publicControls
        {
            get => _publicControls;
            set
            {
                _publicControls = value;
                SetControls();
            }
        }

        Math.Bounds2f rect = new();
        public override float left => rect.low.x;
        public override float right => rect.high.x;
        public override float top => rect.high.y;
        public override float bottom => rect.low.y;

        public override string text
        {
            get => base.text;
            set
            {
                base.text = value;
                SetControls();
                Format();
            }
        }

        public override Align textAlign
        {
            get => base.textAlign;
            set
            {
                base.textAlign = value;
                Format();
            }
        }

        float _imageScale = 1;
        public float imageScale
        {
            get => _imageScale;
            set
            {
                _imageScale = value;
                Format();
            }
        }

        protected override void Create(CreateParameters cparams)
        {
            base.Create(cparams);
            clipChildren = false;
        }

        public void SetControls(string text, params Control[] controls)
        {
            this.text = text;
            publicControls = controls;
            SetControls();
        }

        void SetControls()
        {
            foreach (Control c in children.OfType<Control>())
                if (Array.IndexOf(publicControls, c) == -1)
                    c.Dispose();
            children.Clear();

            if (text == null)
                return;

            //parse text
            bool expectingEnd = false;
            int controlCount = 0;
            StringBuilder currentStr = new();
            LinkedList<Control> lst = [];
            for (int charIndex = 0; charIndex < text.Length; charIndex++)
            {
                char currentChar = text[charIndex];
                if (expectingEnd)
                    if (currentChar == '>')
                    {
                        string imagePath = currentStr.ToString().Trim().ToLower();
                        if (imagePath == CONTROL_SEQUENCE)
                            if (controlCount < publicControls.Length)
                            {
                                lst.Add(publicControls[controlCount++]);
                                goto no_image;
                            }
                            else
                                imagePath = null;

                        Image img = CreateControl<Image>(Vector2f.EMPTY, size * 0.5f * imageScale);
                        img.texture = imagePath == null ? null : scene.context.texProvider.Load(imagePath, img);
                        lst.Add(img);
                    no_image:
                        expectingEnd = false;
                        currentStr.Clear();
                    }
                    else if (charIndex <= text.Length - 1)
                        currentStr.Append(currentChar);
                    else
                        throw new InvalidOperationException("Reached end of string without closing last control statement.");
                else if (currentChar != '<')
                    currentStr.Append(currentChar);
                else if (charIndex == text.Length - 1)
                {
                    string lblText = currentStr.ToString();
                    if (lblText != "")
                        lst.Add(CreateControl<Label>(Vector2f.EMPTY, size, lblText));

                    expectingEnd = true;
                    currentStr.Clear();
                }
            }

            allControls = lst.ToArray();
            Format();
        }

        void Format()
        {
            rect = new Bounds2f();

            float posX = position.x;
            if (allControls == null)
                return;

            foreach (Control c in allControls)
            {
                c.position = new Vector2f(posX, position.y);
                c.textAlign = Align.Left;
                posX = c.right;
                rect.Expand(c.bottomLeft);
                rect.Expand(c.topRight);
            }

            if ((textAlign & Align.Left) == 0)
                if ((textAlign & Align.Right) != 0)
                    foreach (Control c in allControls)
                        c.position -= new Vector2f(rect.width, 0);
                else
                    foreach (Control c in allControls)
                        c.position -= new Vector2f(rect.width / 2, 0);

            if ((textAlign & Align.Top) != 0)
                foreach (Control c in allControls)
                    c.position -= new Vector2f(0, rect.height / 2);
            else if ((textAlign & Align.Bottom) != 0)
                foreach (Control c in allControls)
                    c.position += new Vector2f(0, rect.height / 2);
        }

        public override void Draw() { }

        public override bool IsHovered()
            => false;

    }
}
