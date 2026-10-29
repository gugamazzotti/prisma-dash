using UnityEngine;
using UnityEngine.UI;

namespace PrismaDash
{
    /// <summary>UI montada em código com Text legado (sem TextMeshPro = sem import de assets).</summary>
    public class Hud : MonoBehaviour
    {
        public const float ReferenceWidth = 1080f;
        public const float ReferenceHeight = 1920f;

        public Text Score, Center, Combo;
        private CanvasScaler _scaler;
        private RectTransform _scoreRt, _comboRt, _centerRt;

        public static Hud Build()
        {
            var go = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var hud = go.AddComponent<Hud>();
            hud._scaler = scaler;
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            hud.Score = MakeText(go.transform, font, 52, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(40f, -28f), new Vector2(640f, 200f), false);
            hud.Combo = MakeText(go.transform, font, 40, TextAnchor.UpperRight, new Vector2(1f, 1f), new Vector2(-40f, -28f), new Vector2(640f, 200f), false);
            hud.Center = MakeText(go.transform, font, 40, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(960f, 1500f), true);
            hud._scoreRt = hud.Score.rectTransform;
            hud._comboRt = hud.Combo.rectTransform;
            hud._centerRt = hud.Center.rectTransform;
            hud.ApplyLayout();
            return hud;
        }

        private void Update() => ApplyLayout();

        private void ApplyLayout()
        {
            if (_scaler == null) return;
            float aspect = Screen.width / Mathf.Max(1f, Screen.height);
            float refAspect = ReferenceWidth / ReferenceHeight;
            // Mais largo que 9:16: casa com a altura (sobra nas laterais). Mais alto: casa com a largura.
            _scaler.matchWidthOrHeight = aspect >= refAspect ? 1f : 0f;

            var safe = Screen.safeArea;
            float left = safe.xMin / Mathf.Max(1f, Screen.width);
            float right = safe.xMax / Mathf.Max(1f, Screen.width);
            float top = safe.yMax / Mathf.Max(1f, Screen.height);

            if (_scoreRt != null)
            {
                _scoreRt.anchorMin = _scoreRt.anchorMax = new Vector2(left, top);
                _scoreRt.pivot = new Vector2(0f, 1f);
                _scoreRt.anchoredPosition = new Vector2(40f, -28f);
            }
            if (_comboRt != null)
            {
                _comboRt.anchorMin = _comboRt.anchorMax = new Vector2(right, top);
                _comboRt.pivot = new Vector2(1f, 1f);
                _comboRt.anchoredPosition = new Vector2(-40f, -28f);
            }
            if (_centerRt != null)
            {
                _centerRt.anchorMin = _centerRt.anchorMax = new Vector2(0.5f, 0.5f);
                _centerRt.pivot = new Vector2(0.5f, 0.5f);
                _centerRt.anchoredPosition = Vector2.zero;
            }
        }

        private static Text MakeText(Transform parent, Font font, int size, TextAnchor anchor, Vector2 pivot, Vector2 pos, Vector2 sizeDelta, bool wrap)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text), typeof(Outline));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = sizeDelta;
            var t = go.GetComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.alignment = anchor;
            t.horizontalOverflow = wrap ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.color = Color.white;
            t.supportRichText = true;
            t.raycastTarget = false;
            go.GetComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.65f);
            return t;
        }
    }
}
