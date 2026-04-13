using System.Collections.Generic;
using UnityEngine;
using HollowDescent.Bootstrap;

namespace HollowDescent.LevelGen
{
    /// <summary>
    /// Generates graybox floor: semi-linear branch/reconverge room graph, corridors, doors, occlusion, landmarks, vertical pads.
    /// </summary>
    public class FloorGenerator : MonoBehaviour
    {
        [Header("Room dimensions")]
        [SerializeField] private float roomWidth = 14f;
        [SerializeField] private float roomDepth = 10f;
        [SerializeField] private float wallHeight = 3f;
        [SerializeField] private float corridorWidth = 3f;
        [SerializeField] private float corridorLength = 4f;

        [Header("Occlusion & landmarks")]
        [SerializeField] private float pillarSize = 1.2f;
        [SerializeField] private float landmarkPillarHeight = 4f;
        [SerializeField] private float raisedPadHeight = 0.5f;
        [SerializeField] private float raisedPadSize = 3f;

        [Header("Lighting (wayfinding)")]
        [SerializeField] private float safeRoomLightIntensity = 1.2f;
        [SerializeField] private float combatRoomLightIntensity = 0.85f;
        [SerializeField] private Color safeRoomTint = new Color(0.95f, 1f, 0.9f);
        [SerializeField] private Color combatRoomTint = new Color(1f, 0.9f, 0.85f);

        [Header("Debug")]
        [SerializeField] private bool drawGizmos = true;

        [Header("Doors")]
        [Tooltip("Optional visual door prefab used for encounter blockers. If null, uses a graybox cube.")]
        [SerializeField] private GameObject doorPrefabOrNull;
        [SerializeField, Min(0.05f)] private float doorBlockerThickness = 0.2f;
        [SerializeField, Min(0f)] private float doorTopInset = 0.15f;

        private bool _hasGenerated;
        private int _floorGenLevelIndex = 1;
        private readonly List<RoomDef> _rooms = new List<RoomDef>();
        private readonly List<RoomController> _roomControllers = new List<RoomController>();

        private struct RoomDef
        {
            public Vector3 Center;
            public float W, D;
            public RoomType Type;
            public string Name;
            public List<DoorLink> Doors;
            public int Index;
        }

        private struct DoorLink
        {
            public Vector3 Position;
            public Vector3 Normal;
            public float Width;
            public bool IsLevelExit;
        }

        private Vector3? _lastGeneratedStartPosition;

        public bool HasGenerated() => _hasGenerated;

        public Vector3? GetStartPosition() => _lastGeneratedStartPosition;

        public void Generate()
        {
            GenerateLevel(1);
        }

        public void GenerateLevel(int levelIndex)
        {
            _floorGenLevelIndex = levelIndex;
            _rooms.Clear();
            _roomControllers.Clear();
            _lastGeneratedStartPosition = null;
            var existing = transform.Find("LevelRoot");
            if (existing != null) Destroy(existing.gameObject);

            var levelRoot = new GameObject("LevelRoot");
            levelRoot.transform.SetParent(transform);

            if (levelIndex == 1)
                BuildLevel1Rooms();
            else if (levelIndex == 2)
                BuildLevel2Rooms();
            else if (levelIndex == 3)
                BuildLevel3Rooms();
            else
                BuildLevel1Rooms();

            if (_rooms.Count > 0)
                _lastGeneratedStartPosition = _rooms[0].Center;

            foreach (var r in _rooms)
            {
                var roomGeo = new GameObject("RoomGeometry_" + r.Name);
                roomGeo.transform.SetParent(levelRoot.transform);
                BuildRoom(roomGeo.transform, r);
                BuildRoomController(levelRoot.transform, r, roomGeo.transform);
            }

            _hasGenerated = true;
            if (LevelManager.Instance != null)
                LevelManager.Instance.RegisterLevelRoot(levelRoot);
        }

