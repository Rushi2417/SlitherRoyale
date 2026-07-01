using System;
using System.Collections.Generic;

namespace WormCore
{
    public enum BotSkillTier
    {
        Novice,
        Average,
        Skilled
    }

    public struct BotDecision
    {
        public float DesiredHeading;
        public bool BoostHeld;
    }

    public struct PelletInfo
    {
        public float X;
        public float Y;
        public float Value;
    }

    public class BotContext
    {
        public int PlayerId;
        public BotSkillTier SkillTier;
        public float ReactionTimer;
        public float CurrentTargetHeading;
        public bool BoostWanted;
        public float BoostCooldown;
        public float AggressionCooldown;
        public float ImperfectionTimer;
        public int StuckFrames;
        public float LastX;
        public float LastY;
        public int WobbleSeed;
        public float PersonalityBias;

        public BotContext(BotSkillTier tier, int seed)
        {
            PlayerId = seed;
            SkillTier = tier;
            ReactionTimer = 0f;
            CurrentTargetHeading = 0f;
            BoostWanted = false;
            BoostCooldown = 0f;
            AggressionCooldown = 0f;
            ImperfectionTimer = 0f;
            StuckFrames = 0;
            LastX = 0f;
            LastY = 0f;
            WobbleSeed = seed;
            PersonalityBias = (seed % 100) / 100f * 0.4f - 0.2f;
        }
    }

