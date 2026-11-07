using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Tesela.UI
{
    /// <summary>
    /// Partículas de interfaz con pooling de Image (reemplazo mínimo de ParticleEffectForUGUI, D-05).
    /// Los azulejos limpiados saltan en arco y vuelan al contador del mosaico.
    /// </summary>
    public sealed class UiParticles
    {
        private readonly RectTransform _layer;
        private readonly Stack<Image> _pool = new Stack<Image>();

        public UiParticles(RectTransform layer, int prewarm = 64)
        {
            _layer = layer;
            for (int i = 0; i < prewarm; i++) _pool.Push(Create());
        }

        private Image Create()
        {
            var img = Ui.Image(_layer, "p", null, Color.white);
            img.gameObject.SetActive(false);
            return img;
        }

        private Image Get(Sprite sprite, Color color, Vector2 pos, float size)
        {
            var img = _pool.Count > 0 ? _pool.Pop() : Create();
            img.sprite = sprite;
            img.type = Image.Type.Simple;
            img.color = color;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = pos;
            rt.localScale = Vector3.one;
            rt.localEulerAngles = Vector3.zero;
            img.gameObject.SetActive(true);
            img.transform.SetAsLastSibling();
            return img;
        }

        private void Release(Image img)
        {
            img.gameObject.SetActive(false);
            _pool.Push(img);
        }

        /// <summary>Azulejo que salta y vuela a <paramref name="target"/> (coordenadas de la capa).</summary>
        public void FlyTile(Sprite sprite, Color tint, Vector2 from, Vector2 target, float size, float delay, System.Action onArrive)
        {
            var img = Get(sprite, tint, from, size);
            var rt = img.rectTransform;
            float duration = 0.42f + Random.Range(0f, 0.12f);
            Motion.Instance.Scale(rt, Vector3.one * 1.08f, Vector3.one * 0.45f, duration, Ease.InOut, delay);
            Motion.Instance.Rotate(rt, Random.Range(-160f, 160f), duration, Ease.Out, delay);
            Motion.Instance.Move(rt, target, duration, Ease.InOut, delay, () =>
            {
                Release(img);
                onArrive?.Invoke();
            }, false, Random.Range(140f, 260f));
        }

        /// <summary>Esquirlas de esmalte que saltan desde una celda.</summary>
        public void Burst(Sprite sprite, Color color, Vector2 at, int count, float spread)
        {
            for (int i = 0; i < count; i++)
            {
                var img = Get(sprite, color, at, Random.Range(10f, 22f));
                var rt = img.rectTransform;
                Vector2 to = at + Random.insideUnitCircle * spread + Vector2.down * spread * 0.4f;
                float d = Random.Range(0.25f, 0.4f);
                Motion.Instance.Move(rt, to, d, Ease.Out, 0f, () => Release(img), false, Random.Range(30f, 90f));
                Motion.Instance.Scale(rt, Vector3.one, Vector3.zero, d, Ease.In);
            }
        }
    }
}
