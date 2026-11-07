using System;
using UnityEngine;
using UnityEngine.UI;

namespace Tesela.UI
{
    public enum Ease
    {
        Linear = 0,
        /// <summary>Salida con un leve rebote (asentar azulejo).</summary>
        Settle = 1,
        /// <summary>Salida cúbica suave.</summary>
        Out = 2,
        /// <summary>Entrada-salida para traslados largos.</summary>
        InOut = 3,
        /// <summary>Entrada acelerada (azulejo que cae al contador).</summary>
        In = 4,
        /// <summary>Pulso: va y vuelve (squash &amp; stretch).</summary>
        Pulse = 5
    }

    /// <summary>
    /// Motor de tweens mínimo y propio (reemplazo de LitMotion/PrimeTween, D-05): arreglo de structs reutilizado,
    /// sin asignaciones por frame. Las animaciones "no esenciales" se saltan con Reducir movimiento.
    /// </summary>
    public sealed class Motion : MonoBehaviour
    {
        private enum Kind : byte { Position, Scale, Alpha, Rotation, Float, Color }

        private struct Tween
        {
            public int Id;
            public Kind Kind;
            public RectTransform Rect;
            public Graphic Graphic;
            public CanvasGroup Group;
            public Vector3 From, To;
            public Color ColorFrom, ColorTo;
            public float Delay, Duration, Elapsed;
            public Ease Ease;
            public Action<float> OnValue;
            public Action OnDone;
            public bool Active;
            public bool Arc;
            public float ArcHeight;
        }

        private static Motion _instance;
        private Tween[] _tweens = new Tween[256];
        private int _count;
        private int _nextId = 1;

        public static bool ReduceMotion;

        public static Motion Instance
        {
            get
            {
                if (_instance != null) return _instance;
                var go = new GameObject("Motion");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<Motion>();
                return _instance;
            }
        }

        public static float Evaluate(Ease e, float t)
        {
            t = Mathf.Clamp01(t);
            switch (e)
            {
                case Ease.Settle:
                {
                    float u = 1f - t;
                    return 1f - u * u * u + Mathf.Sin(t * Mathf.PI) * 0.06f * u;
                }
                case Ease.Out: { float u = 1f - t; return 1f - u * u * u; }
                case Ease.InOut: return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
                case Ease.In: return t * t * t;
                case Ease.Pulse: return Mathf.Sin(t * Mathf.PI);
                default: return t;
            }
        }

        private int Add(ref Tween t, bool essential)
        {
            if (ReduceMotion && !essential)
            {
                t.Duration = 0f;
                t.Delay = 0f;
            }
            if (_count == _tweens.Length) Array.Resize(ref _tweens, _tweens.Length * 2);
            t.Id = _nextId++;
            t.Active = true;
            _tweens[_count++] = t;
            return t.Id;
        }

        public int Move(RectTransform rt, Vector2 to, float duration, Ease ease = Ease.Out, float delay = 0f, Action done = null, bool essential = false, float arcHeight = 0f)
        {
            var t = new Tween { Kind = Kind.Position, Rect = rt, From = rt.anchoredPosition, To = to, Duration = duration, Delay = delay, Ease = ease, OnDone = done, Arc = arcHeight != 0f, ArcHeight = arcHeight };
            return Add(ref t, essential);
        }

        public int Scale(RectTransform rt, Vector3 from, Vector3 to, float duration, Ease ease = Ease.Out, float delay = 0f, Action done = null, bool essential = false)
        {
            rt.localScale = from;
            var t = new Tween { Kind = Kind.Scale, Rect = rt, From = from, To = to, Duration = duration, Delay = delay, Ease = ease, OnDone = done };
            return Add(ref t, essential);
        }

        public int Rotate(RectTransform rt, float toDegrees, float duration, Ease ease = Ease.Out, float delay = 0f, Action done = null)
        {
            var t = new Tween { Kind = Kind.Rotation, Rect = rt, From = new Vector3(0, 0, rt.localEulerAngles.z), To = new Vector3(0, 0, toDegrees), Duration = duration, Delay = delay, Ease = ease, OnDone = done };
            return Add(ref t, false);
        }

