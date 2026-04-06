using System.Collections;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HollowDescent.LevelGen
{
    /// <summary>
    /// Door blocker that can animate open/close via Animator (if present) or fallback double-door sliding leaves.
    /// </summary>
    public class DoorBlocker : MonoBehaviour
    {
        private const string DefaultDoorPrefabResourcePath = "Prefabs/Environment/DoorBlocker";
        private const string DefaultCloseSoundResourcesPath = "Audio/close-door";
#if UNITY_EDITOR
        private const string EditorCloseSoundAssetPath = "Assets/Audio/close-door.mp3";
#endif

        [Header("Visual")]
        [SerializeField] private Transform animatedVisualOrNull;
        [SerializeField] private GameObject visualPrefabOrNull;
        [SerializeField] private Animator animatorOrNull;
        [SerializeField] private string animatorOpenBool = "IsOpen";

        [Header("Audio")]
        [SerializeField] private AudioClip closeDoorClipOrNull;
        [SerializeField, Range(0f, 1f)] private float closeDoorVolume = 0.9f;
        [SerializeField, Range(0f, 1f)] private float audioSpatialBlend = 1f;

        [Header("Double Door Fallback")]
        [SerializeField] private bool useDoubleDoorFallback = true;
        [SerializeField] private Transform leftLeafOrNull;
        [SerializeField] private Transform rightLeafOrNull;
        [SerializeField] private float openSlideDistance = 0.8f;
        [SerializeField, Min(0.05f)] private float hiddenInWallThickness = 0.2f;
        [SerializeField, Min(0f)] private float hiddenFromAboveHeightOffset = 0.15f;
        [SerializeField, Min(0.01f)] private float openTransitionSeconds = 0.45f;
        [SerializeField, Min(0.01f)] private float closeTransitionSeconds = 0.25f;

        private Vector3 _leftClosedLocalPos;
        private Vector3 _rightClosedLocalPos;
        private Vector3 _leftOpenLocalPos;
        private Vector3 _rightOpenLocalPos;
        private Coroutine _moveRoutine;
        private Collider _blockerCollider;
        private AudioSource _audioSource;
        private bool _isOpen;

        private void Awake()
        {
            if (animatedVisualOrNull == null)
            {
                var prefab = visualPrefabOrNull != null
                    ? visualPrefabOrNull
                    : Resources.Load<GameObject>(DefaultDoorPrefabResourcePath);
                if (prefab != null)
                {
                    var visual = Instantiate(prefab, transform);
                    visual.name = "DoorVisual";
                    visual.transform.localPosition = Vector3.zero;
                    visual.transform.localRotation = Quaternion.identity;
                    visual.transform.localScale = Vector3.one;
                    animatedVisualOrNull = visual.transform;

                    var baseRenderer = GetComponent<Renderer>();
                    if (baseRenderer != null)
                        baseRenderer.enabled = false;
                }
                else
                {
                    animatedVisualOrNull = transform;
                }
            }
            if (animatorOrNull == null)
                animatorOrNull = animatedVisualOrNull.GetComponent<Animator>();
            _blockerCollider = GetComponent<Collider>();
            if (_blockerCollider == null)
                _blockerCollider = gameObject.AddComponent<BoxCollider>();
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
                _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = audioSpatialBlend;
            _audioSource.rolloffMode = AudioRolloffMode.Linear;
            _audioSource.maxDistance = 22f;
            _audioSource.minDistance = 1.5f;
            ResolveCloseDoorClipIfNeeded();

            // Keep legacy/baked cube blockers thin so they visually tuck into the wall.
            if (animatorOrNull == null && animatedVisualOrNull == transform)
                ApplyThinProfileForLegacyBlocker();

            BuildFallbackDoubleDoorLeavesIfNeeded();
        }

        private void ApplyThinProfileForLegacyBlocker()
        {
            var s = transform.localScale;
            var t = Mathf.Max(0.05f, hiddenInWallThickness);
            if (s.x <= s.z)
                s.x = Mathf.Min(s.x, t);
            else
                s.z = Mathf.Min(s.z, t);
            s.y = Mathf.Max(0.2f, s.y - hiddenFromAboveHeightOffset);
            transform.localScale = s;
        }

        private void ResolveCloseDoorClipIfNeeded()
        {
            if (closeDoorClipOrNull != null) return;
            closeDoorClipOrNull = Resources.Load<AudioClip>(DefaultCloseSoundResourcesPath);
#if UNITY_EDITOR
            if (closeDoorClipOrNull == null)
                closeDoorClipOrNull = AssetDatabase.LoadAssetAtPath<AudioClip>(EditorCloseSoundAssetPath);
#endif
        }

        private void TryPlayCloseDoorSound()
        {
            if (_audioSource == null) return;
            if (closeDoorClipOrNull == null)
                ResolveCloseDoorClipIfNeeded();
            if (closeDoorClipOrNull == null) return;
            _audioSource.PlayOneShot(closeDoorClipOrNull, closeDoorVolume);
        }

        public void SetOpen(bool open, bool instant = false)
        {
            var wasOpen = _isOpen;
            _isOpen = open;

            if (_blockerCollider != null)
                _blockerCollider.enabled = !open;
            if (wasOpen && !open)
                TryPlayCloseDoorSound();

            if (animatorOrNull != null)
            {
                animatorOrNull.SetBool(animatorOpenBool, open);
                if (instant)
                    animatorOrNull.Update(0f);
                return;
            }

            if (!useDoubleDoorFallback || leftLeafOrNull == null || rightLeafOrNull == null) return;
            if (_moveRoutine != null)
                StopCoroutine(_moveRoutine);

            var targetLeft = open ? _leftOpenLocalPos : _leftClosedLocalPos;
            var targetRight = open ? _rightOpenLocalPos : _rightClosedLocalPos;
            var duration = open ? openTransitionSeconds : closeTransitionSeconds;
            if (instant || duration <= 0.001f)
            {
                leftLeafOrNull.localPosition = targetLeft;
                rightLeafOrNull.localPosition = targetRight;
                return;
            }

            _moveRoutine = StartCoroutine(AnimateTo(targetLeft, targetRight, duration));
        }

        private void BuildFallbackDoubleDoorLeavesIfNeeded()
        {
            if (animatorOrNull != null || !useDoubleDoorFallback || animatedVisualOrNull == null) return;

            if (leftLeafOrNull == null)
                leftLeafOrNull = animatedVisualOrNull.Find("LeftDoor");
            if (rightLeafOrNull == null)
                rightLeafOrNull = animatedVisualOrNull.Find("RightDoor");

            if (leftLeafOrNull == null || rightLeafOrNull == null)
                CreateGeneratedLeaves();
            if (leftLeafOrNull == null || rightLeafOrNull == null) return;

            _leftClosedLocalPos = leftLeafOrNull.localPosition;
            _rightClosedLocalPos = rightLeafOrNull.localPosition;

            var splitAlongX = Mathf.Abs(_leftClosedLocalPos.x - _rightClosedLocalPos.x) >= Mathf.Abs(_leftClosedLocalPos.z - _rightClosedLocalPos.z);
            var slide = Mathf.Max(0.2f, openSlideDistance);
            if (splitAlongX)
            {
                _leftOpenLocalPos = _leftClosedLocalPos + Vector3.left * slide;
                _rightOpenLocalPos = _rightClosedLocalPos + Vector3.right * slide;
            }
            else
            {
                _leftOpenLocalPos = _leftClosedLocalPos + Vector3.back * slide;
                _rightOpenLocalPos = _rightClosedLocalPos + Vector3.forward * slide;
            }
        }

        private void CreateGeneratedLeaves()
        {
            if (animatedVisualOrNull == null) return;

            var rootScale = animatedVisualOrNull.localScale;
            var parentScale = transform.localScale;
            if (Mathf.Abs(parentScale.x) < 0.001f) parentScale.x = 1f;
            if (Mathf.Abs(parentScale.y) < 0.001f) parentScale.y = 1f;
            if (Mathf.Abs(parentScale.z) < 0.001f) parentScale.z = 1f;
            var splitAlongX = rootScale.x >= rootScale.z;

            var leafScale = rootScale;
            if (splitAlongX) leafScale.x = Mathf.Max(0.2f, rootScale.x * 0.5f);
            else leafScale.z = Mathf.Max(0.2f, rootScale.z * 0.5f);
            leafScale.y = Mathf.Max(0.2f, rootScale.y - hiddenFromAboveHeightOffset);
            var leafLocalScale = new Vector3(
                leafScale.x / parentScale.x,
                leafScale.y / parentScale.y,
                leafScale.z / parentScale.z);

            var sourceRenderer = animatedVisualOrNull.GetComponent<Renderer>();
            if (sourceRenderer != null) sourceRenderer.enabled = false;

            leftLeafOrNull = CreateLeaf("LeftDoor", leafLocalScale);
            rightLeafOrNull = CreateLeaf("RightDoor", leafLocalScale);
            if (leftLeafOrNull == null || rightLeafOrNull == null) return;

            if (splitAlongX)
            {
                leftLeafOrNull.localPosition = new Vector3(-rootScale.x * 0.25f / parentScale.x, 0f, 0f);
                rightLeafOrNull.localPosition = new Vector3(rootScale.x * 0.25f / parentScale.x, 0f, 0f);
            }
            else
            {
                leftLeafOrNull.localPosition = new Vector3(0f, 0f, -rootScale.z * 0.25f / parentScale.z);
                rightLeafOrNull.localPosition = new Vector3(0f, 0f, rootScale.z * 0.25f / parentScale.z);
            }
            leftLeafOrNull.localRotation = Quaternion.identity;
            rightLeafOrNull.localRotation = Quaternion.identity;

            if (sourceRenderer != null)
            {
                var leftRenderer = leftLeafOrNull.GetComponent<Renderer>();
                var rightRenderer = rightLeafOrNull.GetComponent<Renderer>();
                if (leftRenderer != null) leftRenderer.sharedMaterials = sourceRenderer.sharedMaterials;
                if (rightRenderer != null) rightRenderer.sharedMaterials = sourceRenderer.sharedMaterials;
            }
        }

        private Transform CreateLeaf(string leafName, Vector3 leafScale)
        {
            var leaf = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leaf.name = leafName;
            leaf.transform.SetParent(transform);
            leaf.transform.localScale = leafScale;
            var col = leaf.GetComponent<Collider>();
            if (col != null) Destroy(col);
            return leaf.transform;
        }

        private IEnumerator AnimateTo(Vector3 leftTarget, Vector3 rightTarget, float duration)
        {
            var leftStart = leftLeafOrNull.localPosition;
            var rightStart = rightLeafOrNull.localPosition;
            var t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                var u = Mathf.Clamp01(t / duration);
                u = u * u * (3f - 2f * u);
                leftLeafOrNull.localPosition = Vector3.LerpUnclamped(leftStart, leftTarget, u);
                rightLeafOrNull.localPosition = Vector3.LerpUnclamped(rightStart, rightTarget, u);
                yield return null;
            }
            leftLeafOrNull.localPosition = leftTarget;
            rightLeafOrNull.localPosition = rightTarget;
            _moveRoutine = null;
        }

        public bool IsOpen() => _isOpen;
    }
}
