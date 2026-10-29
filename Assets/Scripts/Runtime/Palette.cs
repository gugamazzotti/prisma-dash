using UnityEngine;

namespace PrismaDash
{
    /// <summary>
    /// Cores e fábrica de materiais. Para sobreviver ao stripping de shaders do WebGL,
    /// o material "lit" é clonado do material padrão de um primitivo (Default-Material,
    /// sempre incluído) e o "unlit" usa Sprites/Default (Always Included por padrão).
    /// </summary>
    public static class Palette
    {
        public static readonly Color[] Polarity =
        {
            new Color(0.10f, 0.95f, 1.00f), // 0 = Ciano
            new Color(1.00f, 0.15f, 0.70f)  // 1 = Magenta
        };
        public static readonly Color Wall = new Color(0.18f, 0.16f, 0.24f);
        public static readonly Color Background = new Color(0.04f, 0.03f, 0.08f);

        private static Material _litTemplate;
        private static Material _unlitTemplate;

        public static Material Lit(Color c, float emission = 0.5f)
        {
            if (_litTemplate == null)
            {
                var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                _litTemplate = new Material(tmp.GetComponent<Renderer>().sharedMaterial);
                Object.Destroy(tmp);
            }
            var m = new Material(_litTemplate) { color = c };
            if (emission > 0f && m.HasProperty("_EmissionColor"))
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", c * emission);
            }
            return m;
        }

        public static Material Unlit(Color c)
        {
            if (_unlitTemplate == null)
            {
                var sh = Shader.Find("Sprites/Default");
                _unlitTemplate = sh != null ? new Material(sh) : Lit(Color.white, 0f);
            }
            return new Material(_unlitTemplate) { color = c };
        }

        /// <summary>Cria um primitivo sem collider (colisão é feita por lógica de pista).</summary>
        public static GameObject Prim(PrimitiveType t, Material m, Transform parent = null)
        {
            var go = GameObject.CreatePrimitive(t);
            Object.Destroy(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial = m;
            if (parent != null) go.transform.SetParent(parent, false);
            return go;
        }
    }
}
