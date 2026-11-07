using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Tesela.UI
{
    public enum TextRole
    {
        Display = 0,
        Body = 1
    }

    /// <summary>
    /// Texto que usa TextMeshPro si las TMP Essential Resources están importadas y, si no, Text clásico con la misma
    /// fuente (nunca queda texto invisible). Fraunces para títulos y cifras grandes; Bricolage Grotesque para interfaz.
    /// </summary>
    public sealed class UiText : MonoBehaviour
    {
        private static bool? _tmpAvailable;
        private static TMP_FontAsset _displayAsset, _bodyAsset;

        private TextMeshProUGUI _tmp;
        private Text _legacy;
        private float _baseSize;

        public static float GlobalScale = 1f;

        public static bool TmpAvailable
        {
            get
            {
                if (_tmpAvailable == null) _tmpAvailable = Resources.Load<TMP_Settings>("TMP Settings") != null;
                return _tmpAvailable.Value;
            }
        }

        public static UiText Create(Transform parent, string name, TextRole role, float size, Color color, TextAnchor align)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<UiText>();
            t._baseSize = size;
            Font font = role == TextRole.Display ? Theme.Instance.Display : Theme.Instance.Interface;
            if (TmpAvailable && font != null)
            {
                t._tmp = go.AddComponent<TextMeshProUGUI>();
                t._tmp.font = AssetFor(role, font);
                t._tmp.fontSize = size * GlobalScale;
                t._tmp.color = color;
                t._tmp.alignment = ToTmp(align);
                t._tmp.raycastTarget = false;
                t._tmp.richText = false;
                t._tmp.enableWordWrapping = true;
                t._tmp.overflowMode = TextOverflowModes.Overflow;
            }
            else
            {
                t._legacy = go.AddComponent<Text>();
                t._legacy.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                t._legacy.fontSize = Mathf.RoundToInt(size * GlobalScale);
                t._legacy.color = color;
                t._legacy.alignment = align;
                t._legacy.raycastTarget = false;
                t._legacy.supportRichText = false;
                t._legacy.horizontalOverflow = HorizontalWrapMode.Wrap;
                t._legacy.verticalOverflow = VerticalWrapMode.Overflow;
            }
            return t;
        }

        private static TMP_FontAsset AssetFor(TextRole role, Font font)
        {
            if (role == TextRole.Display) return _displayAsset ??= TMP_FontAsset.CreateFontAsset(font);
            return _bodyAsset ??= TMP_FontAsset.CreateFontAsset(font);
        }

        private static TextAlignmentOptions ToTmp(TextAnchor a)
        {
            switch (a)
            {
                case TextAnchor.UpperLeft: return TextAlignmentOptions.TopLeft;
                case TextAnchor.UpperCenter: return TextAlignmentOptions.Top;
                case TextAnchor.UpperRight: return TextAlignmentOptions.TopRight;
                case TextAnchor.MiddleLeft: return TextAlignmentOptions.Left;
                case TextAnchor.MiddleRight: return TextAlignmentOptions.Right;
                case TextAnchor.LowerLeft: return TextAlignmentOptions.BottomLeft;
                case TextAnchor.LowerCenter: return TextAlignmentOptions.Bottom;
                case TextAnchor.LowerRight: return TextAlignmentOptions.BottomRight;
                default: return TextAlignmentOptions.Center;
            }
        }

        public string Text
        {
            get => _tmp != null ? _tmp.text : _legacy.text;
            set
            {
                if (_tmp != null) { if (_tmp.text != value) _tmp.text = value; }
                else if (_legacy.text != value) _legacy.text = value;
            }
        }

        public Color Color
        {
            get => _tmp != null ? _tmp.color : _legacy.color;
            set { if (_tmp != null) _tmp.color = value; else _legacy.color = value; }
        }

        public Graphic Graphic => _tmp != null ? (Graphic)_tmp : _legacy;

        public void SetSize(float size)
        {
            _baseSize = size;
            ApplyScale();
        }

        public void ApplyScale()
        {
            if (_tmp != null) _tmp.fontSize = _baseSize * GlobalScale;
            else _legacy.fontSize = Mathf.RoundToInt(_baseSize * GlobalScale);
        }

        public RectTransform Rect => (RectTransform)transform;
    }
}
