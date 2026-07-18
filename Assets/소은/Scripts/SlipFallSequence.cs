using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Soeun.Floor3
{
    /// <summary>
    /// 똥을 밟고 미끄러져 넘어지는 연출.
    /// VR 멀미를 피하기 위해 시야를 강제로 회전시키지 않고,
    /// 암전 -> 리그를 바닥 높이로 이동 -> 페이드 인 순서로 처리한다.
    /// </summary>
    public class SlipFallSequence : MonoBehaviour
    {
        [Header("참조")]
        [Tooltip("XR Origin 루트. 비워두면 이름으로 찾는다.")]
        [SerializeField] Transform m_XROrigin;

        [SerializeField] ScreenFader m_Fader;

        [Header("넘어짐 연출")]
        [Tooltip("미끄러지는 순간 암전까지 걸리는 시간.")]
        [SerializeField] float m_FadeOutDuration = 0.2f;

        [Tooltip("암전 상태 유지 시간. 여기서 '쿵' 소리를 재생한다.")]
        [SerializeField] float m_BlackoutDuration = 0.6f;

        [Tooltip("눈을 뜰 때까지 걸리는 시간.")]
        [SerializeField] float m_FadeInDuration = 0.8f;

        [Tooltip("넘어진 상태에서 리그를 얼마나 낮출지(m). 바닥에 누운 높이.")]
        [SerializeField] float m_FallenHeightDrop = 1.1f;

        [Tooltip("넘어진 상태의 기울기(도). 크게 줄수록 멀미가 심해진다. 10~20 권장.")]
        [Range(0f, 45f)]
        [SerializeField] float m_FallenTiltAngle = 15f;

        [Header("일어나기")]
        [Tooltip("넘어진 채로 머무는 시간.")]
        [SerializeField] float m_LyingDuration = 2.5f;

        [Tooltip("일어나는 데 걸리는 시간.")]
        [SerializeField] float m_StandUpDuration = 1.2f;

        [Header("사운드 (선택)")]
        [SerializeField] AudioSource m_AudioSource;
        [SerializeField] AudioClip m_SlipClip;
        [SerializeField] AudioClip m_ThudClip;

        [Header("디버그")]
        [SerializeField] bool m_VerboseLogging = true;

        [Header("이벤트")]
        [Tooltip("미끄러지기 시작한 순간.")]
        public UnityEvent onFallStarted;

        [Tooltip("넘어져서 눈을 뜬 순간. 여기에 대화 UI 호출을 연결하면 된다.")]
        public UnityEvent onFallenAndAwake;

        [Tooltip("다시 일어선 순간.")]
        public UnityEvent onStoodUp;

        bool m_Playing;
        bool m_Completed;

        public bool hasCompleted => m_Completed;

        void Awake()
        {
            if (m_XROrigin == null)
            {
                var origin = FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
                if (origin != null)
                    m_XROrigin = origin.transform;
            }

            if (m_Fader == null)
                m_Fader = FindFirstObjectByType<ScreenFader>();

            if (m_XROrigin == null)
                Debug.LogError("[넘어짐] XR Origin을 찾을 수 없다. 인스펙터에서 직접 지정해라.", this);
        }

        void Log(string message)
        {
            if (m_VerboseLogging)
                Debug.Log($"[넘어짐] {message}", this);
        }

        /// <summary>미끄러짐 연출을 시작한다. (트리거나 다른 스크립트에서 호출)</summary>
        public void Play()
        {
            if (m_Playing || m_Completed)
            {
                Log($"Play() 무시됨. 진행중={m_Playing}, 완료={m_Completed}");
                return;
            }

            if (m_XROrigin == null)
                return;

            m_Playing = true;
            StartCoroutine(FallRoutine());
        }

        IEnumerator FallRoutine()
        {
            Log("① 미끄러짐 시작");
            onFallStarted?.Invoke();

            if (m_AudioSource != null && m_SlipClip != null)
                m_AudioSource.PlayOneShot(m_SlipClip);

            var uprightPosition = m_XROrigin.position;
            var uprightRotation = m_XROrigin.rotation;

            // 1) 빠르게 암전. 실제 이동은 안 보이는 동안 처리한다.
            var faded = false;
            if (m_Fader != null)
                m_Fader.FadeOut(m_FadeOutDuration, () => faded = true);
            else
                faded = true;

            yield return new WaitUntil(() => faded);

            // 2) 안 보이는 동안 리그를 바닥으로 내리고 살짝 기울인다.
            var fallenPosition = uprightPosition + Vector3.down * m_FallenHeightDrop;
            var fallenRotation = uprightRotation * Quaternion.Euler(0f, 0f, m_FallenTiltAngle);

            m_XROrigin.SetPositionAndRotation(fallenPosition, fallenRotation);
            Log($"② 암전 중 리그 이동 (높이 -{m_FallenHeightDrop}m, 기울기 {m_FallenTiltAngle}도)");

            if (m_AudioSource != null && m_ThudClip != null)
                m_AudioSource.PlayOneShot(m_ThudClip);

            yield return new WaitForSeconds(m_BlackoutDuration);

            // 3) 눈을 뜬다. 누워 있는 시점.
            var restored = false;
            if (m_Fader != null)
                m_Fader.FadeIn(m_FadeInDuration, () => restored = true);
            else
                restored = true;

            yield return new WaitUntil(() => restored);
            Log("③ 눈 뜸 — 누워 있는 상태");

            // 시나리오가 나오면 이 이벤트에 UIController.ShowDialogue(...) 를 연결한다.
            onFallenAndAwake?.Invoke();

            yield return new WaitForSeconds(m_LyingDuration);

            // 4) 천천히 일어난다. 이 구간은 보이는 채로 진행해도 완만해서 안전하다.
            Log("④ 일어나는 중");
            var elapsed = 0f;
            while (elapsed < m_StandUpDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.SmoothStep(0f, 1f, elapsed / m_StandUpDuration);
                m_XROrigin.SetPositionAndRotation(
                    Vector3.Lerp(fallenPosition, uprightPosition, t),
                    Quaternion.Slerp(fallenRotation, uprightRotation, t));
                yield return null;
            }

            m_XROrigin.SetPositionAndRotation(uprightPosition, uprightRotation);

            m_Playing = false;
            m_Completed = true;
            Log("⑤ 일어남 완료");
            onStoodUp?.Invoke();
        }
    }
}
