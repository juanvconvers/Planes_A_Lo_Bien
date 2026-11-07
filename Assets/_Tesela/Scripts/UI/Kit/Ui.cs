using System;
using Tesela.Core;
using Tesela.Core.Audio;
using Tesela.Core.Raster;
using Tesela.Services;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Tesela.UI
{
    /// <summary>Fábrica de elementos uGUI con el estilo de Tesela. Toda la interfaz se construye por código (D-08).</summary>
    public static class Ui
    {
        public static ArtLibrary Art;
        public static AudioService Audio;

        public static readonly Color Ink = Palette.Indigo.ToColor();
        public static readonly Color InkSoft = Palette.IndigoSoft.ToColor();
        public static readonly Color Paper = Palette.SandLight.ToColor();
        public static readonly Color Sand = Palette.Sand.ToColor();
        public static readonly Color Gold = Palette.Gold.ToColor();
        public static readonly Color OnPrimary = new Color32(255, 250, 240, 255);

        public static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static RectTransform Stretch(this RectTransform rt, float left = 0, float bottom = 0, float right = 0, float top = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        /// <summary>Ancla en un punto del padre (0..1) con tamaño fijo.</summary>
        public static RectTransform Place(this RectTransform rt, float ax, float ay, float width, float height, float x = 0, float y = 0)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(ax, ay);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(width, height);
            rt.anchoredPosition = new Vector2(x, y);
            return rt;
        }

        /// <summary>Banda horizontal anclada arriba o abajo (y medido desde ese borde).</summary>
        public static RectTransform Band(this RectTransform rt, bool top, float y, float height, float margin = 0)
        {
            rt.anchorMin = new Vector2(0, top ? 1 : 0);
            rt.anchorMax = new Vector2(1, top ? 1 : 0);
            rt.pivot = new Vector2(0.5f, top ? 1 : 0);
            rt.offsetMin = new Vector2(margin, 0);
            rt.offsetMax = new Vector2(-margin, 0);
            rt.sizeDelta = new Vector2(-margin * 2, height);
            rt.anchoredPosition = new Vector2(0, top ? -y : y);
            return rt;
        }

        public static Image Image(Transform parent, string name, Sprite sprite, Color color, bool raycast = false)
        {
            var rt = Rect(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = raycast;
            if (sprite != null && sprite.border != Vector4.zero) img.type = UnityEngine.UI.Image.Type.Sliced;
            return img;
        }

        public static Image Panel(Transform parent, string name, FrameStyle style = FrameStyle.Panel)
        {
            var img = Image(parent, name, Art.Frame(style), Color.white);
            img.pixelsPerUnitMultiplier = 1f;
            return img;
        }

        public static Image Icon(Transform parent, string id, float size, Color color)
        {
            var img = Image(parent, "icon_" + id, Art.Icon(id), color);
            img.preserveAspect = true;
            img.rectTransform.sizeDelta = new Vector2(size, size);
            return img;
        }

        public static UiText Text(Transform parent, string text, float size, Color color, TextAnchor align = TextAnchor.MiddleCenter, TextRole role = TextRole.Body)
        {
            var t = UiText.Create(parent, "text", role, size, color, align);
            t.Text = text;
            return t;
        }

        public static UiText Title(Transform parent, string text, float size = 72) => Text(parent, text, size, Ink, TextAnchor.MiddleCenter, TextRole.Display);

        public static TeselaButton Button(Transform parent, string label, FrameStyle style, Action onClick, string icon = null, float fontSize = 46)
        {
            var img = Image(parent, "btn_" + label, Art.Frame(style), Color.white, true);
            var btn = img.gameObject.AddComponent<TeselaButton>();
            btn.Init(onClick);
            Color fg = style == FrameStyle.ButtonPrimary ? OnPrimary : Ink;
            if (icon != null)
            {
                var ic = Icon(img.transform, icon, fontSize * 1.15f, fg);
                ic.rectTransform.Place(0, 0.5f, fontSize * 1.15f, fontSize * 1.15f, fontSize * 1.1f, 2);
            }
            if (!string.IsNullOrEmpty(label))
            {
                var t = Text(img.transform, label, fontSize, fg);
                t.Rect.Stretch(icon != null ? fontSize * 2.0f : 24, 8, 24, 8);
                btn.Label = t;
            }
            return btn;
        }

        public static TeselaButton IconButton(Transform parent, string icon, Action onClick, float size = 120, FrameStyle style = FrameStyle.Chip)
        {
            var img = Image(parent, "ibtn_" + icon, Art.Frame(style), Color.white, true);
            img.rectTransform.sizeDelta = new Vector2(size, size);
            var btn = img.gameObject.AddComponent<TeselaButton>();
            btn.Init(onClick);
            var ic = Icon(img.transform, icon, size * 0.52f, style == FrameStyle.ButtonPrimary ? OnPrimary : Ink);
            ic.rectTransform.Place(0.5f, 0.5f, size * 0.52f, size * 0.52f, -2, 3);
            btn.IconImage = ic;
            return btn;
        }

        /// <summary>Grano de terracota sobre un fondo (3–5 %), repetido sin costuras.</summary>
        public static RawImage Grain(Transform parent)
        {
            var rt = Rect(parent, "grain").Stretch();
            var raw = rt.gameObject.AddComponent<RawImage>();
            raw.texture = Art.Grain;
            raw.raycastTarget = false;
            raw.uvRect = new Rect(0, 0, 4, 7);
            return raw;
        }

        public static void Click()
        {
            Audio?.Play(Sfx.Button);
            Haptics.Light();
        }
    }

    /// <summary>Botón con respuesta táctil propia: se hunde 2 px y se encoge levemente (sombra dura que "se pisa").</summary>
    public sealed class TeselaButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler, IPointerExitHandler
    {
        private Action _onClick;
        private bool _pressed;
        public bool Interactable = true;
        public UiText Label;
        public Image IconImage;

        public void Init(Action onClick) => _onClick = onClick;

        public void SetInteractable(bool value)
        {
            Interactable = value;
            var g = GetComponent<CanvasGroup>();
            if (g == null) g = gameObject.AddComponent<CanvasGroup>();
            g.alpha = value ? 1f : 0.5f;
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (!Interactable) return;
            _pressed = true;
            transform.localScale = new Vector3(0.96f, 0.95f, 1f);
        }

        public void OnPointerUp(PointerEventData e) => Release();
        public void OnPointerExit(PointerEventData e) => Release();

        private void Release()
        {
            if (!_pressed) return;
            _pressed = false;
            Motion.Instance.Scale((RectTransform)transform, transform.localScale, Vector3.one, 0.16f, Ease.Settle);
        }

        public void OnPointerClick(PointerEventData e)
        {
            if (!Interactable) return;
            Ui.Click();
            _onClick?.Invoke();
        }
    }
}