        private void BuildLevel1Rooms()
        {
            var cx = 0f;
            var cz = 0f;
            var stepX = roomWidth + corridorLength;
            var stepZ = roomDepth + corridorLength;

            var start = new RoomDef
            {
                Center = new Vector3(cx, 0f, cz),
                W = roomWidth,
                D = roomDepth,
                Type = RoomType.StartSafe,
                Name = "Start (Safe)",
                Doors = new List<DoorLink>(),
                Index = 0
            };
            start.Doors.Add(new DoorLink { Position = start.Center + Vector3.right * (roomWidth * 0.5f), Normal = Vector3.right, Width = corridorWidth, IsLevelExit = false });
            _rooms.Add(start);

            cx += stepX;
            var combat1 = new RoomDef
            {
                Center = new Vector3(cx, 0f, cz),
                W = roomWidth,
                D = roomDepth,
                Type = RoomType.Combat,
                Name = "Combat 1",
                Doors = new List<DoorLink>(),
                Index = 1
            };
            combat1.Doors.Add(new DoorLink { Position = combat1.Center + Vector3.left * (roomWidth * 0.5f), Normal = Vector3.left, Width = corridorWidth, IsLevelExit = false });
            combat1.Doors.Add(new DoorLink { Position = combat1.Center + Vector3.forward * (roomDepth * 0.5f), Normal = Vector3.forward, Width = corridorWidth, IsLevelExit = false });
            combat1.Doors.Add(new DoorLink { Position = combat1.Center + Vector3.back * (roomDepth * 0.5f), Normal = Vector3.back, Width = corridorWidth, IsLevelExit = false });
            _rooms.Add(combat1);

            var branchA = new RoomDef
            {
                Center = new Vector3(cx, 0f, cz + stepZ),
                W = roomWidth,
                D = roomDepth,
                Type = RoomType.Combat,
                Name = "Branch A",
                Doors = new List<DoorLink>(),
                Index = 2
            };
            branchA.Doors.Add(new DoorLink { Position = branchA.Center + Vector3.back * (roomDepth * 0.5f), Normal = Vector3.back, Width = corridorWidth, IsLevelExit = false });
            branchA.Doors.Add(new DoorLink { Position = branchA.Center + Vector3.right * (roomWidth * 0.5f), Normal = Vector3.right, Width = corridorWidth, IsLevelExit = false });
            _rooms.Add(branchA);

            var branchB = new RoomDef
            {
                Center = new Vector3(cx, 0f, cz - stepZ),
                W = roomWidth,
                D = roomDepth,
                Type = RoomType.Combat,
                Name = "Branch B",
                Doors = new List<DoorLink>(),
                Index = 3
            };
            branchB.Doors.Add(new DoorLink { Position = branchB.Center + Vector3.forward * (roomDepth * 0.5f), Normal = Vector3.forward, Width = corridorWidth, IsLevelExit = false });
            branchB.Doors.Add(new DoorLink { Position = branchB.Center + Vector3.right * (roomWidth * 0.5f), Normal = Vector3.right, Width = corridorWidth, IsLevelExit = false });
            _rooms.Add(branchB);

            cx += stepX;
            var reconverge = new RoomDef
            {
                Center = new Vector3(cx, 0f, cz),
                W = roomWidth + 2f,
                // Keep enough vertical span to receive both branch corridors.
                D = roomDepth * 3f,
                Type = RoomType.Combat,
                Name = "Reconverge",
                Doors = new List<DoorLink>(),
                Index = 4
            };
            reconverge.Doors.Add(new DoorLink { Position = reconverge.Center + Vector3.left * (reconverge.W * 0.5f) + Vector3.forward * stepZ, Normal = Vector3.left, Width = corridorWidth, IsLevelExit = false });
            reconverge.Doors.Add(new DoorLink { Position = reconverge.Center + Vector3.left * (reconverge.W * 0.5f) + Vector3.back * stepZ, Normal = Vector3.left, Width = corridorWidth, IsLevelExit = false });
            reconverge.Doors.Add(new DoorLink { Position = reconverge.Center + Vector3.right * (reconverge.W * 0.5f), Normal = Vector3.right, Width = corridorWidth, IsLevelExit = false });
            _rooms.Add(reconverge);

            cx += reconverge.W * 0.5f + corridorLength + roomWidth * 0.5f;
            var shop = new RoomDef
            {
                Center = new Vector3(cx, 0f, cz),
                W = roomWidth,
                D = roomDepth,
                Type = RoomType.Safe,
                Name = "Shop (Safe)",
                Doors = new List<DoorLink>(),
                Index = 5
            };
            shop.Doors.Add(new DoorLink { Position = shop.Center + Vector3.left * (roomWidth * 0.5f), Normal = Vector3.left, Width = corridorWidth, IsLevelExit = false });
            shop.Doors.Add(new DoorLink { Position = shop.Center + Vector3.right * (roomWidth * 0.5f), Normal = Vector3.right, Width = corridorWidth, IsLevelExit = false });
            _rooms.Add(shop);

            cx += stepX;
            var levelExit = new RoomDef
            {
                Center = new Vector3(cx, 0f, cz),
                W = roomWidth,
                D = roomDepth,
                Type = RoomType.LevelExit,
                Name = "To Level 2",
                Doors = new List<DoorLink>(),
                Index = 6
            };
            levelExit.Doors.Add(new DoorLink { Position = levelExit.Center + Vector3.left * (roomWidth * 0.5f), Normal = Vector3.left, Width = corridorWidth, IsLevelExit = false });
            levelExit.Doors.Add(new DoorLink { Position = levelExit.Center + Vector3.right * (roomWidth * 0.5f), Normal = Vector3.right, Width = corridorWidth, IsLevelExit = true });
            _rooms.Add(levelExit);
        }

        private void BuildLevel2Rooms()
        {
            var cx = 0f;
            var cz = 0f;
            var stepX = roomWidth + corridorLength;

            var start = new RoomDef
            {
                Center = new Vector3(cx, 0f, cz),
                W = roomWidth,
                D = roomDepth,
                Type = RoomType.StartSafe,
                Name = "Level 2 Start (Safe)",
                Doors = new List<DoorLink>(),
                Index = 0
            };
            start.Doors.Add(new DoorLink { Position = start.Center + Vector3.right * (roomWidth * 0.5f), Normal = Vector3.right, Width = corridorWidth, IsLevelExit = false });
            _rooms.Add(start);

            cx += stepX;
            var combat1 = new RoomDef
            {
                Center = new Vector3(cx, 0f, cz),
                W = roomWidth,
                D = roomDepth,
                Type = RoomType.Combat,
                Name = "L2 Combat 1",
                Doors = new List<DoorLink>(),
                Index = 1
            };
            combat1.Doors.Add(new DoorLink { Position = combat1.Center + Vector3.left * (roomWidth * 0.5f), Normal = Vector3.left, Width = corridorWidth, IsLevelExit = false });
            combat1.Doors.Add(new DoorLink { Position = combat1.Center + Vector3.right * (roomWidth * 0.5f), Normal = Vector3.right, Width = corridorWidth, IsLevelExit = false });
            _rooms.Add(combat1);

            cx += stepX;
            var combat2 = new RoomDef
            {
                Center = new Vector3(cx, 0f, cz),
                W = roomWidth,
                D = roomDepth,
                Type = RoomType.Combat,
                Name = "L2 Combat 2",
                Doors = new List<DoorLink>(),
                Index = 2
            };
            combat2.Doors.Add(new DoorLink { Position = combat2.Center + Vector3.left * (roomWidth * 0.5f), Normal = Vector3.left, Width = corridorWidth, IsLevelExit = false });
            combat2.Doors.Add(new DoorLink { Position = combat2.Center + Vector3.right * (roomWidth * 0.5f), Normal = Vector3.right, Width = corridorWidth, IsLevelExit = false });
            _rooms.Add(combat2);

            cx += stepX;
            var safe = new RoomDef
            {
                Center = new Vector3(cx, 0f, cz),
                W = roomWidth,
                D = roomDepth,
                Type = RoomType.Safe,
                Name = "L2 Safe",
                Doors = new List<DoorLink>(),
                Index = 3
            };
            safe.Doors.Add(new DoorLink { Position = safe.Center + Vector3.left * (roomWidth * 0.5f), Normal = Vector3.left, Width = corridorWidth, IsLevelExit = false });
            safe.Doors.Add(new DoorLink { Position = safe.Center + Vector3.right * (roomWidth * 0.5f), Normal = Vector3.right, Width = corridorWidth, IsLevelExit = false });
            _rooms.Add(safe);

            cx += stepX;
            var boss = new RoomDef
            {
                Center = new Vector3(cx, 0f, cz),
                W = roomWidth,
                D = roomDepth,
                Type = RoomType.Combat,
                Name = "L2 Boss",
                Doors = new List<DoorLink>(),
                Index = 4
            };
            boss.Doors.Add(new DoorLink { Position = boss.Center + Vector3.left * (roomWidth * 0.5f), Normal = Vector3.left, Width = corridorWidth, IsLevelExit = false });
            boss.Doors.Add(new DoorLink { Position = boss.Center + Vector3.right * (roomWidth * 0.5f), Normal = Vector3.right, Width = corridorWidth, IsLevelExit = false });
            _rooms.Add(boss);

            cx += stepX;
            var toL3 = new RoomDef
            {
                Center = new Vector3(cx, 0f, cz),
                W = roomWidth,
                D = roomDepth,
                Type = RoomType.LevelExit,
                Name = "To Level 3",
                Doors = new List<DoorLink>(),
                Index = 5
            };
            toL3.Doors.Add(new DoorLink { Position = toL3.Center + Vector3.left * (roomWidth * 0.5f), Normal = Vector3.left, Width = corridorWidth, IsLevelExit = false });
            toL3.Doors.Add(new DoorLink { Position = toL3.Center + Vector3.right * (roomWidth * 0.5f), Normal = Vector3.right, Width = corridorWidth, IsLevelExit = true });
            _rooms.Add(toL3);
        }

