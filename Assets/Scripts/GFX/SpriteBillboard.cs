using UnityEngine;

namespace Unbreathe.GFX
{
    /// <summary>
    /// Componente GFX para controlar la orientación tipo Billboard de sprites en el entorno 3D.
    /// Respeta estrictamente la regla de rendimiento de AGENTS.md:
    /// La referencia a Camera.main se almacena en caché durante Awake() para evitar
    /// llamadas costosas a FindGameObjectWithTag() en Update().
    /// </summary>
    [DisallowMultipleComponent]
    public class SpriteBillboard : MonoBehaviour
    {
        [Header("Configuración Billboard")]
        [Tooltip("Si es verdadero, bloquea la rotación en los ejes X y Z, rotando únicamente sobre el eje Y (giro horizontal hacia la cámara).")]
        [SerializeField] private bool freezeXZAxis = true;

        [Header("Cámara Objetivo (Opcional - se auto-asigna en Awake)")]
        [Tooltip("Cámara a la que mirará el sprite. Si no se asigna, se utiliza y almacena en caché Camera.main en Awake().")]
        [SerializeField] private Camera targetCamera;

        private Transform cameraTransform;

        private void Awake()
        {
            // Regla AGENTS.md: Prohibido usar Find(), FindGameObjectWithTag() o GetComponent() en Update().
            // Almacenar referencias en caché en Awake().
            ResolveCamera();
        }

        private void Start()
        {
            if (cameraTransform == null)
            {
                ResolveCamera();
            }
        }

        private void ResolveCamera()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (targetCamera == null)
            {
                targetCamera = FindFirstObjectByType<Camera>();
            }

            if (targetCamera != null)
            {
                cameraTransform = targetCamera.transform;
            }
        }

        private void Update()
        {
            if (cameraTransform == null)
            {
                ResolveCamera();
                if (cameraTransform == null) return;
            }

            // Implementación idéntica a la especificación visual solicitada:
            if (freezeXZAxis)
            {
                transform.rotation = Quaternion.Euler(0f, cameraTransform.rotation.eulerAngles.y, 0f);
            }
            else
            {
                transform.rotation = cameraTransform.rotation;
            }
        }
    }
}
