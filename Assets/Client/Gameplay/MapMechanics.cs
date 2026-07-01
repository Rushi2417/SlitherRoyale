using System.Collections.Generic;
using UnityEngine;

namespace SlitherRoyale.Client.Gameplay
{
    public class MapMechanics : MonoBehaviour
    {
        public MapConfig config;
        private List<ISpeedPad> _speedPads;
        private List<ILaserFence> _laserFences;
        private List<ICurrentZone> _currentZones;
        private List<IJellyfish> _jellyfish;
        private float _currentRadius;
        private List<ILavaPool> _lavaPools;
        private List<ISyrupZone> _syrupZones;
        private List<IAirlockZone> _airlockZones;
        private List<IWisp> _wisps;
        private float _darknessTimer;
        private bool _isDark;
        private float _wispSpawnTimer;

        private void Start()
        {
            _currentRadius = config.arenaRadius;
            _speedPads = new List<ISpeedPad>();
            _laserFences = new List<ILaserFence>();
            _currentZones = new List<ICurrentZone>();
            _jellyfish = new List<IJellyfish>();
            _lavaPools = new List<ILavaPool>();
            _syrupZones = new List<ISyrupZone>();
            _airlockZones = new List<IAirlockZone>();
            _wisps = new List<IWisp>();

            if (config.hasSpeedPads) SpawnSpeedPads();
            if (config.hasLaserFences) SpawnLaserFences();
            if (config.hasCurrents) SpawnCurrents();
            if (config.hasJellyfish) SpawnJellyfish();
            if (config.hasLavaPools) SpawnLavaPools();
            if (config.hasSyrupZones) SpawnSyrupZones();
            if (config.hasAirlockZones) SpawnAirlockZones();
            if (config.hasDarknessEvents) _darknessTimer = Random.Range(15f, 30f);
        }

        private void Update()
        {
            if (config.hasDarknessEvents)
            {
                _darknessTimer -= Time.deltaTime;
                if (_darknessTimer <= 0)
                {
                    _isDark = !_isDark;
                    _darknessTimer = _isDark ? Random.Range(5f, 10f) : Random.Range(15f, 30f);
                }
            }

            if (config.hasWisps)
            {
                _wispSpawnTimer -= Time.deltaTime;
                if (_wispSpawnTimer <= 0)
                {
                    _wispSpawnTimer = Random.Range(3f, 8f);
                    SpawnWisp();
                }
            }

            for (int i = _wisps.Count - 1; i >= 0; i--)
            {
                var w = _wisps[i];
                w.Lifetime -= Time.deltaTime;
                if (w.Lifetime <= 0) _wisps.RemoveAt(i);
                else _wisps[i] = w;
            }
        }

        public float ArenaRadius => _currentRadius;

        public void SetShrinkRadius(float radius)
        {
            _currentRadius = radius;
        }

        public bool IsInLava(Vector3 pos)
        {
            if (!config.hasLavaPools) return false;
            foreach (var pool in _lavaPools)
                if (pool.Contains(pos)) return true;
            return false;
        }

        /// <summary>
        /// BUG-24 FIX: Returns a directional push vector (world-space) for the current zone
        /// at <paramref name="pos"/>.
        /// Old code exposed a scalar float and GameManager applied it along the worm's own
        /// heading — so currents just made worms faster, not actually redirect them.
        /// This method returns the current's actual direction * speed so GameManager can
        /// add it as a world-space displacement regardless of the worm's facing.
        /// </summary>
        public Vector3 GetCurrentForce(Vector3 pos)
        {
            if (!config.hasCurrents) return Vector3.zero;
            foreach (var zone in _currentZones)
                if (zone.Contains(pos)) return zone.Direction * zone.Velocity;
            return Vector3.zero;
        }

        // Keep old name as a zero-returning shim so any server code that still calls it compiles.
        [System.Obsolete("Use GetCurrentForce instead (BUG-24 fix)")]
        public float GetCurrentVelocity(Vector3 pos) => 0f;

        public bool IsSpeedPad(Vector3 pos)
        {
            if (!config.hasSpeedPads) return false;
            foreach (var pad in _speedPads)
                if (pad.Contains(pos)) return true;
            return false;
        }

        public bool HitLaserFence(Vector3 pos, Vector3 prevPos)
        {
            if (!config.hasLaserFences) return false;
            foreach (var fence in _laserFences)
                if (fence.IntersectsSegment(prevPos, pos)) return true;
            return false;
        }

        public bool HitJellyfish(Vector3 pos)
        {
            if (!config.hasJellyfish) return false;
            foreach (var j in _jellyfish)
                if (j.Contains(pos)) return true;
            return false;
        }