        private void BuildLevel3Rooms()
        {
            var cx = 0f;
            var cz = 0f;
            var stepX = roomWidth + corridorLength;

            var start = new RoomDef
            {
                Center = new Vector3(cx, 0f, cz),
                W = roomWidth,
                D = roomDepth,
                Type = RoomType.StartSafe,
                Name = "Level 3 Start (Safe)",
                Doors = new List<DoorLink>(),
                Index = 0
            };
            start.Doors.Add(new DoorLink { Position = start.Center + Vector3.right * (roomWidth * 0.5f), Normal = Vector3.right, Width = corridorWidth, IsLevelExit = false });
            _rooms.Add(start);

            cx += stepX;
            var combat1 = new RoomDef
            {
                Center = new Vector3(cx, 0f, cz),
                W = roomWidth,
                D = roomDepth,
                Type = RoomType.Combat,
                Name = "L3 Combat 1",
                Doors = new List<DoorLink>(),
                Index = 1
            };
            combat1.Doors.Add(new DoorLink { Position = combat1.Center + Vector3.left * (roomWidth * 0.5f), Normal = Vector3.left, Width = corridorWidth, IsLevelExit = false });
            combat1.Doors.Add(new DoorLink { Position = combat1.Center + Vector3.right * (roomWidth * 0.5f), Normal = Vector3.right, Width = corridorWidth, IsLevelExit = false });
            _rooms.Add(combat1);

            cx += stepX;
            var finalBoss = new RoomDef
            {
                Center = new Vector3(cx, 0f, cz),
                W = roomWidth + 2f,
                D = roomDepth + 2f,
                Type = RoomType.FinalBoss,
                Name = "The Architect",
                Doors = new List<DoorLink>(),
                Index = 2
            };
            finalBoss.Doors.Add(new DoorLink { Position = finalBoss.Center + Vector3.left * (finalBoss.W * 0.5f), Normal = Vector3.left, Width = corridorWidth, IsLevelExit = false });
            _rooms.Add(finalBoss);
        }

