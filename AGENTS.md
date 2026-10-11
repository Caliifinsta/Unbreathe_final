# AGENTS.md - Directrices de Desarrollo para "UNBREATHE"

## 1. CONTEXTO DEL PROYECTO
* **Nombre:** UNBREATHE: Sumergido en el Silencio.
* **Motor:** Unity 6 LTS / C# / Físicas y Entorno 3D / Universal Render Pipeline (URP).
* **Género:** Exploración narrativa y tensión psicológica.
* **Estilo y Atmósfera:** Tensión e inmersión profunda. Fuerte dependencia de audio diegético, sistemas de partículas y diseño de interfaces (UI mockups) no intrusivas.

---

## 2. REGLAS ESTRICTAS DE PROGRAMACIÓN
* **Arquitectura (CRÍTICO):** Separar estrictamente la lógica visual (componente GFX), el controlador de físicas y la entrada del jugador (Input).
* **Rendimiento:** Prohibido usar `Find()`, `FindGameObjectWithTag()` o `GetComponent()` dentro de los métodos `Update()` o `FixedUpdate()`. Almacenar referencias en caché en `Awake()`.
* **Namespaces:** Para evitar ambigüedades de compilación entre `UnityEngine.Physics` y `Unbreathe.Physics`, calificar siempre de forma explícita las llamadas de motor como `UnityEngine.Physics.OverlapSphere`.
* **Cámaras y Billboards:** La cámara principal de cada escena debe llevar el tag `MainCamera`. Todo componente visual dependiente (`SpriteBillboard`, `CameraFollow`) debe implementar una resolución segura con `FindFirstObjectByType<Camera>()` como respaldo en `Awake()`.
* **Controles y Mecánicas Oficiales:** 
  * **Movimiento:** WASD (orientado relativo a la rotación de la cámara).
  * **Interacción:** Tecla **'E'** (recoger objetos, examinar entorno, abrir puertas).
  * **Narrativa:** Tecla **'Espacio'** (avanzar texto o cerrar sistema de diálogos).

---

## 3. SISTEMAS FUNDAMENTALES COMPLETADOS

### 3.1. Sistema de Interacción (`Unbreathe.Interaction`)
* **Contrato `IInteractable`:** Métodos estándar `GetPromptText()`, `CanInteract()`, `Interact(GameObject interactor)`.
* **Detección Física:** Los objetos interactuables requieren un `Collider` en modo **Trigger** y un `Rigidbody` cinemático (`isKinematic = true`) para garantizar la detección confiable con `CharacterController`.
* **Doble Comprobación:** `PlayerInteraction` cachea objetivos mediante `OnTriggerEnter/Exit` y ejecuta una comprobación de proximidad en el instante de pulsar 'E' para evitar fallos por capas o jerarquías.
* **UI No Intrusiva (`InteractionPromptUI`):** Muestra un cartel minimalista diegético (`[E] Acción`) únicamente cuando el personaje está en rango.

### 3.2. Sistema Narrativo y Diálogos (`Unbreathe.Dialogue`)
* **Datos Desacoplados (`DialogueData`):** ScriptableObjects que contienen título y líneas narrativas (`speakerName`, `message`, `voiceAudio`).
* **Pausa de Entrada:** Durante la lectura de documentos, computadoras o celulares, el movimiento del jugador se silencia automáticamente (`MoveInput = Vector2.zero`) hasta finalizar la secuencia.
* **Avance con Espacio:** `PlayerDialogueActivator` y `DialogueManager` procesan el avance línea a línea mediante la tecla 'Espacio'.

### 3.3. Transición de Escenas (`SceneTransitionDoor`)
* Permite transportar al jugador entre escenas (ej. `Primer Plano` ➔ `Segundo plano`) al pulsar 'E'.
* Todas las escenas del proyecto (`MainMenu`, `Primer Plano`, `Segundo plano`, `Tercer Plano`) deben estar registradas en `EditorBuildSettings`.

### 3.4. Audio Diegético y Pasos (`Unbreathe.Audio`)
* **Pasos (`PlayerFootsteps`):** Reproduce audio a intervalos rítmicos basados en la velocidad de movimiento y contacto con el suelo (`IsGrounded`). Incorpora micro-variaciones aleatorias de tono y un sintetizador procedural de impacto sordo de respaldo si no hay clips asignados.
* **Música Ambiental (`AmbientMusicPlayer`):** Reproducción en bucle con entrada gradual (*fade-in*) y volumen sutil calibrado (0.2 - 0.3) para preservar la inteligibilidad de diálogos y efectos.

### 3.5. Menú Principal y Persistencia de Audio (`Unbreathe.UI.MainMenuManager`)
* **Control de Volumen Maestro:** Se regula mediante `AudioListener.volume` y se persiste en `PlayerPrefs` con la clave `"MasterVolume"`.
* **Interfaz de Opciones:** El slider de volumen incluye estructura estándar de Unity (`Fill Area`, `Fill`, `Handle`) y lectura de porcentaje en tiempo real.
* **Flujo de Inicio:** Fade-in de botones sobre `Menu.jpeg` ➔ Prólogo interactivo con `InicioPlay.jpeg` ➔ Carga asíncrona de `Primer Plano`.

---

## 4. ESTÁNDARES DE ESCENA E ILUMINACIÓN (UNITY 6 URP)
* **Datos de Iluminación:** Prohibido vincular archivos `LightingData.asset` obsoletos de versiones previas. Toda escena limpia debe tener `m_LightingDataAsset: {fileID: 0}` y `Realtime Lightmaps` desactivado a menos que se regenere con el Bake backend de Unity 6.
* **Presupuesto de Sombras Urbanas:** En escenas abiertas con alta densidad de farolas puntuales (`Punctual Lights`), la luz direccional no debe saturar el atlas de sombras con modos pesados que superen la capacidad de URP.