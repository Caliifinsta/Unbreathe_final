using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unbreathe.Physics;
using Unbreathe.Sanity;
using Unbreathe.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Unbreathe.Memories
{
    /// <summary>
    /// Estructura visual para cada tarjeta Polaroid en el Álbum de Recuerdos.
    /// </summary>
    [System.Serializable]
    public class PolaroidCardSlot
    {
        public GameObject cardRoot;
        public Image photoImage;
        public TMP_Text titleLabel;
        public GameObject lockOverlay; // Máscara oscura con "?"
        public Button cardButton;
    }

    /// <summary>
    /// Gestor del Álbum de Recuerdos estilo Polaroid (Fase 3).
    /// Controla la apertura con Tab/I, almacena el estado de recolección de los 3 recuerdos,
    /// actualiza la interfaz visual y coordina la transición hacia la puerta de salida al completar 3/3.
    /// </summary>
    public class MemoryAlbumManager : MonoBehaviour
    {
        public static MemoryAlbumManager Instance { get; private set; }

        [Header("Lista de Recuerdos")]
        [SerializeField] private MemoryData[] allMemories = new MemoryData[3];

        [Header("Interfaz del Álbum")]
        [Tooltip("Contenedor raíz del panel del álbum.")]
        [SerializeField] private GameObject albumPanelRoot;
        [SerializeField] private CanvasGroup albumCanvasGroup;
        [SerializeField] private TMP_Text albumTitleLabel;
        [SerializeField] private TMP_Text counterLabel;
        [SerializeField] private Button closeButton;

        [Header("Ranuras Polaroid")]
        [SerializeField] private PolaroidCardSlot[] cardSlots = new PolaroidCardSlot[3];

        [Header("Referencias a Otros Sistemas")]
        [SerializeField] private MemoryFlashbackUI flashbackUI;
        [SerializeField] private ObjectiveIndicator objectiveIndicator;
        [SerializeField] private Transform doorTarget;
        [SerializeField] private PlayerController3D playerController;
        [SerializeField] private SanityManager sanityManager;

        private readonly HashSet<string> collectedMemoryIds = new HashSet<string>();
        private bool isAlbumOpen = false;

        public int CollectedCount => collectedMemoryIds.Count;
        public int TotalCount => allMemories.Length;
        public bool IsAlbumOpen => isAlbumOpen;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            if (albumPanelRoot != null)
            {
                albumPanelRoot.SetActive(false);
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(CloseAlbum);
            }

            // Configurar botones de las tarjetas Polaroid
            for (int i = 0; i < cardSlots.Length; i++)
            {
                int index = i;
                if (cardSlots[index].cardButton != null)
                {
                    cardSlots[index].cardButton.onClick.AddListener(() => OnCardClicked(index));
                }
            }

            ResolveReferences();
        }

        private void Start()
        {
            ResolveReferences();
            UpdateAlbumDisplay();
        }

        private void ResolveReferences()
        {
            if (playerController == null)
            {
                playerController = FindFirstObjectByType<PlayerController3D>();
            }

            if (sanityManager == null)
            {
                sanityManager = FindFirstObjectByType<SanityManager>();
            }

            if (objectiveIndicator == null)
            {
                objectiveIndicator = FindFirstObjectByType<ObjectiveIndicator>();
            }

            if (flashbackUI == null)
            {
                flashbackUI = FindFirstObjectByType<MemoryFlashbackUI>();
            }

            if (doorTarget == null)
            {
                var door = GameObject.Find("Puerta_Transicion_SegundoPlano");
                if (door != null) doorTarget = door.transform;
            }
        }

        private void Update()
        {
            // No permitir abrir el álbum si se está reproduciendo un flashback
            if (flashbackUI != null && flashbackUI.IsPlaying) return;

            // Abrir / Cerrar con tecla Tab o I
            bool togglePressed = false;
            bool escapePressed = false;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.tabKey.wasPressedThisFrame || Keyboard.current.iKey.wasPressedThisFrame)
                {
                    togglePressed = true;
                }
                if (Keyboard.current.escapeKey.wasPressedThisFrame)
                {
                    escapePressed = true;
                }
            }
#else
            if (UnityEngine.Input.GetKeyDown(KeyCode.Tab) || UnityEngine.Input.GetKeyDown(KeyCode.I))
            {
                togglePressed = true;
            }
            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
            {
                escapePressed = true;
            }
