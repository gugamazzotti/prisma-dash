using UnityEngine;

namespace PrismaDash
{
    /// <summary>
    /// Câmera de perseguição enquadrada para retrato (~9:16).
    /// O FOV vertical abre em telas mais estreitas que 9:16 para as três pistas
    /// continuarem dentro do quadro; em telas mais largas o FOV de retrato é mantido.
    /// </summary>
    public class CameraRig : MonoBehaviour
    {
        public Transform Target;

        // Pose de referência: jogador no terço inferior, pista subindo pelo quadro.
        private static readonly Vector3 BaseOffset = new Vector3(0f, 7.6f, -11.2f);
        private const float Pitch = 18f;
        // Metade da largura do cenário (arcos + folga do acompanhamento lateral) que precisa caber.
        private const float VisibleHalfWidth = 4.35f;
        private const float HorizontalFill = 0.88f;
        private const float PortraitAspect = 9f / 16f;
        private const float Follow = 0.2f;

        private float _trauma;
        private Camera _cam;
        private Vector3 _basePos;

        public void Shake(float amount) => _trauma = Mathf.Clamp01(_trauma + amount);

        private void Awake()
        {
            _cam = GetComponent<Camera>();
            _basePos = BaseOffset;
            ApplyFov();
        }

        private void LateUpdate()
        {
            ApplyFov();
            float tx = Target != null ? Target.position.x * Follow : 0f;
            _basePos.x = Mathf.Lerp(_basePos.x, tx, 8f * Time.unscaledDeltaTime);
            _basePos.y = BaseOffset.y;
            _basePos.z = BaseOffset.z;

            float s = _trauma * _trauma;
            float tt = Time.unscaledTime * 25f;
            var offset = new Vector3(Mathf.PerlinNoise(tt, 0f) - 0.5f, Mathf.PerlinNoise(0f, tt) - 0.5f, 0f) * s * 1.2f;
            transform.position = _basePos + offset;
            transform.rotation = Quaternion.Euler(Pitch, 0f, (Mathf.PerlinNoise(tt, tt) - 0.5f) * s * 8f);
            _trauma = Mathf.Max(0f, _trauma - Time.unscaledDeltaTime * 1.6f);
        }

        private void ApplyFov()
        {
            if (_cam == null) return;
            float pitchRad = Pitch * Mathf.Deg2Rad;
            var forward = new Vector3(0f, -Mathf.Sin(pitchRad), Mathf.Cos(pitchRad));
            var toPlayer = new Vector3(0f, 0.6f - BaseOffset.y, -BaseOffset.z);
            float depth = Mathf.Max(1f, Vector3.Dot(toPlayer, forward));
            float aspect = Mathf.Max(0.2f, _cam.aspect);
            // Mais largo que 9:16: preserva o FOV vertical do retrato (as pistas só ganham margem).
            // Mais estreito: abre o FOV para a largura das pistas continuar visível.
            float fitAspect = Mathf.Min(aspect, PortraitAspect);
            float tanHalfH = (VisibleHalfWidth / depth) / HorizontalFill;
            float tanHalfV = tanHalfH / fitAspect;
            _cam.fieldOfView = Mathf.Clamp(2f * Mathf.Atan(tanHalfV) * Mathf.Rad2Deg, 40f, 110f);
        }
    }
}