        private void BuildRoom(Transform parent, RoomDef r)
        {
            var halfW = r.W * 0.5f;
            var halfD = r.D * 0.5f;
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor_" + r.Name;
            floor.transform.SetParent(parent);
            floor.transform.position = r.Center;
            floor.transform.localScale = new Vector3(r.W / 10f, 1f, r.D / 10f);
            Color floorCol;
            if (r.Type == RoomType.StartSafe || r.Type == RoomType.Safe || r.Type == RoomType.LevelExit)
                floorCol = new Color(0.5f, 0.55f, 0.45f);
            else if (r.Type == RoomType.Boss)
                floorCol = new Color(0.4f, 0.35f, 0.45f);
            else if (r.Type == RoomType.FinalBoss)
                floorCol = new Color(0.32f, 0.08f, 0.2f);
            else
                floorCol = new Color(0.45f, 0.4f, 0.4f);
            GrayboxTintUtil.Apply(floor.GetComponent<Renderer>(), floorCol);

            var lightGo = new GameObject("RoomLight_" + r.Name);
            lightGo.transform.SetParent(parent);
            lightGo.transform.position = r.Center + Vector3.up * (wallHeight * 0.8f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = Mathf.Max(r.W, r.D) * 0.8f;
            if (r.Type == RoomType.StartSafe || r.Type == RoomType.Safe || r.Type == RoomType.LevelExit)
            {
                light.intensity = safeRoomLightIntensity;
                light.color = safeRoomTint;
            }
            else
            {
                light.intensity = combatRoomLightIntensity;
                light.color = combatRoomTint;
            }

            var wallThickness = 0.5f;
            BuildWallsWithDoorGaps(parent, r, wallThickness);

            foreach (var door in r.Doors)
            {
                if (door.IsLevelExit)
                {
                    var exitTriggerGo = new GameObject("LevelExitTrigger");
                    exitTriggerGo.transform.SetParent(parent);
                    exitTriggerGo.transform.position = door.Position + door.Normal * 1f + Vector3.up * (wallHeight * 0.5f);
                    var box = exitTriggerGo.AddComponent<BoxCollider>();
                    box.isTrigger = true;
                    box.size = new Vector3(door.Width + 1f, wallHeight, 2f);
                    if (door.Normal.x != 0) box.size = new Vector3(2f, wallHeight, door.Width + 1f);
                    var levExit = exitTriggerGo.AddComponent<LevelExitTrigger>();
                    levExit.SetTargetLevel(_floorGenLevelIndex + 1);
                }
                else
                {
                    var doorBlocker = doorPrefabOrNull != null
                        ? Instantiate(doorPrefabOrNull)
                        : GameObject.CreatePrimitive(PrimitiveType.Cube);
                    doorBlocker.name = "DoorBlocker";
                    doorBlocker.transform.SetParent(parent);
                    var doorHeight = Mathf.Max(0.2f, wallHeight - doorTopInset);
                    doorBlocker.transform.position = door.Position + Vector3.up * (doorHeight * 0.5f);
                    doorBlocker.transform.localScale = new Vector3(door.Width, doorHeight, doorBlockerThickness);
                    if (door.Normal.x != 0) doorBlocker.transform.localScale = new Vector3(doorBlockerThickness, doorHeight, door.Width);
                    var doorRenderer = doorBlocker.GetComponent<Renderer>();
                    if (doorPrefabOrNull == null && doorRenderer != null)
                        GrayboxTintUtil.Apply(doorRenderer, new Color(0.3f, 0.25f, 0.2f));
                    if (doorBlocker.GetComponent<DoorBlocker>() == null)
                        doorBlocker.AddComponent<DoorBlocker>();
                    BuildCorridorSegment(parent, door);
                }
            }

            PlaceOcclusionAndLandmarks(parent, r);
            PlaceRaisedPads(parent, r);
            if (r.Type == RoomType.LevelExit)
                PlaceLevelExitObject(parent, r);

            // === ENVIRONMENTAL STORYTELLING ===
            PlaceStoryProps(parent, r);
            PlaceAtmosphericLighting(parent, r);
            PlaceFloorDecals(parent, r);
            PlaceParticleEffects(parent, r);

            // === STEP 4: DYNAMIC WORLDBUILDING � random event per combat room ===
            if (r.Type == RoomType.Combat || r.Type == RoomType.Boss)
            {
                var eventGo = new GameObject("RandomEvent_" + r.Name);
                eventGo.transform.SetParent(parent);
                eventGo.transform.position = r.Center;
                var rre = eventGo.AddComponent<RandomRoomEvents>();
                rre.Init(r.Center, r.W, r.D, wallHeight);
            }
            // Final boss arena: keep arena clear for readability
        }

        private void PlaceLevelExitObject(Transform parent, RoomDef r)
        {
            var exitObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            exitObj.name = "LevelExit_ToLevel2";
            exitObj.transform.SetParent(parent);
            exitObj.transform.position = r.Center + new Vector3(0f, 1.2f, 0f);
            exitObj.transform.localScale = new Vector3(2.5f, 2.5f, 2.5f);
            var rend = exitObj.GetComponent<Renderer>();
            if (rend != null) GrayboxTintUtil.Apply(rend, new Color(0.2f, 0.6f, 1f));
            var col = exitObj.GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
            var lev = exitObj.AddComponent<LevelExitTrigger>();
            lev.SetTargetLevel(_floorGenLevelIndex + 1);
        }

        private void BuildCorridorSegment(Transform parent, DoorLink door)
        {
            var mid = door.Position + door.Normal * (corridorLength * 0.5f);
            var corridor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            corridor.name = "Corridor";
            corridor.transform.SetParent(parent);
            corridor.transform.position = mid;
            if (Mathf.Abs(door.Normal.x) > 0.5f)
                corridor.transform.localScale = new Vector3(corridorLength / 10f, 1f, corridorWidth / 10f);
            else
                corridor.transform.localScale = new Vector3(corridorWidth / 10f, 1f, corridorLength / 10f);
            GrayboxTintUtil.Apply(corridor.GetComponent<Renderer>(), new Color(0.42f, 0.4f, 0.38f));
        }

        private void BuildWallsWithDoorGaps(Transform parent, RoomDef r, float wallThickness)
        {
            var halfW = r.W * 0.5f;
            var halfD = r.D * 0.5f;
            var c = r.Center;
            var doorsF = new List<float>();
            var doorsB = new List<float>();
            var doorsL = new List<float>();
            var doorsR = new List<float>();
            foreach (var d in r.Doors)
            {
                if (d.Normal.z > 0.5f) doorsF.Add(d.Position.x);
                if (d.Normal.z < -0.5f) doorsB.Add(d.Position.x);
                if (d.Normal.x < -0.5f) doorsL.Add(d.Position.z);
                if (d.Normal.x > 0.5f) doorsR.Add(d.Position.z);
            }
            BuildWallSegments(parent, c + Vector3.forward * halfD, Vector3.forward, new Vector2(c.x - halfW, c.x + halfW), doorsF, corridorWidth, wallHeight, wallThickness, r.W, true);
            BuildWallSegments(parent, c + Vector3.back * halfD, Vector3.back, new Vector2(c.x - halfW, c.x + halfW), doorsB, corridorWidth, wallHeight, wallThickness, r.W, true);
            BuildWallSegments(parent, c + Vector3.left * halfW, Vector3.left, new Vector2(c.z - halfD, c.z + halfD), doorsL, corridorWidth, wallHeight, wallThickness, r.D, false);
            BuildWallSegments(parent, c + Vector3.right * halfW, Vector3.right, new Vector2(c.z - halfD, c.z + halfD), doorsR, corridorWidth, wallHeight, wallThickness, r.D, false);
        }

        private void BuildWallSegments(Transform parent, Vector3 wallCenter, Vector3 normal, Vector2 range, List<float> doorPositions, float doorWidth, float height, float thickness, float extent, bool alongX)
        {
            doorPositions.Sort();
            var gapHalf = doorWidth * 0.5f;
            var start = range.x;
            foreach (var dp in doorPositions)
            {
                var gapStart = dp - gapHalf;
                var gapEnd = dp + gapHalf;
                if (gapStart > start + 0.2f)
                {
                    var segLen = gapStart - start;
                    var segCenter = start + segLen * 0.5f;
                    Vector3 pos = wallCenter + Vector3.up * (height * 0.5f);
                    if (alongX) pos.x = segCenter;
                    else pos.z = segCenter;
                    var scale = alongX ? new Vector3(segLen, height, thickness) : new Vector3(thickness, height, segLen);
                    BuildWall(parent, pos, scale, "WallSeg");
                }
                start = gapEnd;
            }
            if (range.y > start + 0.2f)
            {
                var segLen = range.y - start;
                var segCenter = start + segLen * 0.5f;
                Vector3 pos = wallCenter + Vector3.up * (height * 0.5f);
                if (alongX) pos.x = segCenter;
                else pos.z = segCenter;
                var scale = alongX ? new Vector3(segLen, height, thickness) : new Vector3(thickness, height, segLen);
                BuildWall(parent, pos, scale, "WallSeg");
            }
        }

        private void BuildWall(Transform parent, Vector3 pos, Vector3 scale, string name)
        {
            var w = GameObject.CreatePrimitive(PrimitiveType.Cube);
            w.name = name;
            w.transform.SetParent(parent);
            w.transform.position = pos;
            w.transform.localScale = scale;
            GrayboxTintUtil.Apply(w.GetComponent<Renderer>(), new Color(0.35f, 0.35f, 0.38f));
        }

        private void PlaceOcclusionAndLandmarks(Transform parent, RoomDef r)
        {
            var halfW = r.W * 0.5f;
            var halfD = r.D * 0.5f;
            if (r.Type == RoomType.StartSafe || r.Type == RoomType.Safe)
            {
                var landmark = GameObject.CreatePrimitive(PrimitiveType.Cube);
                landmark.name = "Landmark_Safe";
                landmark.transform.SetParent(parent);
                landmark.transform.position = r.Center + new Vector3(-halfW * 0.4f, landmarkPillarHeight * 0.5f, 0f);
                landmark.transform.localScale = new Vector3(pillarSize, landmarkPillarHeight, pillarSize);
                GrayboxTintUtil.Apply(landmark.GetComponent<Renderer>(), new Color(0.6f, 0.7f, 0.5f));
            }
            else if (r.Type == RoomType.Combat || r.Type == RoomType.Boss || r.Type == RoomType.FinalBoss)
            {
                var pillar1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pillar1.name = "Occlusion_Pillar";
                pillar1.transform.SetParent(parent);
                pillar1.transform.position = r.Center + new Vector3(halfW * 0.3f, wallHeight * 0.5f, halfD * 0.2f);
                pillar1.transform.localScale = new Vector3(pillarSize, wallHeight, pillarSize);
                GrayboxTintUtil.Apply(pillar1.GetComponent<Renderer>(), new Color(0.4f, 0.38f, 0.42f));

                var pillar2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pillar2.name = "Occlusion_Pillar";
                pillar2.transform.SetParent(parent);
                pillar2.transform.position = r.Center + new Vector3(-halfW * 0.35f, wallHeight * 0.5f, -halfD * 0.3f);
                pillar2.transform.localScale = new Vector3(pillarSize, wallHeight, pillarSize);
                GrayboxTintUtil.Apply(pillar2.GetComponent<Renderer>(), new Color(0.4f, 0.38f, 0.42f));

                var landmark = GameObject.CreatePrimitive(PrimitiveType.Cube);
                landmark.name = "Landmark_Combat";
                landmark.transform.SetParent(parent);
                landmark.transform.position = r.Center + new Vector3(0f, landmarkPillarHeight * 0.5f, halfD * 0.5f);
                landmark.transform.localScale = new Vector3(pillarSize * 0.8f, landmarkPillarHeight, pillarSize * 0.8f);
                GrayboxTintUtil.Apply(landmark.GetComponent<Renderer>(),
                    r.Type == RoomType.Boss || r.Type == RoomType.FinalBoss ? new Color(0.6f, 0.2f, 0.25f) : new Color(0.55f, 0.45f, 0.35f));
            }
        }

        private void PlaceRaisedPads(Transform parent, RoomDef r)
        {
            if (r.Index != 1 && r.Index != 4) return;
            var halfW = r.W * 0.5f;
            var halfD = r.D * 0.5f;
            var pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pad.name = "RaisedPad";
            pad.transform.SetParent(parent);
            pad.transform.position = r.Center + new Vector3(halfW * 0.25f, raisedPadHeight * 0.5f, -halfD * 0.3f);
            pad.transform.localScale = new Vector3(raisedPadSize, raisedPadHeight, raisedPadSize);
            GrayboxTintUtil.Apply(pad.GetComponent<Renderer>(), new Color(0.5f, 0.48f, 0.52f));
        }

        private void BuildRoomController(Transform parent, RoomDef r, Transform geometryParent)
        {
            var go = new GameObject("Room_" + r.Name);
            go.transform.SetParent(parent);
            go.transform.position = r.Center;
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(r.W - 1f, wallHeight, r.D - 1f);
            box.center = Vector3.zero;

            var rc = go.AddComponent<RoomController>();
            rc.roomType = r.Type;
            rc.roomName = r.Name;
            if (r.Type == RoomType.LevelExit)
            {
                rc.chaserCount = 0;
                rc.shooterCount = 0;
                rc.flankerCount = 0;
            }
            else if (r.Type == RoomType.FinalBoss)
            {
                rc.chaserCount = 0;
                rc.shooterCount = 0;
                rc.flankerCount = 0;
            }
            else
            {
                rc.chaserCount = r.Type == RoomType.Boss ? 3 : 2;
                rc.shooterCount = r.Index >= 2 ? 1 : 0;
                rc.flankerCount = r.Type == RoomType.Combat || r.Type == RoomType.Boss ? 1 : 0;
            }

            for (var i = 0; i < geometryParent.childCount; i++)
            {
                var t = geometryParent.GetChild(i);
                if (t.name == "DoorBlocker") rc.RegisterDoor(t);
            }

            if (r.Type == RoomType.FinalBoss)
            {
                var spBoss = new GameObject("SpawnBoss");
                spBoss.transform.SetParent(go.transform);
                spBoss.transform.localPosition = Vector3.zero;
                rc.RegisterSpawnPoint(spBoss.transform, false);
            }
            else if (r.Type == RoomType.Combat || r.Type == RoomType.Boss)
            {
                var halfW = r.W * 0.5f - 1.5f;
                var halfD = r.D * 0.5f - 1.5f;
                var sp1 = new GameObject("Spawn1");
                sp1.transform.SetParent(go.transform);
                sp1.transform.localPosition = new Vector3(halfW * 0.5f, 0f, halfD * 0.5f);
                rc.RegisterSpawnPoint(sp1.transform, false);
                var sp2 = new GameObject("Spawn2");
                sp2.transform.SetParent(go.transform);
                sp2.transform.localPosition = new Vector3(-halfW * 0.6f, 0f, -halfD * 0.5f);
                rc.RegisterSpawnPoint(sp2.transform, false);
                var spFlank = new GameObject("SpawnFlank");
                spFlank.transform.SetParent(go.transform);
                spFlank.transform.localPosition = new Vector3(-halfW * 0.4f, 0f, halfD * 0.3f);
                rc.RegisterSpawnPoint(spFlank.transform, true);
            }

            if (r.Name == "Shop (Safe)")
            {
                go.AddComponent<ShopTrigger>();
            }

            // Narrative text triggers for key rooms
            string[] narrativeLines = null;
            if (r.Name == "Start (Safe)")
                narrativeLines = new[] { "The air hums. The Hollow remembers you." };
            else if (r.Name == "Level 2 Start (Safe)" || r.Name == "L2 Start")
                narrativeLines = new[] { "Deeper. The walls shift when you're not looking." };
            else if (r.Name == "L2 Boss")
                narrativeLines = new[] { "This one was never meant to be contained." };
            else if (r.Name == "Level 3 Start (Safe)")
                narrativeLines = new[] { "The architecture ends here. Whatever built this is waiting." };
            else if (r.Name == "The Architect")
                narrativeLines = new[] { "Face it. There is nothing behind you anymore." };

            if (narrativeLines != null)
            {
                var nt = go.AddComponent<NarrativeTriggerEvent>();
                nt.SetLines(narrativeLines);
            }

            _roomControllers.Add(rc);
        }

        // =========================================================
        // ENVIRONMENTAL STORYTELLING
        // =========================================================

        /// <summary>
        /// Part 1 � Prop-based storytelling.
        /// Combat rooms: tipped table + scattered crates = signs of struggle.
        /// Safe rooms: neat bench + supply crate = place of rest/refuge.
        /// </summary>
        private void PlaceStoryProps(Transform parent, RoomDef r)
        {
            var halfW = r.W * 0.5f;
            var halfD = r.D * 0.5f;

            if (r.Type == RoomType.Combat || r.Type == RoomType.Boss || r.Type == RoomType.FinalBoss)
            {
                // Tipped table (rotated flat cube) � suggests someone was in a hurry / struggle
                var table = GameObject.CreatePrimitive(PrimitiveType.Cube);
                table.name = "Prop_TippedTable";
                table.transform.SetParent(parent);
                table.transform.position = r.Center + new Vector3(-halfW * 0.5f, 0.25f, halfD * 0.4f);
                table.transform.localScale = new Vector3(1.8f, 0.12f, 0.9f);
                table.transform.rotation = Quaternion.Euler(0f, 15f, 52f); // tipped over
                GrayboxTintUtil.Apply(table.GetComponent<Renderer>(), new Color(0.45f, 0.3f, 0.2f));

                // Scattered crates � debris from the struggle
                var offsets = new Vector3[]
                {
                    new Vector3(-halfW * 0.45f, 0.2f,  halfD * 0.55f),
                    new Vector3(-halfW * 0.6f,  0.15f, halfD * 0.35f),
                    new Vector3(-halfW * 0.3f,  0.18f, halfD * 0.65f),
                };
                float[] rotations = { 25f, -40f, 10f };
                for (var i = 0; i < offsets.Length; i++)
                {
                    var crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    crate.name = "Prop_Crate";
                    crate.transform.SetParent(parent);
                    crate.transform.position = r.Center + offsets[i];
                    crate.transform.localScale = new Vector3(0.55f, 0.55f, 0.55f);
                    crate.transform.rotation = Quaternion.Euler(0f, rotations[i], 0f);
                    GrayboxTintUtil.Apply(crate.GetComponent<Renderer>(), new Color(0.4f, 0.28f, 0.18f));
                }

                // Skeletal remains (thin flat slab on the floor)
                var remains = GameObject.CreatePrimitive(PrimitiveType.Cube);
                remains.name = "Prop_Remains";
                remains.transform.SetParent(parent);
                remains.transform.position = r.Center + new Vector3(halfW * 0.4f, 0.03f, -halfD * 0.4f);
                remains.transform.localScale = new Vector3(0.4f, 0.06f, 1.5f);
                remains.transform.rotation = Quaternion.Euler(0f, 30f, 0f);
                GrayboxTintUtil.Apply(remains.GetComponent<Renderer>(), new Color(0.85f, 0.82f, 0.75f));
            }
            else if (r.Type == RoomType.Safe || r.Type == RoomType.StartSafe)
            {
                // Bench � a place of rest
                var bench = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bench.name = "Prop_Bench";
                bench.transform.SetParent(parent);
                bench.transform.position = r.Center + new Vector3(-halfW * 0.6f, 0.3f, halfD * 0.5f);
                bench.transform.localScale = new Vector3(2f, 0.2f, 0.5f);
                GrayboxTintUtil.Apply(bench.GetComponent<Renderer>(), new Color(0.5f, 0.38f, 0.25f));

                // Supply crate � someone prepared for survival here
                var supply = GameObject.CreatePrimitive(PrimitiveType.Cube);
                supply.name = "Prop_SupplyCrate";
                supply.transform.SetParent(parent);
                supply.transform.position = r.Center + new Vector3(-halfW * 0.6f, 0.35f, halfD * 0.35f);
                supply.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
                GrayboxTintUtil.Apply(supply.GetComponent<Renderer>(), new Color(0.55f, 0.5f, 0.3f));
            }
        }

        /// <summary>
        /// Part 2 � Light and Atmosphere.
        /// Combat/Boss: dim red flickering light = danger, horror.
        /// Safe/Start:  warm golden light = relief, safety.
        /// </summary>
        private void PlaceAtmosphericLighting(Transform parent, RoomDef r)
        {
            if (r.Type == RoomType.Combat || r.Type == RoomType.Boss || r.Type == RoomType.FinalBoss)
            {
                // Main danger light � red/orange, flickers
                var dangerLightGo = new GameObject("AtmosLight_Danger");
                dangerLightGo.transform.SetParent(parent);
                dangerLightGo.transform.position = r.Center + new Vector3(halfWHelper(r) * 0.3f, wallHeight * 0.7f, 0f);
                var dangerLight = dangerLightGo.AddComponent<Light>();
                dangerLight.type = LightType.Point;
                dangerLight.color = r.Type == RoomType.Boss || r.Type == RoomType.FinalBoss ? new Color(0.8f, 0.1f, 0.1f) : new Color(1f, 0.35f, 0.1f);
                dangerLight.intensity = 1.6f;
                dangerLight.range = Mathf.Max(r.W, r.D) * 0.6f;
                dangerLightGo.AddComponent<FlickerLight>();

                // Secondary deep shadow light offset to one corner
                var shadowLightGo = new GameObject("AtmosLight_Shadow");
                shadowLightGo.transform.SetParent(parent);
                shadowLightGo.transform.position = r.Center + new Vector3(-halfWHelper(r) * 0.4f, wallHeight * 0.5f, halfDHelper(r) * 0.4f);
                var shadowLight = shadowLightGo.AddComponent<Light>();
                shadowLight.type = LightType.Point;
                shadowLight.color = new Color(0.5f, 0.05f, 0.05f);
                shadowLight.intensity = 0.5f;
                shadowLight.range = Mathf.Max(r.W, r.D) * 0.4f;
                shadowLightGo.AddComponent<FlickerLight>();
            }
            else if (r.Type == RoomType.Safe || r.Type == RoomType.StartSafe)
            {
                // Warm golden safe light � calm, welcoming
                var safeLightGo = new GameObject("AtmosLight_Safe");
                safeLightGo.transform.SetParent(parent);
                safeLightGo.transform.position = r.Center + new Vector3(0f, wallHeight * 0.75f, 0f);
                var safeLight = safeLightGo.AddComponent<Light>();
                safeLight.type = LightType.Point;
                safeLight.color = new Color(1f, 0.85f, 0.5f);
                safeLight.intensity = 2.2f;
                safeLight.range = Mathf.Max(r.W, r.D) * 0.9f;
                // No flicker � safe rooms are stable
            }
        }

        private float halfWHelper(RoomDef r) => r.W * 0.5f;
        private float halfDHelper(RoomDef r) => r.D * 0.5f;

        /// <summary>
        /// Part 3 � Decals and Details.
        /// Flat quads on the floor simulating bloodstains near where enemies spawn.
        /// Also adds scorch marks near the level exit portal.
        /// </summary>
        private void PlaceFloorDecals(Transform parent, RoomDef r)
        {
            if (r.Type == RoomType.Combat || r.Type == RoomType.Boss || r.Type == RoomType.FinalBoss)
            {
                // Bloodstain decals near each spawn point quadrant
                var stainPositions = new Vector3[]
                {
                    r.Center + new Vector3( r.W * 0.25f,  0.01f,  r.D * 0.2f),
                    r.Center + new Vector3(-r.W * 0.3f,   0.01f, -r.D * 0.15f),
                    r.Center + new Vector3( r.W * 0.1f,   0.01f, -r.D * 0.3f),
                };
                float[] stainSizes = { 1.2f, 0.9f, 1.5f };
                for (var i = 0; i < stainPositions.Length; i++)
                {
                    var stain = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    stain.name = "Decal_Bloodstain";
                    stain.transform.SetParent(parent);
                    stain.transform.position = stainPositions[i];
                    stain.transform.rotation = Quaternion.Euler(90f, Random.Range(0f, 360f), 0f);
                    stain.transform.localScale = Vector3.one * stainSizes[i];
                    GrayboxTintUtil.Apply(stain.GetComponent<Renderer>(), new Color(0.45f, 0.05f, 0.05f, 0.9f));
                    stain.GetComponent<Collider>().enabled = false;
                }

                // Scuff/footprint trail decals leading toward room center
                for (var i = 0; i < 3; i++)
                {
                    var footprint = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    footprint.name = "Decal_Scuff";
                    footprint.transform.SetParent(parent);
                    footprint.transform.position = r.Center + new Vector3(-r.W * 0.1f * i, 0.01f, r.D * 0.05f * i);
                    footprint.transform.rotation = Quaternion.Euler(90f, 45f, 0f);
                    footprint.transform.localScale = new Vector3(0.4f, 0.25f, 1f);
                    GrayboxTintUtil.Apply(footprint.GetComponent<Renderer>(), new Color(0.25f, 0.2f, 0.18f));
                    footprint.GetComponent<Collider>().enabled = false;
                }
            }
            else if (r.Type == RoomType.LevelExit)
            {
                // Scorch ring around the exit portal
                var scorch = GameObject.CreatePrimitive(PrimitiveType.Quad);
                scorch.name = "Decal_ScorchMark";
                scorch.transform.SetParent(parent);
                scorch.transform.position = r.Center + new Vector3(0f, 0.01f, 0f);
                scorch.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                scorch.transform.localScale = Vector3.one * 3.5f;
                GrayboxTintUtil.Apply(scorch.GetComponent<Renderer>(), new Color(0.15f, 0.12f, 0.1f));
                scorch.GetComponent<Collider>().enabled = false;
            }
        }

        /// <summary>
        /// Particle Effects � floating dust in combat rooms (long abandonment),
        /// embers/sparks near the level exit portal (magical energy).
        /// </summary>
        private void PlaceParticleEffects(Transform parent, RoomDef r)
        {
            if (r.Type == RoomType.Combat || r.Type == RoomType.Boss || r.Type == RoomType.FinalBoss)
            {
                // Dust/smoke particles drifting slowly � room has been abandoned a long time
                var dustGo = new GameObject("Particles_Dust");
                dustGo.transform.SetParent(parent);
                dustGo.transform.position = r.Center + new Vector3(0f, 1.2f, 0f);

                var dust = dustGo.AddComponent<ParticleSystem>();

                // Main module
                var main = dust.main;
                main.loop = true;
                main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 6f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.2f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.12f);
                main.startColor = new ParticleSystem.MinMaxGradient(
                    new Color(0.6f, 0.55f, 0.5f, 0.25f),
                    new Color(0.4f, 0.38f, 0.35f, 0.15f)
                );
                main.maxParticles = 60;
                main.simulationSpace = ParticleSystemSimulationSpace.World;

                // Emission � slow constant drizzle
                var emission = dust.emission;
                emission.rateOverTime = 8f;

                // Shape � spread across floor area so dust fills the room
                var shape = dust.shape;
                shape.enabled = true;
                shape.shapeType = ParticleSystemShapeType.Box;
                shape.scale = new Vector3(r.W * 0.7f, 0.1f, r.D * 0.7f);

                // Velocity over lifetime � slow upward drift
                var vel = dust.velocityOverLifetime;
                vel.enabled = true;
                vel.space = ParticleSystemSimulationSpace.World;
                vel.y = new ParticleSystem.MinMaxCurve(0.03f, 0.12f);
                vel.x = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);
                vel.z = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);