#endif

            if (togglePressed)
            {
                if (isAlbumOpen) CloseAlbum();
                else OpenAlbum();
            }
            else if (escapePressed && isAlbumOpen)
            {
                CloseAlbum();
            }
        }

        public void OpenAlbum()
        {
            if (isAlbumOpen) return;
            isAlbumOpen = true;

            ResolveReferences();

            // Pausar controles y cordura
            if (playerController != null) playerController.enabled = false;
            if (sanityManager != null) sanityManager.SetAutoDecay(false);

            if (albumPanelRoot != null) albumPanelRoot.SetActive(true);
            if (albumCanvasGroup != null)
            {
                albumCanvasGroup.alpha = 1f;
                albumCanvasGroup.interactable = true;
                albumCanvasGroup.blocksRaycasts = true;
            }

            UpdateAlbumDisplay();
        }

        public void CloseAlbum()
        {
            if (!isAlbumOpen) return;
            isAlbumOpen = false;

            if (albumPanelRoot != null) albumPanelRoot.SetActive(false);
            if (albumCanvasGroup != null)
            {
                albumCanvasGroup.alpha = 0f;
                albumCanvasGroup.interactable = false;
                albumCanvasGroup.blocksRaycasts = false;
            }

            // Devolver control solo si no hay flashback activo
            if (flashbackUI == null || !flashbackUI.IsPlaying)
            {
                if (playerController != null) playerController.enabled = true;
                if (sanityManager != null) sanityManager.SetAutoDecay(true);
            }
        }

        /// <summary>
        /// Registra la recolección de un recuerdo en la calle y reproduce su flashback cinemático.
        /// </summary>
        public void CollectMemory(MemoryData data)
        {
            if (data == null) return;

            bool isFirst = !collectedMemoryIds.Contains(data.memoryId);
            if (isFirst)
            {
                collectedMemoryIds.Add(data.memoryId);
            }

            UpdateAlbumDisplay();

            // Actualizar objetivo en el HUD
            if (collectedMemoryIds.Count >= allMemories.Length)
            {
                if (objectiveIndicator != null && doorTarget != null)
                {
                    objectiveIndicator.SetObjective(doorTarget, "Objetivo: Cruza la puerta hacia El Departamento");
                }
            }
            else
            {
                if (objectiveIndicator != null)
                {
                    objectiveIndicator.SetObjective(null, $"Objetivo: Explora la calle y busca tus recuerdos ({collectedMemoryIds.Count}/3)");
                }
            }

            // Reproducir Flashback
            if (flashbackUI != null)
            {
                flashbackUI.PlayFlashback(data, isFirst, () =>
                {
                    // Al terminar flashback, si todos están recolectados, asegurar que el objetivo apunta a la puerta
                    if (collectedMemoryIds.Count >= allMemories.Length && objectiveIndicator != null && doorTarget != null)
                    {
                        objectiveIndicator.SetObjective(doorTarget, "Objetivo: Cruza la puerta hacia El Departamento");
                    }
                });
            }
        }

        private void OnCardClicked(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= allMemories.Length) return;
            var data = allMemories[slotIndex];
            if (data == null) return;

            // Solo se puede reproducir si ya fue recolectado
            if (collectedMemoryIds.Contains(data.memoryId))
            {
                CloseAlbum();
                if (flashbackUI != null)
                {
                    flashbackUI.PlayFlashback(data, isFirstDiscovery: false, null);
                }
            }
        }

        private void UpdateAlbumDisplay()
        {
            if (counterLabel != null)
            {
                counterLabel.text = $"Recuerdos: {collectedMemoryIds.Count} / {allMemories.Length}";
            }

            for (int i = 0; i < cardSlots.Length; i++)
            {
                if (i >= allMemories.Length) continue;

                var data = allMemories[i];
                var slot = cardSlots[i];
                if (slot == null || slot.cardRoot == null) continue;

                bool isUnlocked = data != null && collectedMemoryIds.Contains(data.memoryId);

                if (isUnlocked)
                {
                    if (slot.lockOverlay != null) slot.lockOverlay.SetActive(false);
                    if (slot.titleLabel != null) slot.titleLabel.text = data.title;
                    if (slot.photoImage != null)
                    {
                        if (data.photo != null)
                        {
                            slot.photoImage.sprite = data.photo;
                            slot.photoImage.color = Color.white;
                        }
                        else
                        {
                            slot.photoImage.sprite = null;
                            slot.photoImage.color = new Color(0.25f, 0.32f, 0.40f, 1f);
                        }
                    }
                    if (slot.cardButton != null) slot.cardButton.interactable = true;
                }
                else
                {
                    if (slot.lockOverlay != null) slot.lockOverlay.SetActive(true);
                    if (slot.titleLabel != null) slot.titleLabel.text = "???";
                    if (slot.photoImage != null)
                    {
                        slot.photoImage.sprite = null;
                        slot.photoImage.color = new Color(0.12f, 0.14f, 0.18f, 1f);
                    }
                    if (slot.cardButton != null) slot.cardButton.interactable = false;
                }
            }
        }
    }
}
