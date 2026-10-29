using UnityEngine;

namespace PrismaDash
{
    /// <summary>
    /// Ponto de entrada. Roda automaticamente depois que qualquer cena carrega,
    /// então o jogo funciona até numa cena vazia. Não precisa de prefab nenhum.
    /// </summary>
    public static class Bootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (Object.FindObjectOfType<GameManager>() != null) return;
            var go = new GameObject("PrismaDash");
            go.AddComponent<GameManager>();
        }
    }
}
