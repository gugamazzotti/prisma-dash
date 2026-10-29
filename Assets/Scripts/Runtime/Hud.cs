using UnityEngine;
using UnityEngine.UI;

namespace PrismaDash
{
    /// <summary>UI montada em código com Text legado (sem TextMeshPro = sem import de assets).</summary>
    public class Hud : MonoBehaviour
    {
        public const float ReferenceWidth = 1080f;
        public const float ReferenceHeight = 1920f;

        public Text Score, Center, Combo, Flash;
        private CanvasScaler _scaler;
        private RectTransform _scoreRt, _comboRt, _centerRt, _flashRt;
        private Outline _flashOutline;
        private float _flashUntil;
        private Color _flashColor = Color.white;

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
            hud.Flash = MakeText(go.transform, font, 68, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.4f), Vector2.zero, new Vector2(900f, 160f), false);
            hud.Flash.gameObject.name = "Flash";
            hud._scoreRt = hud.Score.rectTransform;
            hud._comboRt = hud.Combo.rectTransform;
            hud._centerRt = hud.Center.rectTransform;
            hud._flashRt = hud.Flash.rectTransform;
            hud._flashOutline = hud.Flash.GetComponent<Outline>();
            hud.ApplyLayout();
            return hud;
        }

        /// <summary>Texto curto no meio da pista (graze). Some sozinho.</summary>
        public void ShowFlash(string text, Color color)
        {
            _flashColor = color;
            Flash.text = $"<b>{text}</b>";
            _flashUntil = Time.unscaledTime + 0.6f;
        }

        public void ClearFlash()
        {
            _flashUntil = 0f;
            if (Flash != null) Flash.text = "";
            if (_flashRt != null) _flashRt.localScale = Vector3.one;
        }

        private void Update()
        {
            ApplyLayout();
            AnimateFlash();
        }

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
            if (_flashRt != null)
            {
                _flashRt.anchorMin = _flashRt.anchorMax = new Vector2(0.5f, 0.4f);
                _flashRt.pivot = new Vector2(0.5f, 0.5f);
                _flashRt.anchoredPosition = Vector2.zero;
            }
        }

        private void AnimateFlash()
        {
            if (Flash == null || _flashRt == null) return;
            float life = _flashUntil - Time.unscaledTime;
            if (life <= 0f)
            {
                if (!string.IsNullOrEmpty(Flash.text)) Flash.text = "";
                _flashRt.localScale = Vector3.one;
                return;
            }

            float u = 1f - Mathf.Clamp01(life / 0.6f);
            float punch = 1f + 0.4f * Mathf.Exp(-u * 7f);
            _flashRt.localScale = new Vector3(punch, punch, 1f);
            var c = _flashColor;
            c.a = life < 0.18f ? Mathf.Clamp01(life / 0.18f) : 1f;
            Flash.color = c;
            if (_flashOutline != null)
            {
                var oc = _flashOutline.effectColor;
                oc.a = 0.65f * c.a;
                _flashOutline.effectColor = oc;
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
