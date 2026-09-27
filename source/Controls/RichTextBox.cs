using System;
using System.Text;
using ChaosFramework.Collections.Immutable;
using ChaosFramework.Components;
using ChaosFramework.Graphics;
using ChaosFramework.Graphics.Colors;
using ChaosFramework.Graphics.OpenGl.AssetContainers;
using ChaosFramework.Graphics.OpenGl.Text;
using ChaosFramework.Graphics.Text.Formatting;
using ChaosFramework.Input;
using ChaosFramework.Input.InputEvents;
using ChaosFramework.Math.Vectors;
using static ChaosFramework.Math.Clamping;
using static ChaosFramework.Math.Exponentials;
using Key = ChaosFramework.Input.Keyboard.HidUsage;
using SysCol = System.Collections.Generic;

namespace ChaosFramework.Menu.OpenGl.Controls
{
    public class RichTextBox
        : ScrollBox
        , ValueControl<string>
    {
        enum ScrollReason
        {
            TextChange,
            DiscreteNavigate,
            Click
        };

        public const float CURSOR_BLINK_INTERVAL = 1;
        const float SCROLL_INTERPOLATION_FACTOR = 10.0f;
        const float WHEN_TO_START_SCROLLING_LEFT = 5.0f;
        const float WHEN_TO_START_SCROLLING_RIGHT = 10.0f;
        const float WHEN_TO_START_SCROLLING_TOP = 3.0f;
        const float WHEN_TO_START_SCROLLING_BOTTOM = 4.0f;

        public static readonly ImmutableHashSet<char> WORD_SEPARATORS
            = (SysCol.HashSet<char>)[
                ' ',
                '\t',
                '\n',
                '\r',
                '(',
                ')',
                '[',
                ']',
                '{',
                '}',
                '\"',
                '\'',
                '\\',
                '/',
                '+',
                '-',
                '*',
                '#',
                '~',
                '.',
                ',',
                ':',
                ';',
                '<',
                '>',
                '='
                ];

        static readonly ImmutableHashSet<char> LINE_BREAKS = (SysCol.HashSet<char>)['\n'];

        static readonly ImmutableHashSet<Key> invalidKeys = (SysCol.HashSet<Key>)[Key.Backspace];

        static string clipBoard = "";

        static string EliminiateEscapeSequences(string text, bool indexContainsEscapeCodes, int start, int end)
        {
            StringBuilder bldr = new(end - start);

            int charIndex = 0;
            CharEnumerator textEnumerator = text.GetEnumerator();
            while (textEnumerator.MoveNext())
            {
                if (textEnumerator.Current == Scope.ESCAPE_CODE)
                {
                    do
                    {
                        if (indexContainsEscapeCodes)
                            charIndex++;
                        if (!textEnumerator.MoveNext())
                            throw new InvalidOperationException("Text contained invalid escape code");
                    }
                    while (textEnumerator.Current != Scope.ESCAPE_CODE);
                }
                else
                {
                    if (charIndex++ >= start)
                        bldr.Append(textEnumerator.Current);
                    if (charIndex >= end)
                        break;
                }
            }

            return bldr.ToString();
        }

        public Action<ValueControl> valueChanged { get; set; }
        public bool isReadonly = false;

        RichLabel lblTxt;
        ScrollReason currentReasonToScroll = ScrollReason.TextChange;
        SysCol.Dictionary<KeyboardChars.Modifier, Key[]> definedKeys;

        bool ignoreMouseDown = false;

        public bool canSelect
        {
            get => lblTxt.canSelect;
            set => lblTxt.canSelect = value;
        }

        public override FontContainer.Entry font
        {
            get => lblTxt.font;
            set => base.font = lblTxt.font = value;
        }

        public Vector2f textSize
        {
            get => lblTxt.size;
            set => lblTxt.size = value;
        }

        bool _allowScroll = true;
        public bool allowScroll
        {
            get => _allowScroll;
            set
            {
                _allowScroll = value;
                if (!value)
                    scrollMargin = Vector2f.NAN;
            }
        }

        int _maxLength = int.MaxValue;
        public int maxLength
        {
            get => _maxLength;
            set
            {
                _maxLength = value;
                if (maxLength < (text == null ? 0 : text.Length))
                    text = text.Remove(maxLength);
            }
        }

        public float maxLineWidth
        {
            get => lblTxt.maxLineWidth + 2;
            set => lblTxt.maxLineWidth = value - 2;
        }

        bool _limitLineLength;
        public bool limitLineLength
        {
            get => _limitLineLength;
            set
            {
                _limitLineLength = value;
                string[] lines = text.Split('\n');
                StringBuilder newBuilder = new(text.Length);
                float maxWidth = width / lblTxt.size.x - 1;
                for (int i = 0; i < lines.Length; i++)
                {
                    int k = 0;
                    float d = 0;
                    for (; k < lines[i].Length && d < maxWidth; k++)
                        d += lblTxt.font.content.GetGlyph(lines[i][k]).advanceCursor.x;
                    newBuilder.Append(lines[i][..k]);
                    if (i < lines.Length)
                        newBuilder.AppendLine();
                }
                text = newBuilder.ToString();
            }
        }

        int _maxLines = int.MaxValue;
        public int maxLines
        {
            get => _maxLines;
            set
            {
                _maxLines = value;
                string[] lines = text.Split('\n');
                if (lines.Length > value)
                {
                    StringBuilder newBuilder = new(text.Length);
                    for (int i = 0; i < value; i++)
                    {
                        newBuilder.Append(lines[i]);
                        if (i < lines.Length - 1)
                            newBuilder.AppendLine();
                    }
                    text = newBuilder.ToString();
                }
            }
        }

        bool _multiLine = false;
        public bool multiLine
        {
            get => _multiLine;
            set
            {
                _multiLine = value;
                if (!multiLine)
                {
                    StringBuilder newText = new(text.Length);
                    foreach (char c in text)
                        if (c != '\n')
                            newText.Append(c);
                    text = newText.ToString();
                }
            }
        }

        bool _numbersOnly = false;
        public bool numbersOnly
        {
            get => _numbersOnly;
            set
            {
                _numbersOnly = value;
                if (numbersOnly)
                {
                    StringBuilder newText = new(text.Length);
                    foreach (char c in text)
                        if (int.TryParse(c.ToString(), out _))
                            newText.Append(c);
                    text = newText.ToString();
                }
            }
        }

        ImmutableHashSet<char> _invalidChars = [];
        public ImmutableHashSet<char> invalidCharacters
        {
            get => _invalidChars;
            set
            {
                _invalidChars = value;
                UpdateText();
            }
        }

        public override Rgba textColor
        {
            get => lblTxt ? lblTxt.textColor : GetParentTextColor();
            set
            {
                if (lblTxt)
                    lblTxt.textColor = value;
            }
        }

        public RichLabel.ColorText colorText
        {
            get => lblTxt.colorText;
            set => lblTxt.colorText = value;
        }

        public bool enableColorCodes
        {
            get => lblTxt.enableColorCodes;
            set => lblTxt.enableColorCodes = value;
        }

        public override void UpdateTextGeometry()
            => lblTxt.UpdateTextGeometry();

        public override TextMesh textGeometry
        {
            get => lblTxt.textGeometry;
            set => lblTxt.textGeometry = value;
        }

        public override Align textAlign
        {
            get => lblTxt.textAlign;
            set
            {
                lblTxt.anchor = value;
                lblTxt.textAlign = value;
                Align horizontalAlign = value & Align.LeftRight;
                Align verticalAlign = value & Align.BottomTop;

                lblTxt.position = new Vector2f(
                    horizontalAlign switch
                    {
                        Align.Left => left + fontSize,
                        Align.Right => right - fontSize,
                        Align.Center => position.x,
                        _ => lblTxt.x
                    },
                    verticalAlign switch
                    {
                        Align.Bottom => bottom + fontSize,
                        Align.Top => top - fontSize,
                        Align.Center => position.y,
                        _ => lblTxt.y
                    }
                );
            }
        }

        float _fontSize = 0.045f;
        public float fontSize
        {
            get => _fontSize;
            set
            {
                _fontSize = value;
                if (lblTxt != null)
                    lblTxt.size = value;
            }
        }

        public bool hasSelection
            => lblTxt.selectionCursor != cursor;

        bool _suspendTexUpdate;
        bool suspendTextUpdate
        {
            get => _suspendTexUpdate;
            set
            {
                _suspendTexUpdate = value;
                if (!value)
                {
                    text = textWhileSuspended;
                    cursor = cusorWhileSuspended;
                }
            }
        }

        int cusorWhileSuspended;
        int cursor
        {
            get => suspendTextUpdate ? cusorWhileSuspended : Clamp(0, text.Length, cusorWhileSuspended);
            set
            {
                cusorWhileSuspended = value;
                if (!suspendTextUpdate)
                {
                    cusorWhileSuspended = Clamp(0, text.Length, value);
                    lblTxt.SetCursor(cusorWhileSuspended);
                    ScrollTo(cursor);
                }
            }
        }

        string textWhileSuspended;
        public override string text
        {
            get => suspendTextUpdate ? textWhileSuspended : lblTxt.text;
            set
            {
                value = value?.Replace("\r", string.Empty).Replace("\b", string.Empty) ?? string.Empty;
                textWhileSuspended = value;
                currentReasonToScroll = ScrollReason.TextChange;
                if (!suspendTextUpdate)
                {
                    string tmp = lblTxt.text;
                    lblTxt.text = value;
                    UpdateText();
                    if (valueChanged != null && lblTxt.text != tmp)
                        valueChanged(this);
                    cursor = Clamp(0, lblTxt.text.Length, cursor);
                    lblTxt.selectionCursor = Clamp(0, text.Length, lblTxt.selectionCursor);
                }
            }
        }

        KeyboardChars.Modifier mod = KeyboardChars.Modifier.None;
        bool shiftDown => (mod & KeyboardChars.Modifier.Shift) != 0;

        string ValueControl<string>.GetValue() => text;
        void ValueControl<string>.SetValue(string value)
            => this.SetValueChecked(() => text = value);

        protected override void Create(CreateParameters cparams)
        {
            lblTxt = CreateControl<RichLabel>(0, fontSize);
            text = string.Empty;
            textAlign = Align.TopLeft;
            base.Create(cparams);
            definedKeys = KeyboardChars.GetDefinedKeys(scene.context.keyboardLayout);
            scrollMargin = Vector2f.EMPTY;

            mouseDown[0] += Click;
            doubleClickLeft += DoubleClick;
            AddMultiClick(3, TripleClick);
        }

        public void SetCursor(int newCursor, bool moveSelectionCursor)
        {
            cursor = newCursor;
            if (moveSelectionCursor)
                lblTxt.selectionCursor = cursor;
        }

        void UpdateText()
        {
            StringBuilder newText = new(text.Length);
            foreach (char c in text)
                if ((multiLine || (c != '\r' && c != '\n')) && !invalidCharacters.Contains(c))
                    newText.Append(c);
            lblTxt.text = newText.ToString();
        }

        void Click(Control sender)
        {
            SetCursorToMouse();
            currentReasonToScroll = ScrollReason.Click;
            if ((mod & KeyboardChars.Modifier.Shift) == 0)
                lblTxt.selectionCursor = cursor;
        }

        void DoubleClick(Control sender)
            => SelectChunk(WORD_SEPARATORS);

        void TripleClick(Control sender)
            => SelectChunk(LINE_BREAKS);

        void SelectChunk(ImmutableHashSet<char> delimiter)
        {
            SetCursorToMouse();
            ignoreMouseDown = true;
            currentReasonToScroll = ScrollReason.Click;
            int clicked = cursor;

            for (int i = clicked; i <= text.Length; i++)
                if (i == text.Length || delimiter.Contains(text[i]))
                {
                    cursor = i;
                    break;
                }

            for (int i = clicked - 1; i >= 0; i--)
                if (delimiter.Contains(text[i]))
                {
                    lblTxt.selectionCursor = Min(i + 1, clicked);
                    break;
                }
                else if (i == 0)
                {
                    lblTxt.selectionCursor = 0;
                    break;
                }
        }

        public void ScrollTo(int characterIndex)
        {
            UpdateScroll();
            Vector2f high = textGeometry.geo.GetCursorPosFromIndex(characterIndex);
            high = Vector2f.ComponentWiseMul(high, textSize);
            high = lblTxt.position + high - lblTxt.bottomLeft;
            Vector2f low = high - baseArea.size;

            float interpol = EaseIn(ftime * SCROLL_INTERPOLATION_FACTOR);

            float marginX = (textAlign & Align.LeftRight) == 0 ? 0 : fontSize;
            float denomX = lblTxt.width + marginX - baseArea.width;
            float xLow = (low.x + WHEN_TO_START_SCROLLING_RIGHT * textSize.x) / denomX + textMarginLeft;
            float xHigh = (high.x - WHEN_TO_START_SCROLLING_LEFT * textSize.x) / denomX - textMarginRight;
            float x = Clamp(xLow, xHigh, scrollX);
            if (x != scrollX)
                switch (currentReasonToScroll)
                {
                    case ScrollReason.Click:
                        SetScrollX(scrollX + (x - scrollX) * interpol);
                        break;

                    case ScrollReason.TextChange:
                    case ScrollReason.DiscreteNavigate:
                        SetScrollX(x);
                        break;
                }

            float marginY = (textAlign & Align.BottomTop) == 0 ? 0 : fontSize;
            float denomY = lblTxt.height + marginY - baseArea.height;
            float yLow = 1 - (low.y + WHEN_TO_START_SCROLLING_TOP * textSize.y) / denomY;
            float yHigh = 1 - (high.y - WHEN_TO_START_SCROLLING_BOTTOM * textSize.y) / denomY;
            float y = Clamp(yHigh, yLow, scrollY);
            if (y != scrollY)
                switch (currentReasonToScroll)
                {
                    case ScrollReason.Click:
                        SetScrollY(scrollY + (y - scrollY) * interpol);
                        break;

                    case ScrollReason.TextChange:
                    case ScrollReason.DiscreteNavigate:
                        SetScrollY(y);
                        break;
                }
        }

        public override void SetUpdateCalls()
        {
            lblTxt.showCursor = false;
            base.SetUpdateCalls();
        }

        protected internal override void SetInputHandlers()
        {
            scene.context.input.AddHandler<InputReleaseEvent<Keyboard.Key>, Keyboard.Key, InputChange>(
                scene.context.inputLayer,
                ReleaseKey
                );

            scene.context.input.AddHandler<InputPushEvent<Keyboard.Key>, Keyboard.Key, InputChange>(
                scene.context.inputLayer,
                PushKey
                );

            base.SetInputHandlers();
        }

        bool PushKey(InputPushEvent<Keyboard.Key> e)
        {
            switch (e.axis.hidKey)
            {
                case Key.ShiftLeft:
                case Key.ShiftRight:
                    mod |= KeyboardChars.Modifier.Shift;
                    return true;
                case Key.ControlLeft:
                case Key.ControlRight:
                    mod |= KeyboardChars.Modifier.Ctrl;
                    return true;
                case Key.AltLeft:
                    mod |= KeyboardChars.Modifier.Alt;
                    return true;
                case Key.AltRight:
                    mod |= KeyboardChars.Modifier.AltGr;
                    return true;
            }

            bool catchInput = false;
            suspendTextUpdate = true;
            bool ctrl = (mod & KeyboardChars.Modifier.Ctrl) == KeyboardChars.Modifier.Ctrl;

            if (ctrl && e.axis.hidKey == Key.A)
            {
                catchInput = true;
                lblTxt.selectionCursor = 0;
                cursor = text.Length;
            }

            if (lblTxt.canSelect)
            {
                if (hasSelection && ctrl && e.axis.hidKey == Key.C)
                {
                    catchInput = true;
                    CopySelectionToClipboard();
                }

                if (ctrl && e.axis.hidKey == Key.L)
                    CtrlL(ref catchInput);

                if (ctrl && e.axis.hidKey == Key.X)
                    CtrlX(ref catchInput);
            }

            if (!isReadonly)
                HandleTextChangingInput(ref catchInput, ctrl, e);

            int tmpCursor = cursor;
            suspendTextUpdate = false;
            bool separators = cursor < text.Length && WORD_SEPARATORS.Contains(text[cursor]);

            // TODO: catching input is inconsistent here: left and right arrows have extra checks, up and down are always caught
            if (cursor < text.Length && e.axis.hidKey == Key.ArrowRight)
            {
                currentReasonToScroll = ScrollReason.DiscreteNavigate;
                catchInput = true;
                do
                    cursor++;
                while (ctrl && cursor < text.Length && !WORD_SEPARATORS.Contains(text[cursor]) ^ separators);
            }

            separators = cursor > 0 && WORD_SEPARATORS.Contains(text[cursor - 1]);
            if (cursor > 0 && e.axis.hidKey == Key.ArrowLeft)
            {
                currentReasonToScroll = ScrollReason.DiscreteNavigate;
                catchInput = true;
                do
                    cursor--;
                while (ctrl && cursor > 0 && !WORD_SEPARATORS.Contains(text[cursor - 1]) ^ separators);
            }

            void MoveCursorVertically(int sign)
            {
                currentReasonToScroll = ScrollReason.DiscreteNavigate;
                catchInput = true;
                cursor = lblTxt.textGeometry.geo.GetCursorIndexFromPos(
                    lblTxt.textGeometry.geo.GetCursorPosFromIndex(cursor) + new Vector2f(0, sign)
                    );
            }

            if (e.axis.hidKey == Key.ArrowDown)
                MoveCursorVertically(-1);

            if (e.axis.hidKey == Key.ArrowUp)
                MoveCursorVertically(1);

            if (cursor != tmpCursor && !shiftDown)
                lblTxt.selectionCursor = cursor;

            cursor = Clamp(0, text.Length, cursor);

            if (allowScroll)
                UpdateScroll();

            scrollPerStop = lblTxt.size.y;

            return catchInput;
        }

        bool ReleaseKey(InputReleaseEvent<Keyboard.Key> e)
        {
            switch (e.axis.hidKey)
            {
                case Key.ShiftLeft:
                case Key.ShiftRight:
                    mod &= ~KeyboardChars.Modifier.Shift;
                    return true;
                case Key.ControlLeft:
                case Key.ControlRight:
                    mod &= ~KeyboardChars.Modifier.Ctrl;
                    return true;
                case Key.AltLeft:
                    mod &= ~KeyboardChars.Modifier.Alt;
                    return true;
                case Key.AltRight:
                    mod &= ~KeyboardChars.Modifier.AltGr;
                    return true;
                default:
                    return false;
            }
        }

        void CtrlL(ref bool catchInput)
        {
            catchInput = true;
            int lineStart = Max(0, cursor); ;
            while (lineStart > 0 && text[lineStart - 1] != '\n')
                lineStart--;

            int lineEnd = cursor;
            while (lineEnd < text.Length && text[lineEnd] != '\n')
                lineEnd++;

            clipBoard = EliminiateEscapeSequences(text, true, lineStart, lineEnd + 1);
            if (lineEnd == text.Length)
                clipBoard += '\n';

            if (!isReadonly)
                text = text.Remove(lineStart, Min(text.Length, lineEnd + 1) - lineStart);

            cursor = Min(lineStart, text.Length);
            lblTxt.selectionCursor = cursor;
        }

        void CtrlX(ref bool catchInput)
        {
            if (hasSelection)
            {
                catchInput = true;
                CopySelectionToClipboard();
                if (!isReadonly)
                    RemoveSelection();
            }
        }

        void HandleTextChangingInput(ref bool catchInput, bool ctrl, InputPushEvent<Keyboard.Key> e)
        {
            if (!invalidKeys.Contains(e.axis.hidKey))
                if (Array.IndexOf(definedKeys[mod], e.axis.hidKey) >= 0)
                {
                    if (hasSelection)
                        RemoveSelection();

                    PutChar(KeyboardChars.GetChar(scene.context.keyboardLayout, mod, e.axis.hidKey));
                    lblTxt.selectionCursor = cursor;
                    catchInput = true;
                }

            if (e.axis.hidKey == Key.Backspace)
            {
                catchInput = true;
                if (hasSelection)
                    RemoveSelection();
                else if (cursor > 0)
                {
                    bool blanks = WORD_SEPARATORS.Contains(text[cursor - 1]);
                    char removed;
                    do
                    {
                        removed = text[--cursor];
                        text = text.Remove(cursor, 1);
                    } while (ctrl && cursor > 0 && (!WORD_SEPARATORS.Contains(text[cursor - 1]) ^ blanks) && removed != '\n');
                }
                lblTxt.selectionCursor = cursor;
            }

            if (e.axis.hidKey == Key.Delete)
            {
                catchInput = true;
                if (hasSelection)
                    RemoveSelection();
                else if (cursor < text.Length)
                {
                    bool blanks = WORD_SEPARATORS.Contains(text[cursor]);
                    char remove;
                    do
                    {
                        remove = text[cursor];
                        text = text.Remove(cursor, 1);
                    }
                    while (
                        ctrl
                        && cursor < text.Length
                        && (!WORD_SEPARATORS.Contains(text[cursor]) ^ blanks)
                        && text[cursor] != '\n'
                        && remove != '\n'
                        );
                }
                lblTxt.selectionCursor = cursor;
            }

            if (ctrl && e.axis.hidKey == Key.T && cursor > 0 && cursor < text.Length)
            {
                catchInput = true;
                if (hasSelection)
                    if (cursor > lblTxt.selectionCursor)
                    {
                        char c = text[cursor];
                        text = text.Insert(lblTxt.selectionCursor, c.ToString());
                        text = text.Remove(cursor + 1, 1);
                        int oldCursor = cursor;
                        cursor = lblTxt.selectionCursor + 1;
                        lblTxt.selectionCursor = oldCursor + 1;
                    }
                    else
                    {
                        char c = text[cursor - 1];
                        text = text.Remove(cursor - 1, 1);
                        text = text.Insert(lblTxt.selectionCursor - 1, c.ToString());
                        int oldCursor = cursor;
                        cursor = lblTxt.selectionCursor - 1;
                        lblTxt.selectionCursor = oldCursor - 1;
                    }
                else
                {
                    char c = text[cursor];
                    text = text.Insert(cursor - 1, c.ToString());
                    text = text.Remove(cursor + 1, 1);
                }
            }

            if (ctrl && e.axis.hidKey == Key.V)
            {
                catchInput = true;
                if (hasSelection)
                    RemoveSelection();

                text = text.Insert(cursor, clipBoard);
                cursor += clipBoard.Length;
                lblTxt.selectionCursor = cursor;
            }
        }

        void PutChar(char c)
        {
            if (isReadonly) return;
            if (!multiLine && c == '\n') return;
            if (text.Length >= maxLength) return;
            if (numbersOnly && !int.TryParse(c.ToString(), out _)) return;
            if (invalidCharacters.Contains(c)) return;

            if (c != '\n')
            {
                float maxWidth = width / lblTxt.size.x - 1 - lblTxt.font.content.GetGlyph(c).advanceCursor.x;
                float d = 0;
                if (float.IsNaN(scrollMargin.x))
                    for (int i = text.Length - 1; i >= 0; i--)
                    {
                        if (text[i] == '\n')
                            break;

                        if ((d += lblTxt.font.content.GetGlyph(text[i]).advanceCursor.x) >= maxWidth)
                            return;
                    }

                if (cursor == text.Length)
                    text += c;
                else
                    text = text.Insert(cursor, c.ToString());

                cursor++;
            }
            else
            {
                int numTabs = 0, numSpace = 0;
                for (int i = cursor - 1; i >= 0 && text[i] != '\n'; i--)
                {
                    if (text[i] == '\t')
                        numTabs++;
                    else if (text[i] == ' ')
                    {
                        if (numTabs == 0)
                            numSpace++;
                    }
                    else
                        numTabs = numSpace = 0;
                }
                string newLineText = $"\n{new string('\t', numTabs)}{new string(' ', numSpace)}";
                if (cursor == text.Length)
                    text += newLineText;
                else
                    text = text.Insert(cursor, newLineText);

                cursor += newLineText.Length;
            }
        }

        void RemoveSelection()
        {
            int len = Min(lblTxt.selectionCursor, cursor);
            text = $"{text[..len]}{text[Max(lblTxt.selectionCursor, cursor)..]}";
            cursor = len;
            lblTxt.selectionCursor = cursor;
        }

        void CopySelectionToClipboard()
        {
            int start = Min(lblTxt.selectionCursor, cursor);
            int end = Max(lblTxt.selectionCursor, cursor);
            if (end > start)
                clipBoard = EliminiateEscapeSequences(lblTxt.text, false, start, end);
        }

        public override void UpdateInteraction()
        {
            mod = KeyboardChars.Modifier.None;
            foreach (Keyboard keyboard in scene.context.input.EnumerateDevices<Keyboard>())
                mod |= KeyboardChars.GetCharModifier(keyboard);

            lblTxt.showCursor = !isReadonly;
            base.UpdateInteraction();

            if (scene.mouseDown[0] > 0)
            {
                if (!ignoreMouseDown)
                    SetCursorToMouse();
            }
            else
                ignoreMouseDown = false;

            base.UpdateInteraction();
        }

        int SetCursorToMouse()
        {
            Vector2f pos = scene.context.cursorPosition - lblTxt.scrolledPosition;
            pos = Vector2f.ComponentWiseMul(pos, 1 / textSize);
            return cursor = textGeometry.geo.GetCursorIndexFromPos(pos);
        }
    }
}
