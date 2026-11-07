using UnityEngine;

namespace Tesela.UI
{
    /// <summary>Ajusta el contenido al área segura (muescas, barras de gestos).</summary>
    public sealed class SafeArea : MonoBehaviour
    {
        private Rect _applied;

        private void Update()
        {
            Rect safe = Screen.safeArea;
            if (safe == _applied || Screen.width == 0) return;
            _applied = safe;
            var rt = (RectTransform)transform;
            rt.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            rt.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