                // Fade out smoothly at end of life
                var col = dust.colorOverLifetime;
                col.enabled = true;
                var grad = new Gradient();
                grad.SetKeys(
                    new GradientColorKey[] {
                        new GradientColorKey(new Color(0.55f, 0.5f, 0.45f), 0f),
                        new GradientColorKey(new Color(0.55f, 0.5f, 0.45f), 0.7f),
                        new GradientColorKey(new Color(0.3f, 0.28f, 0.25f), 1f)
                    },
                    new GradientAlphaKey[] {
                        new GradientAlphaKey(0f, 0f),
                        new GradientAlphaKey(0.3f, 0.2f),
                        new GradientAlphaKey(0f, 1f)
                    }
                );
                col.color = new ParticleSystem.MinMaxGradient(grad);

                // Size shrinks as it rises
                var size = dust.sizeOverLifetime;
                size.enabled = true;
                var sizeCurve = new AnimationCurve();
                sizeCurve.AddKey(0f, 0.3f);
                sizeCurve.AddKey(0.5f, 1f);
                sizeCurve.AddKey(1f, 0.1f);
                size.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
            }

            if (r.Type == RoomType.LevelExit)
            {
                // Ember/spark particles swirling around the portal � magical energy
                var emberGo = new GameObject("Particles_Embers");
                emberGo.transform.SetParent(parent);
                emberGo.transform.position = r.Center + new Vector3(0f, 0.5f, 0f);

                var embers = emberGo.AddComponent<ParticleSystem>();

                var main = embers.main;
                main.loop = true;
                main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 3f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 2f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
                main.startColor = new ParticleSystem.MinMaxGradient(
                    new Color(1f, 0.6f, 0.1f, 0.9f),
                    new Color(0.2f, 0.5f, 1f, 0.9f)
                );
                main.maxParticles = 80;
                main.simulationSpace = ParticleSystemSimulationSpace.World;

                // Emission � frequent bursts
                var emission = embers.emission;
                emission.rateOverTime = 20f;

                // Shape � cone shooting upward from portal base
                var shape = embers.shape;
                shape.enabled = true;
                shape.shapeType = ParticleSystemShapeType.Cone;
                shape.angle = 35f;
                shape.radius = 1.2f;

                // Velocity � swirl upward
                var vel = embers.velocityOverLifetime;
                vel.enabled = true;
                vel.space = ParticleSystemSimulationSpace.World;
                vel.x = new ParticleSystem.MinMaxCurve(0f, 0f);
                vel.y = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
                vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
                vel.orbitalX = new ParticleSystem.MinMaxCurve(0f, 0f);
                vel.orbitalY = new ParticleSystem.MinMaxCurve(2f, 4f);
                vel.orbitalZ = new ParticleSystem.MinMaxCurve(0f, 0f);

                // Color fades from bright orange/blue to transparent
                var col = embers.colorOverLifetime;
                col.enabled = true;
                var grad = new Gradient();
                grad.SetKeys(
                    new GradientColorKey[] {
                        new GradientColorKey(new Color(1f, 0.8f, 0.3f), 0f),
                        new GradientColorKey(new Color(0.4f, 0.6f, 1f), 0.5f),
                        new GradientColorKey(new Color(0.2f, 0.2f, 0.8f), 1f)
                    },
                    new GradientAlphaKey[] {
                        new GradientAlphaKey(1f, 0f),
                        new GradientAlphaKey(0.7f, 0.5f),
                        new GradientAlphaKey(0f, 1f)
                    }
                );
                col.color = new ParticleSystem.MinMaxGradient(grad);

                // Particles shrink as they die
                var size = embers.sizeOverLifetime;
                size.enabled = true;
                var sizeCurve = new AnimationCurve();
                sizeCurve.AddKey(0f, 1f);
                sizeCurve.AddKey(1f, 0f);
                size.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
            }
        }

        private void OnDrawGizmos()
        {
            if (!drawGizmos || _rooms == null || _rooms.Count == 0) return;
            foreach (var r in _rooms)
            {
                Gizmos.color = r.Type == RoomType.StartSafe || r.Type == RoomType.Safe ? Color.green : Color.red;
                Gizmos.DrawWireCube(r.Center + Vector3.up * (wallHeight * 0.5f), new Vector3(r.W, wallHeight, r.D));
            }
        }
    }
}



