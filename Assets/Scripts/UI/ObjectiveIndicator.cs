using System;
using UnityEngine;
using TMPro;
using Unbreathe.Cutscenes;

namespace Unbreathe.UI
{
    /// <summary>
    /// Gestiona la visualización del objetivo actual y el marcador direccional (waypoint) en pantalla.
    /// Proyecta la posición 3D del objetivo a coordenadas de pantalla uGUI, manteniéndolo en los bordes
    /// con clamp si está fuera del cono visual o detrás de la cámara.
    /// </summary>
    public class ObjectiveIndicator : MonoBehaviour
    {
        [Header("Referencias del Marcador en Pantalla")]
        [Tooltip("Transform 3D hacia el que apunta actualmente el indicador.")]
        [SerializeField] private Transform currentTarget;

        [Tooltip("Contenedor raíz del marcador (ícono y distancia).")]
        [SerializeField] private GameObject markerRoot;

        [Tooltip("RectTransform del marcador para moverlo en pantalla.")]
        [SerializeField] private RectTransform markerRect;

        [Tooltip("CanvasGroup para controlar la transparencia del marcador.")]
        [SerializeField] private CanvasGroup markerCanvasGroup;

        [Tooltip("Etiqueta de texto que muestra la distancia en metros.")]
        [SerializeField] private TMP_Text distanceLabel;

        [Header("Etiqueta de Objetivo")]
        [Tooltip("Texto en la esquina superior derecha que describe la tarea actual.")]
        [SerializeField] private TMP_Text objectiveLabel;

        [Header("Configuración de Visualización")]
        [Tooltip("Margen en píxeles respecto al borde de la pantalla al fijar el marcador en off-screen.")]
        [SerializeField] private float edgeMargin = 55f;

        [Tooltip("Distancia mínima al objetivo por debajo de la cual el marcador se oculta.")]
        [SerializeField] private float hideDistance = 2.5f;

        [Header("Sistemas Vinculados")]
        [Tooltip("Referencia a la cinemática inicial para ocultar el marcador mientras corre.")]
        [SerializeField] private IntroCutsceneManager introCutsceneManager;

        // Referencias cacheadas según AGENTS.md (sin GetComponent ni Find en Update)
        private Camera mainCamera;
        private Transform playerTransform;
        private RectTransform canvasRect;

        public Transform CurrentTarget => currentTarget;

        private void Awake()
        {
            // Resolución segura de cámara y canvas en Awake
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = FindFirstObjectByType<Camera>();
            }

            Canvas parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas != null)
            {
                canvasRect = parentCanvas.GetComponent<RectTransform>();
            }

            // Localizar al Player
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }

            if (introCutsceneManager == null)
            {
                introCutsceneManager = FindFirstObjectByType<IntroCutsceneManager>();
            }

            if (markerRoot != null)
            {
                markerRoot.SetActive(false);
            }
        }

        private void Start()
        {
            if (playerTransform == null)
            {
                var playerObj = GameObject.Find("Player");
                if (playerObj != null) playerTransform = playerObj.transform;
            }
        }

        /// <summary>
        /// Asigna un nuevo objetivo 3D y actualiza la descripción en el HUD.
        /// </summary>
        public void SetObjective(Transform newTarget, string objectiveText)
        {
            currentTarget = newTarget;

            if (objectiveLabel != null && !string.IsNullOrEmpty(objectiveText))
            {
                objectiveLabel.text = objectiveText;
            }

            if (currentTarget == null && markerRoot != null)
            {
                markerRoot.SetActive(false);
            }
        }

        private void Update()
        {
            // 1. Ocultar si la cinemática de inicio sigue activa
            if (introCutsceneManager != null && !introCutsceneManager.IsCutsceneFinished)
            {
                SetMarkerActive(false);
                return;
            }

            // 2. Ocultar si no hay objetivo asignado
            if (currentTarget == null)
            {
                SetMarkerActive(false);
                return;
            }

            // 3. Comprobar distancia respecto al jugador
            if (playerTransform != null)
            {
                float distance = Vector3.Distance(playerTransform.position, currentTarget.position);
                if (distance <= hideDistance)
                {
                    SetMarkerActive(false);
                    return;
                }

                if (distanceLabel != null)
                {
                    distanceLabel.text = $"{Mathf.RoundToInt(distance)} m";
                }
            }

            if (mainCamera == null || markerRect == null || canvasRect == null)
            {
                return;
            }

            // 4. Proyección de posición 3D a coordenadas de pantalla
            Vector3 worldPos = currentTarget.position + Vector3.up * 0.25f; // Ligera elevación visual
            Vector3 screenPoint = mainCamera.WorldToScreenPoint(worldPos);
            bool isBehind = screenPoint.z < 0;

            Vector2 screenPos = new Vector2(screenPoint.x, screenPoint.y);

            if (isBehind)
            {
                // Invertir respecto al centro de pantalla para orientar al jugador
                Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                Vector2 fromCenter = screenPos - screenCenter;
                screenPos = screenCenter - fromCenter;
            }

            // 5. Fijar (clamp) en los bordes de la pantalla respetando el margen
            screenPos.x = Mathf.Clamp(screenPos.x, edgeMargin, Screen.width - edgeMargin);
            screenPos.y = Mathf.Clamp(screenPos.y, edgeMargin, Screen.height - edgeMargin);

            // 6. Convertir coordenadas de pantalla a posición local dentro del Canvas
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, null, out Vector2 localPos))
            {
                markerRect.anchoredPosition = localPos;
            }

            SetMarkerActive(true);
        }

        private void SetMarkerActive(bool active)
        {
            if (markerRoot != null && markerRoot.activeSelf != active)
            {
                markerRoot.SetActive(active);
            }

            if (markerCanvasGroup != null)
            {
                markerCanvasGroup.alpha = active ? 1f : 0f;
            }
        }
    }
}