        public bool IsInSyrup(Vector3 pos)
        {
            if (!config.hasSyrupZones) return false;
            foreach (var s in _syrupZones)
                if (s.Contains(pos)) return true;
            return false;
        }

        public Vector3? GetAirlockTarget(Vector3 pos)
        {
            if (!config.hasAirlockZones) return null;
            foreach (var a in _airlockZones)
                if (a.Contains(pos)) return a.Target;
            return null;
        }

        public bool HasLowGravity => config.hasLowGravity;

        public bool IsDark => _isDark;

        public IReadOnlyList<IWisp> Wisps => _wisps;

        private void SpawnSpeedPads()
        {
            float padRadius = config.arenaRadius * 0.055f; // ~44 units in 800-radius arena
            for (int i = 0; i < 6; i++)
            {
                float angle = i * 60f * Mathf.Deg2Rad;
                float r = config.arenaRadius * 0.5f;
                var p = new SpeedPad(
                    new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r, 0),
                    padRadius);
                _speedPads.Add(p);
            }
        }

        private void SpawnLaserFences()
        {
            for (int i = 0; i < 4; i++)
            {
                float angle = i * 45f * Mathf.Deg2Rad;
                float r = config.arenaRadius * 0.4f;
                var p = new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r, 0);
                var p2 = new Vector3(Mathf.Cos(angle + 0.3f) * r, Mathf.Sin(angle + 0.3f) * r, 0);
                _laserFences.Add(new LaserFence(p, p2));
            }
        }

        private void SpawnCurrents()
        {
            float zoneRadius = config.arenaRadius * 0.15f; // ~120 units in 800-radius
            for (int i = 0; i < 4; i++)
            {
                float angle = i * 90f * Mathf.Deg2Rad;
                var center = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * config.arenaRadius * 0.3f;

                // BUG-24 FIX: Direction is perpendicular (clockwise tangent) to the radial
                // so the current flows around the arena in a rotating ring pattern.
                // Old code only stored a scalar Velocity and GameManager pushed in the
                // worm's own heading direction instead of the current's actual direction.
                float perpAngle = angle + Mathf.PI * 0.5f; // 90° clockwise
                var dir = new Vector3(Mathf.Cos(perpAngle), Mathf.Sin(perpAngle), 0);

                _currentZones.Add(new CurrentZone(center, zoneRadius, dir, 80f));
            }
        }

        private void SpawnJellyfish()
        {
            float jRadius = config.arenaRadius * 0.045f; // ~36 units in 800-radius
            for (int i = 0; i < 6; i++)
            {
                float angle = Random.value * 360f * Mathf.Deg2Rad;
                float r = Random.Range(config.arenaRadius * 0.1f, config.arenaRadius * 0.8f);
                var p = new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r, 0);
                _jellyfish.Add(new Jellyfish(p, jRadius));
            }
        }

        private void SpawnLavaPools()
        {
            float lavaRadius = config.arenaRadius * 0.05f; // ~40 units in 800-radius
            for (int i = 0; i < 5; i++)
            {
                float angle = Random.value * 360f * Mathf.Deg2Rad;
                float r = Random.Range(config.arenaRadius * 0.1f, config.arenaRadius * 0.8f);
                var p = new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r, 0);
                _lavaPools.Add(new LavaPool(p, lavaRadius));
            }
        }

        private void SpawnSyrupZones()
        {
            float syrupRadius = config.arenaRadius * 0.08f; // ~64 units in 800-radius
            for (int i = 0; i < 4; i++)
            {
                float angle = i * 90f * Mathf.Deg2Rad + 0.5f;
                float r = config.arenaRadius * 0.35f;
                var p = new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r, 0);
                _syrupZones.Add(new SyrupZone(p, syrupRadius));
            }
        }

        private void SpawnAirlockZones()
        {
            float airlockRadius = config.arenaRadius * 0.06f; // ~48 units in 800-radius
            for (int i = 0; i < 3; i++)
            {
                float angle = Random.value * 360f * Mathf.Deg2Rad;
                float r = Random.Range(config.arenaRadius * 0.2f, config.arenaRadius * 0.8f);
                var p = new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r, 0);
                float targetAngle = Random.value * 360f * Mathf.Deg2Rad;
                float targetR = Random.Range(config.arenaRadius * 0.1f, config.arenaRadius * 0.7f);
                var t = new Vector3(Mathf.Cos(targetAngle) * targetR, Mathf.Sin(targetAngle) * targetR, 0);
                _airlockZones.Add(new AirlockZone(p, airlockRadius, t));
            }
        }

        private void SpawnWisp()
        {
            float angle = Random.value * 360f * Mathf.Deg2Rad;
            float r = Random.Range(5f, _currentRadius - 3f);
            var p = new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r, 0);
            _wisps.Add(new Wisp(p, Random.Range(4f, 8f)));
        }

        private interface ISpeedPad { bool Contains(Vector3 pos); }
        private interface ILaserFence { bool IntersectsSegment(Vector3 a, Vector3 b); }
        // BUG-24 FIX: Direction added so the current pushes in a specific world direction
        private interface ICurrentZone { bool Contains(Vector3 pos); Vector3 Direction { get; } float Velocity { get; } }
        private interface IJellyfish { bool Contains(Vector3 pos); }
        private interface ILavaPool { bool Contains(Vector3 pos); }

        private struct SpeedPad : ISpeedPad
        {
            public Vector3 Center; public float Radius;
            public SpeedPad(Vector3 c, float r) { Center = c; Radius = r; }
            public bool Contains(Vector3 pos) => (pos - Center).magnitude <= Radius;
        }

        private struct LaserFence : ILaserFence
        {
            public Vector3 A, B; public float Thickness;
            public LaserFence(Vector3 a, Vector3 b) { A = a; B = b; Thickness = 1f; }
            public bool IntersectsSegment(Vector3 p1, Vector3 p2)
            {
                return SegmentDistance(p1, p2, A, B) < Thickness;
            }
            private float SegmentDistance(Vector3 p1, Vector3 p2, Vector3 q1, Vector3 q2)
            {
                float d1 = PointSegmentDistance(p1, q1, q2);
                float d2 = PointSegmentDistance(p2, q1, q2);
                float d3 = PointSegmentDistance(q1, p1, p2);
                float d4 = PointSegmentDistance(q2, p1, p2);
                return Mathf.Min(d1, d2, d3, d4);
            }
            private float PointSegmentDistance(Vector3 p, Vector3 a, Vector3 b)
            {
                var ab = b - a; var ap = p - a; float t = Vector3.Dot(ap, ab) / ab.sqrMagnitude;
                t = Mathf.Clamp01(t);
                return (a + t * ab - p).magnitude;
            }
        }

        private struct CurrentZone : ICurrentZone
        {
            public Vector3 Center; public float Radius;
            private readonly Vector3 _direction;
            private readonly float _velocity;
            // BUG-24 FIX: expose Direction so GetCurrentForce can return a world-space push vector
            public Vector3 Direction => _direction;
            public float Velocity => _velocity;
            public CurrentZone(Vector3 c, float r, Vector3 dir, float v)
            {
                Center = c; Radius = r; _direction = dir.normalized; _velocity = v;
            }
            public bool Contains(Vector3 pos) => (pos - Center).magnitude <= Radius;
        }

        private struct Jellyfish : IJellyfish
        {
            public Vector3 Center; public float Radius;
            public Jellyfish(Vector3 c, float r) { Center = c; Radius = r; }
            public bool Contains(Vector3 pos) => (pos - Center).magnitude <= Radius;
        }

        private struct LavaPool : ILavaPool
        {
            public Vector3 Center; public float Radius;
            public LavaPool(Vector3 c, float r) { Center = c; Radius = r; }
            public bool Contains(Vector3 pos) => (pos - Center).magnitude <= Radius;
        }

        private interface ISyrupZone { bool Contains(Vector3 pos); }
        private interface IAirlockZone { bool Contains(Vector3 pos); Vector3 Target { get; } }

        private struct SyrupZone : ISyrupZone
        {
            public Vector3 Center; public float Radius;
            public SyrupZone(Vector3 c, float r) { Center = c; Radius = r; }
            public bool Contains(Vector3 pos) => (pos - Center).magnitude <= Radius;
        }

        private struct AirlockZone : IAirlockZone
        {
            public Vector3 Center; public float Radius; private Vector3 _target;
            public Vector3 Target => _target;
            public AirlockZone(Vector3 c, float r, Vector3 t) { Center = c; Radius = r; _target = t; }
            public bool Contains(Vector3 pos) => (pos - Center).magnitude <= Radius;
        }

        public struct Wisp : IWisp
        {
            private Vector3 _position;
            private float _lifetime;
            public Vector3 Position { get { return _position; } set { _position = value; } }
            public float Lifetime { get { return _lifetime; } set { _lifetime = value; } }
            public Wisp(Vector3 p, float t) { _position = p; _lifetime = t; }
        }

        public interface IWisp
        {
            Vector3 Position { get; }
            float Lifetime { get; set; }
        }
    }
}
