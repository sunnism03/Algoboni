using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Soeun.Delivery
{
    /// <summary>
    /// 택배 아저씨 연출 담당.
    /// 문 열림 -> 꾸벅 인사 반복 -> 플레이어 진입 -> 커피 내밀기 -> 플레이어가 잡으면 종료.
    /// </summary>
    public class DeliveryManController : MonoBehaviour
    {
        public enum State
        {
            Idle,       // 문 닫힘 상태. 아무것도 안 함
            Bowing,     // 꾸벅꾸벅 반복
            Offering,   // 커피 내밀고 대기
            Done        // 커피 전달 완료
        }

        [Header("Animator")]
        [SerializeField] Animator m_Animator;

        [Tooltip("꾸벅 상태 진입용 Bool 파라미터 이름")]
        [SerializeField] string m_BowingBoolParam = "IsBowing";

        [Tooltip("커피 내미는 동작 Trigger 파라미터 이름")]
        [SerializeField] string m_OfferTriggerParam = "Offer";

        [Header("커피")]
        [Tooltip("씬에 배치된 캔커피 오브젝트. 아저씨 손 본에 자식으로 두면 손 따라 움직인다.")]
        [SerializeField] CoffeeCanHandoff m_CoffeeCan;

        [Tooltip("커피를 내미는 애니메이션 길이. 이 시간 뒤에 잡기가 허용된다.")]
        [SerializeField] float m_OfferAnimationDelay = 1.0f;

        [Header("사운드 (선택)")]
        [SerializeField] AudioSource m_AudioSource;
        [SerializeField] AudioClip m_GreetingClip;

        [Header("디버그")]
        [Tooltip("체크하면 진행 상황을 Console에 출력한다.")]
        [SerializeField] bool m_VerboseLogging = true;

        [Space]
        public UnityEvent onStartedBowing;
        public UnityEvent onCoffeeOffered;
        public UnityEvent onCoffeeTaken;

        State m_State = State.Idle;
        public State currentState => m_State;

        void Log(string message)
        {
            if (m_VerboseLogging)
                Debug.Log($"[택배아저씨] {message}", this);
        }

        void Awake()
        {
            if (m_Animator == null)
                m_Animator = GetComponentInChildren<Animator>();

            Log(m_Animator != null
                ? "Animator 연결됨."
                : "Animator 없음 — 애니메이션 없이 로직만 동작한다. (테스트 중이면 정상)");

            if (m_CoffeeCan == null)
            {
                Debug.LogError("[택배아저씨] Coffee Can 슬롯이 비어 있다! " +
                    "인스펙터에서 캔커피 오브젝트를 드래그해야 커피를 잡을 수 있다.", this);
                return;
            }

            m_CoffeeCan.SetGrabbable(false);
            Log($"준비 완료. 상태 = {m_State} / 커피 잡기 = 차단됨");
        }

        void OnEnable()
        {
            if (m_CoffeeCan != null)
                m_CoffeeCan.onFirstGrabbed.AddListener(HandleCoffeeTaken);
        }

        void OnDisable()
        {
            if (m_CoffeeCan != null)
                m_CoffeeCan.onFirstGrabbed.RemoveListener(HandleCoffeeTaken);
        }

        /// <summary>
        /// 엘리베이터 문이 열렸을 때 호출. (엘리베이터 담당자 스크립트의 UnityEvent에 연결)
        /// </summary>
        public void OnDoorOpened()
        {
            if (m_State != State.Idle)
            {
                Log($"OnDoorOpened() 호출됐지만 무시됨. 현재 상태 = {m_State} (Idle일 때만 반응)");
                return;
            }

            m_State = State.Bowing;
            Log("① 문 열림 → 꾸벅 인사 시작. 상태 = Bowing");

            if (m_Animator != null && !string.IsNullOrEmpty(m_BowingBoolParam))
                m_Animator.SetBool(m_BowingBoolParam, true);

            if (m_AudioSource != null && m_GreetingClip != null)
                m_AudioSource.PlayOneShot(m_GreetingClip);

            onStartedBowing?.Invoke();
        }

        /// <summary>
        /// 플레이어가 엘리베이터 안으로 들어왔을 때 호출.
        /// (ElevatorTriggerZone.onPlayerEntered 에 연결)
        /// </summary>
        public void OnPlayerEnteredElevator()
        {
            if (m_State != State.Bowing)
            {
                Log($"OnPlayerEnteredElevator() 호출됐지만 무시됨. 현재 상태 = {m_State} " +
                    "(먼저 OnDoorOpened()가 호출돼야 한다)");
                return;
            }

            m_State = State.Offering;
            Log($"② 플레이어 진입 → 커피 내미는 중. {m_OfferAnimationDelay}초 뒤 잡기 허용.");
            StartCoroutine(OfferRoutine());
        }

        IEnumerator OfferRoutine()
        {
            if (m_Animator != null)
            {
                if (!string.IsNullOrEmpty(m_BowingBoolParam))
                    m_Animator.SetBool(m_BowingBoolParam, false);

                if (!string.IsNullOrEmpty(m_OfferTriggerParam))
                    m_Animator.SetTrigger(m_OfferTriggerParam);
            }

            // 팔을 뻗는 동안은 못 잡게 막아둔다.
            yield return new WaitForSeconds(m_OfferAnimationDelay);

            if (m_CoffeeCan != null)
                m_CoffeeCan.SetGrabbable(true);

            Log("③ 커피 잡기 허용됨! 이제 컨트롤러로 잡을 수 있다.");
            onCoffeeOffered?.Invoke();
        }

        void HandleCoffeeTaken()
        {
            if (m_State == State.Done)
                return;

            m_State = State.Done;
            Log("④ 플레이어가 커피를 받았다. 연출 종료. 상태 = Done");
            onCoffeeTaken?.Invoke();
        }
    }
}
