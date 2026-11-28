using System.Collections.Generic;
using UnityEngine;

namespace Unbreathe.Sanity
{
    /// <summary>
    /// Generador y gestor de acumulación de maniquíes observadores basado en la cordura del jugador.
    /// A menor cordura, más figuras se congregan en un radio seguro y visible alrededor de la protagonista,
    /// colocándose sobre el suelo e incrementando la sensación de acecho psicológico.
    /// </summary>
    public class ApparitionSpawner : MonoBehaviour
    {
        [Header("Referencias")]
        [Tooltip("Referencia al SanityManager. Si es nulo se busca en este mismo GameObject o por Instance.")]
        [SerializeField] private SanityManager sanityManager;

        [Tooltip("Transform del Player alrededor del cual aparecerán los maniquíes.")]
        [SerializeField] private Transform playerTransform;

        [Tooltip("Prefabs de maniquíes a instanciar (Burak Uzun).")]
        [SerializeField] private GameObject[] mannequinPrefabs;

        [Header("Umbrales y Límites de Cordura")]
        [Tooltip("Umbral de cordura por debajo del cual comienzan las apariciones (ej. 70).")]
        [Range(10f, 95f)]
        [SerializeField] private float sanityThreshold = 70f;

        [Tooltip("Cantidad máxima de maniquíes simultáneos cuando la cordura llega a 0.")]
        [Range(1, 20)]
        [SerializeField] private int maxApparitions = 10;

        [Header("Rango de Distancias")]
        [Tooltip("Distancia mínima al jugador para no bloquear su paso ni invadir bruscamente.")]
        [SerializeField] private float minSpawnDistance = 4f;

        [Tooltip("Distancia máxima al jugador para que sigan siendo claramente visibles.")]
        [SerializeField] private float maxSpawnDistance = 9f;

        [Header("Cadencia y Frecuencia")]
        [Tooltip("Intervalo en segundos entre cada intento de aparición gradual.")]
        [SerializeField] private float spawnInterval = 1.5f;

        [Tooltip("Intervalo en segundos entre la retirada de cada figura cuando la cordura se recupera.")]
        [SerializeField] private float despawnInterval = 1.5f;

        [Header("Físicas de Colocación")]
        [Tooltip("Capas de colisión consideradas suelo para apoyar la figura.")]
        [SerializeField] private LayerMask groundLayer = ~0;

        [Tooltip("Contenedor opcional en la jerarquía para mantener limpia la escena.")]
        [SerializeField] private Transform apparitionsParent;

        private readonly List<WatcherApparition> activeApparitions = new List<WatcherApparition>();
        private float spawnTimer = 0f;
        private float despawnTimer = 0f;

        private void Awake()
        {
            if (sanityManager == null)
            {
                sanityManager = GetComponent<SanityManager>();
                if (sanityManager == null) sanityManager = SanityManager.Instance;
            }

            if (playerTransform == null)
            {
                var playerObj = GameObject.FindWithTag("Player");
                if (playerObj == null) playerObj = GameObject.Find("Player");
                if (playerObj != null) playerTransform = playerObj.transform;
            }

            if (apparitionsParent == null)
            {
                var existing = transform.Find("Active_Apparitions_Container");
                if (existing != null)
                {
                    apparitionsParent = existing;
                }
                else
                {
                    var container = new GameObject("Active_Apparitions_Container");
                    container.transform.SetParent(transform);
                    apparitionsParent = container.transform;
                }
            }
        }

        private void Update()
        {
            if (sanityManager == null)
            {
                sanityManager = SanityManager.Instance;
                if (sanityManager == null) return;
            }

            if (playerTransform == null)
            {
                var playerObj = GameObject.FindWithTag("Player");
                if (playerObj == null) playerObj = GameObject.Find("Player");
                if (playerObj != null) playerTransform = playerObj.transform;
                else return;
            }

            if (mannequinPrefabs == null || mannequinPrefabs.Length == 0) return;

            CleanDeadApparitions();

            int desiredCount = CalculateDesiredApparitionCount();

            // Si hay menos maniquíes de los deseados, generar con cadencia
            if (activeApparitions.Count < desiredCount)
            {
                spawnTimer += Time.deltaTime;
                if (spawnTimer >= spawnInterval)
                {
                    spawnTimer = 0f;
                    TrySpawnMannequin();
                }
            }
            else
            {
                spawnTimer = 0f;
            }

            // Si la cordura sube y hay más maniquíes de los deseados, retirar con cadencia
            if (activeApparitions.Count > desiredCount)
            {
                despawnTimer += Time.deltaTime;
                if (despawnTimer >= despawnInterval)
                {
                    despawnTimer = 0f;
                    DismissFarthestApparition();
                }
            }
            else
            {
                despawnTimer = 0f;
            }
        }

