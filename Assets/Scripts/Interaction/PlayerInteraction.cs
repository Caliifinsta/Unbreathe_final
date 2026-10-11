using System;
using System.Collections.Generic;
using UnityEngine;
using Unbreathe.Input;

namespace Unbreathe.Interaction
{
    /// <summary>
    /// Gestiona la interacción del jugador con el entorno y la recolección de objetos con la tecla 'E'.
    /// Regla de rendimiento AGENTS.md: Prohibido usar GetComponent() en Update().
    /// Utiliza eventos OnTriggerEnter/Exit para cachear interactuables sin sobrecargar Update().
    /// </summary>
    [RequireComponent(typeof(PlayerInputHandler))]
    [DisallowMultipleComponent]
    public class PlayerInteraction : MonoBehaviour
    {
        [Header("Configuración de Interacción")]
        [Tooltip("Radio de detección de objetos interactuables en 3D si se usa chequeo por proximidad.")]
        [SerializeField] private float interactionRadius = 2.0f;

        [Tooltip("Capa (Layer) de objetos interactuables para optimizar las consultas físicas.")]
        [SerializeField] private LayerMask interactableLayer = ~0;

        // Referencias cacheadas
        private PlayerInputHandler inputHandler;
        private readonly List<IInteractable> nearbyInteractables = new List<IInteractable>();

        public IInteractable CurrentInteractable => nearbyInteractables.Count > 0 ? nearbyInteractables[0] : null;

        public event Action<IInteractable> OnInteractableTargetChanged;

        private void Awake()
        {
            // Regla AGENTS.md: Almacenar referencias en caché en Awake()
            inputHandler = GetComponent<PlayerInputHandler>();
        }

        private void OnEnable()
        {
            if (inputHandler != null)
            {
                inputHandler.OnInteractPressed += HandleInteractPressed;
            }
        }

        private void OnDisable()
        {
            if (inputHandler != null)
            {
                inputHandler.OnInteractPressed -= HandleInteractPressed;
            }
        }

        private void HandleInteractPressed()
        {
            // Limpiar posibles elementos nulos o destruidos
            nearbyInteractables.RemoveAll(item => item == null || (item is UnityEngine.Object uObj && uObj == null));

            // Si la lista está vacía, comprobación de proximidad en el momento exacto de presionar 'E'
            if (nearbyInteractables.Count == 0)
            {
                Collider[] colliders = UnityEngine.Physics.OverlapSphere(transform.position, interactionRadius, interactableLayer);
                for (int i = 0; i < colliders.Length; i++)
                {
                    IInteractable found = colliders[i].GetComponent<IInteractable>() ?? colliders[i].GetComponentInParent<IInteractable>();
                    if (found != null && found.CanInteract() && !nearbyInteractables.Contains(found))
                    {
                        nearbyInteractables.Add(found);
                        break;
                    }
                }
            }

            if (nearbyInteractables.Count > 0)
            {
                IInteractable target = nearbyInteractables[0];
                if (target != null && target.CanInteract())
                {
                    target.Interact(gameObject);

                    // Si ya no se puede interactuar tras la acción, removerlo
                    if (!target.CanInteract())
                    {
                        nearbyInteractables.Remove(target);
                        OnInteractableTargetChanged?.Invoke(CurrentInteractable);
                    }
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // Regla AGENTS.md: GetComponent() se llama únicamente en el momento del contacto físico, jamás en Update().
            if (((1 << other.gameObject.layer) & interactableLayer) == 0) return;

            IInteractable interactable = other.GetComponent<IInteractable>() ?? other.GetComponentInParent<IInteractable>();
            if (interactable != null && !nearbyInteractables.Contains(interactable))
            {
                nearbyInteractables.Add(interactable);
                OnInteractableTargetChanged?.Invoke(CurrentInteractable);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (((1 << other.gameObject.layer) & interactableLayer) == 0) return;

            IInteractable interactable = other.GetComponent<IInteractable>() ?? other.GetComponentInParent<IInteractable>();
            if (interactable != null && nearbyInteractables.Contains(interactable))
            {
                nearbyInteractables.Remove(interactable);
                OnInteractableTargetChanged?.Invoke(CurrentInteractable);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, interactionRadius);
        }
    }
}
