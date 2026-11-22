using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Unbreathe.Interaction
{
    /// <summary>
    /// Aviso en pantalla para interactuables (puertas, objetos...) con fade-in / fade-out.
    /// Dos fuentes para saber qué interactuable está cerca (sin modificar PlayerInteraction):
    /// 1) El evento PlayerInteraction.OnInteractableTargetChanged (triggers).
    /// 2) Un sondeo de cercanía cada 0.1 s con el MISMO criterio que usa la tecla E
    ///    (OverlapSphere alrededor del jugador). Cubre interactuables cuyo trigger no llega a
    ///    registrarse, como la puerta de Primer Plano: si la E funcionaría, el aviso aparece.
    /// - El texto sale de IInteractable.GetPromptText(), así sirve para cualquier puerta u objeto.
    /// - Se oculta solo mientras el juego está en pausa (Time.timeScale = 0).
    /// - El fade usa tiempo sin escalar para no quedarse congelado a media animación.
    /// Convive con InteractionPromptUI (el aviso original de Segundo plano) sin modificarlo.
    /// </summary>
    [DisallowMultipleComponent]
    public class InteractionPromptView : MonoBehaviour
    {
        [Header("Referencias Visuales")]
        [SerializeField] private CanvasGroup grupo;
        [SerializeField] private TextMeshProUGUI etiqueta;

        [Header("Fade")]
        [Tooltip("Segundos que tarda en aparecer el aviso.")]
        [SerializeField] private float duracionFadeIn = 0.25f;

        [Tooltip("Segundos que tarda en desaparecer el aviso.")]
        [SerializeField] private float duracionFadeOut = 0.2f;

        [Header("Detección por cercanía (respaldo)")]
        [Tooltip("Radio de detección. Debe coincidir con 'Interaction Radius' del Player (por defecto 2).")]
        [SerializeField] private float radioDeteccion = 2f;

        [Tooltip("Cada cuántos segundos se revisa la cercanía.")]
        [SerializeField] private float intervaloSondeo = 0.1f;

        [Header("Jugador (opcional: se autovincula en Start)")]
        [SerializeField] private PlayerInteraction playerInteraction;

        // Mensajes de diagnóstico en la Console. Poner en true solo para depurar.
        private const bool Diagnostico = false;

        private const int TamanoBuffer = 64;

        private IInteractable objetivoActual;
        private IInteractable objetivoPorEvento;
        private IInteractable objetivoPorCercania;
        private bool hayObjetivo;

        private float tiempoDesdeSondeo;
        private Transform transformJugador;
        private readonly Collider[] bufferColliders = new Collider[TamanoBuffer];

        // Caché por collider: evita llamar GetComponent en cada sondeo (regla AGENTS.md).
        private readonly Dictionary<int, IInteractable> cacheInteractables = new Dictionary<int, IInteractable>();

        private void Awake()
        {
            if (grupo == null)
            {
                grupo = GetComponentInChildren<CanvasGroup>();
            }

            if (grupo != null)
            {
                grupo.alpha = 0f;
                grupo.interactable = false;
                grupo.blocksRaycasts = false;
            }
        }

        private void Start()
        {
            if (playerInteraction == null)
            {
                playerInteraction = FindFirstObjectByType<PlayerInteraction>();
            }

            if (playerInteraction == null)
            {
                Debug.LogWarning("[InteractionPromptView] No hay PlayerInteraction en la escena. El aviso no se mostrará.");
                enabled = false;
                return;
            }

            transformJugador = playerInteraction.transform;
            playerInteraction.OnInteractableTargetChanged += AlCambiarObjetivoPorEvento;

            if (Diagnostico) Debug.Log("<color=yellow>[InteractionPromptView] Suscrito a PlayerInteraction en '" + playerInteraction.name + "'.</color>");

            // Por si el jugador ya está dentro de un trigger cuando se crea el aviso.
            AlCambiarObjetivoPorEvento(playerInteraction.CurrentInteractable);
        }

        private void OnDestroy()
        {
            if (playerInteraction != null)
            {
                playerInteraction.OnInteractableTargetChanged -= AlCambiarObjetivoPorEvento;
            }
        }

        // ------------------------------------------------------------------
        // Fuentes del objetivo
        // ------------------------------------------------------------------

        private void AlCambiarObjetivoPorEvento(IInteractable interactable)
        {
            objetivoPorEvento = EsValido(interactable) ? interactable : null;
            ReevaluarObjetivo("evento");
        }

        private void SondearCercania()
        {
            if (transformJugador == null) return;

            int cantidad = UnityEngine.Physics.OverlapSphereNonAlloc(
                transformJugador.position,
                radioDeteccion,
                bufferColliders,
                ~0,
                QueryTriggerInteraction.Collide);

            IInteractable encontrado = null;

            for (int i = 0; i < cantidad; i++)
            {
                IInteractable candidato = ObtenerInteractable(bufferColliders[i]);
                if (EsValido(candidato))
                {
                    encontrado = candidato;
                    break;
                }
            }

            objetivoPorCercania = encontrado;
            ReevaluarObjetivo("cercanía");
        }

        private IInteractable ObtenerInteractable(Collider col)
        {
            if (col == null) return null;

            int id = col.GetInstanceID();
            if (cacheInteractables.TryGetValue(id, out IInteractable guardado))
            {
                return guardado;
            }

            IInteractable interactable = col.GetComponent<IInteractable>() ?? col.GetComponentInParent<IInteractable>();
            cacheInteractables[id] = interactable; // también se guarda null: ese collider no es interactuable
            return interactable;
        }

        private static bool EsValido(IInteractable interactable)
        {
            if (interactable == null) return false;
            if (interactable is UnityEngine.Object objetoUnity && objetoUnity == null) return false; // destruido
            return interactable.CanInteract();
        }

        private void ReevaluarObjetivo(string origen)
        {
            IInteractable porEvento = EsValido(objetivoPorEvento) ? objetivoPorEvento : null;
            IInteractable porCercania = EsValido(objetivoPorCercania) ? objetivoPorCercania : null;
            IInteractable nuevo = porEvento != null ? porEvento : porCercania;

            if (ReferenceEquals(nuevo, objetivoActual)) return;

            objetivoActual = nuevo;
            hayObjetivo = nuevo != null;

            if (nuevo != null && etiqueta != null)
            {
                etiqueta.text = nuevo.GetPromptText();
            }
            // Si no hay objetivo, el texto se conserva para que el fade-out no muestre una caja vacía.

            if (Diagnostico)
            {
                string detalle = nuevo == null ? "ninguno" : "'" + nuevo.GetPromptText() + "'";
                Debug.Log("<color=yellow>[InteractionPromptView] Objetivo cambiado (" + origen + "): " + detalle + "</color>");
            }
        }

        // ------------------------------------------------------------------
        // Actualización: sondeo periódico + fade
        // ------------------------------------------------------------------

        private void Update()
        {
            if (grupo == null) return;

            // Sondeo de cercanía cada 'intervaloSondeo' segundos (sin Find ni GetComponent aquí).
            tiempoDesdeSondeo += Time.unscaledDeltaTime;
            if (tiempoDesdeSondeo >= intervaloSondeo)
            {
                tiempoDesdeSondeo = 0f;
                SondearCercania();
            }
            else if (hayObjetivo && !EsValido(objetivoActual))
            {
                // El objetivo dejó de ser interactuable (por ejemplo, la puerta empezó a cargar).
                objetivoPorEvento = null;
                objetivoPorCercania = null;
                ReevaluarObjetivo("estado");
            }

            bool enPausa = Time.timeScale <= 0.0001f;
            float alphaObjetivo = (hayObjetivo && !enPausa) ? 1f : 0f;

            if (Mathf.Approximately(grupo.alpha, alphaObjetivo)) return;

            float duracion = alphaObjetivo > grupo.alpha ? duracionFadeIn : duracionFadeOut;
            float paso = duracion > 0.0001f ? Time.unscaledDeltaTime / duracion : 1f;
            grupo.alpha = Mathf.MoveTowards(grupo.alpha, alphaObjetivo, paso);
        }
    }
}
