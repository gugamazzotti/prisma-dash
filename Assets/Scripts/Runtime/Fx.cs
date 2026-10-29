using UnityEngine;

namespace PrismaDash
{
    /// <summary>Um único ParticleSystem reutilizado para todas as explosões (Emit manual).</summary>
    public class Fx : MonoBehaviour
    {
        private ParticleSystem _ps;

        private void Awake()
        {
            _ps = gameObject.AddComponent<ParticleSystem>();
            _ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = _ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = 0.7f;
            main.startSpeed = 7f;
            main.startSize = 0.25f;
            main.gravityModifier = 0.6f;
            main.maxParticles = 2000;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = _ps.emission; em.enabled = false;
            var shape = _ps.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = 0.3f;
            var sol = _ps.sizeOverLifetime; sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
            GetComponent<ParticleSystemRenderer>().sharedMaterial = Palette.Unlit(Color.white);
        }

        public void Burst(Vector3 pos, Color c, int count, float speed = 7f)
        {
            var p = new ParticleSystem.EmitParams { applyShapeToPosition = true };
            for (int i = 0; i < count; i++)
            {
                p.position = pos;
                p.velocity = Random.onUnitSphere * speed * Random.Range(0.4f, 1f);
                p.startColor = Color.Lerp(c, Color.white, Random.value * 0.4f);
                _ps.Emit(p, 1);
            }
        }
    }
}
