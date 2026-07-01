using UnityEngine;

namespace SlitherRoyale.Client.VFX
{
    /// <summary>
    /// Biome-specific ambient particle system — doc 05 §1 and doc 10 §1.
    /// Each map gets a distinct ambient particle matching its glow-color palette:
    ///   Neon Grid    → neon dust (cyan sparks)
    ///   Coral Reef   → bioluminescent plankton (blue-green motes)
    ///   Magma Core   → embers (orange-red upward drifts)
    ///   Candy Kingdom→ sugar sparkles (pastel pink/purple)
    ///   Space Station→ star particles (white glints)
    ///   Haunted Forest→ green wisps/spores
    /// Particles are spawned procedurally via a ParticleSystem component.
    /// </summary>
    public class BiomeAmbientVFX : MonoBehaviour
    {
        [Header("Config")]
        public string mapName;

        private ParticleSystem _particles;

        private void Start()
        {
            _particles = gameObject.AddComponent<ParticleSystem>();
            ApplyBiomeConfig(mapName);
        }

        public void SetBiome(string biome)
        {
            mapName = biome;
            if (_particles == null) _particles = gameObject.AddComponent<ParticleSystem>();
            ApplyBiomeConfig(biome);
        }

        private void ApplyBiomeConfig(string biome)
        {
            if (_particles == null) return;

            var main = _particles.main;
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 300;

            var emission = _particles.emission;
            var shape    = _particles.shape;
            var size     = _particles.sizeOverLifetime;
            var vel      = _particles.velocityOverLifetime;
            var col      = _particles.colorOverLifetime;

            // Shared: size fade out
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f,
                AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

            // Shape: fill arena
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 600f;

            switch (biome?.ToLower())
            {
                case "neongrid":
                case "neon grid":
                    main.startColor  = new ParticleSystem.MinMaxGradient(
                        new Color(0.2f, 1f, 1f, 0.6f), new Color(0.42f, 0.31f, 1f, 0.4f));
                    main.startSpeed  = new ParticleSystem.MinMaxCurve(5f, 15f);
                    main.startSize   = new ParticleSystem.MinMaxCurve(2f, 5f);
                    main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 6f);
                    emission.rateOverTime = 30f;
                    SetRandomVelocity(vel, 20f);
                    break;

                case "coralreef":
                case "coral reef":
                    main.startColor  = new ParticleSystem.MinMaxGradient(
                        new Color(0.25f, 0.88f, 0.77f, 0.5f), new Color(0.1f, 0.5f, 1f, 0.4f));
                    main.startSpeed  = new ParticleSystem.MinMaxCurve(2f, 8f);
                    main.startSize   = new ParticleSystem.MinMaxCurve(3f, 8f);
                    main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 10f);
                    emission.rateOverTime = 25f;
                    // Gently drift upward
                    vel.enabled = true;
                    vel.y = new ParticleSystem.MinMaxCurve(5f, 12f);
                    break;

                case "magmacore":
                case "magma core":
                    main.startColor  = new ParticleSystem.MinMaxGradient(
                        new Color(1f, 0.42f, 0.1f, 0.7f), new Color(1f, 0.8f, 0.1f, 0.5f));
                    main.startSpeed  = new ParticleSystem.MinMaxCurve(10f, 30f);
                    main.startSize   = new ParticleSystem.MinMaxCurve(2f, 4f);
                    main.startLifetime = new ParticleSystem.MinMaxCurve(2f, 4f);
                    main.gravityModifier = new ParticleSystem.MinMaxCurve(-0.5f); // float upward
                    emission.rateOverTime = 50f;
                    SetRandomVelocity(vel, 15f);
                    break;

                case "candykingdom":
                case "candy kingdom":
                    main.startColor  = new ParticleSystem.MinMaxGradient(
                        new Color(1f, 0.6f, 0.8f, 0.6f), new Color(0.8f, 0.5f, 1f, 0.5f));
                    main.startSpeed  = new ParticleSystem.MinMaxCurve(3f, 10f);
                    main.startSize   = new ParticleSystem.MinMaxCurve(4f, 10f);
                    main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 8f);
                    emission.rateOverTime = 20f;
                    SetRandomVelocity(vel, 8f);
                    break;

                case "spacestation":
                case "space station":
                    main.startColor  = new ParticleSystem.MinMaxGradient(
                        new Color(0.9f, 0.9f, 1f, 0.4f), new Color(0.5f, 0.8f, 1f, 0.3f));
                    main.startSpeed  = new ParticleSystem.MinMaxCurve(0f, 3f);
                    main.startSize   = new ParticleSystem.MinMaxCurve(1f, 3f);
                    main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 12f);
                    emission.rateOverTime = 15f;
                    // Very gentle drift — space
                    vel.enabled = true;
                    vel.x = new ParticleSystem.MinMaxCurve(-3f, 3f);
                    vel.y = new ParticleSystem.MinMaxCurve(-3f, 3f);
                    break;

                case "hauntedforest":
                case "haunted forest":
                default:
                    main.startColor  = new ParticleSystem.MinMaxGradient(
                        new Color(0.3f, 0.9f, 0.3f, 0.5f), new Color(0.1f, 0.6f, 0.2f, 0.35f));
                    main.startSpeed  = new ParticleSystem.MinMaxCurve(5f, 15f);
                    main.startSize   = new ParticleSystem.MinMaxCurve(3f, 7f);
                    main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 8f);
                    emission.rateOverTime = 25f;
                    // Ghostly drift
                    vel.enabled = true;
                    vel.x = new ParticleSystem.MinMaxCurve(-8f, 8f);
                    vel.y = new ParticleSystem.MinMaxCurve(3f, 10f);
                    break;
            }

            // Apply colour fade-to-transparent over lifetime
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(grad);

            if (!_particles.isPlaying)
                _particles.Play();
        }

        private static void SetRandomVelocity(ParticleSystem.VelocityOverLifetimeModule vel, float range)
        {
            vel.enabled = true;
            vel.x = new ParticleSystem.MinMaxCurve(-range, range);
            vel.y = new ParticleSystem.MinMaxCurve(-range, range);
        }

        /// <summary>
        /// Spawns a BiomeAmbientVFX GameObject for the given map config.
        /// Call this from Bootstrapper after scene/arena setup.
        /// </summary>
        public static BiomeAmbientVFX SpawnForMap(string biome, Transform parent = null)
        {
            var go = new GameObject("BiomeAmbientVFX");
            if (parent != null) go.transform.SetParent(parent);
            go.transform.localPosition = Vector3.zero;
            var vfx = go.AddComponent<BiomeAmbientVFX>();
            vfx.mapName = biome;
            return vfx;
        }
    }
}
