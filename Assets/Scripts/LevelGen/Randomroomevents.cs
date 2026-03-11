using UnityEngine;

namespace HollowDescent.LevelGen
{
    /// <summary>
    /// Step 4 – Procedural Worldbuilding.
    /// Rolls a random event each time the room is generated, making every run feel different.
    /// Events: CollapseRubble, PoisonHazard, WebTrap, BloodPool, StormLight, AcidPuddle, SpikeField, CursedAltar.
    /// Attach to a room's parent transform; call Init() from FloorGenerator.
    /// </summary>
    public class RandomRoomEvents : MonoBehaviour
    {
        public enum RoomEvent
        {
            CollapseRubble,   // ceiling chunks on the floor – debris from a cave-in
            PoisonHazard,     // green particle cloud – toxic atmosphere
            WebTrap,          // white sticky web meshes covering corners
            BloodPool,        // large dark blood splatter cluster
            StormLight,       // rapid random lightning-flash point light
            AcidPuddle,       // yellow-green glowing floor patches
            SpikeField,       // thin spike pillars erupting from floor
            CursedAltar       // glowing purple ritual altar in room center
        }

        private RoomEvent _chosenEvent;
        private float _roomW;
        private float _roomD;
        private Vector3 _roomCenter;
        private float _wallHeight;

        // Called by FloorGenerator right after the room is built
        public void Init(Vector3 center, float w, float d, float wallH)
        {
            _roomCenter = center;
            _roomW = w;
            _roomD = d;
            _wallHeight = wallH;

            // Pick a random event
            var values = System.Enum.GetValues(typeof(RoomEvent));
            _chosenEvent = (RoomEvent)values.GetValue(Random.Range(0, values.Length));

            SpawnEvent();
        }

        private void SpawnEvent()
        {
            switch (_chosenEvent)
            {
                case RoomEvent.CollapseRubble: SpawnCollapseRubble(); break;
                case RoomEvent.PoisonHazard: SpawnPoisonHazard(); break;
                case RoomEvent.WebTrap: SpawnWebTraps(); break;
                case RoomEvent.BloodPool: SpawnBloodPool(); break;
                case RoomEvent.StormLight: SpawnStormLight(); break;
                case RoomEvent.AcidPuddle: SpawnAcidPuddles(); break;
                case RoomEvent.SpikeField: SpawnSpikeField(); break;
                case RoomEvent.CursedAltar: SpawnCursedAltar(); break;
            }
        }

        // ── 1. COLLAPSE RUBBLE ──────────────────────────────────────────
        // Large and small chunks scattered as if ceiling caved in
        private void SpawnCollapseRubble()
        {
            var count = Random.Range(5, 9);
            for (var i = 0; i < count; i++)
            {
                var chunk = GameObject.CreatePrimitive(PrimitiveType.Cube);
                chunk.name = "Event_Rubble";
                chunk.transform.SetParent(transform);
                chunk.transform.position = RandomFloorPos(1.5f) + Vector3.up * Random.Range(0.1f, 0.4f);
                var s = Random.Range(0.3f, 1.1f);
                chunk.transform.localScale = new Vector3(s, s * Random.Range(0.4f, 0.9f), s);
                chunk.transform.rotation = Quaternion.Euler(
                    Random.Range(-30f, 30f), Random.Range(0f, 360f), Random.Range(-20f, 20f));
                chunk.GetComponent<Renderer>().material.color =
                    new Color(Random.Range(0.35f, 0.5f), Random.Range(0.32f, 0.45f), Random.Range(0.3f, 0.42f));
            }
        }

        // ── 2. POISON HAZARD ────────────────────────────────────────────
        // Green toxic particle cloud hovering at floor level
        private void SpawnPoisonHazard()
        {
            var go = new GameObject("Event_PoisonCloud");
            go.transform.SetParent(transform);
            go.transform.position = _roomCenter + new Vector3(
                Random.Range(-_roomW * 0.2f, _roomW * 0.2f), 0.3f,
                Random.Range(-_roomD * 0.2f, _roomD * 0.2f));

            var ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.1f, 0.7f, 0.1f, 0.35f),
                new Color(0.3f, 0.9f, 0.2f, 0.2f));
            main.maxParticles = 80;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var em = ps.emission;
            em.rateOverTime = 18f;

