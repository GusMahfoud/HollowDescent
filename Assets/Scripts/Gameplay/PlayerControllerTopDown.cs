using UnityEngine;
using UnityEngine.InputSystem;

namespace HollowDescent.Gameplay
{
    /// <summary>
    /// Top-down WASD movement, aim at mouse, left-click shoot.
    /// Uses the new Input System (UnityEngine.InputSystem).
    /// </summary>
    public class PlayerControllerTopDown : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 8f;

        [Header("Combat")]
        [SerializeField] private float fireRate = 0.2f;
        [SerializeField] private float projectileSpeed = 18f;
        [SerializeField] private float projectileLifetime = 2f;
        [SerializeField] private float projectileRadius = 0.25f;

        private float _nextFireTime;
        private Plane _groundPlane = new Plane(Vector3.up, Vector3.zero);
        private Camera _cam;

        private void Awake()
        {
            _cam = Camera.main;
        }

        private void Update()
        {
            var h = 0f;
            var v = 0f;
            if (Keyboard.current != null)
            {
                if (Keyboard.current[Key.D].isPressed) h += 1f;
                if (Keyboard.current[Key.A].isPressed) h -= 1f;
                if (Keyboard.current[Key.W].isPressed) v += 1f;
                if (Keyboard.current[Key.S].isPressed) v -= 1f;
            }
            var dir = new Vector3(h, 0f, v).normalized;
            if (dir.sqrMagnitude > 0.01f)
                transform.position += dir * (moveSpeed * Time.deltaTime);

            AimAtMouse();
            if (Mouse.current != null && Mouse.current.leftButton.isPressed && Time.time >= _nextFireTime)
                Shoot();
        }

        private void AimAtMouse()
        {
            if (_cam == null) return;
            var mousePos = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
            var ray = _cam.ScreenPointToRay(mousePos);
            if (_groundPlane.Raycast(ray, out var enter))
            {
                var world = ray.GetPoint(enter);
                world.y = transform.position.y;
                transform.LookAt(world);
            }
        }

        private void Shoot()
        {
            _nextFireTime = Time.time + fireRate;
            var proj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            proj.name = "Projectile";
            proj.transform.position = transform.position + Vector3.up * 1f + transform.forward * 1.5f;
            proj.transform.localScale = Vector3.one * (projectileRadius * 2f);
            var col = proj.GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
            var rb = proj.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            var p = proj.AddComponent<Projectile>();
            p.Init(transform.forward, projectileSpeed, projectileLifetime);
        }
    }
}