    public static class BotAI
    {
        public static readonly string[] BotNames = new[]
        {
            // English
            "Viper", "Shadow", "Blaze", "Frost", "Thunder", "Storm", "Ghost", "Fang",
            "Scale", "Venom", "Toxin", "Spike", "Shade", "Wisp", "Ember", "Ash",
            "Smoke", "Slate", "Flint", "Steel", "Blade", "Edge", "Claw", "Talon",
            "Sting", "Bolt", "Flash", "Crash", "Dash", "Drift", "Glide", "Slide",
            "Slink", "Prowl", "Stalk", "Hunt", "Byte", "Pixel", "Neon", "Cyber",
            "Nova", "Star", "Comet", "Solar", "Lunar", "Eclipse", "Phantom", "Raven",
            "Crow", "Wolf", "Fox", "Lynx", "Tiger", "Cobra", "Adder", "Boa",
            "Python", "Krait", "Mamba", "Basilisk", "Hydra", "Chimera", "Griffin",
            "Pegasus", "Phoenix", "Sphinx", "Cyclone", "Typhoon", "Hurricane",
            "Vortex", "Tempest", "Gale", "Breeze", "Mist", "Fog", "Haze",
            "Dew", "Frost", "Hail", "Sleet", "Glacier", "Tundra", "Taiga",
            "Jungle", "Safari", "Savanna", "Desert", "Dune", "Oasis", "Delta",
            "River", "Brook", "Stream", "Creek", "Lake", "Pond", "Ocean",
            "Wave", "Tide", "Surge", "Current", "Ripple", "Splash", "Dive",
            "Deep", "Abyss", "Trench", "Reef", "Coral", "Kelp", "Pearl",
            // Spanish
            "Sombra", "Fuego", "Tormenta", "Rayo", "Trueno", "Viento", "Luna",
            "Estrella", "Cometa", "Nube", "Lluvia", "Rio", "Mar", "Ola",
            "Pez", "Serpiente", "Vibora", "Dragon", "Jaguar", "Pantera", "Lobo",
            "Zorro", "Aguila", "Halcon", "Cuervo", "Llamarada", "Brasa", "Ceniza",
            "Humo", "Hielo", "Nieve", "Escarcha", "Senda", "Rastro", "Eco",
            "Raudo", "Veloz", "Punta", "Filo", "Acero", "Bronce", "Plata",
            "Oro", "Jade", "Rubi", "Zafiro", "Topacio", "Ambar", "Brillo",
            "Luz", "Rayo", "Chispa", "Llama", "Ascua", "Carbon", "Polvo",
            "Roca", "Piedra", "Metal", "Hierro", "Cobre", "Níquel", "Cobalto",
            "Sombrio", "Misterio", "Niebla", "Vapor", "Cascada", "Torrente",
            "Rapido", "Flecha", "Lanza", "Espada", "Daga", "Garfa", "Colmillo",
            // Portuguese
            "Chama", "Nevoeiro", "Tempestade", "Relampago", "Trovao", "Vento",
            "Sol", "Estrela", "Cometa", "Chuva", "Rio", "Mar", "Peixe",
            "Cobra", "Serpente", "Dragao", "Onca", "Lobo", "Raposa", "Aguia",
            "Falcao", "Corvo", "Fagulha", "Brasa", "Cinza", "Fumaca", "Gelo",
            "Neve", "Geada", "Trilha", "Rastro", "Eco", "Rapido", "Veloz",
            "Ponta", "Fio", "Aco", "Bronze", "Prata", "Ouro", "Jade",
            "Rubi", "Safira", "Topazio", "Ambar", "Coral", "Vinha", "Flecha",
            "Raio", "Brilho", "Luz", "Fogo", "Agua", "Terra", "Ar",
            "Pedra", "Rocha", "Ferro", "Cobre", "Poeira", "Areia", "Duna",
            "Vale", "Monte", "Pico", "Cume", "Onda", "Marola", "Espuma",
            "Brisa", "Ventania", "Furacao", "Tufao", "Ciclone", "Vortice",
            // Hindi
            "Aag", "Bijlee", "Badal", "Barish", "Nadi", "Samundar", "Suraj",
            "Chand", "Taara", "Hawa", "Aandhi", "Toofan", "Saanp", "Naag",
            "Garjan", "Udaan", "Pankh", "Baadal", "Neer", "Agni", "Vayu",
            "Jal", "Prithvi", "Akash", "Pavan", "Megh", "Jyoti", "Prakash",
            "Ujala", "Andhera", "Chhaya", "Tej", "Dhuan", "Ret", "Pathar",
            "Loha", "Sona", "Chandi", "Tara", "Sitara", "Nakshatra", "Mangal",
            "Shukra", "Budh", "Guru", "Shani", "Rahu", "Ketu", "Aaditya",
            "Vikram", "Ajay", "Vijay", "Amit", "Ravi", "Kiran", "Deep",
            "Anand", "Shanti", "Prem", "Milan", "Sagar", "Gagan", "Dharati",
            "Van", "Pahar", "Nadiya", "Srot", "Lahar", "Prabhat", "Sandhya",
            "Ratri", "Divas", "Ush", "Nisha", "Amrit", "Vish", "Neel",
            "Peet", "Rakt", "Shwet", "Hara", "Pavan", "Dhwani", "Naad",
            // Indonesian
            "Api", "Petir", "Angin", "Badai", "Hujan", "Sungai", "Laut",
            "Ombak", "Matahari", "Bulan", "Bintang", "Kilat", "Guntur",
            "Awan", "Kabut", "Asap", "Es", "Salju", "Ular", "Naga",
            "Macan", "Serigala", "Rubah", "Elang", "Rajawali", "Gagak",
            "Hantu", "Bayangan", "Cahaya", "Terang", "Gelap", "Malam",
            "Siang", "Fajar", "Senja", "Purnama", "Sabit", "Rembulan",
            "Surya", "Samudra", "Darat", "Angkasa", "Langit", "Bumi",
            "Gunung", "Air", "Tanah", "Halilintar", "Gemuruh", "Debu",
            "Batu", "Karang", "Pasir", "Embun", "Pelangi", "Remang",
            "Silau", "Suram", "Redup", "Nyala", "Pijar", "Kobar",
            "Kilau", "Semilir", "Topan", "Tsunami", "Gelombang", "Arus",
            "Lahar", "Kawah", "Lembah", "Bukit", "Teluk", "Selat",
            "Bara", "Duri", "Taring", "Cakar", "Sengat", "Bisa",
            "Racun", "Tumbuh", "Lilit", "Sembur", "Kelimun", "Gemerlap",
            "Surya", "Chandra", "Kartika", "Windu"
        };

