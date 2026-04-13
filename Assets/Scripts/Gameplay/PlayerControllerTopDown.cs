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
        [SerializeField] private float fireRate = 0.5f;
        [SerializeField] private float projectileSpeed = 18f;
        [SerializeField] private float projectileLifetime = 4f;
        [SerializeField] private float projectileRadius = 0.25f;
        [SerializeField] private float projectileSpawnHeight = 0f;

        [Header("Debug (aim/shoot)")]
        [SerializeField] private bool enableAimDebug = false;
        [SerializeField] private float aimDebugThrottleSeconds = 0.5f;
        private float _lastAimDebugTime = -999f;

        private float _nextFireTime;
        private Camera _cam;
        private Rigidbody _rb;
        private Vector3 _moveInput;
        private PlayerStatModifiers _mods;

        private PlayerStatModifiers Mods
        {
            get
            {
                if (_mods == null) _mods = GetComponent<PlayerStatModifiers>();
                return _mods;
            }
        }

        private void Awake()
        {
            _cam = Camera.main;
            _rb = GetComponent<Rigidbody>();
            // Guard against stale serialized values from older prefabs/scenes.
            if (fireRate < 0.4f) fireRate = 0.5f;
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
            _moveInput = new Vector3(h, 0f, v).normalized;

            AimAtMouse();
            if (Mouse.current != null && Mouse.current.leftButton.isPressed && Time.time >= _nextFireTime)
                Shoot();
        }

        private void FixedUpdate()
        {
            var speed = Mods != null ? Mods.GetEffectiveMoveSpeed(moveSpeed) : moveSpeed;
            if (_rb != null)
            {
                var horizontal = new Vector3(_moveInput.x, 0f, _moveInput.z);
                if (horizontal.sqrMagnitude > 0.01f)
                {
                    var v = horizontal.normalized * speed;
                    var vel = _rb.linearVelocity;
                    _rb.linearVelocity = new Vector3(v.x, vel.y, v.z);
                }
                else
                {
                    var vel = _rb.linearVelocity;
                    _rb.linearVelocity = new Vector3(0f, vel.y, 0f);
                }
                _rb.angularVelocity = Vector3.zero;
                return;
            }
            if (_moveInput.sqrMagnitude > 0.01f)
                transform.position += _moveInput * (speed * Time.fixedDeltaTime);
        }

        private bool GetCursorAimDirection(out Vector3 direction)
        {
            return GetAimDirectionFromScreenPoint(out direction, Mouse.current != null ? Mouse.current.position.ReadValue() : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
        }

        private bool GetAimDirectionFromScreenPoint(out Vector3 direction, Vector2 screenPoint)
        {
            direction = Vector3.forward;
            if (_cam == null) _cam = Camera.main;
            if (_cam == null)
            {
                if (enableAimDebug && Time.time - _lastAimDebugTime >= aimDebugThrottleSeconds)
                {
                    Debug.Log("[Aim] Camera is null (Camera.main not found).");
                    _lastAimDebugTime = Time.time;
                }
                return false;
            }
            var ray = _cam.ScreenPointToRay(screenPoint);
            var aimPlane = new Plane(Vector3.up, Vector3.zero);
            if (!aimPlane.Raycast(ray, out var enter))
            {
                direction = _cam.transform.forward;
                direction.y = 0f;
                if (direction.sqrMagnitude < 0.01f) direction = new Vector3(0f, 0f, 1f);
                direction = direction.normalized;
                if (enableAimDebug && Time.time - _lastAimDebugTime >= aimDebugThrottleSeconds)
                {
                    Debug.Log($"[Aim] Raycast missed plane (e.g. cursor at top of view); using camera forward. screenPoint=({screenPoint.x:F0},{screenPoint.y:F0})");
                    _lastAimDebugTime = Time.time;
                }
                return true;
            }
            var world = ray.GetPoint(enter);
            world.y = transform.position.y;
            direction = (world - transform.position);
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f)
            {
                direction = _cam.transform.forward;
                direction.y = 0f;
                if (direction.sqrMagnitude < 0.01f) direction = new Vector3(0f, 0f, 1f);
                direction = direction.normalized;
                if (enableAimDebug && Time.time - _lastAimDebugTime >= aimDebugThrottleSeconds)
                {
                    Debug.Log("[Aim] Direction too small (cursor on player?); using camera forward.");
                    _lastAimDebugTime = Time.time;
                }
                return true;
            }
            direction = direction.normalized;
            return true;
        }

        private void AimAtMouse()
        {
            var usedCursor = GetCursorAimDirection(out var dir);
            if (usedCursor)
            {
                transform.LookAt(transform.position + dir);
                return;
            }
            if (GetAimDirectionFromScreenPoint(out dir, new Vector2(Screen.width * 0.5f, Screen.height * 0.5f)))
            {
                transform.LookAt(transform.position + dir);
                if (enableAimDebug && Time.time - _lastAimDebugTime >= aimDebugThrottleSeconds)
                {
                    Debug.Log("[Aim] Using screen-center fallback (cursor ray failed).");
                    _lastAimDebugTime = Time.time;
                }
            }
        }

        private void Shoot()
        {
            Vector3 aimDir;
            var usedCursor = GetCursorAimDirection(out aimDir);
            if (!usedCursor)
            {
                aimDir = transform.forward.y == 0f && transform.forward.sqrMagnitude > 0.01f ? transform.forward : new Vector3(0f, 0f, 1f);
                if (enableAimDebug)
                    Debug.Log($"[Shoot] Using fallback direction (cursor ray failed). aimDir={aimDir}");
            }
            else if (enableAimDebug)
            {
                Debug.Log($"[Shoot] Fired toward cursor. aimDir={aimDir}");
            }
            aimDir.y = 0f;
            if (aimDir.sqrMagnitude < 0.01f) aimDir = new Vector3(0f, 0f, 1f);
            aimDir = aimDir.normalized;
            var effectiveFireRate = Mods != null ? Mods.GetEffectiveFireRate(fireRate) : fireRate;
            _nextFireTime = Time.time + effectiveFireRate;
            var shootOrigin = transform.position + Vector3.up * projectileSpawnHeight + aimDir * 1.5f;
            var proj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            proj.name = "Projectile";
            proj.transform.position = shootOrigin;
            proj.transform.localScale = Vector3.one * (projectileRadius * 2f);
            var col = proj.GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
            var rb = proj.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            var p = proj.AddComponent<Projectile>();
            var hitR = Mathf.Max(0.08f, projectileRadius * 1.15f);
            var dmg = Mathf.Max(1, Mathf.RoundToInt(Mods != null ? Mods.GetDamageMultiplier() : 1f));
            p.Init(aimDir, projectileSpeed, projectileLifetime, hitR, dmg);
        }
    }
}
