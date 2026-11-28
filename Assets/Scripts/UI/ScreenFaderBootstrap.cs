using UnityEngine;

namespace Unbreathe.UI
{
    /// <summary>
    /// Instancia Resources/ScreenFader.prefab una sola vez, antes de que cargue la primera escena.
    /// El ScreenFader se marca como DontDestroyOnLoad y sobrevive a todos los cambios de escena,
    /// así que no hay que agregar nada a ninguna escena (.unity).
    /// </summary>
    public static class ScreenFaderBootstrap
    {
        private const string RutaPrefabEnResources = "ScreenFader";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Crear()
        {
            if (ScreenFader.Instancia != null) return;

            GameObject prefab = Resources.Load<GameObject>(RutaPrefabEnResources);
            if (prefab == null)
            {
                Debug.LogWarning("[ScreenFaderBootstrap] No se encontró Resources/ScreenFader.prefab. " +
                                 "Ejecuta el menú: Unbreathe > Transicion > Crear ScreenFader Prefab. " +
                                 "Mientras tanto las escenas cambian sin fundido.");
                return;
            }

            GameObject instancia = Object.Instantiate(prefab);
            instancia.name = "ScreenFader";
        }
    }
}
