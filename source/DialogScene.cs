using System;
using ChaosFramework.Collections;
using ChaosFramework.Components;
using ChaosFramework.Math.Vectors;
using ChaosFramework.Platform;

namespace ChaosFramework.Menu.OpenGl
{
    using Controls;

    public class DialogScene : GuiScene
    {
        public enum StopMode
        {
            None,
            DrawOnly,
            UseDummy,
        }

        protected record struct StoppedScene(Scene scene, bool? preDoUpdate, bool? preDoDraw);

        public delegate StopMode StopScene(Scene s);

        public bool disposeOnSuicide = true;
        public LayoutContainer.MasterLayoutContainer panel { get; protected set; }
        public bool canceled { get; protected set; }

        protected readonly Button btnOk;

        protected Action callBack;
        LinkedList<StoppedScene> scenesToStop = [];
        LinkedList<DummyScene> dummies = [];

        public DialogScene(MenuContext context, Vector2f position, Vector2f size, bool createOkButton = true)
            : base(context)
        {
            panel = CreateControl<LayoutContainer.MasterLayoutContainer>(position, size);
            panel.texture = context.texProvider.dialog;
            if (createOkButton)
            {
                btnOk = panel.CreateControl<Button>(
                    panel.position + new Vector2f(0, -panel.size.y + 0.1f),
                    new Vector2f(0.25f, 0.05f),
                    "OK"
                    );
                btnOk.mouseDownLeft = KindlyRequestSuicide;
            }
        }

        public DialogScene(MenuContext context)
            : base(context)
        {
            panel = CreateControl<LayoutContainer.MasterLayoutContainer>(Vector2f.EMPTY, new Vector2f(context.window.Ratio(), 1));
            panel.fillContext = true;
            panel.texture = context.texProvider.dialog;
        }

        public DialogScene(MenuContext context, Vector2f size, bool createOkButton = true)
            : this(context, Vector2f.EMPTY, size, createOkButton)
        { }

        public void BuildMenu(string layoutFile, string layoutPath, string btnApplyId = "btnApply", string btnCancelId = "btnCancel")
        {
            panel.DisposeChildren();
            LayoutContainer.CreateMenuInControl(panel, context.language.GetNode(layoutFile, layoutPath));
            if (btnApplyId != null && panel.GetControlById(btnApplyId) is Button btnApply)
                btnApply.mouseDownLeft += Apply;
            if (btnCancelId != null && panel.GetControlById(btnCancelId) is Button btnCancel)
                btnCancel.mouseDownLeft += Cancel;
        }

        void Apply(Control sender)
        {
            canceled = false;
            KindlyRequestSuicide(sender);
        }

        void Cancel(Control sender)
        {
            canceled = true;
            KindlyRequestSuicide(sender);
        }

        protected void KindlyRequestSuicide(Control _)
            => KindlyRequestSuicide();

        protected virtual void KindlyRequestSuicide()
            => PerformSuicide();

        protected void PerformSuicide()
        {
            ReactivateStoppedScenes();

            callBack?.Invoke();

            if (disposeOnSuicide)
                Dispose();
            else
                game.scenes.Remove(this);
        }

        void ReactivateStoppedScenes()
        {
            foreach (DummyScene dummy in dummies)
                dummy.Dispose();

            foreach (StoppedScene s in scenesToStop)
            {
                if (s.preDoUpdate.HasValue)
                    s.scene.doUpdate = s.preDoUpdate.Value;

                if (s.preDoDraw.HasValue)
                    s.scene.doDraw = s.preDoDraw.Value;
            }
        }

        /// <param name="stopScenes">
        ///     What to do with each scene in the game.
        ///     If null, <see cref="StopMode.UseDummy"/> is assumed for every scene.
        /// </param>
        public virtual void ShowDialog(StopScene stopScenes, Action dialogCallBackFunction)
        {
            scenesToStop = [];
            dummies = [];
            int i = 0;
            LinkedList<Scene> forNextDummy = [];
            LinkedList<Scene> newScenes = [];

            void AddDummy()
            {
                if (!forNextDummy.empty)
                {
                    DummyScene dummy = new(context, game, forNextDummy);
                    dummies.Add(dummy);
                    newScenes.Add(dummy);
                    forNextDummy = [];
                }
            }

            foreach (Scene s in game.scenes)
            {
                bool breaker = false;
                switch (stopScenes == null ? StopMode.UseDummy : stopScenes(s))
                {
                    case StopMode.None:
                        breaker = s.doDraw;
                        break;
                    case StopMode.DrawOnly:
                        scenesToStop.Add(new StoppedScene(s, s.doUpdate, null));
                        s.doUpdate = false;
                        goto case StopMode.None;
                    case StopMode.UseDummy:
                        scenesToStop.Add(new StoppedScene(s, s.doUpdate, s.doDraw));
                        s.doUpdate = false;
                        s.doDraw = false;
                        forNextDummy.Add(s);
                        break;
                }
                newScenes.Add(s);
                if (breaker)
                    AddDummy();
                i++;
            }
            AddDummy();

            game.scenes.Clear();
            foreach (Scene s in newScenes)
                game.scenes.Add(s);

            game.scenes.Add(this);
            callBack = dialogCallBackFunction;
            doUpdate = true;
        }
    }
}
