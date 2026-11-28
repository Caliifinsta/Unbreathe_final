using UnityEngine;
using Unbreathe.Interaction;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Unbreathe.Memories
{
    /// <summary>
    /// Punto interactivo en la calle donde yace un recuerdo (Fase 3).
    /// Detecta al jugador por proximidad y mediante el sistema estándar IInteractable,
    /// emite un brillo senoidal suave y al pulsar 'E' se recoge, registrándolo
    /// en el álbum y lanzando el flashback.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class MemoryPickup : MonoBehaviour, IInteractable
    {
        [Header("Datos del Recuerdo")]
        [SerializeField] private MemoryData memoryData;

        [Header("Detección")]
        [SerializeField] private float pickupDistance = 3.0f;

        [Header("Luz y Efecto Visual")]
        [SerializeField] private Light pointLight;
        [SerializeField] private float baseIntensity = 1.6f;
        [SerializeField] private float pulseAmount = 0.4f;
        [SerializeField] private float pulseSpeed = 2.0f;

        private Transform playerTransform;
        private bool isPlayerInRange = false;
        private bool isCollected = false;

        public MemoryData Data => memoryData;
        public bool IsCollected => isCollected;

        private void Awake()
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }
            rb.isKinematic = true;

            ResolveReferences();
        }

        private void Start()
        {
            ResolveReferences();
        }

        private void ResolveReferences()
        {
            if (playerTransform == null)
            {
                var playerObj = GameObject.FindWithTag("Player") ?? GameObject.Find("Player");
                if (playerObj != null) playerTransform = playerObj.transform;
            }

            if (pointLight == null)
            {
                pointLight = GetComponentInChildren<Light>();
            }
        }

        private void Update()
        {
            if (isCollected) return;

            // Pulso senoidal de la luz
            if (pointLight != null)
            {
                pointLight.intensity = baseIntensity + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            }

            if (playerTransform == null)
            {
                ResolveReferences();
                if (playerTransform == null) return;
            }

            // Detección por distancia directa de respaldo
            float dist = Vector3.Distance(transform.position, playerTransform.position);
            isPlayerInRange = dist <= pickupDistance;

            // Pulsar 'E' si está en rango
            if (isPlayerInRange)
            {
                bool interactPressed = false;

#if ENABLE_INPUT_SYSTEM
                if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                {
                    interactPressed = true;
                }
#else
                if (UnityEngine.Input.GetKeyDown(KeyCode.E))
                {
                    interactPressed = true;
                }
#endif

                if (interactPressed)
                {
                    Collect();
                }
            }
        }

        public void Collect()
        {
            if (isCollected) return;
            isCollected = true;
            isPlayerInRange = false;

            // Desactivar el punto de la calle
            gameObject.SetActive(false);

            // Registrar en el álbum y lanzar el flashback
            if (MemoryAlbumManager.Instance != null && memoryData != null)
            {
                MemoryAlbumManager.Instance.CollectMemory(memoryData);
            }
        }

        // Implementación de IInteractable (utilizado por InteractionPromptView oficial de Unbreathe)
        public string GetPromptText() => "[E] Recoger recuerdo";
        public bool CanInteract() => !isCollected;
        public void Interact(GameObject interactor) => Collect();
    }
}
