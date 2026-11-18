using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unbreathe.Interaction
{
    /// <summary>
    /// Instancia automáticamente Resources/InteractionPrompt.prefab en cada escena que tenga
    /// un jugador con PlayerInteraction y que todavía no tenga un aviso de interacción.
    /// - Primer Plano: no tiene aviso, se crea uno.
    /// - Segundo plano: ya tiene InteractionPromptUI, se respeta tal cual.
    /// - MainMenu / escenas sin jugador: no se crea nada.
    /// Así no se modifica ninguna escena (.unity) ni hay conflictos de merge.
    /// </summary>
    public static class InteractionPromptBootstrap
    {
        private const string RutaPrefabEnResources = "InteractionPrompt";

        // Mensajes temporales de diagnóstico en la Console. Poner en false cuando todo funcione.
        private const bool Diagnostico = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Inicializar()
        {
            // Corre una sola vez al arrancar; las escenas siguientes llegan por sceneLoaded.
            // El -= evita doble suscripción si el Editor tiene el Domain Reload desactivado.
            SceneManager.sceneLoaded -= AlCargarEscena;
            SceneManager.sceneLoaded += AlCargarEscena;

            CrearAviso();
        }

        private static void AlCargarEscena(Scene escena, LoadSceneMode modo)
        {
            if (modo != LoadSceneMode.Single) return;
            CrearAviso();
        }

        private static void CrearAviso()
        {
            string escena = SceneManager.GetActiveScene().name;

            // Sin jugador no hay a quién avisarle.
            if (Object.FindFirstObjectByType<PlayerInteraction>() == null)
            {
                Log($"Escena '{escena}': no hay PlayerInteraction, no se crea aviso.");
                return;
            }

            // Evitar duplicados: ya hay un aviso nuevo o el original de la escena.
            if (Object.FindFirstObjectByType<InteractionPromptView>() != null)
            {
                Log($"Escena '{escena}': ya hay un InteractionPromptView, no se crea otro.");
                return;
            }

            if (Object.FindFirstObjectByType<InteractionPromptUI>() != null)
            {
                Log($"Escena '{escena}': ya tiene un InteractionPromptUI original, no se crea otro.");
                return;
            }

            GameObject prefab = Resources.Load<GameObject>(RutaPrefabEnResources);
            if (prefab == null)
            {
                Debug.LogWarning("[InteractionPromptBootstrap] No se encontró Resources/InteractionPrompt.prefab. " +
                                 "Ejecuta el menú: Unbreathe > Interaccion > Crear InteractionPrompt Prefab.");
                return;
            }

            GameObject instancia = Object.Instantiate(prefab);
            instancia.name = "InteractionPrompt";
            Log($"Escena '{escena}': aviso creado correctamente.");
        }

        private static void Log(string mensaje)
        {
            if (Diagnostico)
            {
                Debug.Log("<color=yellow>[InteractionPromptBootstrap] " + mensaje + "</color>");
            }
        }
    }
}