        public static readonly IReadOnlyList<string> NamePool = BotNames;

        private struct BotParams
        {
            public float ReactionTime;
            public float ThreatRange;
            public float ThreatMargin;
            public float AggressionChance;
            public float BoostAggressionChance;
            public float BoostEscapeChance;
            public float ImperfectionRate;
            public float WobbleAmount;
            public float PelletWeight;
            public float ThreatWeight;
            public float AggroWeight;
        }

        private static readonly Dictionary<BotSkillTier, BotParams> Params = new Dictionary<BotSkillTier, BotParams>
        {
            [BotSkillTier.Novice] = new BotParams
            {
                ReactionTime = 0.35f, ThreatRange = 250f, ThreatMargin = 120f,
                AggressionChance = 0.15f, BoostAggressionChance = 0.1f, BoostEscapeChance = 0.3f,
                ImperfectionRate = 0.25f, WobbleAmount = 0.6f,
                PelletWeight = 1.0f, ThreatWeight = 2.0f, AggroWeight = 0.3f
            },
            [BotSkillTier.Average] = new BotParams
            {
                ReactionTime = 0.2f, ThreatRange = 300f, ThreatMargin = 80f,
                AggressionChance = 0.35f, BoostAggressionChance = 0.25f, BoostEscapeChance = 0.5f,
                ImperfectionRate = 0.12f, WobbleAmount = 0.3f,
                PelletWeight = 1.0f, ThreatWeight = 1.5f, AggroWeight = 0.7f
            },
            [BotSkillTier.Skilled] = new BotParams
            {
                ReactionTime = 0.08f, ThreatRange = 380f, ThreatMargin = 50f,
                AggressionChance = 0.6f, BoostAggressionChance = 0.5f, BoostEscapeChance = 0.7f,
                ImperfectionRate = 0.04f, WobbleAmount = 0.12f,
                PelletWeight = 0.8f, ThreatWeight = 1.2f, AggroWeight = 1.2f
            }
        };

