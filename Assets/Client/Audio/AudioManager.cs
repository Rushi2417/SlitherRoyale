using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SlitherRoyale.Client.Audio
{
    /// <summary>
    /// Central audio manager. Uses procedurally-generated clips for SFX so the game
    /// has sound immediately without requiring external audio assets.
    /// Art/audio pass in Phase 7 replaces procedural clips with real authored sounds.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        // ── Music ──────────────────────────────────────────────────────────────
        [Header("Music")]
        public AudioClip menuMusicClip;
        public AudioClip matchMusicClip;
        public AudioClip resultsMusicClip;

        // ── Volume Settings ────────────────────────────────────────────────────
        [Header("Settings")]
        [Range(0f, 1f)] public float musicVolume = 0.45f;
        [Range(0f, 1f)] public float sfxVolume = 0.75f;

        private AudioSource _musicSource;
        private AudioSource _sfxSource;

        // Procedural clip cache
        private readonly Dictionary<SfxType, AudioClip> _clipCache = new();
        private static int _sampleRate;

        public enum SfxType
        {
            PelletEat,
            PelletEatLarge,
            BoostStart,
            BoostLoop,
            BoostEnd,
            Death,
            DeathBurst,
            DoubleKill,
            TripleKill,
            Rampage,
            GodLike,
            UITap,
            UITransition,
            PurchaseConfirm,
            RewardClaim,
            QuestComplete,
            HazardLaserFence,
            HazardLava,
            HazardAirlock,
            LevelUp,
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _sampleRate = AudioSettings.outputSampleRate;
            BuildSources();
            PrewarmClips();
        }

        private void BuildSources()
        {
            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.loop = true;
            _musicSource.volume = musicVolume;
            _musicSource.playOnAwake = false;

            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.loop = false;
            _sfxSource.volume = sfxVolume;
            _sfxSource.playOnAwake = false;
        }

        // ── Music API ──────────────────────────────────────────────────────────

        public void PlayMenuMusic()   => PlayMusic(menuMusicClip,    GenerateMenuMusic());
        public void PlayMatchMusic()  => PlayMusic(matchMusicClip,   GenerateMatchMusic());
        public void PlayResultsMusic()=> PlayMusic(resultsMusicClip, GenerateResultsStinger());

        private void PlayMusic(AudioClip clip, AudioClip fallback)
        {
            var toPlay = clip != null ? clip : fallback;
            if (_musicSource.clip == toPlay && _musicSource.isPlaying) return;
            _musicSource.clip = toPlay;
            _musicSource.volume = musicVolume;
            _musicSource.Play();
        }

        public void StopMusic(float fadeDuration = 0.5f)
        {
            StartCoroutine(FadeOutMusic(fadeDuration));
        }

        private IEnumerator FadeOutMusic(float duration)
        {
            float start = _musicSource.volume;
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                _musicSource.volume = Mathf.Lerp(start, 0f, t / duration);
                yield return null;
            }
            _musicSource.Stop();
            _musicSource.volume = musicVolume;
        }

        // ── SFX API ────────────────────────────────────────────────────────────

        public void Play(SfxType type, float pitchVariance = 0.08f)
        {
            if (!_clipCache.TryGetValue(type, out var clip)) return;
            _sfxSource.pitch = 1f + Random.Range(-pitchVariance, pitchVariance);
            _sfxSource.PlayOneShot(clip, sfxVolume);
        }

        public void PlayAt(SfxType type, Vector3 worldPos, float pitchVariance = 0.08f)
        {
            if (!_clipCache.TryGetValue(type, out var clip)) return;
            AudioSource.PlayClipAtPoint(clip, worldPos, sfxVolume);
        }

        public void SetMusicVolume(float v) { musicVolume = v; _musicSource.volume = v; }
        public void SetSfxVolume(float v)   { sfxVolume   = v; _sfxSource.volume   = v; }

        // ── Procedural Clip Generation ─────────────────────────────────────────

        private void PrewarmClips()
        {
            _clipCache[SfxType.PelletEat]        = GeneratePelletEat(false);
            _clipCache[SfxType.PelletEatLarge]   = GeneratePelletEat(true);
            _clipCache[SfxType.BoostStart]        = GenerateBoostStart();
            // BUG-03 FIX: BoostLoop was declared in enum but never generated
            _clipCache[SfxType.BoostLoop]         = GenerateBoostLoop();
            _clipCache[SfxType.BoostEnd]          = GenerateBoostEnd();
            _clipCache[SfxType.Death]             = GenerateDeath();
            _clipCache[SfxType.DeathBurst]        = GenerateDeathBurst();
            _clipCache[SfxType.DoubleKill]        = GenerateComboSting(1);
            _clipCache[SfxType.TripleKill]        = GenerateComboSting(2);
            _clipCache[SfxType.Rampage]           = GenerateComboSting(3);
            _clipCache[SfxType.GodLike]           = GenerateComboSting(4);
            _clipCache[SfxType.UITap]             = GenerateUITap();
            _clipCache[SfxType.UITransition]      = GenerateUITransition();
            _clipCache[SfxType.PurchaseConfirm]   = GeneratePurchaseConfirm();
            _clipCache[SfxType.RewardClaim]       = GenerateRewardClaim();
            _clipCache[SfxType.QuestComplete]     = GenerateQuestComplete();
            _clipCache[SfxType.HazardLaserFence]  = GenerateLaserFence();
            _clipCache[SfxType.HazardLava]        = GenerateLava();
            _clipCache[SfxType.HazardAirlock]     = GenerateAirlock();
            _clipCache[SfxType.LevelUp]           = GenerateLevelUp();
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private static AudioClip MakeClip(string name, float[] samples)
        {
            var clip = AudioClip.Create(name, samples.Length, 1, _sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static float[] Synthesize(float durationSec,
            System.Func<float, float, float> sampleFn,
            System.Func<float, float> envelopeFn = null)
        {
            int n = Mathf.RoundToInt(durationSec * _sampleRate);
            float[] buf = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / _sampleRate;
                float progress = (float)i / n;
                float env = envelopeFn != null ? envelopeFn(progress) : Mathf.Clamp01(1f - progress * 2f);
                buf[i] = Mathf.Clamp(sampleFn(t, progress) * env, -1f, 1f);
            }
            return buf;
        }

        private static float Sine(float freq, float t) => Mathf.Sin(t * freq * Mathf.PI * 2f);

        // ── Clip Generators ────────────────────────────────────────────────────

        private static AudioClip GeneratePelletEat(bool large)
        {
            float baseFreq = large ? 320f : 520f;
            var s = Synthesize(large ? 0.18f : 0.1f, (t, p) =>
                Sine(baseFreq + 400f * (1f - p), t) * 0.7f,
                p => Mathf.Exp(-p * 8f));
            return MakeClip("PelletEat", s);
        }

        private static AudioClip GenerateBoostStart()
        {
            var s = Synthesize(0.15f, (t, p) =>
                (Sine(200f + 600f * p, t) + Sine(310f + 400f * p, t) * 0.5f) * 0.6f,
                p => p < 0.1f ? p * 10f : Mathf.Exp(-(p - 0.1f) * 4f));
            return MakeClip("BoostStart", s);
        }

        private static AudioClip GenerateBoostEnd()
        {
            var s = Synthesize(0.2f, (t, p) =>
                Sine(500f - 300f * p, t) * 0.5f,
                p => Mathf.Exp(-p * 6f));
            return MakeClip("BoostEnd", s);
        }

        // BUG-03 FIX: generates a loopable boost hum (flat envelope)
        private static AudioClip GenerateBoostLoop()
        {
            var s = Synthesize(0.5f,
                (t, p) => (Sine(180f + 40f * Mathf.Sin(t * 3f), t)
                           + Sine(260f, t) * 0.4f
                           + Sine(370f, t) * 0.15f) * 0.45f,
                p => 1f); // flat envelope so the clip loops seamlessly
            return MakeClip("BoostLoop", s);
        }

        private static AudioClip GenerateDeath()
        {
            var s = Synthesize(0.5f, (t, p) =>
            {
                float noise = (Random.value - 0.5f) * 0.4f;
                return (Sine(300f - 200f * p, t) * 0.5f + noise) * 0.8f;
            }, p => Mathf.Pow(1f - p, 2f));
            return MakeClip("Death", s);
        }

        private static AudioClip GenerateDeathBurst()
        {
            var s = Synthesize(0.6f, (t, p) =>
            {
                float noise = (Random.value - 0.5f) * 0.8f;
                float sub   = Sine(80f - 40f * p, t) * 0.4f;
                return (noise + sub) * Mathf.Pow(1f - p, 1.5f);
            });
            return MakeClip("DeathBurst", s);
        }

        private static AudioClip GenerateComboSting(int tier)
        {
            float baseFreq = 440f + tier * 110f;
            int noteCount = 2 + tier;
            float dur = 0.08f + tier * 0.04f;
            int totalSamples = Mathf.RoundToInt((dur * noteCount + 0.1f) * _sampleRate);
            float[] buf = new float[totalSamples];
            for (int n = 0; n < noteCount; n++)
            {
                float freq = baseFreq * Mathf.Pow(1.25f, n);
                int offset = Mathf.RoundToInt(n * dur * _sampleRate);
                int noteLen = Mathf.RoundToInt(dur * _sampleRate);
                for (int i = 0; i < noteLen && (offset + i) < totalSamples; i++)
                {
                    float t = (float)i / _sampleRate;
                    float env = Mathf.Exp(-(float)i / noteLen * 5f);
                    buf[offset + i] += Sine(freq, t) * env * 0.7f;
                }
            }
            return MakeClip("ComboSting" + tier, buf);
        }

        private static AudioClip GenerateUITap()
        {
            var s = Synthesize(0.08f, (t, p) => Sine(800f, t) * 0.6f,
                p => Mathf.Exp(-p * 12f));
            return MakeClip("UITap", s);
        }

        private static AudioClip GenerateUITransition()
        {
            var s = Synthesize(0.2f, (t, p) =>
                (Sine(400f + 200f * p, t) + Sine(600f + 300f * p, t) * 0.4f) * 0.5f,
                p => Mathf.Exp(-p * 5f));
            return MakeClip("UITransition", s);
        }

        private static AudioClip GeneratePurchaseConfirm()
        {
            // Three ascending tones
            float[] freqs = {523f, 659f, 784f};
            int totalSamples = Mathf.RoundToInt(0.5f * _sampleRate);
            float[] buf = new float[totalSamples];
            for (int n = 0; n < 3; n++)
            {
                int offset = Mathf.RoundToInt(n * 0.14f * _sampleRate);
                int noteLen = Mathf.RoundToInt(0.2f * _sampleRate);
                for (int i = 0; i < noteLen && (offset + i) < totalSamples; i++)
                {
                    float t = (float)i / _sampleRate;
                    float env = Mathf.Exp(-(float)i / noteLen * 4f);
                    buf[offset + i] += Sine(freqs[n], t) * env * 0.65f;
                }
            }
            return MakeClip("PurchaseConfirm", buf);
        }

        private static AudioClip GenerateRewardClaim()
        {
            var s = Synthesize(0.35f, (t, p) =>
                (Sine(600f + 400f * (1f - p), t) * 0.5f + Sine(900f - 200f * p, t) * 0.3f),
                p => p < 0.05f ? p / 0.05f : Mathf.Exp(-(p - 0.05f) * 5f));
            return MakeClip("RewardClaim", s);
        }

        private static AudioClip GenerateQuestComplete()
        {
            float[] freqs = {523f, 784f, 1047f};
            int totalSamples = Mathf.RoundToInt(0.65f * _sampleRate);
            float[] buf = new float[totalSamples];
            for (int n = 0; n < 3; n++)
            {
                int offset = Mathf.RoundToInt(n * 0.16f * _sampleRate);
                int noteLen = Mathf.RoundToInt(0.3f * _sampleRate);
                for (int i = 0; i < noteLen && (offset + i) < totalSamples; i++)
                {
                    float t = (float)i / _sampleRate;
                    buf[offset + i] += Sine(freqs[n], t) * Mathf.Exp(-(float)i / noteLen * 4f) * 0.6f;
                }
            }
            return MakeClip("QuestComplete", buf);
        }

        private static AudioClip GenerateLaserFence()
        {
            var s = Synthesize(0.3f, (t, p) =>
            {
                float zap = Sine(2000f - 1500f * p, t) * 0.5f;
                float noise = (Random.value - 0.5f) * 0.5f;
                return zap + noise * (1f - p);
            }, p => Mathf.Exp(-p * 3f));
            return MakeClip("LaserFence", s);
        }

        private static AudioClip GenerateLava()
        {
            var s = Synthesize(0.4f, (t, p) =>
            {
                float bubble = Sine(150f + 50f * Mathf.Sin(t * 5f), t) * 0.6f;
                float noise = (Random.value - 0.5f) * 0.3f;
                return bubble + noise;
            }, p => p < 0.1f ? p * 10f : Mathf.Exp(-(p - 0.1f) * 2f));
            return MakeClip("Lava", s);
        }

        private static AudioClip GenerateAirlock()
        {
            var s = Synthesize(0.5f, (t, p) =>
                Sine(800f - 600f * p, t) * 0.6f + Sine(400f + 400f * p, t) * 0.3f,
                p => p < 0.05f ? p / 0.05f : Mathf.Exp(-(p - 0.05f) * 3f));
            return MakeClip("Airlock", s);
        }

        private static AudioClip GenerateLevelUp()
        {
            float[] freqs = {392f, 523f, 659f, 784f, 1047f};
            int totalSamples = Mathf.RoundToInt(0.9f * _sampleRate);
            float[] buf = new float[totalSamples];
            for (int n = 0; n < freqs.Length; n++)
            {
                int offset = Mathf.RoundToInt(n * 0.12f * _sampleRate);
                int noteLen = Mathf.RoundToInt(0.35f * _sampleRate);
                for (int i = 0; i < noteLen && (offset + i) < totalSamples; i++)
                {
                    float t = (float)i / _sampleRate;
                    buf[offset + i] += Sine(freqs[n], t) * Mathf.Exp(-(float)i / noteLen * 3.5f) * 0.55f;
                }
            }
            return MakeClip("LevelUp", buf);
        }

        // ── Background Music Generators ────────────────────────────────────────

        private static AudioClip GenerateMenuMusic()
        {
            // 4-bar loop in A minor pentatonic feel, 120bpm
            float bpm = 120f;
            float barDur = 4f * 60f / bpm;
            float loopDur = barDur * 4f;
            int n = Mathf.RoundToInt(loopDur * _sampleRate);
            float[] buf = new float[n];

            float[] melody = {220f, 261f, 294f, 349f, 392f, 440f, 294f, 261f};
            float noteDur = barDur / melody.Length;

            for (int ni = 0; ni < melody.Length * 4; ni++)
            {
                float freq = melody[ni % melody.Length];
                int offset = Mathf.RoundToInt(ni * noteDur * _sampleRate);
                int noteLen = Mathf.RoundToInt(noteDur * 0.85f * _sampleRate);
                for (int i = 0; i < noteLen && (offset + i) < n; i++)
                {
                    float t = (float)i / _sampleRate;
                    float env = i < noteLen / 10 ? (float)i / (noteLen / 10f) :
                                Mathf.Exp(-(float)(i - noteLen / 10) / noteLen * 3f);
                    buf[offset + i] += (Sine(freq, t) * 0.3f + Sine(freq * 2f, t) * 0.1f) * env * 0.4f;
                }
                // Bass octave below
                int bOffset = offset;
                for (int i = 0; i < noteLen && (bOffset + i) < n; i++)
                {
                    float t = (float)i / _sampleRate;
                    float env = Mathf.Exp(-(float)i / noteLen * 5f);
                    buf[bOffset + i] += Sine(freq / 2f, t) * env * 0.2f;
                }
            }
            return MakeClip("MenuMusic", buf);
        }

        private static AudioClip GenerateMatchMusic()
        {
            // Energetic 140bpm loop
            float bpm = 140f;
            float barDur = 4f * 60f / bpm;
            float loopDur = barDur * 4f;
            int n = Mathf.RoundToInt(loopDur * _sampleRate);
            float[] buf = new float[n];

            // Driving bass pattern
            float[] bassFreqs = {110f, 110f, 146f, 130f};
            float noteDur = barDur;
            for (int ni = 0; ni < bassFreqs.Length; ni++)
            {
                float freq = bassFreqs[ni];
                int offset = Mathf.RoundToInt(ni * noteDur * _sampleRate);
                int noteLen = Mathf.RoundToInt(noteDur * _sampleRate);
                for (int i = 0; i < noteLen && (offset + i) < n; i++)
                {
                    float t = (float)i / _sampleRate;
                    float env = Mathf.Exp(-(float)i / noteLen * 2.5f);
                    buf[offset + i] += Sine(freq, t) * env * 0.35f;
                    // Add noise hi-hat feel every beat
                    if (i % Mathf.RoundToInt(60f / bpm * _sampleRate) < 200)
                        buf[offset + i] += (Random.value - 0.5f) * Mathf.Exp(-(i % 200) / 30f) * 0.2f;
                }
            }
            return MakeClip("MatchMusic", buf);
        }

        private static AudioClip GenerateResultsStinger()
        {
            float[] freqs = {330f, 415f, 523f, 622f, 784f};
            int totalSamples = Mathf.RoundToInt(1.2f * _sampleRate);
            float[] buf = new float[totalSamples];
            for (int ni = 0; ni < freqs.Length; ni++)
            {
                int offset = Mathf.RoundToInt(ni * 0.15f * _sampleRate);
                int noteLen = Mathf.RoundToInt(0.5f * _sampleRate);
                for (int i = 0; i < noteLen && (offset + i) < totalSamples; i++)
                {
                    float t = (float)i / _sampleRate;
                    float env = Mathf.Exp(-(float)i / noteLen * 2.5f);
                    buf[offset + i] += (Sine(freqs[ni], t) * 0.5f + Sine(freqs[ni] * 2f, t) * 0.2f) * env * 0.5f;
                }
            }
            return MakeClip("ResultsStinger", buf);
        }
    }
}
