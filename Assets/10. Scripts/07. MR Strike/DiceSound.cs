using UnityEngine;

namespace StudyXR.Strike
{
    /// <summary>
    /// 주사위 한 개의 소리. 던질 때 "휙", 부딪힐 때 "딱", 경기장에서 데구르르 구를 때 "따각따각"을 낸다.
    /// 짝을 맞춰 가져갈 때의 "띵"은 <see cref="PlayMatchChime"/>로 턴 진행이 부른다.
    /// </summary>
    /// <remarks>
    /// 던짐·부딪힘 클립 슬롯이 비어 있으면 코드로 합성한 간단한 소리를 쓴다 (가정). 나중에 진짜 소리 파일을 넣으면 그것을 쓴다. 짝 맞춤 소리는 늘 합성 소리다.
    /// Dice 프리팹에 붙이는 일은 메뉴 "8. 주사위 소리 추가"가 한다.
    /// </remarks>
    [RequireComponent(typeof(AudioSource), typeof(Rigidbody))]
    public class DiceSound : MonoBehaviour
    {
        // ── 합성 소리 ──
        private const int SampleRate = 44100;
        // 짝 맞춤 "띵" 크기 (0~1). 정적 호출이라 Inspector 대신 여기서 바꾼다
        private const float MatchVolume = 0.7f;

        [Header("클립 (비우면 합성 소리)")]
        [SerializeField] private AudioClip throwClip;
        [SerializeField] private AudioClip hitClip;

        [Header("부딪힘 (가정)")]
        [Tooltip("이 속도보다 약하게 부딪히면 소리를 내지 않는다 (m/s)")]
        [SerializeField] private float minHitSpeedMetersPerSecond = 0.15f;
        [Tooltip("이 속도 이상으로 부딪히면 가장 큰 소리 (m/s)")]
        [SerializeField] private float loudestHitSpeedMetersPerSecond = 1.5f;
        [Tooltip("소리 사이 최소 간격 (초). 한꺼번에 여러 번 닿아도 소리가 겹쳐 시끄럽지 않게 한다")]
        [SerializeField] private float minSoundIntervalSeconds = 0.05f;

        [Header("구름 (가정)")]
        [Tooltip("바닥에 닿은 채 이만큼 움직일 때마다 '따각' 한 번 (m). 주사위 한 변 정도면 한 면 넘어갈 때마다 난다")]
        [SerializeField] private float rollClickSpacingMeters = 0.02f;
        [Tooltip("이 속도보다 느리면 구르는 소리를 내지 않는다 (m/s)")]
        [SerializeField] private float minRollSpeedMetersPerSecond = 0.05f;
        [Tooltip("구르는 소리 크기 배율 (부딪힘 소리 기준)")]
        [SerializeField] private float rollVolumeScale = 0.4f;

        [Header("소리 크기 (가정)")]
        [Range(0f, 1f)] [SerializeField] private float throwVolume = 0.5f;
        [Range(0f, 1f)] [SerializeField] private float hitVolume = 0.8f;

        // 합성 클립은 모든 주사위가 같은 것을 쓴다 (처음 필요할 때 한 번 만든다)
        private static AudioClip synthesizedThrowClip;
        private static AudioClip synthesizedHitClip;
        private static AudioClip synthesizedMatchClip;

        private AudioSource audioSource;
        private Rigidbody body;
        private float lastSoundTime = -1f;

        // ── 구름 ──
        // 이번 물리 프레임에 무언가에 닿아 있는지. OnCollisionStay가 켜고 FixedUpdate가 읽은 뒤 끈다
        private bool isTouching;
        private float rolledMetersSinceClick;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            body = GetComponent<Rigidbody>();
        }

        /// <summary>
        /// 손을 떠나 던져진 순간 "휙" 소리를 낸다. 턴 진행이 던진 직후 부른다.
        /// </summary>
        public void PlayThrow()
        {
            PlayOnce(ThrowClip(), throwVolume);
        }

        /// <summary>
        /// 짝을 맞춰 주사위를 가져갈 때 "띵" 소리를 그 자리에서 한 번 낸다. 가져간 주사위는 곧 지워지므로 위치에 소리만 남긴다.
        /// </summary>
        /// <param name="position">소리를 낼 월드 위치 (보통 경기장 가운데)</param>
        public static void PlayMatchChime(Vector3 position)
        {
            if (synthesizedMatchClip == null) synthesizedMatchClip = CreateChimeClip();
            AudioSource.PlayClipAtPoint(synthesizedMatchClip, position, MatchVolume);
        }

        /// <summary>
        /// 부딪힌 세기에 맞춰 "딱" 소리를 낸다.
        /// </summary>
        private void OnCollisionEnter(Collision collision)
        {
            float hitSpeed = collision.relativeVelocity.magnitude;
            if (hitSpeed < minHitSpeedMetersPerSecond) return;

            float loudness = Mathf.Clamp01(hitSpeed / loudestHitSpeedMetersPerSecond);
            PlayLimited(HitClip(), hitVolume * loudness);
        }

