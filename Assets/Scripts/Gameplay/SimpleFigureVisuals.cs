using UnityEngine;
using HollowDescent.AI;
using HollowDescent.LevelGen;

namespace HollowDescent.Gameplay
{
    /// <summary>
    /// Builds simple humanoid-ish figures from primitives (body + head + extras) so characters
    /// are not single blobs. Swap root visuals later for real meshes without changing gameplay code.
    /// </summary>
    public static class SimpleFigureVisuals
    {
        private static void DestroyColliderSafe(Collider col)
        {
            if (col == null) return;
            if (Application.isPlaying)
                Object.Destroy(col);
            else
                Object.DestroyImmediate(col);
        }

        private static GameObject AddPrimitiveVisual(Transform parent, PrimitiveType type, Vector3 localPos, Vector3 localScale, Color color)
        {
            var p = GameObject.CreatePrimitive(type);
            p.name = type + "_Visual";
            p.transform.SetParent(parent, false);
            p.transform.localPosition = localPos;
            p.transform.localScale = localScale;
            DestroyColliderSafe(p.GetComponent<Collider>());
            var r = p.GetComponent<Renderer>();
            if (r != null) GrayboxTintUtil.Apply(r, color);
            return p;
        }

        /// <summary>Capsule collider with feet at the root transform origin (good for top-down Y-locked rigidbodies).</summary>
        public static void SetupHumanoidCapsulePhysics(GameObject root)
        {
            var col = root.GetComponent<CapsuleCollider>();
            if (col == null) col = root.AddComponent<CapsuleCollider>();
            col.height = 1.8f;
            col.radius = 0.42f;
            col.center = new Vector3(0f, 0.9f, 0f);
            col.isTrigger = false;

            var rb = root.GetComponent<Rigidbody>();
            if (rb == null) rb = root.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }

