using UnityEngine;

namespace PrismaDash
{
    /// <summary>
    /// Cores e materiais. Os templates vivem em Resources e referenciam shaders
    /// embutidos (Standard e Sprites/Default). Nada de Shader.Find nem do material
    /// padrão do primitivo: num build WebGL de cena vazia esses shaders são
    /// removidos e tudo fica magenta.
    /// </summary>
    public static class Palette
    {
        public static readonly Color[] Polarity =
        {
            new Color(0.10f, 0.95f, 1.00f), // 0 = cyan
            new Color(1.00f, 0.15f, 0.70f)  // 1 = magenta
        };
        public static readonly Color Wall = new Color(0.18f, 0.16f, 0.24f);
        public static readonly Color Background = new Color(0.04f, 0.03f, 0.08f);

        private static Material _litTemplate;
        private static Material _unlitTemplate;

        public static Material Lit(Color c, float emission = 0.5f)
        {
            var template = LitTemplate();
            var m = new Material(template);
            m.color = c;
            if (m.HasProperty("_EmissionColor"))
            {
                // O asset já nasce com _EMISSION, então a variante existe no player.
                // Emissão zero continua no mesmo variant, só com a cor preta.
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emission > 0f ? c * emission : Color.black);
            }
            return m;
        }

        public static Material Unlit(Color c)
        {
            var m = new Material(UnlitTemplate());
            m.color = c;
            return m;
        }

        private static Material LitTemplate()
        {
            if (_litTemplate == null)
                _litTemplate = Resources.Load<Material>("Materials/Lit");
            if (_litTemplate == null)
                Debug.LogError("[PrismaDash] Resources/Materials/Lit está ausente. O WebGL vai ficar magenta.");
            return _litTemplate;
        }

        private static Material UnlitTemplate()
        {
            if (_unlitTemplate == null)
                _unlitTemplate = Resources.Load<Material>("Materials/Unlit");
            if (_unlitTemplate == null)
                Debug.LogError("[PrismaDash] Resources/Materials/Unlit está ausente.");
            return _unlitTemplate;
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
