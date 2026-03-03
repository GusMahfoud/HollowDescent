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

        private bool _hasGenerated;
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
        }

        public bool HasGenerated() => _hasGenerated;

        public void Generate()
        {
            if (_hasGenerated) return;
            _rooms.Clear();
            _roomControllers.Clear();

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
            start.Doors.Add(new DoorLink { Position = start.Center + Vector3.right * (roomWidth * 0.5f), Normal = Vector3.right, Width = corridorWidth });
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
            combat1.Doors.Add(new DoorLink { Position = combat1.Center + Vector3.left * (roomWidth * 0.5f), Normal = Vector3.left, Width = corridorWidth });
            combat1.Doors.Add(new DoorLink { Position = combat1.Center + Vector3.forward * (roomDepth * 0.5f), Normal = Vector3.forward, Width = corridorWidth });
            combat1.Doors.Add(new DoorLink { Position = combat1.Center + Vector3.back * (roomDepth * 0.5f), Normal = Vector3.back, Width = corridorWidth });
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
            branchA.Doors.Add(new DoorLink { Position = branchA.Center + Vector3.back * (roomDepth * 0.5f), Normal = Vector3.back, Width = corridorWidth });
            branchA.Doors.Add(new DoorLink { Position = branchA.Center + Vector3.right * (roomWidth * 0.5f), Normal = Vector3.right, Width = corridorWidth });
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
            branchB.Doors.Add(new DoorLink { Position = branchB.Center + Vector3.forward * (roomDepth * 0.5f), Normal = Vector3.forward, Width = corridorWidth });
            branchB.Doors.Add(new DoorLink { Position = branchB.Center + Vector3.right * (roomWidth * 0.5f), Normal = Vector3.right, Width = corridorWidth });
            _rooms.Add(branchB);

            cx += stepX;
            var reconverge = new RoomDef
            {
                Center = new Vector3(cx, 0f, cz),
                W = roomWidth + 2f,
                D = roomDepth * 2f + corridorLength,
                Type = RoomType.Combat,
                Name = "Reconverge",
                Doors = new List<DoorLink>(),
                Index = 4
            };
            reconverge.Doors.Add(new DoorLink { Position = reconverge.Center + Vector3.left * (reconverge.W * 0.5f), Normal = Vector3.left, Width = corridorWidth });
            reconverge.Doors.Add(new DoorLink { Position = reconverge.Center + Vector3.right * (reconverge.W * 0.5f), Normal = Vector3.right, Width = corridorWidth });
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
            shop.Doors.Add(new DoorLink { Position = shop.Center + Vector3.left * (roomWidth * 0.5f), Normal = Vector3.left, Width = corridorWidth });
            shop.Doors.Add(new DoorLink { Position = shop.Center + Vector3.right * (roomWidth * 0.5f), Normal = Vector3.right, Width = corridorWidth });
            _rooms.Add(shop);

            cx += stepX;
            var boss = new RoomDef
            {
                Center = new Vector3(cx, 0f, cz),
                W = roomWidth,
                D = roomDepth,
                Type = RoomType.Boss,
                Name = "Boss / Exit",
                Doors = new List<DoorLink>(),
                Index = 6
            };
            boss.Doors.Add(new DoorLink { Position = boss.Center + Vector3.left * (roomWidth * 0.5f), Normal = Vector3.left, Width = corridorWidth });
            _rooms.Add(boss);

            var levelRoot = new GameObject("LevelRoot");
            levelRoot.transform.SetParent(transform);

            foreach (var r in _rooms)
            {
                var roomGeo = new GameObject("RoomGeometry_" + r.Name);
                roomGeo.transform.SetParent(levelRoot.transform);
                BuildRoom(roomGeo.transform, r);
                BuildRoomController(levelRoot.transform, r, roomGeo.transform);
            }

            _hasGenerated = true;
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
            var floorMat = floor.GetComponent<Renderer>().material;
            if (r.Type == RoomType.StartSafe || r.Type == RoomType.Safe)
                floorMat.color = new Color(0.5f, 0.55f, 0.45f);
            else if (r.Type == RoomType.Boss)
                floorMat.color = new Color(0.4f, 0.35f, 0.45f);
            else
                floorMat.color = new Color(0.45f, 0.4f, 0.4f);

            var lightGo = new GameObject("RoomLight_" + r.Name);
            lightGo.transform.SetParent(parent);
            lightGo.transform.position = r.Center + Vector3.up * (wallHeight * 0.8f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = Mathf.Max(r.W, r.D) * 0.8f;
            if (r.Type == RoomType.StartSafe || r.Type == RoomType.Safe)
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
                var doorBlocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                doorBlocker.name = "DoorBlocker";
                doorBlocker.transform.SetParent(parent);
                doorBlocker.transform.position = door.Position + Vector3.up * (wallHeight * 0.5f);
                doorBlocker.transform.localScale = new Vector3(door.Width, wallHeight, 1f);
                if (door.Normal.x != 0) doorBlocker.transform.localScale = new Vector3(1f, wallHeight, door.Width);
                doorBlocker.GetComponent<Renderer>().material.color = new Color(0.3f, 0.25f, 0.2f);
                doorBlocker.tag = "DoorBlocker";
                BuildCorridorSegment(parent, door);
            }

            PlaceOcclusionAndLandmarks(parent, r);
            PlaceRaisedPads(parent, r);
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
            corridor.GetComponent<Renderer>().material.color = new Color(0.42f, 0.4f, 0.38f);
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
            w.GetComponent<Renderer>().material.color = new Color(0.35f, 0.35f, 0.38f);
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
                landmark.GetComponent<Renderer>().material.color = new Color(0.6f, 0.7f, 0.5f);
            }
            else if (r.Type == RoomType.Combat || r.Type == RoomType.Boss)
            {
                var pillar1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pillar1.name = "Occlusion_Pillar";
                pillar1.transform.SetParent(parent);
                pillar1.transform.position = r.Center + new Vector3(halfW * 0.3f, wallHeight * 0.5f, halfD * 0.2f);
                pillar1.transform.localScale = new Vector3(pillarSize, wallHeight, pillarSize);
                pillar1.GetComponent<Renderer>().material.color = new Color(0.4f, 0.38f, 0.42f);

                var pillar2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pillar2.name = "Occlusion_Pillar";
                pillar2.transform.SetParent(parent);
                pillar2.transform.position = r.Center + new Vector3(-halfW * 0.35f, wallHeight * 0.5f, -halfD * 0.3f);
                pillar2.transform.localScale = new Vector3(pillarSize, wallHeight, pillarSize);
                pillar2.GetComponent<Renderer>().material.color = new Color(0.4f, 0.38f, 0.42f);

                var landmark = GameObject.CreatePrimitive(PrimitiveType.Cube);
                landmark.name = "Landmark_Combat";
                landmark.transform.SetParent(parent);
                landmark.transform.position = r.Center + new Vector3(0f, landmarkPillarHeight * 0.5f, halfD * 0.5f);
                landmark.transform.localScale = new Vector3(pillarSize * 0.8f, landmarkPillarHeight, pillarSize * 0.8f);
                landmark.GetComponent<Renderer>().material.color = r.Type == RoomType.Boss ? new Color(0.6f, 0.2f, 0.25f) : new Color(0.55f, 0.45f, 0.35f);
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
            pad.GetComponent<Renderer>().material.color = new Color(0.5f, 0.48f, 0.52f);
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
            rc.chaserCount = r.Type == RoomType.Boss ? 3 : 2;
            rc.shooterCount = r.Index >= 2 ? 1 : 0;
            rc.flankerCount = r.Type == RoomType.Combat || r.Type == RoomType.Boss ? 1 : 0;

            for (var i = 0; i < geometryParent.childCount; i++)
            {
                var t = geometryParent.GetChild(i);
                if (t.name == "DoorBlocker") rc.RegisterDoor(t);
            }

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

            _roomControllers.Add(rc);
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