        /// <summary>
        /// Calcula cuántos maniquíes deben estar presentes en base a la cordura actual.
        /// </summary>
        private int CalculateDesiredApparitionCount()
        {
            float current = sanityManager.CurrentSanity;

            if (current >= sanityThreshold)
            {
                return 0;
            }

            // Interpolación entre 1 y maxApparitions desde sanityThreshold hasta 0
            float t = 1f - Mathf.Clamp01(current / sanityThreshold);
            return Mathf.Max(1, Mathf.RoundToInt(Mathf.Lerp(1, maxApparitions, t)));
        }

        /// <summary>
        /// Intenta calcular una posición válida en un anillo alrededor del jugador y colocar un maniquí sobre el suelo.
        /// </summary>
        private void TrySpawnMannequin()
        {
            // Intentar hasta 12 muestras de ángulo y distancia aleatoria
            for (int attempt = 0; attempt < 12; attempt++)
            {
                // Alternar intentos: la mitad en el cono frontal visible del jugador (-60° a +60°)
                // y la otra mitad en 360° para envolver el entorno.
                float angleDegrees;
                if (attempt % 2 == 0)
                {
                    float forwardAngle = playerTransform.eulerAngles.y;
                    angleDegrees = forwardAngle + Random.Range(-65f, 65f);
                }
                else
                {
                    angleDegrees = Random.Range(0f, 360f);
                }

                float angleRad = angleDegrees * Mathf.Deg2Rad;
                float distance = Random.Range(minSpawnDistance, maxSpawnDistance);

                Vector3 candidateHorizontal = playerTransform.position + new Vector3(
                    Mathf.Sin(angleRad) * distance,
                    0f,
                    Mathf.Cos(angleRad) * distance
                );

                // Evitar superposición excesiva con maniquíes ya colocados (mínimo 1.5 metros de distancia entre sí)
                if (IsTooCloseToExisting(candidateHorizontal, 1.5f))
                {
                    continue;
                }

                // Raycast vertical para detectar suelo físico real (Regla AGENTS.md: calificar UnityEngine.Physics)
                Vector3 rayOrigin = candidateHorizontal + Vector3.up * 6f;
                Vector3 spawnPosition = candidateHorizontal;

                if (UnityEngine.Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 20f, groundLayer, QueryTriggerInteraction.Ignore))
                {
                    spawnPosition = hit.point;
                }
                else
                {
                    // Fallback seguro a la altura de los pies del jugador
                    spawnPosition.y = playerTransform.position.y;
                }

                // Rotación inicial hacia Karol
                Vector3 dirToPlayer = playerTransform.position - spawnPosition;
                dirToPlayer.y = 0f;
                Quaternion initialRot = dirToPlayer.sqrMagnitude > 0.001f
                    ? Quaternion.LookRotation(dirToPlayer.normalized)
                    : Quaternion.identity;

                GameObject chosenPrefab = mannequinPrefabs[Random.Range(0, mannequinPrefabs.Length)];

                if (chosenPrefab != null)
                {
                    GameObject instance = Instantiate(chosenPrefab, spawnPosition, initialRot, apparitionsParent);

                    // Garantizar componente WatcherApparition
                    WatcherApparition watcher = instance.GetComponent<WatcherApparition>();
                    if (watcher == null)
                    {
                        watcher = instance.AddComponent<WatcherApparition>();
                    }
                    watcher.Initialize(playerTransform);

                    activeApparitions.Add(watcher);
                    return;
                }
            }
        }

        private bool IsTooCloseToExisting(Vector3 position, float minSeparation)
        {
            float sqrSeparation = minSeparation * minSeparation;
            foreach (var watcher in activeApparitions)
            {
                if (watcher != null && !watcher.IsDismissing)
                {
                    Vector3 diff = watcher.transform.position - position;
                    diff.y = 0f;
                    if (diff.sqrMagnitude < sqrSeparation)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Despide el maniquí más alejado del jugador cuando la cordura se recupera.
        /// </summary>
        private void DismissFarthestApparition()
        {
            WatcherApparition farthest = null;
            float maxDistSqr = -1f;

            foreach (var watcher in activeApparitions)
            {
                if (watcher != null && !watcher.IsDismissing)
                {
                    float distSqr = (watcher.transform.position - playerTransform.position).sqrMagnitude;
                    if (distSqr > maxDistSqr)
                    {
                        maxDistSqr = distSqr;
                        farthest = watcher;
                    }
                }
            }

            if (farthest != null)
            {
                farthest.Dismiss();
                activeApparitions.Remove(farthest);
            }
        }

        private void CleanDeadApparitions()
        {
            for (int i = activeApparitions.Count - 1; i >= 0; i--)
            {
                if (activeApparitions[i] == null)
                {
                    activeApparitions.RemoveAt(i);
                }
            }
        }

        public int ActiveCount => activeApparitions.Count;
    }
}
