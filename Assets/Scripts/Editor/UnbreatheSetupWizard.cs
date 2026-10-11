using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using Unbreathe.Physics;
using Unbreathe.Cameras;
using Unbreathe.Interaction;
using Unbreathe.Dialogue;
using Unbreathe.Audio;

namespace Unbreathe.EditorTools
{
    /// <summary>
    /// Asistente automático para configurar suelo, colisiones y cámara en UNBREATHE con 1 solo clic.
    /// Resuelve de forma inmediata:
    /// 1. Sustituye BoxColliders gigantes por MeshColliders precisos en las calles de la ciudad.
    /// 2. Ajusta el CharacterController del Player (centro y altura para pisar firme).
    /// 3. Garantiza que WASD se mueva relativo a la vista de la cámara.
    /// </summary>
    public static class UnbreatheSetupWizard
    {
        [MenuItem("Unbreathe/Configurar Escena, Suelo y Controles Automaticamente")]
        public static void SetupSceneAutomatically()
        {
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();

            Debug.Log("<color=cyan>[Unbreathe Setup] Iniciando configuración automática de escena...</color>");

            // 1. Configurar colisiones de la ciudad
            SetupCityColliders();

            // 2. Configurar el Player y su controlador
            SetupPlayer();

            // 3. Configurar la Cámara
            SetupCamera();

            Undo.CollapseUndoOperations(undoGroup);
            EditorUtility.DisplayDialog(
                "UNBREATHE - Configuración Completada",
                "¡Todo configurado con éxito!\n\n" +
                "1. Se reemplazaron los BoxColliders erróneos por MeshColliders en las calles.\n" +
                "2. Se ajustó el CharacterController del Player para pisar firme.\n" +
                "3. Se activó el movimiento WASD según la dirección de la cámara.\n\n" +
                "¡Ya puedes darle al botón PLAY para probar!",
                "Entendido"
            );
        }

        private static void SetupCityColliders()
        {
            // Buscar las partes de la ciudad donde están las calles y aceras
            string[] cityPartNames = new string[] { "city_part_demo_main1", "city_part_demo_main2", "city_part_highway" };

            foreach (string partName in cityPartNames)
            {
                GameObject obj = GameObject.Find(partName);
                if (obj != null)
                {
                    // Eliminar cualquier BoxCollider erróneo que encierre la ciudad en un bloque sólido
                    BoxCollider badBox = obj.GetComponent<BoxCollider>();
                    if (badBox != null)
                    {
                        Undo.DestroyObjectImmediate(badBox);
                        Debug.Log($"[Unbreathe Setup] Se eliminó BoxCollider gigante erróneo de '{partName}'.");
                    }

                    // Asegurar MeshCollider preciso que sigue la geometría del asfalto
                    MeshCollider meshCol = obj.GetComponent<MeshCollider>();
                    if (meshCol == null)
                    {
                        meshCol = Undo.AddComponent<MeshCollider>(obj);
                        MeshFilter mf = obj.GetComponent<MeshFilter>();
                        if (mf != null && mf.sharedMesh != null)
                        {
                            meshCol.sharedMesh = mf.sharedMesh;
                        }
                        meshCol.convex = false;
                        meshCol.isTrigger = false;
                        Debug.Log($"<color=green>[Unbreathe Setup] Se añadió MeshCollider con geometría real a '{partName}'.</color>");
                    }
                    else
                    {
                        meshCol.isTrigger = false;
                    }
                }
            }

            // Eliminar contenedores de cubos temporales si existen
            GameObject tempColliders = GameObject.Find("FLOOR_COLLIDERS");
            if (tempColliders != null)
            {
                Undo.DestroyObjectImmediate(tempColliders);
                Debug.Log("[Unbreathe Setup] Se eliminó el contenedor temporal 'FLOOR_COLLIDERS'.");
            }
        }

        private static void SetupPlayer()
        {
            GameObject playerObj = GameObject.Find("Player");
            if (playerObj == null)
            {
                var pm = Object.FindFirstObjectByType<PlayerMovementController>();
                if (pm != null) playerObj = pm.gameObject;
            }

            if (playerObj == null)
            {
                Debug.LogWarning("[Unbreathe Setup] No se encontró el GameObject 'Player' en la escena.");
                return;
            }

            Undo.RecordObject(playerObj, "Setup Player");
            playerObj.tag = "Player";

            // Configurar CharacterController
            CharacterController cc = playerObj.GetComponent<CharacterController>();
            if (cc != null)
            {
                Undo.RecordObject(cc, "Setup CharacterController");
                cc.height = 2f;
                cc.radius = 0.45f;
                // Centro en Y: 1 para que el pivote del Player esté exactamente en la base de los pies (Y: 0)
                cc.center = new Vector3(0f, 1f, 0f);
                cc.stepOffset = 0.35f;
                cc.slopeLimit = 45f;
                cc.skinWidth = 0.08f;
            }

            // Configurar PlayerMovementController
            PlayerMovementController pmc = playerObj.GetComponent<PlayerMovementController>();
            if (pmc != null)
            {
                Undo.RecordObject(pmc, "Setup PlayerMovementController");
                SerializedObject so = new SerializedObject(pmc);
                var camRelProp = so.FindProperty("cameraRelativeMovement");
                if (camRelProp != null)
                {
                    camRelProp.boolValue = true;
                }
                var refCamProp = so.FindProperty("referenceCamera");
                if (refCamProp != null && refCamProp.objectReferenceValue == null)
                {
                    refCamProp.objectReferenceValue = Camera.main;
                }
                so.ApplyModifiedProperties();
            }

            // Posicionar al Player sobre el asfalto seguro si está en zona de riesgo
            RaycastHit hit;
            Vector3 rayStart = playerObj.transform.position + Vector3.up * 10f;
            if (UnityEngine.Physics.Raycast(rayStart, Vector3.down, out hit, 50f))
            {
                playerObj.transform.position = hit.point + Vector3.up * 0.1f;
                Debug.Log($"<color=green>[Unbreathe Setup] Player posicionado sobre el suelo firme en Y: {playerObj.transform.position.y:F2}</color>");
            }
        }

        private static void SetupCamera()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                cam = Object.FindFirstObjectByType<Camera>();
            }