        public static BotDecision Decide(
            WormState self,
            BotContext ctx,
            List<WormState> allWorms,
            List<PelletInfo> visiblePellets,
            float deltaTime,
            float gameTime)
        {
            var p = Params[ctx.SkillTier];
            float desired = self.Heading;
            bool boost = false;

            ctx.ReactionTimer -= deltaTime;
            ctx.BoostCooldown -= deltaTime;
            ctx.AggressionCooldown -= deltaTime;

            if (ctx.ReactionTimer > 0f)
            {
                float aimError = (ctx.CurrentTargetHeading - self.Heading) * 0.3f * deltaTime;
                return new BotDecision { DesiredHeading = self.Heading + aimError, BoostHeld = ctx.BoostWanted };
            }

            float distMoved = (float)System.Math.Sqrt(
                (self.X - ctx.LastX) * (self.X - ctx.LastX) +
                (self.Y - ctx.LastY) * (self.Y - ctx.LastY));
            if (distMoved < 1f)
                ctx.StuckFrames++;
            else
                ctx.StuckFrames = 0;
            ctx.LastX = self.X;
            ctx.LastY = self.Y;

            float aggroTargetAngle = self.Heading;
            bool hasAggroTarget = false;
            float threatAngle = self.Heading;
            float threatWeight = 0f;
            float pelletAngle = self.Heading;

            foreach (var other in allWorms)
            {
                if (other.Id == self.Id || other.IsDead) continue;
                float dx = other.X - self.X;
                float dy = other.Y - self.Y;
                float dist = (float)System.Math.Sqrt(dx * dx + dy * dy);
                if (dist < 0.01f) continue;
                float angleToOther = (float)System.Math.Atan2(dy, dx);

                if (other.Mass > self.Mass * 1.15f && dist < p.ThreatRange)
                {
                    float danger = (p.ThreatRange - dist) / p.ThreatRange;
                    float threatAngleLocal = angleToOther + (float)System.Math.PI;
                    float margin = p.ThreatMargin * (1f - danger * 0.5f);
                    float perpAngle = angleToOther + (dist < margin ? ((float)System.Math.PI * 0.5f * (dx > 0 ? 1 : -1)) : 0f);
                    float weight = danger * p.ThreatWeight;

                    if (weight > threatWeight)
                    {
                        threatWeight = weight;
                        threatAngle = perpAngle;
                    }

                    if (!boost && dist < margin * 0.6f)
                        boost = ctx.BoostCooldown <= 0f && ctx.PersonalityBias + 0.5f < p.BoostEscapeChance;
                }

                if (other.Mass < self.Mass * 0.85f && dist < p.ThreatRange * 0.7f && ctx.AggressionCooldown <= 0f)
                {
                    if (ctx.PersonalityBias + 0.5f < p.AggressionChance)
                    {
                        float interceptAngle = PredictInterceptAngle(self, other);
                        if (Math.Abs((float)System.Math.Atan2(
                                (float)System.Math.Sin(interceptAngle - angleToOther),
                                (float)System.Math.Cos(interceptAngle - angleToOther))) < 1.2f)
                        {
                            aggroTargetAngle = interceptAngle;
                            hasAggroTarget = true;
                            if (!boost && ctx.BoostCooldown <= 0f &&
                                ctx.PersonalityBias + 0.5f < p.BoostAggressionChance)
                                boost = true;
                        }
                    }
                }
            }

            float pelletWeight = p.PelletWeight;
            float bestPelletDist = float.MaxValue;
            if (visiblePellets != null && visiblePellets.Count > 0)
            {
                float nearestPx = 0, nearestPy = 0;

                // Use cached cluster center (computed once per frame for all bots)
                Vector2 clusterCenter = _cachedClusterCenter;
                float clusterDist = (float)System.Math.Sqrt(
                    (clusterCenter.X - self.X) * (clusterCenter.X - self.X) +
                    (clusterCenter.Y - self.Y) * (clusterCenter.Y - self.Y));
                float nearestDist = float.MaxValue;

                for (int i = 0; i < visiblePellets.Count && i < 30; i++)
                {
                    var pel = visiblePellets[i];
                    float d = (pel.X - self.X) * (pel.X - self.X) + (pel.Y - self.Y) * (pel.Y - self.Y);
                    if (d < nearestDist)
                    {
                        nearestDist = d;
                        nearestPx = pel.X;
                        nearestPy = pel.Y;
                    }
                }

                float usePx, usePy;
                if (clusterDist < 300f && clusterDist > 0f)
                {
                    usePx = clusterCenter.X;
                    usePy = clusterCenter.Y;
                    bestPelletDist = clusterDist;
                }
                else
                {
                    usePx = nearestPx;
                    usePy = nearestPy;
                    bestPelletDist = (float)System.Math.Sqrt(nearestDist);
                }

                pelletAngle = (float)System.Math.Atan2(usePy - self.Y, usePx - self.X);
                pelletWeight = p.PelletWeight * (1f - bestPelletDist / 600f * 0.5f);
            }

            float finalAngle;
            if (hasAggroTarget && threatWeight < 0.5f)
            {
                float aggroW = p.AggroWeight * 0.5f;
                float pelletW = pelletWeight * 0.5f;
                finalAngle = WeightedAngle(aggroTargetAngle, aggroW, pelletAngle, pelletW);
            }
            else if (threatWeight > 0.3f)
            {
                float threatW = threatWeight;
                float pelletW = pelletWeight * 0.3f;
                finalAngle = WeightedAngle(threatAngle, threatW, pelletAngle, pelletW);
            }
            else
            {
                finalAngle = pelletAngle;
            }

            float wobble = 0f;
            if (ctx.ImperfectionTimer <= 0f)
            {
                if (ctx.PersonalityBias + 0.5f < p.ImperfectionRate)
                {
                    wobble = (ctx.WobbleSeed % 3 - 1) * p.WobbleAmount;
                    ctx.ImperfectionTimer = 0.3f + ctx.PersonalityBias * 0.5f;
                }
            }
            else
            {
                ctx.ImperfectionTimer -= deltaTime;
            }

            if (ctx.StuckFrames > 30)
            {
                finalAngle += (ctx.WobbleSeed % 2 == 0 ? 1f : -1f) * 1.5f;
                ctx.StuckFrames = 0;
            }

            finalAngle += wobble;

            if (boost && ctx.BoostCooldown <= 0f)
            {
                ctx.BoostWanted = true;
                ctx.BoostCooldown = 0.5f + ctx.PersonalityBias * 1.0f;
            }
            else
            {
                ctx.BoostWanted = false;
            }

            float reactionJitter = p.ReactionTime * (ctx.PersonalityBias * 0.5f);
            ctx.ReactionTimer = p.ReactionTime + reactionJitter;
            ctx.CurrentTargetHeading = finalAngle;

            return new BotDecision
            {
                DesiredHeading = finalAngle,
                BoostHeld = ctx.BoostWanted
            };
        }

