using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Soeun.Floor3
{
    /// <summary>
    /// 3층 "강아지똥, 알고보니" 전체 흐름 관리.
    /// 문 열림 → 미끄러짐 → 이동 → 강아지 탐색 → 복귀 → 똥 치우기 → 엘리베이터 열림
    /// </summary>
    public class Floor3Director : MonoBehaviour
    {
        public enum Stage
        {
            WaitingDoorOpen,   // 엘리베이터 문 열리기 대기
            Falling,           // 미끄러져 넘어지는 중
            Exploring,         // 자유 이동 — 골목 탐색
            SearchingDog,      // 강아지 고르는 중
            ReturningToPoop,   // 똥 치우러 복귀
            CleaningUp,        // 똥 잡아서 쓰레기통에
            Done               // 완료 — 엘리베이터 열림
        }

        [Header("구성 요소")]
        [SerializeField] SlipFallSequence m_FallSequence;
        [SerializeField] DogSearchManager m_DogSearch;
        [SerializeField] TrashDisposal m_Trash;

        [Tooltip("바닥의 똥. 처음에는 잡을 수 없다가 강아지를 찾은 뒤 잡을 수 있게 된다.")]
        [SerializeField] XRGrabInteractable m_Poop;

        [Header("타이밍")]
        [Tooltip("강아지를 찾은 뒤 복귀 안내가 뜨기까지의 시간. 목걸이 연출 길이에 맞춘다.")]
        [SerializeField] float m_CollarSequenceDuration = 4f;

        [Header("디버그")]
        [SerializeField] bool m_VerboseLogging = true;

        [Header("이벤트 — 시나리오 나오면 여기에 UI 호출을 연결한다")]
        [Tooltip("넘어졌다가 일어나 자유 이동이 시작될 때.")]
        public UnityEvent onExploreStarted;

        [Tooltip("강아지를 찾고 똥을 치우러 돌아가는 단계가 시작될 때.")]
        public UnityEvent onCleanupStarted;

        [Tooltip("모든 단계가 끝났을 때. 엘리베이터 문 열기를 여기 연결한다.")]
        public UnityEvent onAllStagesComplete;

        Stage m_Stage = Stage.WaitingDoorOpen;
        public Stage currentStage => m_Stage;

        void Awake()
        {
            // 똥은 강아지를 찾기 전까지 못 잡게 막아둔다.
            SetPoopGrabbable(false);
        }

        void OnEnable()
        {
            if (m_FallSequence != null)
                m_FallSequence.onStoodUp.AddListener(HandleStoodUp);

            if (m_DogSearch != null)
                m_DogSearch.onCulpritFound.AddListener(HandleCulpritFound);

            if (m_Trash != null)
                m_Trash.onDisposed.AddListener(HandleDisposed);
        }

        void OnDisable()
        {
            if (m_FallSequence != null)
                m_FallSequence.onStoodUp.RemoveListener(HandleStoodUp);

            if (m_DogSearch != null)
                m_DogSearch.onCulpritFound.RemoveListener(HandleCulpritFound);

            if (m_Trash != null)
                m_Trash.onDisposed.RemoveListener(HandleDisposed);
        }

        void Log(string message)
        {
            if (m_VerboseLogging)
                Debug.Log($"[3층연출] {message} (현재 단계: {m_Stage})", this);
        }

        void SetPoopGrabbable(bool grabbable)
        {
            if (m_Poop == null)
                return;

            m_Poop.enabled = grabbable;

            foreach (var col in m_Poop.GetComponentsInChildren<Collider>())
            {
                // 밟기 판정용 트리거 콜라이더는 건드리지 않는다.
                if (!col.isTrigger)
                    col.enabled = grabbable;
            }
        }

        // ── 1단계: 엘리베이터 문 열림 ───────────────────────────────

        /// <summary>엘리베이터 문이 열렸을 때 호출. (엘리베이터 담당자가 연결)</summary>
        public void OnElevatorDoorOpened()
        {
            if (m_Stage != Stage.WaitingDoorOpen)
                return;

            m_Stage = Stage.Falling;
            Log("① 문 열림 → 미끄러짐 연출 시작");

            if (m_FallSequence != null)
                m_FallSequence.Play();
            else
                HandleStoodUp();
        }

        // ── 2단계: 일어나서 탐색 ───────────────────────────────────

        void HandleStoodUp()
        {
            m_Stage = Stage.Exploring;
            Log("② 일어남 → 자유 이동 시작");

            onExploreStarted?.Invoke();
        }

        // ── 3단계: 강아지 무리 도착 ────────────────────────────────

        /// <summary>골목의 트리거존에 연결. 플레이어가 강아지 무리에 도착했을 때.</summary>
        public void OnReachedDogArea()
        {
            if (m_Stage != Stage.Exploring)
            {
                Log("OnReachedDogArea() 무시됨");
                return;
            }

            m_Stage = Stage.SearchingDog;
            Log("③ 강아지 무리 도착 → 탐색 시작");

            if (m_DogSearch != null)
                m_DogSearch.BeginSearch();
        }

        // ── 4단계: 범인 발견 → 복귀 ────────────────────────────────

        void HandleCulpritFound()
        {
            if (m_Stage != Stage.SearchingDog)
                return;

            Log("④ 범인 발견 → 목걸이 연출 대기");
            StartCoroutine(AfterCollarRoutine());
        }

        IEnumerator AfterCollarRoutine()
        {
            yield return new WaitForSeconds(m_CollarSequenceDuration);

            m_Stage = Stage.ReturningToPoop;
            Log("⑤ 복귀 안내 → 똥 잡기 허용");

            SetPoopGrabbable(true);

            onCleanupStarted?.Invoke();

            m_Stage = Stage.CleaningUp;
        }

        // ── 5단계: 똥 처리 완료 ────────────────────────────────────

        void HandleDisposed()
        {
            if (m_Stage == Stage.Done)
                return;

            m_Stage = Stage.Done;
            Log("⑥ 똥 처리 완료 → 엘리베이터 문 열림");

            onAllStagesComplete?.Invoke();
        }
    }
}
