using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unbreathe.UI
{
    /// <summary>
    /// Instancia automáticamente Resources/PauseMenu.prefab en cada escena de juego,
    /// salvo en el menú principal. Así no hay que modificar ninguna escena (.unity)
    /// y se evitan conflictos de merge con el resto del equipo.
    /// </summary>
    public static class PauseMenuBootstrap
    {
        private const string RutaPrefabEnResources = "PauseMenu";
        private const string EscenaSinPausa = "MainMenu";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Inicializar()
        {
            // Este método corre UNA sola vez al arrancar; para las escenas siguientes
            // hay que escuchar sceneLoaded. El -= evita doble suscripción si el Editor
            // tiene desactivado el Domain Reload.
            SceneManager.sceneLoaded -= AlCargarEscena;
            SceneManager.sceneLoaded += AlCargarEscena;

            // La primera escena ya se cargó antes de suscribirnos.
            CrearMenuDePausa(SceneManager.GetActiveScene());
        }

        private static void AlCargarEscena(Scene escena, LoadSceneMode modo)
        {
            if (modo != LoadSceneMode.Single) return;
            CrearMenuDePausa(escena);
        }

        private static void CrearMenuDePausa(Scene escena)
        {
            if (escena.name == EscenaSinPausa) return;
            if (Object.FindFirstObjectByType<MenuPausa>() != null) return;

            GameObject prefab = Resources.Load<GameObject>(RutaPrefabEnResources);
            if (prefab == null)
            {
                Debug.LogWarning("[PauseMenuBootstrap] No se encontró Resources/PauseMenu.prefab. " +
                                 "Ejecuta el menú: Unbreathe > Pausa > Crear PauseMenu Prefab.");
                return;
            }

            GameObject instancia = Object.Instantiate(prefab);
            instancia.name = "PauseMenu";
        }
    }
}