        private static float PredictInterceptAngle(WormState self, WormState target)
        {
            float dx = target.X - self.X;
            float dy = target.Y - self.Y;
            float dist = (float)System.Math.Sqrt(dx * dx + dy * dy);
            if (dist < 1f) return self.Heading;

            float leadTime = dist / MovementMath.BaseSpeed * 0.4f;
            float predictedX = target.X + (float)System.Math.Cos(target.Heading) * MovementMath.BaseSpeed * leadTime;
            float predictedY = target.Y + (float)System.Math.Sin(target.Heading) * MovementMath.BaseSpeed * leadTime;

            return (float)System.Math.Atan2(predictedY - self.Y, predictedX - self.X);
        }

        // Per-frame cached pellet cluster (computed once, shared by all bots this frame)
        private static Vector2 _cachedClusterCenter;

        public static void PreTickUpdate(List<PelletInfo> pellets, float selfX, float selfY)
        {
            // Call once per game tick before all bot Decide() calls
            if (pellets != null && pellets.Count > 0)
                _cachedClusterCenter = FindDensestCluster(selfX, selfY, pellets, 200f);
            else
                _cachedClusterCenter = new Vector2(selfX, selfY);
        }

        private static float WeightedAngle(float a1, float w1, float a2, float w2)
        {
            // Correct: sin + cos combined into atan2(sin_sum, cos_sum)
            float sinSum = (float)System.Math.Sin(a1) * w1 + (float)System.Math.Sin(a2) * w2;
            float cosSum = (float)System.Math.Cos(a1) * w1 + (float)System.Math.Cos(a2) * w2;
            return (float)System.Math.Atan2(sinSum, cosSum);
        }

        private struct Vector2
        {
            public float X; public float Y;
            public Vector2(float x, float y) { X = x; Y = y; }
        }

        private static Vector2 FindDensestCluster(float selfX, float selfY, List<PelletInfo> pellets, float radius)
        {
            if (pellets == null || pellets.Count == 0) return new Vector2(selfX, selfY);

            float bestScore = 0f;
            float bestX = selfX, bestY = selfY;
            int step = Math.Max(1, pellets.Count / 15);

            for (int i = 0; i < pellets.Count; i += step)
            {
                var center = pellets[i];
                int count = 0;
                float sumDx = 0, sumDy = 0;

                for (int j = 0; j < pellets.Count && j < 40; j++)
                {
                    float dx = pellets[j].X - center.X;
                    float dy = pellets[j].Y - center.Y;
                    float d = dx * dx + dy * dy;
                    if (d < radius * radius)
                    {
                        count++;
                        sumDx += pellets[j].X;
                        sumDy += pellets[j].Y;
                    }
                }

                if (count > bestScore)
                {
                    bestScore = count;
                    bestX = sumDx / count;
                    bestY = sumDy / count;
                }
            }

            return new Vector2(bestX, bestY);
        }

        public static BotSkillTier GetRandomTier(int seed)
        {
            int roll = seed % 100;
            if (roll < 30) return BotSkillTier.Novice;
            if (roll < 70) return BotSkillTier.Average;
            return BotSkillTier.Skilled;
        }

        public static string GetBotName(int index)
        {
            return BotNames[Math.Abs(index) % BotNames.Length];
        }
    }
}