        public static void SetupHumanoidCapsulePhysicsPlayer(GameObject root)
        {
            var col = root.GetComponent<CapsuleCollider>();
            if (col == null) col = root.AddComponent<CapsuleCollider>();
            col.height = 1.8f;
            col.radius = 0.4f;
            col.center = new Vector3(0f, 0.9f, 0f);
            col.isTrigger = false;

            var rb = root.GetComponent<Rigidbody>();
            if (rb == null) rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        public static GameObject CreateChaserEnemy(Vector3 worldPos)
        {
            var root = new GameObject("Enemy_Chaser");
            root.transform.position = worldPos;
            SetupHumanoidCapsulePhysics(root);

            var body = new Color(0.92f, 0.22f, 0.2f);
            var head = new Color(0.98f, 0.55f, 0.48f);
            AddPrimitiveVisual(root.transform, PrimitiveType.Capsule, new Vector3(0f, 0.9f, 0f), new Vector3(0.62f, 0.55f, 0.62f), body);
            AddPrimitiveVisual(root.transform, PrimitiveType.Sphere, new Vector3(0f, 1.52f, 0f), Vector3.one * 0.38f, head);
            AddPrimitiveVisual(root.transform, PrimitiveType.Cube, new Vector3(0f, 1.12f, 0.28f), new Vector3(0.55f, 0.2f, 0.35f), body * 0.85f);

            var chaser = root.AddComponent<EnemyChaser>();
            chaser.SetPlayer(GameObject.FindGameObjectWithTag("Player")?.transform);
            return root;
        }

        public static GameObject CreateShooterEnemy(Vector3 worldPos)
        {
            var root = new GameObject("Enemy_Shooter");
            root.transform.position = worldPos;
            SetupHumanoidCapsulePhysics(root);

            var body = new Color(0.82f, 0.42f, 0.12f);
            var head = new Color(0.95f, 0.72f, 0.45f);
            AddPrimitiveVisual(root.transform, PrimitiveType.Capsule, new Vector3(0f, 0.88f, 0f), new Vector3(0.52f, 0.52f, 0.52f), body);
            AddPrimitiveVisual(root.transform, PrimitiveType.Sphere, new Vector3(0f, 1.45f, 0f), Vector3.one * 0.34f, head);
            var gun = AddPrimitiveVisual(root.transform, PrimitiveType.Cube, new Vector3(0.35f, 1.05f, 0.35f), new Vector3(0.22f, 0.16f, 0.45f), new Color(0.18f, 0.18f, 0.2f));
            gun.name = "GunHint_Visual";

            var shooter = root.AddComponent<EnemyShooter>();
            shooter.SetPlayer(GameObject.FindGameObjectWithTag("Player")?.transform);
            return root;
        }

        public static GameObject CreateFlankerEnemy(Vector3 worldPos)
        {
            var root = new GameObject("Enemy_Flanker");
            root.transform.position = worldPos;
            SetupHumanoidCapsulePhysics(root);

            var body = new Color(0.62f, 0.18f, 0.58f);
            var head = new Color(0.85f, 0.45f, 0.78f);
            AddPrimitiveVisual(root.transform, PrimitiveType.Cube, new Vector3(0f, 0.55f, 0f), new Vector3(0.75f, 0.65f, 0.55f), body);
            AddPrimitiveVisual(root.transform, PrimitiveType.Sphere, new Vector3(0f, 1.05f, 0f), Vector3.one * 0.36f, head);
            AddPrimitiveVisual(root.transform, PrimitiveType.Cube, new Vector3(-0.42f, 0.45f, 0f), new Vector3(0.22f, 0.35f, 0.28f), body * 0.9f);
            AddPrimitiveVisual(root.transform, PrimitiveType.Cube, new Vector3(0.42f, 0.45f, 0f), new Vector3(0.22f, 0.35f, 0.28f), body * 0.9f);

            var flanker = root.AddComponent<EnemyFlanker>();
            flanker.SetPlayer(GameObject.FindGameObjectWithTag("Player")?.transform);
            return root;
        }

        /// <summary>Visual + physics only; <see cref="EnemyFinalBoss"/> is added by <see cref="RoomController"/>.</summary>
        public static GameObject CreateFinalBossEnemy(Vector3 worldPos)
        {
            var root = new GameObject("Enemy_FinalBoss");
            root.transform.position = worldPos;
            SetupHumanoidCapsulePhysics(root);

            var robe = new Color(0.25f, 0.08f, 0.42f);
            var head = new Color(0.55f, 0.35f, 0.72f);
            AddPrimitiveVisual(root.transform, PrimitiveType.Capsule, new Vector3(0f, 0.95f, 0f), new Vector3(0.72f, 0.75f, 0.72f), robe);
            AddPrimitiveVisual(root.transform, PrimitiveType.Sphere, new Vector3(0f, 1.62f, 0f), Vector3.one * 0.42f, head);
            AddPrimitiveVisual(root.transform, PrimitiveType.Cylinder, new Vector3(0f, 1.98f, 0f), new Vector3(0.55f, 0.06f, 0.55f), new Color(0.35f, 0.2f, 0.55f));

            return root;
        }

        public static GameObject CreateBossMinion(Vector3 worldPos)
        {
            var root = new GameObject("Enemy_BossMinion");
            root.transform.position = worldPos;
            SetupHumanoidCapsulePhysics(root);

            var body = new Color(0.52f, 0.12f, 0.48f);
            var head = new Color(0.78f, 0.48f, 0.72f);
            AddPrimitiveVisual(root.transform, PrimitiveType.Capsule, new Vector3(0f, 0.75f, 0f), new Vector3(0.52f, 0.48f, 0.52f), body);
            AddPrimitiveVisual(root.transform, PrimitiveType.Sphere, new Vector3(0f, 1.28f, 0f), Vector3.one * 0.32f, head);

            var chaser = root.AddComponent<EnemyChaser>();
            chaser.SetPlayer(GameObject.FindGameObjectWithTag("Player")?.transform);
            return root;
        }

        public static GameObject CreatePlayerFallback(Vector3 worldPos)
        {
            var root = new GameObject("Player");
            root.transform.position = worldPos;
            root.tag = "Player";
            SetupHumanoidCapsulePhysicsPlayer(root);

            var body = new Color(0.18f, 0.55f, 0.82f);
            var head = new Color(0.92f, 0.8f, 0.72f);
            AddPrimitiveVisual(root.transform, PrimitiveType.Capsule, new Vector3(0f, 0.9f, 0f), new Vector3(0.58f, 0.52f, 0.58f), body);
            AddPrimitiveVisual(root.transform, PrimitiveType.Sphere, new Vector3(0f, 1.48f, 0f), Vector3.one * 0.36f, head);
            AddPrimitiveVisual(root.transform, PrimitiveType.Cube, new Vector3(0.28f, 0.95f, 0.12f), new Vector3(0.35f, 0.25f, 0.2f), new Color(0.12f, 0.35f, 0.55f));

            return root;
        }

        public static GameObject CreateWitnessNpcFallback(Vector3 worldPos)
        {
            var root = new GameObject("NarrativeWitnessNPC");
            root.transform.position = worldPos;
            var col = root.GetComponent<CapsuleCollider>();
            if (col == null) col = root.AddComponent<CapsuleCollider>();
            col.height = 1.85f;
            col.radius = 0.44f;
            col.center = new Vector3(0f, 0.92f, 0f);
            col.isTrigger = false;

            var body = new Color(0.72f, 0.74f, 0.88f);
            var head = new Color(0.92f, 0.88f, 0.82f);
            AddPrimitiveVisual(root.transform, PrimitiveType.Capsule, new Vector3(0f, 0.88f, 0f), new Vector3(0.65f, 0.58f, 0.65f), body);
            AddPrimitiveVisual(root.transform, PrimitiveType.Sphere, new Vector3(0f, 1.45f, 0f), Vector3.one * 0.34f, head);

            return root;
        }
    }
}
