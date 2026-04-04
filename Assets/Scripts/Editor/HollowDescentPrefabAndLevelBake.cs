using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using HollowDescent.Gameplay;
using HollowDescent.LevelGen;

namespace HollowDescent.EditorTools
{
    /// <summary>
    /// One-time / rebuild: character prefabs and baked level prefabs under Assets/Resources (runtime loads by path).
    /// </summary>
    public static class HollowDescentPrefabAndLevelBake
    {
        private const string ResourcesRoot = "Assets/Resources";
        private const string CharactersDir = ResourcesRoot + "/Prefabs/Characters";
        private const string LevelsDir = ResourcesRoot + "/Prefabs/Levels";
        /// <summary>Real .mat assets so prefabs serialize material refs (runtime Material instances → fileID 0 → pink).</summary>
        private const string BakedMaterialsDir = "Assets/Materials/BakedGraybox";

        [MenuItem("Hollow Descent/Setup/Create Character Prefabs (Player + Witness)")]
        public static void MenuCreateCharacters()
        {
            EnsureFolders();
            CreatePlayerPrefabAsset();
            CreateWitnessPrefabAsset();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Hollow Descent", "Character prefabs saved under Resources/Prefabs/Characters.", "OK");
        }

        [MenuItem("Hollow Descent/Setup/Bake Level_01 Prefab")]
        public static void MenuBakeLevel1() => BakeLevelPrefab(1);

        [MenuItem("Hollow Descent/Setup/Bake Level_02 Prefab")]
        public static void MenuBakeLevel2() => BakeLevelPrefab(2);

        /// <summary>Batchmode / CI: -executeMethod HollowDescent.EditorTools.HollowDescentPrefabAndLevelBake.BatchFullSetup</summary>
        public static void BatchFullSetup()
        {
            RunFullSetupCore();
        }

        [MenuItem("Hollow Descent/Setup/Full Setup (Characters + Both Levels)")]
        public static void MenuFullSetup()
        {
            RunFullSetupCore();
            EditorUtility.DisplayDialog(
                "Hollow Descent",
                "Created Player + NarrativeWitnessNPC prefabs and Level_01 / Level_02 under Assets/Resources/Prefabs.\n\n" +
                "If AI uses NavMesh, open each level prefab and bake navigation data for static geometry.",
                "OK");
        }

        private static void RunFullSetupCore()
        {
            EnsureFolders();
            CreatePlayerPrefabAsset();
            CreateWitnessPrefabAsset();
            BakeLevelPrefab(1);
            BakeLevelPrefab(2);
            AssetDatabase.Refresh();
        }

        private static void EnsureFolders()
        {
            void Ensure(string path)
            {
                if (!AssetDatabase.IsValidFolder(path))
                {
                    var parent = path[..path.LastIndexOf('/')];
                    var folder = path[(path.LastIndexOf('/') + 1)..];
                    if (!AssetDatabase.IsValidFolder(parent))
                        Ensure(parent);
                    AssetDatabase.CreateFolder(parent, folder);
                }
            }

            Ensure(ResourcesRoot);
            Ensure(CharactersDir);
            Ensure(LevelsDir);
        }

        private static void CreatePlayerPrefabAsset()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "Player";
            go.tag = "Player";
            var col = go.GetComponent<Collider>();
            if (col != null) col.isTrigger = false;
            var rb = go.GetComponent<Rigidbody>();
            if (rb == null) rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            go.AddComponent<PlayerControllerTopDown>();
            go.AddComponent<PlayerHealth>();
            go.AddComponent<PlayerHitFlash>();
            go.transform.position = Vector3.zero;

            PersistLitMaterialsForHierarchy(go);
            var path = CharactersDir + "/Player.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }

        private static void CreateWitnessPrefabAsset()
        {
            var npc = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            npc.name = "NarrativeWitnessNPC";
            npc.transform.localScale = new Vector3(1.2f, 1.25f, 1.2f);
            var npcRenderer = npc.GetComponent<Renderer>();
            if (npcRenderer != null) GrayboxTintUtil.Apply(npcRenderer, new Color(0.75f, 0.75f, 0.85f));
            var col = npc.GetComponent<Collider>();
            if (col != null) col.isTrigger = false;
            var agent = npc.AddComponent<NavMeshAgent>();
            agent.speed = 3.2f;
            agent.angularSpeed = 720f;
            agent.acceleration = 14f;
            agent.stoppingDistance = 0.6f;
            npc.AddComponent<NPCNavReact>();

            PersistLitMaterialsForHierarchy(npc);
            var path = CharactersDir + "/NarrativeWitnessNPC.prefab";
            PrefabUtility.SaveAsPrefabAsset(npc, path);
            Object.DestroyImmediate(npc);
        }