        public int Fade(CanvasGroup group, float to, float duration, Ease ease = Ease.Out, float delay = 0f, Action done = null, bool essential = true)
        {
            var t = new Tween { Kind = Kind.Alpha, Group = group, From = new Vector3(group.alpha, 0, 0), To = new Vector3(to, 0, 0), Duration = duration, Delay = delay, Ease = ease, OnDone = done };
            return Add(ref t, essential);
        }

        public int Tint(Graphic g, Color to, float duration, Ease ease = Ease.Out, float delay = 0f, Action done = null)
        {
            var t = new Tween { Kind = Kind.Color, Graphic = g, ColorFrom = g.color, ColorTo = to, Duration = duration, Delay = delay, Ease = ease, OnDone = done };
            return Add(ref t, true);
        }

        public int Value(float from, float to, float duration, Action<float> onValue, Ease ease = Ease.Out, float delay = 0f, Action done = null, bool essential = true)
        {
            var t = new Tween { Kind = Kind.Float, From = new Vector3(from, 0, 0), To = new Vector3(to, 0, 0), Duration = duration, Delay = delay, Ease = ease, OnValue = onValue, OnDone = done };
            return Add(ref t, essential);
        }

        public static int Delay(float seconds, Action done) => Instance.Value(0, 1, seconds, null, Ease.Linear, 0f, done, true);

        public void Kill(int id)
        {
            for (int i = 0; i < _count; i++) if (_tweens[i].Id == id) _tweens[i].Active = false;
        }

        public void KillTarget(RectTransform rt)
        {
            for (int i = 0; i < _count; i++) if (_tweens[i].Rect == rt) _tweens[i].Active = false;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            int write = 0;
            int n = _count;
            for (int i = 0; i < n; i++)
            {
                ref Tween t = ref _tweens[i];
                if (t.Active)
                {
                    if (t.Delay > 0f) t.Delay -= dt;
                    else
                    {
                        t.Elapsed += dt;
                        float p = t.Duration <= 0f ? 1f : Mathf.Clamp01(t.Elapsed / t.Duration);
                        Apply(ref t, Evaluate(t.Ease, p), p);
                        if (p >= 1f)
                        {
                            t.Active = false;
                            var done = t.OnDone;
                            done?.Invoke();
                        }
                    }
                }
                if (t.Active) _tweens[write++] = t;
            }
            // Tweens añadidos durante los callbacks quedaron al final: se compactan detrás de los activos.
            for (int i = n; i < _count; i++) _tweens[write++] = _tweens[i];
            for (int i = write; i < _count; i++) _tweens[i] = default;
            _count = write;
        }

        private static void Apply(ref Tween t, float k, float p)
        {
            switch (t.Kind)
            {
                case Kind.Position:
                    if (t.Rect == null) return;
                    Vector3 pos = Vector3.LerpUnclamped(t.From, t.To, k);
                    if (t.Arc) pos.y += Mathf.Sin(p * Mathf.PI) * t.ArcHeight;
                    t.Rect.anchoredPosition = pos;
                    break;
                case Kind.Scale:
                    if (t.Rect != null) t.Rect.localScale = Vector3.LerpUnclamped(t.From, t.To, k);
                    break;
                case Kind.Rotation:
                    if (t.Rect != null) t.Rect.localEulerAngles = new Vector3(0, 0, Mathf.LerpUnclamped(t.From.z, t.To.z, k));
                    break;
                case Kind.Alpha:
                    if (t.Group != null) t.Group.alpha = Mathf.LerpUnclamped(t.From.x, t.To.x, k);
                    break;
                case Kind.Color:
                    if (t.Graphic != null) t.Graphic.color = Color.LerpUnclamped(t.ColorFrom, t.ColorTo, k);
                    break;
                case Kind.Float:
                    t.OnValue?.Invoke(Mathf.LerpUnclamped(t.From.x, t.To.x, k));
                    break;
            }
        }
    }
}
