using UnityEngine;

namespace PrismaDash
{
    /// <summary>Sons sintetizados em código (sem assets). Compatível com WebGL.</summary>
    public class ProceduralAudio : MonoBehaviour
    {
        private const int Rate = 44100;
        private AudioSource _src;
        private AudioClip _switch, _pass, _orb, _death, _move, _graze;

        private void Awake()
        {
            _src = gameObject.AddComponent<AudioSource>();
            _src.playOnAwake = false;
            _switch = Sweep("switch", 380f, 900f, 0.09f, 0.35f);
            _pass   = Sweep("pass", 520f, 1040f, 0.12f, 0.30f);
            _orb    = Sweep("orb", 990f, 1480f, 0.07f, 0.25f);
            _move   = Sweep("move", 220f, 260f, 0.04f, 0.20f);
            _graze  = Sweep("graze", 740f, 1560f, 0.07f, 0.32f);
            _death  = Noise("death", 0.6f, 0.5f);
        }

        public void Switch() => _src.PlayOneShot(_switch);
        public void Move() => _src.PlayOneShot(_move);
        public void Graze() { _src.pitch = 1.25f; _src.PlayOneShot(_graze); }
        public void Death() => _src.PlayOneShot(_death);
        public void Pass(float pitch) { _src.pitch = pitch; _src.PlayOneShot(_pass); }
        public void Orb(int combo) { _src.pitch = 1f + Mathf.Min(combo, 12) * 0.06f; _src.PlayOneShot(_orb); }

        private static AudioClip Sweep(string name, float f0, float f1, float dur, float vol)
        {
            int n = (int)(Rate * dur);
            var data = new float[n];
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                phase += 2f * Mathf.PI * Mathf.Lerp(f0, f1, t) / Rate;
                float env = (1f - t) * Mathf.Min(1f, t * 40f);
                // Mistura seno + quadrada leve para um timbre "chiptune".
                data[i] = vol * env * (0.7f * Mathf.Sin(phase) + 0.3f * Mathf.Sign(Mathf.Sin(phase)));
            }
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip Noise(string name, float dur, float vol)
        {
            int n = (int)(Rate * dur);
            var data = new float[n];
            var rng = new System.Random(7);
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                lp = Mathf.Lerp(lp, (float)rng.NextDouble() * 2f - 1f, 0.15f + 0.5f * (1f - t));
                data[i] = vol * (1f - t) * (1f - t) * lp;
            }
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
