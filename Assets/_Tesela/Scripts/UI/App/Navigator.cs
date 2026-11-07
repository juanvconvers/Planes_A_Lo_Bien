using System.Collections.Generic;
using UnityEngine;

namespace Tesela.UI
{
    /// <summary>Pantalla construida por código. Las subclases son delgadas: leen estado y llaman a controladores.</summary>
    public abstract class ScreenBase
    {
        public RectTransform Root { get; private set; }
        protected AppContext Ctx { get; private set; }
        protected Navigator Nav { get; private set; }
        public CanvasGroup Group { get; private set; }

        /// <summary>Los diálogos se apilan encima sin ocultar la pantalla de abajo.</summary>
        public virtual bool IsDialog => false;
        /// <summary>Pantallas de menú: allí puede verse el banner (nunca sobre el tablero).</summary>
        public virtual bool IsMenu => true;

        internal void Attach(Navigator nav, AppContext ctx, RectTransform parent)
        {
            Nav = nav;
            Ctx = ctx;
            Root = Ui.Rect(parent, GetType().Name).Stretch();
            Group = Root.gameObject.AddComponent<CanvasGroup>();
            Build();
        }

        protected abstract void Build();
        public virtual void OnShow() { }
        public virtual void OnHide() { }
        public virtual void Refresh() { }

        /// <summary>Botón Atrás de Android. Devuelve true si lo consumió.</summary>
        public virtual bool OnBack()
        {
            Nav.Pop();
            return true;
        }
    }

    public sealed class Navigator
    {
        private readonly AppContext _ctx;
        private readonly RectTransform _layer;
        private readonly List<ScreenBase> _stack = new List<ScreenBase>();
        public System.Action<ScreenBase> Changed;

        public Navigator(AppContext ctx, RectTransform layer)
        {
            _ctx = ctx;
            _layer = layer;
        }

        public ScreenBase Top => _stack.Count > 0 ? _stack[_stack.Count - 1] : null;
        public int Depth => _stack.Count;

        public T Push<T>(T screen) where T : ScreenBase
        {
            var previous = Top;
            if (previous != null && !screen.IsDialog)
            {
                previous.OnHide();
                previous.Root.gameObject.SetActive(false);
            }
            screen.Attach(this, _ctx, _layer);
            _stack.Add(screen);
            Show(screen);
            return screen;
        }

        /// <summary>Reemplaza toda la pila (p. ej. ir a Inicio).</summary>
        public T Reset<T>(T screen) where T : ScreenBase
        {
            while (_stack.Count > 0) Close(_stack[_stack.Count - 1]);
            return Push(screen);
        }

        public void Pop()
        {
            if (_stack.Count <= 1) return;
            Close(Top);
            var top = Top;
            if (!top.Root.gameObject.activeSelf)
            {
                top.Root.gameObject.SetActive(true);
                Show(top);
            }
            else top.Refresh();
            Changed?.Invoke(top);
        }

        public void Remove(ScreenBase screen)
        {
            if (screen == Top) { Pop(); return; }
            if (_stack.Remove(screen)) Object.Destroy(screen.Root.gameObject);
        }

        private void Close(ScreenBase s)
        {
            _stack.Remove(s);
            s.OnHide();
            Object.Destroy(s.Root.gameObject);
        }

        private void Show(ScreenBase s)
        {
            s.Root.SetAsLastSibling();
            s.Group.alpha = 0f;
            Motion.Instance.Fade(s.Group, 1f, 0.18f, Ease.Out);
            if (!s.IsDialog)
            {
                s.Root.anchoredPosition = new Vector2(0, -24);
                Motion.Instance.Move(s.Root, Vector2.zero, 0.22f, Ease.Out);
            }
            s.OnShow();
            Changed?.Invoke(s);
        }

        public void Back()
        {
            var top = Top;
            if (top == null) return;
            if (!top.OnBack() && _stack.Count <= 1) Application.Quit();
        }
    }
}