        private void OnCollisionStay(Collision collision)
        {
            isTouching = true;
        }

        /// <summary>
        /// 닿은 채 움직인 거리를 모아 일정 거리마다 "따각" 소리를 낸다. 빠를수록 자주, 크게 난다.
        /// </summary>
        private void FixedUpdate()
        {
            bool wasTouching = isTouching;
            isTouching = false;

            float speed = body.linearVelocity.magnitude;
            bool isRolling = wasTouching && speed >= minRollSpeedMetersPerSecond && body.isKinematic == false;
            if (isRolling == false)
            {
                rolledMetersSinceClick = 0f;
                return;
            }

            rolledMetersSinceClick += speed * Time.fixedDeltaTime;
            if (rolledMetersSinceClick < rollClickSpacingMeters) return;

            rolledMetersSinceClick = 0f;
            float loudness = Mathf.Clamp01(speed / loudestHitSpeedMetersPerSecond);
            PlayLimited(HitClip(), hitVolume * rollVolumeScale * loudness);
        }

        /// <summary>
        /// 최소 간격이 지났을 때만 소리를 낸다. 음 높이를 조금씩 흔들어 같은 소리 반복이 덜 티 나게 한다.
        /// </summary>
        private void PlayLimited(AudioClip clip, float volume)
        {
            bool isTooSoon = Time.time - lastSoundTime < minSoundIntervalSeconds;
            if (isTooSoon) return;

            PlayOnce(clip, volume);
        }

        private void PlayOnce(AudioClip clip, float volume)
        {
            lastSoundTime = Time.time;
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(clip, volume);
        }

        private AudioClip ThrowClip()
        {
            if (throwClip != null) return throwClip;
            if (synthesizedThrowClip == null) synthesizedThrowClip = CreateWhooshClip();
            return synthesizedThrowClip;
        }

        private AudioClip HitClip()
        {
            if (hitClip != null) return hitClip;
            if (synthesizedHitClip == null) synthesizedHitClip = CreateClickClip();
            return synthesizedHitClip;
        }

        // ───────────── 합성 소리 ─────────────

        /// <summary>
        /// 짧은 "딱": 아주 빨리 줄어드는 잡음 + 높은 사인파. 플라스틱 주사위가 나무에 닿는 소리를 흉내 낸다.
        /// </summary>
        /// <returns>0.05초 클립</returns>
        private static AudioClip CreateClickClip()
        {
            float[] samples = new float[(int)(SampleRate * 0.05f)];
            for (int index = 0; index < samples.Length; index++)
            {
                float time = (float)index / SampleRate;
                float envelope = Mathf.Exp(-time * 120f);
                float noise = Random.Range(-1f, 1f) * 0.6f;
                float tone = Mathf.Sin(2f * Mathf.PI * 2400f * time) * 0.4f;
                samples[index] = (noise + tone) * envelope;
            }
            return CreateClip("Dice Click", samples);
        }

        /// <summary>
        /// 작은 "휙": 커졌다 작아지는 부드러운 잡음.
        /// </summary>
        /// <returns>0.15초 클립</returns>
        private static AudioClip CreateWhooshClip()
        {
            float[] samples = new float[(int)(SampleRate * 0.15f)];
            float smoothedNoise = 0f;
            for (int index = 0; index < samples.Length; index++)
            {
                float progress = (float)index / samples.Length;
                float envelope = Mathf.Sin(progress * Mathf.PI);
                // 잡음을 이전 값과 섞어 높은 소리를 깎는다 (바람 소리처럼)
                smoothedNoise = Mathf.Lerp(smoothedNoise, Random.Range(-1f, 1f), 0.15f);
                samples[index] = smoothedNoise * envelope * 0.8f;
            }
            return CreateClip("Dice Whoosh", samples);
        }

        /// <summary>
        /// 맑은 "띵": 두 음(도·솔 비슷한 5도 화음) 사인파가 천천히 줄어든다.
        /// </summary>
        /// <returns>0.6초 클립</returns>
        private static AudioClip CreateChimeClip()
        {
            float[] samples = new float[(int)(SampleRate * 0.6f)];
            for (int index = 0; index < samples.Length; index++)
            {
                float time = (float)index / SampleRate;
                float envelope = Mathf.Exp(-time * 6f);
                float lowTone = Mathf.Sin(2f * Mathf.PI * 1320f * time);
                float highTone = Mathf.Sin(2f * Mathf.PI * 1980f * time) * 0.5f;
                samples[index] = (lowTone + highTone) * 0.5f * envelope;
            }
            return CreateClip("Dice Match Chime", samples);
        }

        private static AudioClip CreateClip(string clipName, float[] samples)
        {
            AudioClip clip = AudioClip.Create(clipName, samples.Length, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