        private static void BakeLevelPrefab(int levelIndex)
        {
            EnsureFolders();
            var tmp = new GameObject("EditorBake_TempLevel");
            try
            {
                var fg = tmp.AddComponent<FloorGenerator>();
                fg.GenerateLevel(levelIndex);
                var root = tmp.transform.Find("LevelRoot");
                if (root == null)
                {
                    Debug.LogError($"[HollowDescent Bake] LevelRoot missing after GenerateLevel({levelIndex}).");
                    return;
                }

                var start = fg.GetStartPosition();
                if (start.HasValue)
                {
                    var ps = new GameObject("PlayerStart");
                    ps.transform.SetParent(root);
                    ps.transform.position = start.Value + Vector3.up;
                    ps.transform.rotation = Quaternion.identity;
                }

                PersistLitMaterialsForHierarchy(root.gameObject);

                var assetPath = $"{LevelsDir}/Level_{levelIndex:00}.prefab";
                PrefabUtility.SaveAsPrefabAsset(root.gameObject, assetPath);
                Debug.Log($"[HollowDescent Bake] Saved {assetPath}");
            }
            finally
            {
                Object.DestroyImmediate(tmp);
            }
        }

        private static void EnsureBakedMaterialsFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Materials"))
                AssetDatabase.CreateFolder("Assets", "Materials");
            if (!AssetDatabase.IsValidFolder(BakedMaterialsDir))
                AssetDatabase.CreateFolder("Assets/Materials", "BakedGraybox");
        }

        private static Shader ResolveLitShader()
        {
            var s = Shader.Find("Universal Render Pipeline/Lit");
            if (s == null) s = Shader.Find("HDRP/Lit");
            if (s == null) s = Shader.Find("Standard");
            return s;
        }

        /// <summary>
        /// Assigns saved <see cref="Material"/> assets under BakedGraybox so prefab YAML keeps valid fileIDs.
        /// </summary>
        private static void PersistLitMaterialsForHierarchy(GameObject root)
        {
            if (root == null) return;
            var shader = ResolveLitShader();
            if (shader == null)
            {
                Debug.LogError("[HollowDescent Bake] No Lit shader found (URP/HDRP/Standard). Cannot fix materials.");
                return;
            }

            EnsureBakedMaterialsFolder();
            var toDestroy = new HashSet<Material>();
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                var oldMats = r.sharedMaterials;
                if (oldMats == null || oldMats.Length == 0) continue;
                var repl = new Material[oldMats.Length];
                for (var i = 0; i < oldMats.Length; i++)
                {
                    Color baseCol = new Color(0.45f, 0.45f, 0.45f, 1f);
                    var old = oldMats[i];
                    if (old != null)
                    {
                        if (old.HasProperty("_BaseColor")) baseCol = old.GetColor("_BaseColor");
                        else if (old.HasProperty("_Color")) baseCol = old.GetColor("_Color");
                    }

                    repl[i] = GetOrCreateLitMaterialAsset(baseCol, shader);
                }

                r.sharedMaterials = repl;
                foreach (var m in oldMats)
                    if (m != null && !EditorUtility.IsPersistent(m))
                        toDestroy.Add(m);
            }

            foreach (var m in toDestroy)
                Object.DestroyImmediate(m);

            AssetDatabase.SaveAssets();
        }

        private static Material GetOrCreateLitMaterialAsset(Color baseCol, Shader shader)
        {
            var rq = Mathf.Clamp(Mathf.RoundToInt(baseCol.r * 63f), 0, 63);
            var gq = Mathf.Clamp(Mathf.RoundToInt(baseCol.g * 63f), 0, 63);
            var bq = Mathf.Clamp(Mathf.RoundToInt(baseCol.b * 63f), 0, 63);
            var aq = Mathf.Clamp(Mathf.RoundToInt(baseCol.a * 15f), 0, 15);
            var fileName = $"GrayboxLit_{rq}_{gq}_{bq}_{aq}.mat";
            var path = $"{BakedMaterialsDir}/{fileName}";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            var mat = new Material(shader);
            mat.name = fileName.Replace(".mat", "");
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseCol);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", baseCol);
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }
    }
}