            if (cam != null)
            {
                CameraFollow cf = cam.GetComponent<CameraFollow>();
                if (cf == null)
                {
                    cf = Undo.AddComponent<CameraFollow>(cam.gameObject);
                }

                GameObject playerObj = GameObject.Find("Player");
                if (playerObj != null && cf != null)
                {
                    Undo.RecordObject(cf, "Setup CameraFollow");
                    SerializedObject so = new SerializedObject(cf);
                    var targetProp = so.FindProperty("target");
                    if (targetProp != null)
                    {
                        targetProp.objectReferenceValue = playerObj.transform;
                    }
                    so.ApplyModifiedProperties();
                }
            }
        }

        // ==========================================
        // HERRAMIENTAS PARA GRUPOS Y SELECCIÓN MASIVA
        // ==========================================

        [MenuItem("Unbreathe/Colisionadores/Generar Colisionadores en Árboles, Edificios y Vallas")]
        public static void GenerateCollidersForCityElements()
        {
            int count = 0;

            // 1. Árboles (CapsuleColliders o BoxColliders en troncos)
            GameObject treesGroup = GameObject.Find("TREES");
            if (treesGroup != null)
            {
                // Limpiar collider erróneo en el padre
                BoxCollider parentBox = treesGroup.GetComponent<BoxCollider>();
                if (parentBox != null) Undo.DestroyObjectImmediate(parentBox);

                count += AddCollidersToChildren<CapsuleCollider>(treesGroup.transform);
            }

            // 2. Edificios
            GameObject buildingsGroup = GameObject.Find("BUILDINGS");
            if (buildingsGroup != null)
            {
                BoxCollider parentBox = buildingsGroup.GetComponent<BoxCollider>();
                if (parentBox != null) Undo.DestroyObjectImmediate(parentBox);

                count += AddCollidersToChildren<BoxCollider>(buildingsGroup.transform);
            }

            // 3. Vallas
            GameObject fencesGroup = GameObject.Find("city_part_fences");
            if (fencesGroup != null)
            {
                BoxCollider parentBox = fencesGroup.GetComponent<BoxCollider>();
                if (parentBox != null) Undo.DestroyObjectImmediate(parentBox);

                count += AddCollidersToChildren<BoxCollider>(fencesGroup.transform);
            }

            EditorUtility.DisplayDialog(
                "UNBREATHE - Colisionadores Generados",
                $"¡Se añadieron colisionadores individuales a {count} elementos (árboles, edificios y vallas) automáticamente!",
                "Excelente"
            );
        }

        [MenuItem("GameObject/Unbreathe/Añadir BoxColliders a todos los hijos", false, 0)]
        public static void AddBoxCollidersToSelection()
        {
            if (Selection.activeTransform == null) return;
            // Si el padre tiene collider, removerlo
            BoxCollider parentCol = Selection.activeGameObject.GetComponent<BoxCollider>();
            if (parentCol != null) Undo.DestroyObjectImmediate(parentCol);

            int added = AddCollidersToChildren<BoxCollider>(Selection.activeTransform);
            Debug.Log($"<color=green>[Unbreathe] Añadidos {added} BoxColliders a los hijos de '{Selection.activeGameObject.name}'.</color>");
        }

        [MenuItem("GameObject/Unbreathe/Añadir CapsuleColliders a todos los hijos (Árboles)", false, 0)]
        public static void AddCapsuleCollidersToSelection()
        {
            if (Selection.activeTransform == null) return;
            BoxCollider parentCol = Selection.activeGameObject.GetComponent<BoxCollider>();
            if (parentCol != null) Undo.DestroyObjectImmediate(parentCol);

            int added = AddCollidersToChildren<CapsuleCollider>(Selection.activeTransform);
            Debug.Log($"<color=green>[Unbreathe] Añadidos {added} CapsuleColliders a los hijos de '{Selection.activeGameObject.name}'.</color>");
        }

        private static int AddCollidersToChildren<T>(Transform parent) where T : Collider
        {
            int addedCount = 0;
            // Buscar todos los MeshRenderers en los hijos
            MeshRenderer[] renderers = parent.GetComponentsInChildren<MeshRenderer>(true);

            foreach (MeshRenderer mr in renderers)
            {
                // No añadir al mismo padre
                if (mr.transform == parent) continue;

                // Si ya tiene un collider de cualquier tipo, omitir
                Collider existingCol = mr.GetComponent<Collider>();
                if (existingCol == null)
                {
                    Undo.AddComponent<T>(mr.gameObject);
                    addedCount++;
                }
            }

            return addedCount;
        }

        // ==========================================
        // CONFIGURACIÓN DE SPRITES DE KAROL
        // ==========================================

        [MenuItem("Unbreathe/Personaje/Configurar y Asignar Sprites de Karol")]
        public static void SetupKarolSprites()
        {
            string downPath = "Assets/Sprids/Personaje/Karol-iso_walk_down-trimmed.png";
            string upPath = "Assets/Sprids/Personaje/Karol-iso_walk_up-trimmed.png";
            string leftPath = "Assets/Sprids/Personaje/Karol-iso_walk_left-trimmed.png";
            string rightPath = "Assets/Sprids/Personaje/Karol-iso_walk_right-trimmed.png";

            Debug.Log("<color=cyan>[Unbreathe Setup] Configurando y recortando las 4 direcciones de Karol...</color>");

            // 1. Recortar texturas en modo Multiple (cuadrícula 5x5, 256x256 cada frame)
            SliceSpritesheet(downPath, "walk_down", 21);
            SliceSpritesheet(upPath, "walk_up", 21);
            SliceSpritesheet(leftPath, "walk_left", 21);
            SliceSpritesheet(rightPath, "walk_right", 21);

            AssetDatabase.Refresh();

            // 2. Cargar los sprites generados
            List<Sprite> downSprites = LoadSortedSprites(downPath);
            List<Sprite> upSprites = LoadSortedSprites(upPath);
            List<Sprite> leftSprites = LoadSortedSprites(leftPath);
            List<Sprite> rightSprites = LoadSortedSprites(rightPath);

            // 3. Asignar a Player -> GFX
            GameObject playerObj = GameObject.Find("Player");
            if (playerObj == null)
            {
                var pm = Object.FindFirstObjectByType<PlayerMovementController>();
                if (pm != null) playerObj = pm.gameObject;
            }

            if (playerObj != null)
            {
                Unbreathe.GFX.PlayerGFX gfx = playerObj.GetComponentInChildren<Unbreathe.GFX.PlayerGFX>();
                if (gfx != null)
                {
                    Undo.RecordObject(gfx, "Configurar Sprites de Karol");
                    gfx.SetAllAnimationFrames(downSprites.ToArray(), upSprites.ToArray(), leftSprites.ToArray(), rightSprites.ToArray());
                    EditorUtility.SetDirty(gfx);
                }

                SpriteRenderer sr = playerObj.GetComponentInChildren<SpriteRenderer>();
                if (sr != null && downSprites.Count > 0)
                {
                    Undo.RecordObject(sr, "Asignar Sprite Karol");
                    sr.sprite = downSprites[0];
                    EditorUtility.SetDirty(sr);
                }
            }

            EditorUtility.DisplayDialog(
                "UNBREATHE - Sprites de Karol Listos (4 Direcciones)",
                $"¡Las 4 hojas de sprites de Karol fueron recortadas y configuradas con éxito!\n\n" +
                $"• Walk Down (Frente): {downSprites.Count} frames.\n" +
                $"• Walk Up (Espalda): {upSprites.Count} frames.\n" +
                $"• Walk Left (Izquierda): {leftSprites.Count} frames.\n" +
                $"• Walk Right (Derecha): {rightSprites.Count} frames.\n\n" +
                "¡El cuadrado blanco fue reemplazado por Karol!\nDale a Play para verla caminar en todas las direcciones.",
                "Excelente"
            );
        }

        private static void SliceSpritesheet(string assetPath, string prefix, int totalFrames)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning($"[Unbreathe Setup] No se encontró la textura en {assetPath}");
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.spritePixelsPerUnit = 100;

            int cols = 5;
            int rows = 5;
            int frameW = 256;
            int frameH = 256;

            var metas = new System.Collections.Generic.List<SpriteMetaData>();
            int index = 0;

            // En Unity, la coordenada Y=0 es la base inferior de la textura
            for (int r = rows - 1; r >= 0; r--)
            {
                for (int c = 0; c < cols; c++)
                {
                    if (index >= totalFrames) break;

                    SpriteMetaData smd = new SpriteMetaData();
                    smd.rect = new Rect(c * frameW, r * frameH, frameW, frameH);
                    smd.name = $"{prefix}_{index}";
                    smd.alignment = (int)SpriteAlignment.Custom;
                    // Pivote en los pies
                    smd.pivot = new Vector2(0.5f, 0.05f);
                    metas.Add(smd);

                    index++;
                }
            }

            importer.spritesheet = metas.ToArray();
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }

        private static List<Sprite> LoadSortedSprites(string assetPath)
        {
            Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            List<Sprite> list = new List<Sprite>();

            foreach (Object obj in allAssets)
            {
                if (obj is Sprite s)
                {
                    list.Add(s);
                }
            }

            list.Sort((a, b) =>
            {
                int idxA = ExtractTrailingNumber(a.name);
                int idxB = ExtractTrailingNumber(b.name);
                return idxA.CompareTo(idxB);
            });

            return list;
        }

        private static int ExtractTrailingNumber(string str)
        {
            string[] parts = str.Split('_');
            if (parts.Length > 0 && int.TryParse(parts[parts.Length - 1], out int val))
            {
                return val;
            }
            return 0;
        }

        // ==========================================
        // CREADOR DE MENÚ PRINCIPAL
        // ==========================================

        [MenuItem("Unbreathe/Menu Principal/Crear Escena de Menú Principal Completa")]
        public static void CreateMainMenuScene()
        {
            string menuImagePath = "Assets/Sprids/Recursos/Menu.jpeg";
            string introPlayPath = "Assets/Sprids/Recursos/InicioPlay.jpeg";
            string scenePath = "Assets/Scenes/MainMenu.unity";

            Debug.Log("<color=cyan>[Unbreathe Setup] Creando escena de Menú Principal...</color>");

            // 1. Convertir imágenes a Sprites UI
            ConvertToSingleSprite(menuImagePath);
            ConvertToSingleSprite(introPlayPath);
            AssetDatabase.Refresh();

            Sprite menuSprite = AssetDatabase.LoadAssetAtPath<Sprite>(menuImagePath);
            Sprite introPlaySprite = AssetDatabase.LoadAssetAtPath<Sprite>(introPlayPath);

            // 2. Crear nueva escena limpia
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
                UnityEditor.SceneManagement.NewSceneMode.Single
            );

            // 3. Crear Cámara 2D / UI
            GameObject camObj = new GameObject("Main Camera");
            Camera cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.orthographic = true;
            camObj.AddComponent<AudioListener>();

            // 4. Crear EventSystem
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
#if ENABLE_INPUT_SYSTEM
            esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif

            // 5. Crear Canvas
            GameObject canvasObj = new GameObject("MainMenu_Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            AudioSource menuAudio = canvasObj.AddComponent<AudioSource>();
            menuAudio.loop = true;
            menuAudio.playOnAwake = true;
            menuAudio.volume = 0.7f;

            Unbreathe.UI.MainMenuManager menuManager = canvasObj.AddComponent<Unbreathe.UI.MainMenuManager>();

            // 6. Fondo del Menú (Menu.jpeg)
            GameObject bgObj = new GameObject("Background_Menu");
            bgObj.transform.SetParent(canvasObj.transform, false);
            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.sprite = menuSprite;
            bgImg.color = Color.white;
            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;

            // 7. Contenedor de Botones (con CanvasGroup)
            GameObject buttonsContainer = new GameObject("Buttons_Container", typeof(RectTransform));
            buttonsContainer.transform.SetParent(canvasObj.transform, false);
            CanvasGroup btnCg = buttonsContainer.AddComponent<CanvasGroup>();
            btnCg.alpha = 0f; // Comienza invisible para el fade-in tras unos segundos
            RectTransform btnContainerRect = buttonsContainer.GetComponent<RectTransform>();
            // Anclado en la parte inferior centrada
            btnContainerRect.anchorMin = new Vector2(0.35f, 0.08f);
            btnContainerRect.anchorMax = new Vector2(0.65f, 0.32f);
            btnContainerRect.sizeDelta = Vector2.zero;

            VerticalLayoutGroup vlg = buttonsContainer.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 16f;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = true;

            // Crear Botones estilizados
            Button playBtn = CreateStylizedButton(buttonsContainer.transform, "Btn_Play", "JUGAR", new Color(0f, 0.85f, 1f, 0.85f));
            Button optionsBtn = CreateStylizedButton(buttonsContainer.transform, "Btn_Options", "OPCIONES", new Color(0.7f, 0.6f, 1f, 0.85f));
            Button quitBtn = CreateStylizedButton(buttonsContainer.transform, "Btn_Quit", "SALIR", new Color(1f, 0.35f, 0.45f, 0.85f));

            // 8. Panel de InicioPlay (Prólogo al pulsar Play)
            GameObject introPanel = new GameObject("Panel_InicioPlay", typeof(RectTransform));
            introPanel.transform.SetParent(canvasObj.transform, false);
            CanvasGroup introCg = introPanel.AddComponent<CanvasGroup>();
            RectTransform introRect = introPanel.GetComponent<RectTransform>();
            introRect.anchorMin = Vector2.zero;
            introRect.anchorMax = Vector2.one;
            introRect.sizeDelta = Vector2.zero;

            Image introImg = introPanel.AddComponent<Image>();
            introImg.sprite = introPlaySprite;
            introImg.color = Color.white;

            // Texto indicador en la parte inferior
            GameObject hintObj = new GameObject("Txt_Hint", typeof(RectTransform));
            hintObj.transform.SetParent(introPanel.transform, false);
            TextMeshProUGUI hintTxt = hintObj.AddComponent<TextMeshProUGUI>();
            hintTxt.text = "[ Presiona cualquier tecla para continuar ]";
            hintTxt.fontSize = 26;
            hintTxt.alignment = TextAlignmentOptions.Center;
            hintTxt.color = new Color(1f, 1f, 1f, 0.8f);
            RectTransform hintRect = hintObj.GetComponent<RectTransform>();
            hintRect.anchorMin = new Vector2(0.1f, 0.05f);
            hintRect.anchorMax = new Vector2(0.9f, 0.12f);
            hintRect.sizeDelta = Vector2.zero;

            introPanel.SetActive(false);

            // 9. Panel de Opciones
            GameObject optPanel = new GameObject("Panel_Opciones", typeof(RectTransform));
            optPanel.transform.SetParent(canvasObj.transform, false);
            RectTransform optRect = optPanel.GetComponent<RectTransform>();
            optRect.anchorMin = new Vector2(0.3f, 0.25f);
            optRect.anchorMax = new Vector2(0.7f, 0.75f);
            optRect.sizeDelta = Vector2.zero;

            Image optBg = optPanel.AddComponent<Image>();
            optBg.color = new Color(0.06f, 0.07f, 0.11f, 0.94f);

            // Título Opciones
            GameObject optTitleObj = new GameObject("Txt_Title", typeof(RectTransform));
            optTitleObj.transform.SetParent(optPanel.transform, false);
            TextMeshProUGUI optTitle = optTitleObj.AddComponent<TextMeshProUGUI>();
            optTitle.text = "CONFIGURACIÓN";
            optTitle.fontSize = 32;
            optTitle.alignment = TextAlignmentOptions.Center;
            optTitle.color = new Color(0f, 0.85f, 1f, 1f);
            RectTransform optTitleRect = optTitleObj.GetComponent<RectTransform>();
            optTitleRect.anchorMin = new Vector2(0.1f, 0.8f);
            optTitleRect.anchorMax = new Vector2(0.9f, 0.95f);
            optTitleRect.sizeDelta = Vector2.zero;

            // Header de Volumen con texto y porcentaje
            GameObject volHeaderObj = new GameObject("Header_Volume", typeof(RectTransform));
            volHeaderObj.transform.SetParent(optPanel.transform, false);
            RectTransform volHeaderRect = volHeaderObj.GetComponent<RectTransform>();
            volHeaderRect.anchorMin = new Vector2(0.15f, 0.62f);
            volHeaderRect.anchorMax = new Vector2(0.85f, 0.72f);
            volHeaderRect.sizeDelta = Vector2.zero;

            GameObject volLabelObj = new GameObject("Txt_VolLabel", typeof(RectTransform));
            volLabelObj.transform.SetParent(volHeaderObj.transform, false);
            TextMeshProUGUI volLabel = volLabelObj.AddComponent<TextMeshProUGUI>();
            volLabel.text = "Volumen General";
            volLabel.fontSize = 22;
            volLabel.alignment = TextAlignmentOptions.Left;
            volLabel.color = Color.white;
            RectTransform volLabelRect = volLabelObj.GetComponent<RectTransform>();
            volLabelRect.anchorMin = new Vector2(0f, 0f);
            volLabelRect.anchorMax = new Vector2(0.7f, 1f);
            volLabelRect.sizeDelta = Vector2.zero;

            GameObject volPercentObj = new GameObject("Txt_VolPercent", typeof(RectTransform));
            volPercentObj.transform.SetParent(volHeaderObj.transform, false);
            TextMeshProUGUI volPercent = volPercentObj.AddComponent<TextMeshProUGUI>();
            volPercent.text = "100%";
            volPercent.fontSize = 22;
            volPercent.alignment = TextAlignmentOptions.Right;
            volPercent.color = new Color(0f, 0.85f, 1f, 1f);
            RectTransform volPercentRect = volPercentObj.GetComponent<RectTransform>();
            volPercentRect.anchorMin = new Vector2(0.7f, 0f);
            volPercentRect.anchorMax = new Vector2(1f, 1f);
            volPercentRect.sizeDelta = Vector2.zero;

            // Slider GameObject completo
            GameObject sliderObj = new GameObject("Slider_Volume", typeof(RectTransform));
            sliderObj.transform.SetParent(optPanel.transform, false);
            RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0.15f, 0.5f);
            sliderRect.anchorMax = new Vector2(0.85f, 0.58f);
            sliderRect.sizeDelta = Vector2.zero;

            Slider volSlider = sliderObj.AddComponent<Slider>();
            volSlider.minValue = 0f;
            volSlider.maxValue = 1f;
            volSlider.value = 1f;

            Image sliderBg = sliderObj.AddComponent<Image>();
            sliderBg.color = new Color(0.15f, 0.18f, 0.25f, 0.9f);

            // Fill Area
            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0f, 0.2f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.8f);
            fillAreaRect.sizeDelta = new Vector2(-10f, 0f);

            GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.sizeDelta = Vector2.zero;
            Image fillImg = fill.GetComponent<Image>();
            fillImg.color = new Color(0f, 0.85f, 1f, 1f);

            // Handle Slide Area
            GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(sliderObj.transform, false);
            RectTransform handleAreaRect = handleArea.GetComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.sizeDelta = new Vector2(-20f, 0f);

            GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(handleArea.transform, false);
            RectTransform handleRect = handle.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(24f, 0f);
            Image handleImg = handle.GetComponent<Image>();
            handleImg.color = Color.white;

            volSlider.fillRect = fillRect;
            volSlider.handleRect = handleRect;
            volSlider.targetGraphic = handleImg;
            volSlider.direction = Slider.Direction.LeftToRight;

            // Toggle Pantalla Completa
            GameObject toggleObj = new GameObject("Toggle_Fullscreen", typeof(RectTransform));
            toggleObj.transform.SetParent(optPanel.transform, false);
            Toggle fullToggle = toggleObj.AddComponent<Toggle>();
            fullToggle.isOn = Screen.fullScreen;
            RectTransform toggleRect = toggleObj.GetComponent<RectTransform>();
            toggleRect.anchorMin = new Vector2(0.15f, 0.35f);
            toggleRect.anchorMax = new Vector2(0.85f, 0.45f);
            toggleRect.sizeDelta = Vector2.zero;

            GameObject toggleLabelObj = new GameObject("Txt_ToggleLabel", typeof(RectTransform));
            toggleLabelObj.transform.SetParent(toggleObj.transform, false);
            TextMeshProUGUI toggleLabel = toggleLabelObj.AddComponent<TextMeshProUGUI>();
            toggleLabel.text = "Pantalla Completa";
            toggleLabel.fontSize = 22;
            toggleLabel.alignment = TextAlignmentOptions.Left;
            toggleLabel.color = Color.white;
            RectTransform toggleLabelRect = toggleLabelObj.GetComponent<RectTransform>();
            toggleLabelRect.anchorMin = Vector2.zero;
            toggleLabelRect.anchorMax = Vector2.one;
            toggleLabelRect.sizeDelta = Vector2.zero;

            // Botón Cerrar Opciones
            Button closeOptBtn = CreateStylizedButton(optPanel.transform, "Btn_CloseOptions", "VOLVER", new Color(0.5f, 0.5f, 0.6f, 0.85f));
            RectTransform closeOptRect = closeOptBtn.GetComponent<RectTransform>();
            closeOptRect.anchorMin = new Vector2(0.3f, 0.08f);
            closeOptRect.anchorMax = new Vector2(0.7f, 0.22f);
            closeOptRect.sizeDelta = Vector2.zero;

            optPanel.SetActive(false);

            // 10. Conectar campos serializados en MainMenuManager
            SerializedObject so = new SerializedObject(menuManager);
            so.FindProperty("menuBackgroundImage").objectReferenceValue = bgImg;
            so.FindProperty("introPlayPanel").objectReferenceValue = introPanel;
            so.FindProperty("introPlayImage").objectReferenceValue = introImg;
            so.FindProperty("buttonsCanvasGroup").objectReferenceValue = btnCg;
            so.FindProperty("optionsPanel").objectReferenceValue = optPanel;
            so.FindProperty("volumeSlider").objectReferenceValue = volSlider;
            so.FindProperty("volumePercentText").objectReferenceValue = volPercent;
            so.FindProperty("fullscreenToggle").objectReferenceValue = fullToggle;
            so.FindProperty("menuAudioSource").objectReferenceValue = menuAudio;
            so.FindProperty("gameplaySceneName").stringValue = "Primer Plano";
            so.ApplyModifiedProperties();

            // 11. Conectar eventos OnClick
            UnityEditor.Events.UnityEventTools.AddPersistentListener(playBtn.onClick, menuManager.OnPlayClicked);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(optionsBtn.onClick, menuManager.OnOptionsClicked);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(quitBtn.onClick, menuManager.OnQuitClicked);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(closeOptBtn.onClick, menuManager.OnCloseOptionsClicked);

            // 12. Guardar escena
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, scenePath);

            // 13. Actualizar Build Settings
            UpdateBuildSettings(scenePath, "Assets/Scenes/Primer Plano.unity");

            EditorUtility.DisplayDialog(
                "UNBREATHE - Menú Principal Creado",
                "¡La escena 'MainMenu' fue creada y configurada con éxito!\n\n" +
                "• Fondo de inicio con 'Menu.jpeg' (se muestra limpio los primeros segundos).\n" +
                "• Botones estilizados (Jugar, Opciones, Salir) con aparición suave.\n" +
                "• Transición a 'InicioPlay.jpeg' al pulsar JUGAR antes de cargar la ciudad.\n" +
                "• Panel de opciones con volumen y pantalla completa.\n" +
                "• Registrado como Escena 0 en Build Settings.\n\n" +
                "¡Dale a Play para probar la experiencia cinematográfica del inicio!",
                "Excelente"
            );
        }

        private static Button CreateStylizedButton(Transform parent, string name, string text, Color accentColor)
        {
            GameObject btnObj = new GameObject(name, typeof(RectTransform));
            btnObj.transform.SetParent(parent, false);

            Image btnImg = btnObj.AddComponent<Image>();
            btnImg.color = new Color(0.08f, 0.1f, 0.16f, 0.85f);

            Button btn = btnObj.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = new Color(0.08f, 0.1f, 0.16f, 0.85f);
            cb.highlightedColor = accentColor;
            cb.pressedColor = new Color(accentColor.r * 0.7f, accentColor.g * 0.7f, accentColor.b * 0.7f, 1f);
            btn.colors = cb;

            GameObject textObj = new GameObject("Text", typeof(RectTransform));
            textObj.transform.SetParent(btnObj.transform, false);
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 28;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.fontStyle = FontStyles.Bold;

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            return btn;
        }

        private static void ConvertToSingleSprite(string assetPath)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
            }
        }

        [InitializeOnLoadMethod]
        [MenuItem("Unbreathe/Escenas/Registrar Todas las Escenas en Build Settings")]
        public static void RegisterAllProjectScenesInBuildSettings()
        {
            string[] sceneOrder = new string[]
            {
                "Assets/Scenes/MainMenu.unity",
                "Assets/Scenes/Primer Plano.unity",
                "Assets/Scenes/Segundo plano.unity",
                "Assets/Scenes/Tercer Plano.unity"
            };

            var sceneList = new List<EditorBuildSettingsScene>();
            foreach (var path in sceneOrder)
            {
                if (System.IO.File.Exists(path))
                {
                    sceneList.Add(new EditorBuildSettingsScene(path, true));
                }
            }

            // Registrar cualquier otra escena dentro de Assets/Scenes que no estuviera en la lista básica
            string[] allSceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" });
            foreach (var guid in allSceneGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!sceneList.Exists(s => s.path == path))
                {
                    sceneList.Add(new EditorBuildSettingsScene(path, true));
                }
            }

            EditorBuildSettings.scenes = sceneList.ToArray();
            Debug.Log($"<color=green>[Unbreathe Setup] {sceneList.Count} escenas registradas en Build Settings (MainMenu -> Primer Plano -> Segundo plano -> Tercer Plano).</color>");
        }

        private static void UpdateBuildSettings(string menuScenePath, string gameScenePath)
        {
            RegisterAllProjectScenesInBuildSettings();
        }

        [MenuItem("Unbreathe/Iluminación/Limpiar Datos de Iluminación Obsoletos")]
        public static void ClearObsoleteLightingData()
        {
            Lightmapping.lightingDataAsset = null;
            Lightmapping.Clear();
            Lightmapping.ClearLightingDataAsset();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("<color=green>[Unbreathe] Datos de iluminación obsoletos eliminados correctamente de la escena activa.</color>");
            EditorUtility.DisplayDialog(
                "UNBREATHE - Iluminación Limpia",
                "Se han desvinculado los datos de iluminación obsoletos de la escena activa.\n\n" +
                "La escena ahora utiliza iluminación en tiempo real limpia y sin advertencias en consola.\n" +
                "Si en algún momento deseas generar sombras horneadas (baked) compatibles con Unity 6, puedes ir a:\n" +
                "Window -> Rendering -> Lighting -> Generate Lighting.",
                "Entendido"
            );
        }

        [MenuItem("Unbreathe/Iluminación/Restaurar Iluminación Original de Ciudad (Primer Plano)")]
        public static void RestoreCityLighting()
        {
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();

            Debug.Log("<color=cyan>[Unbreathe Iluminación] Restaurando iluminación original de la ciudad (Primer Plano)...</color>");

            // 1. Restaurar Skybox y RenderSettings originales
            string skyboxPath = "Assets/Versatile Studio Assets/Demo City By Versatile Studio/Scenes/SkyBox/CustomSky.mat";
            Material skyboxMat = AssetDatabase.LoadAssetAtPath<Material>(skyboxPath);
            if (skyboxMat != null)
            {
                RenderSettings.skybox = skyboxMat;
            }

            RenderSettings.fog = false;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientSkyColor = new Color(0.1764706f, 0.16207367f, 0.15686275f, 1f);
            RenderSettings.ambientEquatorColor = new Color(0.114f, 0.125f, 0.133f, 1f);
            RenderSettings.ambientGroundColor = new Color(0.047f, 0.043f, 0.035f, 1f);
            RenderSettings.ambientIntensity = 4.0f;
            RenderSettings.subtractiveShadowColor = new Color(0.42f, 0.478f, 0.627f, 1f);
            RenderSettings.reflectionIntensity = 1.0f;

            // 2. Restaurar Luz Direccional de la ciudad (sin sombras pesadas para no saturar URP)
            Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var l in lights)
            {
                if (l.type == LightType.Directional)
                {
                    Undo.RecordObject(l, "Restaurar Luz Ciudad");
                    l.color = Color.white;
                    l.intensity = 0.5f;
                    l.shadows = LightShadows.None;
                }
            }

            // 3. Eliminar lámpara de habitación si se creó en la ciudad
            GameObject lamp = GameObject.Find("Lampara_Noche_Interior");
            if (lamp != null)
            {
                Undo.DestroyObjectImmediate(lamp);
                Debug.Log("[Unbreathe Iluminación] Se eliminó 'Lampara_Noche_Interior' de la ciudad.");
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Undo.CollapseUndoOperations(undoGroup);

            EditorUtility.DisplayDialog(
                "UNBREATHE - Iluminación de Ciudad Restaurada",
                "¡Iluminación original de la ciudad restaurada con éxito!\n\n" +
                "• Niebla desactivada.\n" +
                "• Luz ambiental restablecida a su intensidad original (4.0).\n" +
                "• Luz direccional restablecida a blanco sin sombras excesivas (resuelve el aviso de las 386 sombras en URP).\n" +
                "• El suelo y edificios han recuperado su aspecto mate original.",
                "¡Perfecto!"
            );
        }

        [MenuItem("Unbreathe/Atmósfera/Convertir Habitacion (Segundo Plano) a Modo Noche")]
        public static void ConvertCurrentSceneToNight()
        {
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();

            // 1. Skybox nocturno
            string nightSkyboxGuid = "3411f94e3a532654884762b61b7a4d8d"; // CustomSky.mat
            string skyboxPath = AssetDatabase.GUIDToAssetPath(nightSkyboxGuid);
            Material nightSky = AssetDatabase.LoadAssetAtPath<Material>(skyboxPath);
            if (nightSky != null)
            {
                RenderSettings.skybox = nightSky;
            }

            // 2. Iluminación ambiental nocturna (azul oscuro tenue)
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.12f, 0.15f, 0.24f);
            RenderSettings.ambientEquatorColor = new Color(0.07f, 0.08f, 0.13f);
            RenderSettings.ambientGroundColor = new Color(0.03f, 0.03f, 0.05f);
            RenderSettings.ambientIntensity = 0.5f;

            // 3. Niebla atmosférica suave
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = 0.012f;
            RenderSettings.fogColor = new Color(0.03f, 0.05f, 0.09f);

            // 4. Luz direccional -> Luz de luna
            Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var l in lights)
            {
                if (l.type == LightType.Directional)
                {
                    Undo.RecordObject(l, "Configurar Luz de Luna");
                    l.color = new Color(0.45f, 0.58f, 0.88f);
                    l.intensity = 0.35f;
                    l.shadows = LightShadows.Soft;
                    l.shadowStrength = 0.85f;
                }
            }

            // 5. Asegurar que la cámara esté etiquetada como MainCamera
            Camera cam = Object.FindFirstObjectByType<Camera>();
            if (cam != null && !cam.CompareTag("MainCamera"))
            {
                Undo.RecordObject(cam.gameObject, "Etiquetar MainCamera");
                cam.gameObject.tag = "MainCamera";
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Undo.CollapseUndoOperations(undoGroup);

            Debug.Log("<color=cyan>[Unbreathe Atmósfera] Modo noche aplicado con éxito a la escena.</color>");
            EditorUtility.DisplayDialog(
                "UNBREATHE - Modo Noche Aplicado",
                "¡La atmósfera nocturna ha sido configurada con éxito!\n\n" +
                "• Skybox: Noche estrellada (CustomSky).\n" +
                "• Iluminación Direccional: Luz de luna azul frío suave (0.35).\n" +
                "• Ambiente: Tono nocturno con niebla suave envolvente.\n" +
                "• Cámara: Etiquetada correctamente como 'MainCamera'.",
                "Genial"
            );
        }

        [MenuItem("Unbreathe/Interacción/Crear Puerta de Transición a Segundo Plano")]
        public static void SetupDoorTransitionToSecondFloor()
        {
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();

            Debug.Log("<color=cyan>[Unbreathe Setup] Configurando puerta de transición a 'Segundo plano'...</color>");

            // 1. Determinar el edificio o punto donde colocar la puerta
            GameObject targetBuilding = Selection.activeGameObject;
            if (targetBuilding == null || (!targetBuilding.name.Contains("office_building") && !targetBuilding.name.Contains("building")))
            {
                // Buscar office_building_2 como se ve en la captura del usuario
                GameObject found = GameObject.Find("office_building_2");
                if (found != null)
                {
                    targetBuilding = found;
                }
            }

            Vector3 doorPosition;
            if (targetBuilding != null)
            {
                Renderer rend = targetBuilding.GetComponent<Renderer>();
                if (rend != null)
                {
                    Bounds b = rend.bounds;
                    // Situar la puerta en la entrada frontal inferior del edificio
                    doorPosition = new Vector3(b.center.x, b.min.y + 1.2f, b.min.z);
                }
                else
                {
                    doorPosition = targetBuilding.transform.position;
                }
            }
            else
            {
                GameObject player = GameObject.Find("Player");
                if (player != null)
                {
                    doorPosition = player.transform.position + player.transform.forward * 3f;
                }
                else
                {
                    doorPosition = new Vector3(62.6f, 3.8f, 76.5f);
                }
            }

            // 2. Buscar o crear el objeto de la puerta interactuable
            GameObject doorObj = GameObject.Find("Puerta_Transicion_SegundoPlano");
            if (doorObj == null)
            {
                doorObj = new GameObject("Puerta_Transicion_SegundoPlano");
                Undo.RegisterCreatedObjectUndo(doorObj, "Crear Puerta Transicion");
            }

            doorObj.transform.position = doorPosition;

            // BoxCollider Trigger
            BoxCollider box = doorObj.GetComponent<BoxCollider>();
            if (box == null)
            {
                box = Undo.AddComponent<BoxCollider>(doorObj);
            }
            box.isTrigger = true;
            box.size = new Vector3(4.5f, 3.5f, 4.0f);
            box.center = new Vector3(0f, 0f, 0f);

            // Rigidbody Cinemático para garantizar detección física con CharacterController
            Rigidbody rb = doorObj.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = Undo.AddComponent<Rigidbody>(doorObj);
            }
            rb.isKinematic = true;
            rb.useGravity = false;

            // Componente SceneTransitionDoor
            SceneTransitionDoor transition = doorObj.GetComponent<SceneTransitionDoor>();
            if (transition == null)
            {
                transition = Undo.AddComponent<SceneTransitionDoor>(doorObj);
            }

            // 3. Crear / Configurar Canvas con el indicador visual "[E] Entrar al edificio"
            SetupInteractionPromptUI();

            // 4. Seleccionar la puerta para que el usuario pueda ajustarla visualmente si lo desea
            Selection.activeGameObject = doorObj;

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Undo.CollapseUndoOperations(undoGroup);

            EditorUtility.DisplayDialog(
                "UNBREATHE - Puerta Configurada",
                "¡Puerta interactiva hacia 'Segundo plano' configurada con éxito!\n\n" +
                "• Objeto 'Puerta_Transicion_SegundoPlano' creado con BoxCollider Trigger y Rigidbody cinemático.\n" +
                "• Al acercarse con Karol y pulsar la tecla 'E', cargará la escena 'Segundo plano'.\n" +
                "• Cartel UI no intrusivo agregado: se mostrará '[E] Entrar al edificio' en pantalla al estar cerca.\n" +
                "• El objeto ha quedado seleccionado en la ventana Scene para que puedas moverlo exactamente a la puerta.",
                "¡Excelente!"
            );
        }

        private static void SetupInteractionPromptUI()
        {
            // Buscar o crear Canvas de interacción
            GameObject canvasObj = GameObject.Find("Canvas_InteractionPrompt");
            if (canvasObj == null)
            {
                canvasObj = new GameObject("Canvas_InteractionPrompt", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                Undo.RegisterCreatedObjectUndo(canvasObj, "Create Interaction Prompt Canvas");

                Canvas canvas = canvasObj.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 50;

                CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;

                // Contenedor del cartel
                GameObject promptContainer = new GameObject("Prompt_Container", typeof(RectTransform), typeof(Image));
                promptContainer.transform.SetParent(canvasObj.transform, false);

                RectTransform containerRect = promptContainer.GetComponent<RectTransform>();
                containerRect.anchorMin = new Vector2(0.5f, 0.12f);
                containerRect.anchorMax = new Vector2(0.5f, 0.12f);
                containerRect.pivot = new Vector2(0.5f, 0.5f);
                containerRect.sizeDelta = new Vector2(360f, 54f);

                Image containerImg = promptContainer.GetComponent<Image>();
                containerImg.color = new Color(0.08f, 0.1f, 0.15f, 0.85f); // Fondo oscuro semitransparente diegético

                // Texto del aviso
                GameObject textObj = new GameObject("Txt_Prompt", typeof(RectTransform), typeof(TextMeshProUGUI));
                textObj.transform.SetParent(promptContainer.transform, false);

                RectTransform textRect = textObj.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.sizeDelta = Vector2.zero;

                TextMeshProUGUI promptTmp = textObj.GetComponent<TextMeshProUGUI>();
                promptTmp.text = "[E] Entrar al edificio";
                promptTmp.fontSize = 22f;
                promptTmp.alignment = TextAlignmentOptions.Center;
                promptTmp.color = Color.white;

                // Componente InteractionPromptUI
                InteractionPromptUI promptUI = canvasObj.AddComponent<InteractionPromptUI>();
                SerializedObject so = new SerializedObject(promptUI);
                so.FindProperty("promptContainer").objectReferenceValue = promptContainer;
                so.FindProperty("promptLabel").objectReferenceValue = promptTmp;
                so.ApplyModifiedProperties();

                promptContainer.SetActive(false);
                Debug.Log("<color=green>[Unbreathe Setup] Canvas de aviso de interacción creado.</color>");
            }
        }

        public static DialogueData GetOrCreateNotebookDialogue()
        {
            string folder = "Assets/Data/Dialogue";
            if (!AssetDatabase.IsValidFolder("Assets/Data")) AssetDatabase.CreateFolder("Assets", "Data");
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Data", "Dialogue");

            string path = $"{folder}/Libreta_Plaza_Data.asset";
            DialogueData data = AssetDatabase.LoadAssetAtPath<DialogueData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<DialogueData>();
                data.sequenceTitle = "Libreta en la Plaza";
                data.lines = new DialogueLine[]
                {
                    new DialogueLine
                    {
                        speakerName = "Libreta - Apunte Borroso",
                        message = "\"Nos vemos en la Plaza a las 6.\"\n\n(La hora está tachada repetidas veces con fuerza...)"
                    },
                    new DialogueLine
                    {
                        speakerName = "Libreta - Nota apresurada",
                        message = "\"La niebla bajó de golpe y el ruido de la calle desapareció por completo. Me dejaron sola con este zumbido sordo en los oídos.\""
                    },
                    new DialogueLine
                    {
                        speakerName = "Libreta - Nota apresurada",
                        message = "\"Si logras encontrar esto, no te quites los audífonos. Afina el oído. Sintoniza más allá del gris.\""
                    }
                };
                AssetDatabase.CreateAsset(data, path);
                AssetDatabase.SaveAssets();
                Debug.Log("<color=green>[Unbreathe Narrativa] Archivo 'Libreta_Plaza_Data' creado.</color>");
            }
            return data;
        }

        public static DialogueData GetOrCreatePhoneDialogue()
        {
            string folder = "Assets/Data/Dialogue";
            if (!AssetDatabase.IsValidFolder("Assets/Data")) AssetDatabase.CreateFolder("Assets", "Data");
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Data", "Dialogue");

            string path = $"{folder}/Celular_SegundoPlano_Data.asset";
            DialogueData data = AssetDatabase.LoadAssetAtPath<DialogueData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<DialogueData>();
                data.sequenceTitle = "Teléfono de Caro";
                data.lines = new DialogueLine[]
                {
                    new DialogueLine
                    {
                        speakerName = "[Buscando frecuencia de resonancia local...]",
                        message = "(La pantalla agrietada ilumina débilmente la penumbra. En la bandeja de salida hay un mensaje de texto atascado...)"
                    },
                    new DialogueLine
                    {
                        speakerName = "Mensaje no enviado -> Para: Caro",
                        message = "\"¿Sigues ahí? Llevo horas caminando. Puse nuestra playlist de madrugada para no perder la cabeza, pero hasta la música suena ahogada, como si estuviera bajo el agua.\""
                    },
                    new DialogueLine
                    {
                        speakerName = "Mensaje no enviado -> Para: Caro",
                        message = "\"Siento que las calles están llenas, juro que respiro a otras personas cerca, pero no veo a nadie.\""
                    },
                    new DialogueLine
                    {
                        speakerName = "Sistema ECHO",
                        message = "[Frecuencia detectada en fase Abismo. La ciudad jamás estuvo vacía. Sintoniza y activa el nodo ECHO.]"
                    }
                };
                AssetDatabase.CreateAsset(data, path);
                AssetDatabase.SaveAssets();
                Debug.Log("<color=green>[Unbreathe Narrativa] Archivo 'Celular_SegundoPlano_Data' creado.</color>");
            }
            return data;
        }

        public static void SetupDialogueUI()
        {
            // 1. DialogueManager Singleton
            GameObject managerObj = GameObject.Find("DialogueManager");
            if (managerObj == null)
            {
                managerObj = new GameObject("DialogueManager", typeof(AudioSource), typeof(DialogueManager));
                Undo.RegisterCreatedObjectUndo(managerObj, "Create DialogueManager");
            }

            // 2. Canvas de Diálogos
            GameObject canvasObj = GameObject.Find("Canvas_DialogueUI");
            if (canvasObj == null)
            {
                canvasObj = new GameObject("Canvas_DialogueUI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                Undo.RegisterCreatedObjectUndo(canvasObj, "Create Dialogue UI Canvas");

                Canvas canvas = canvasObj.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;

                CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;

                // Panel principal de diálogo (parte inferior)
                GameObject panelObj = new GameObject("Panel_Dialogue", typeof(RectTransform), typeof(Image));
                panelObj.transform.SetParent(canvasObj.transform, false);

                RectTransform panelRect = panelObj.GetComponent<RectTransform>();
                panelRect.anchorMin = new Vector2(0.18f, 0.05f);
                panelRect.anchorMax = new Vector2(0.82f, 0.28f);
                panelRect.pivot = new Vector2(0.5f, 0.5f);
                panelRect.sizeDelta = Vector2.zero;

                Image panelImg = panelObj.GetComponent<Image>();
                panelImg.color = new Color(0.04f, 0.06f, 0.10f, 0.94f); // Fondo oscuro diegético

                // Título / Emisor
                GameObject speakerObj = new GameObject("Txt_Speaker", typeof(RectTransform), typeof(TextMeshProUGUI));
                speakerObj.transform.SetParent(panelObj.transform, false);

                RectTransform speakerRect = speakerObj.GetComponent<RectTransform>();
                speakerRect.anchorMin = new Vector2(0.04f, 0.72f);
                speakerRect.anchorMax = new Vector2(0.96f, 0.95f);
                speakerRect.sizeDelta = Vector2.zero;

                TextMeshProUGUI speakerTmp = speakerObj.GetComponent<TextMeshProUGUI>();
                speakerTmp.text = "Emisor";
                speakerTmp.fontSize = 22f;
                speakerTmp.fontStyle = FontStyles.Bold;
                speakerTmp.color = new Color(0.25f, 0.88f, 1f, 1f); // Cian brillante de resonancia

                // Mensaje narrativo
                GameObject msgObj = new GameObject("Txt_Message", typeof(RectTransform), typeof(TextMeshProUGUI));
                msgObj.transform.SetParent(panelObj.transform, false);

                RectTransform msgRect = msgObj.GetComponent<RectTransform>();
                msgRect.anchorMin = new Vector2(0.04f, 0.18f);
                msgRect.anchorMax = new Vector2(0.96f, 0.70f);
                msgRect.sizeDelta = Vector2.zero;

                TextMeshProUGUI msgTmp = msgObj.GetComponent<TextMeshProUGUI>();
                msgTmp.text = "Texto...";
                msgTmp.fontSize = 20f;
                msgTmp.color = Color.white;
                msgTmp.enableWordWrapping = true;

                // Indicador de tecla Espacio
                GameObject hintObj = new GameObject("Txt_PromptHint", typeof(RectTransform), typeof(TextMeshProUGUI));
                hintObj.transform.SetParent(panelObj.transform, false);

                RectTransform hintRect = hintObj.GetComponent<RectTransform>();
                hintRect.anchorMin = new Vector2(0.70f, 0.04f);
                hintRect.anchorMax = new Vector2(0.96f, 0.18f);
                hintRect.sizeDelta = Vector2.zero;

                TextMeshProUGUI hintTmp = hintObj.GetComponent<TextMeshProUGUI>();
                hintTmp.text = "[Espacio] Continuar";
                hintTmp.fontSize = 16f;
                hintTmp.alignment = TextAlignmentOptions.Right;
                hintTmp.color = new Color(0.8f, 0.8f, 0.8f, 0.8f);

                // Componente DialogueUI
                DialogueUI ui = canvasObj.AddComponent<DialogueUI>();
                SerializedObject so = new SerializedObject(ui);
                so.FindProperty("dialoguePanel").objectReferenceValue = panelObj;
                so.FindProperty("speakerText").objectReferenceValue = speakerTmp;
                so.FindProperty("messageText").objectReferenceValue = msgTmp;
                so.FindProperty("promptHintText").objectReferenceValue = hintTmp;
                so.FindProperty("dialogueManager").objectReferenceValue = managerObj.GetComponent<DialogueManager>();
                so.ApplyModifiedProperties();

                panelObj.SetActive(false);
                Debug.Log("<color=green>[Unbreathe Setup] Canvas de Diálogo creado con éxito.</color>");
            }
        }

        [MenuItem("Unbreathe/Narrativa/Configurar Objetos Narrativos (Libreta y Celular)")]
        public static void SetupNarrativeObjects()
        {
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();

            bool foundAny = false;

            // 1. Configurar Libreta (Magic Book) si está en la escena activa
            GameObject magicBook = GameObject.Find("Magic Book");
            if (magicBook != null)
            {
                DialogueData bookData = GetOrCreateNotebookDialogue();

                BoxCollider box = magicBook.GetComponent<BoxCollider>();
                if (box == null) box = Undo.AddComponent<BoxCollider>(magicBook);
                box.isTrigger = true;
                box.size = new Vector3(2.0f, 1.5f, 2.0f);
                box.center = new Vector3(0f, 0.2f, 0f);

                Rigidbody rb = magicBook.GetComponent<Rigidbody>();
                if (rb == null) rb = Undo.AddComponent<Rigidbody>(magicBook);
                rb.isKinematic = true;
                rb.useGravity = false;

                NarrativeInteractableItem item = magicBook.GetComponent<NarrativeInteractableItem>();
                if (item == null) item = Undo.AddComponent<NarrativeInteractableItem>(magicBook);
                item.SetDialogueData(bookData, "[E] Leer libreta");

                foundAny = true;
                Debug.Log("<color=cyan>[Unbreathe Narrativa] 'Magic Book' configurado con apunte narrativo.</color>");
            }

            // 2. Configurar Celular (phone2k) si está en la escena activa
            GameObject phone = GameObject.Find("phone2k");
            if (phone != null)
            {
                DialogueData phoneData = GetOrCreatePhoneDialogue();

                BoxCollider box = phone.GetComponent<BoxCollider>();
                if (box == null) box = Undo.AddComponent<BoxCollider>(phone);
                box.isTrigger = true;
                box.size = new Vector3(1.5f, 1.5f, 1.5f);
                box.center = new Vector3(0f, 0.2f, 0f);

                Rigidbody rb = phone.GetComponent<Rigidbody>();
                if (rb == null) rb = Undo.AddComponent<Rigidbody>(phone);
                rb.isKinematic = true;
                rb.useGravity = false;

                NarrativeInteractableItem item = phone.GetComponent<NarrativeInteractableItem>();
                if (item == null) item = Undo.AddComponent<NarrativeInteractableItem>(phone);
                item.SetDialogueData(phoneData, "[E] Revisar teléfono");

                foundAny = true;
                Debug.Log("<color=cyan>[Unbreathe Narrativa] 'phone2k' configurado con chat de Caro y fase Abismo.</color>");
            }

            // 3. Asegurar Canvas de Interacción (Prompt [E]) y Canvas de Diálogo ([Espacio])
            SetupInteractionPromptUI();
            SetupDialogueUI();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Undo.CollapseUndoOperations(undoGroup);

            string msg = foundAny
                ? "¡Objetos narrativos configurados correctamente!\n\n" +
                  "• Puedes acercarte con Karol y pulsar 'E' para examinar.\n" +
                  "• Pulsa 'Espacio' para avanzar las páginas o el chat.\n" +
                  "• El movimiento se pausa automáticamente mientras lees."
                : "Se configuró la interfaz de diálogo y avisos, pero no se encontró 'Magic Book' ni 'phone2k' en la escena activa.\n" +
                  "Asegúrate de tener abierta la escena correspondiente ('Primer Plano' para la libreta o 'Segundo plano' para el teléfono).";

            EditorUtility.DisplayDialog("UNBREATHE - Narrativa Configurada", msg, "Entendido");
        }

        [MenuItem("Unbreathe/Audio/Configurar Música Ambiental y Pasos")]
        public static void SetupAudioAndFootsteps()
        {
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();

            // 1. Configurar Pasos en el Player
            GameObject player = GameObject.Find("Player");
            if (player != null)
            {
                PlayerFootsteps footsteps = player.GetComponent<PlayerFootsteps>();
                if (footsteps == null)
                {
                    footsteps = Undo.AddComponent<PlayerFootsteps>(player);
                }
                Debug.Log("<color=cyan>[Unbreathe Audio] Sistema de pasos (PlayerFootsteps) añadido a Player.</color>");
            }
            else
            {
                Debug.LogWarning("[Unbreathe Audio] No se encontró el objeto 'Player' en la escena activa.");
            }

            // 2. Configurar Música Ambiental en bucle
            GameObject musicObj = GameObject.Find("Ambience_Music_Manager");
            if (musicObj == null)
            {
                musicObj = new GameObject("Ambience_Music_Manager");
                Undo.RegisterCreatedObjectUndo(musicObj, "Create Ambience Music Manager");
            }

            AmbientMusicPlayer ambient = musicObj.GetComponent<AmbientMusicPlayer>();
            if (ambient == null)
            {
                ambient = Undo.AddComponent<AmbientMusicPlayer>(musicObj);
            }

            AudioClip track = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Musica/mixkit-silent-descent-614.mp3");
            if (track != null)
            {
                SerializedObject so = new SerializedObject(ambient);
                so.FindProperty("ambientTrack").objectReferenceValue = track;
                so.FindProperty("targetVolume").floatValue = 0.25f;
                so.ApplyModifiedProperties();
                Debug.Log("<color=green>[Unbreathe Audio] Música 'mixkit-silent-descent-614' asignada con volumen atmosférico (0.25).</color>");
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Undo.CollapseUndoOperations(undoGroup);

            EditorUtility.DisplayDialog(
                "UNBREATHE - Audio y Pasos Configurados",
                "¡Audio ambiental y pasos configurados con éxito!\n\n" +
                "1. Pasos diegéticos añadidos a Karol (suenan al caminar con WASD con variación de tono).\n" +
                "2. Música ambiental envolvente ('mixkit-silent-descent-614') con fade-in suave.\n" +
                "3. Si tienes clips de pasos propios (.wav o .mp3), puedes arrastrarlos a la casilla 'Footstep Clips' en el componente PlayerFootsteps de Karol.",
                "¡Excelente!"
            );
        }

        [MenuItem("Unbreathe/Todo en Uno: Configurar Narrativa, Audio e Interacciones")]
        public static void SetupEverything()
        {
            SetupNarrativeObjects();
            SetupAudioAndFootsteps();
        }
    }
}