            var sh = ps.shape;
            sh.enabled = true;
            sh.shapeType = ParticleSystemShapeType.Box;
            sh.scale = new Vector3(_roomW * 0.45f, 0.05f, _roomD * 0.45f);

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.y = new ParticleSystem.MinMaxCurve(0.02f, 0.1f);

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(0.2f, 0.9f, 0.2f), 0f),
                    new GradientColorKey(new Color(0.1f, 0.5f, 0.1f), 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0f,    0f),
                    new GradientAlphaKey(0.35f, 0.3f),
                    new GradientAlphaKey(0f,    1f)
                });
            col.color = new ParticleSystem.MinMaxGradient(g);

            // Green glow light to complement the cloud
            var lightGo = new GameObject("PoisonLight");
            lightGo.transform.SetParent(go.transform);
            lightGo.transform.localPosition = Vector3.up * 0.5f;
            var l = lightGo.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(0.1f, 0.9f, 0.2f);
            l.intensity = 1.2f;
            l.range = _roomW * 0.5f;
            lightGo.AddComponent<FlickerLight>();
        }

        // ── 3. WEB TRAP ─────────────────────────────────────────────────
        // White flat quads in corners simulating spider webs
        private void SpawnWebTraps()
        {
            var corners = new Vector3[]
            {
                new Vector3(-_roomW * 0.4f, 0.8f,  _roomD * 0.4f),
                new Vector3( _roomW * 0.4f, 0.8f,  _roomD * 0.4f),
                new Vector3(-_roomW * 0.4f, 0.8f, -_roomD * 0.4f),
            };
            var pick = Random.Range(1, corners.Length + 1);
            for (var i = 0; i < pick; i++)
            {
                var web = GameObject.CreatePrimitive(PrimitiveType.Quad);
                web.name = "Event_Web";
                web.transform.SetParent(transform);
                web.transform.position = _roomCenter + corners[i % corners.Length];
                web.transform.rotation = Quaternion.Euler(
                    Random.Range(-15f, 15f), Random.Range(30f, 60f), Random.Range(-15f, 15f));
                var s = Random.Range(1.2f, 2.2f);
                web.transform.localScale = new Vector3(s, s, 1f);
                web.GetComponent<Renderer>().material.color = new Color(0.92f, 0.92f, 0.88f, 0.55f);
                web.GetComponent<Collider>().enabled = false;
            }
        }

        // ── 4. BLOOD POOL ───────────────────────────────────────────────
        // Large overlapping dark red quads on the floor – massacre happened here
        private void SpawnBloodPool()
        {
            var count = Random.Range(3, 6);
            for (var i = 0; i < count; i++)
            {
                var pool = GameObject.CreatePrimitive(PrimitiveType.Quad);
                pool.name = "Event_BloodPool";
                pool.transform.SetParent(transform);
                pool.transform.position = RandomFloorPos(2f) + Vector3.up * 0.012f;
                pool.transform.rotation = Quaternion.Euler(90f, Random.Range(0f, 360f), 0f);
                var s = Random.Range(1.5f, 3f);
                pool.transform.localScale = new Vector3(s, s * Random.Range(0.5f, 1f), 1f);
                pool.GetComponent<Renderer>().material.color =
                    new Color(Random.Range(0.35f, 0.5f), 0.03f, 0.03f, 0.85f);
                pool.GetComponent<Collider>().enabled = false;
            }
        }

        // ── 5. STORM LIGHT ──────────────────────────────────────────────
        // Rapid lightning-flash effect – ominous electrical disturbance
        private void SpawnStormLight()
        {
            var go = new GameObject("Event_StormLight");
            go.transform.SetParent(transform);
            go.transform.position = _roomCenter + Vector3.up * (_wallHeight * 0.85f);
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(0.7f, 0.75f, 1f);
            l.intensity = 2.5f;
            l.range = Mathf.Max(_roomW, _roomD) * 0.9f;
            go.AddComponent<StormLightEffect>();
        }

        // ── 6. ACID PUDDLES ─────────────────────────────────────────────
        // Yellow-green glowing floor patches hinting at chemical spills
        private void SpawnAcidPuddles()
        {
            var count = Random.Range(3, 6);
            for (var i = 0; i < count; i++)
            {
                var puddle = GameObject.CreatePrimitive(PrimitiveType.Quad);
                puddle.name = "Event_AcidPuddle";
                puddle.transform.SetParent(transform);
                puddle.transform.position = RandomFloorPos(2f) + Vector3.up * 0.012f;
                puddle.transform.rotation = Quaternion.Euler(90f, Random.Range(0f, 360f), 0f);
                var s = Random.Range(0.8f, 2f);
                puddle.transform.localScale = new Vector3(s, s * Random.Range(0.6f, 1f), 1f);
                puddle.GetComponent<Renderer>().material.color =
                    new Color(0.5f, Random.Range(0.75f, 0.9f), 0.05f, 0.8f);
                puddle.GetComponent<Collider>().enabled = false;

                // Small glow light above each puddle
                var lg = new GameObject("AcidGlow");
                lg.transform.SetParent(puddle.transform);
                lg.transform.localPosition = Vector3.up * 0.3f;
                var al = lg.AddComponent<Light>();
                al.type = LightType.Point;
                al.color = new Color(0.4f, 1f, 0.1f);
                al.intensity = 0.6f;
                al.range = 2.5f;
            }
        }

        // ── 7. SPIKE FIELD ──────────────────────────────────────────────
        // Thin sharp pillars jutting from floor – ancient trap or natural hazard
        private void SpawnSpikeField()
        {
            var count = Random.Range(4, 8);
            for (var i = 0; i < count; i++)
            {
                var spike = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                spike.name = "Event_Spike";
                spike.transform.SetParent(transform);
                var h = Random.Range(0.4f, 1.4f);
                spike.transform.position = RandomFloorPos(2f) + Vector3.up * (h * 0.5f);
                spike.transform.localScale = new Vector3(
                    Random.Range(0.1f, 0.22f), h * 0.5f, Random.Range(0.1f, 0.22f));
                spike.transform.rotation = Quaternion.Euler(
                    Random.Range(-8f, 8f), Random.Range(0f, 360f), Random.Range(-8f, 8f));
                spike.GetComponent<Renderer>().material.color =
                    new Color(0.25f, 0.22f, 0.2f);
            }
        }

        // ── 8. CURSED ALTAR ─────────────────────────────────────────────
        // Glowing purple ritual altar – someone (or something) was worshipping here
        private void SpawnCursedAltar()
        {
            // Base slab
            var base_ = GameObject.CreatePrimitive(PrimitiveType.Cube);
            base_.name = "Event_AltarBase";
            base_.transform.SetParent(transform);
            base_.transform.position = _roomCenter + new Vector3(
                Random.Range(-_roomW * 0.2f, _roomW * 0.2f), 0.2f,
                Random.Range(-_roomD * 0.2f, _roomD * 0.2f));
            base_.transform.localScale = new Vector3(1.8f, 0.4f, 1f);
            base_.GetComponent<Renderer>().material.color = new Color(0.18f, 0.1f, 0.22f);

            // Top offering cube
            var top = GameObject.CreatePrimitive(PrimitiveType.Cube);
            top.name = "Event_AltarOffering";
            top.transform.SetParent(transform);
            top.transform.position = base_.transform.position + Vector3.up * 0.45f;
            top.transform.localScale = Vector3.one * 0.35f;
            top.transform.rotation = Quaternion.Euler(0f, 45f, 0f);
            top.GetComponent<Renderer>().material.color = new Color(0.55f, 0.1f, 0.75f);

            // Purple glow + flicker
            var lightGo = new GameObject("AltarGlow");
            lightGo.transform.SetParent(base_.transform);
            lightGo.transform.localPosition = Vector3.up * 0.6f;
            var l = lightGo.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(0.65f, 0.1f, 1f);
            l.intensity = 1.8f;
            l.range = Mathf.Max(_roomW, _roomD) * 0.55f;
            lightGo.AddComponent<FlickerLight>();

            // Swirling purple particle effect above altar
            var psGo = new GameObject("AltarParticles");
            psGo.transform.SetParent(base_.transform);
            psGo.transform.localPosition = Vector3.up * 0.5f;
            var ps = psGo.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 2.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.18f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.7f, 0.1f, 1f, 0.9f), new Color(0.3f, 0.05f, 0.6f, 0.7f));
            main.maxParticles = 60;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var em = ps.emission;
            em.rateOverTime = 15f;

            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Cone;
            sh.angle = 25f;
            sh.radius = 0.4f;

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.y = new ParticleSystem.MinMaxCurve(0.4f, 1f);
            vel.orbitalY = new ParticleSystem.MinMaxCurve(3f, 5f);
        }

        // ── HELPERS ─────────────────────────────────────────────────────
        private Vector3 RandomFloorPos(float margin)
        {
            return new Vector3(
                _roomCenter.x + Random.Range(-_roomW * 0.5f + margin, _roomW * 0.5f - margin),
                _roomCenter.y,
                _roomCenter.z + Random.Range(-_roomD * 0.5f + margin, _roomD * 0.5f - margin));
        }
    }

    /// <summary>
    /// Rapid lightning-flash behaviour used by the StormLight event.
    /// </summary>
    public class StormLightEffect : MonoBehaviour
    {
        private Light _light;
        private float _baseIntensity;
        private float _nextFlash;

        private void Start()
        {
            _light = GetComponent<Light>();
            if (_light != null) _baseIntensity = _light.intensity;
            ScheduleNextFlash();
        }

        private void Update()
        {
            if (_light == null) return;
            if (Time.time >= _nextFlash)
            {
                // Burst bright then go dim quickly
                _light.intensity = _baseIntensity * Random.Range(0.1f, 2.5f);
                ScheduleNextFlash();
            }
        }

        private void ScheduleNextFlash() =>
            _nextFlash = Time.time + Random.Range(0.05f, 0.6f);
    }
}
