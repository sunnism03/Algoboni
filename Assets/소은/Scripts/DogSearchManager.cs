using UnityEngine;
using UnityEngine.Events;

namespace Soeun.Floor3
{
    /// <summary>
    /// 강아지 탐색 구간 전체를 관리한다.
    /// 씬의 모든 SelectableDog 를 모아서 켜고 끄고, 정답을 찾으면 다음 단계로 넘긴다.
    /// </summary>
    public class DogSearchManager : MonoBehaviour
    {
        [Header("강아지 목록")]
        [Tooltip("비워두면 씬에서 자동으로 모은다.")]
        [SerializeField] SelectableDog[] m_Dogs;

        [Header("디버그")]
        [SerializeField] bool m_VerboseLogging = true;

        [Header("이벤트")]
        [Tooltip("탐색 구간이 시작될 때. 여기에 미션/대화 UI 호출을 연결한다.")]
        public UnityEvent onSearchStarted;

        [Tooltip("정답 강아지를 찾았을 때.")]
        public UnityEvent onCulpritFound;

        bool m_Started;
        bool m_Finished;

        public bool isFinished => m_Finished;

        void Awake()
        {
            if (m_Dogs == null || m_Dogs.Length == 0)
                m_Dogs = FindObjectsByType<SelectableDog>(FindObjectsSortMode.None);

            ValidateSetup();

            // 탐색 단계 전에는 선택할 수 없게 막아둔다.
            foreach (var dog in m_Dogs)
            {
                if (dog != null)
                    dog.SetSelectable(false);
            }
        }

        void OnEnable()
        {
            foreach (var dog in m_Dogs)
            {
                if (dog != null)
                    dog.onCorrectSelected.AddListener(HandleCulpritFound);
            }
        }

        void OnDisable()
        {
            foreach (var dog in m_Dogs)
            {
                if (dog != null)
                    dog.onCorrectSelected.RemoveListener(HandleCulpritFound);
            }
        }

        void ValidateSetup()
        {
            if (m_Dogs.Length == 0)
            {
                Debug.LogError("[강아지탐색] 씬에 SelectableDog 가 하나도 없다.", this);
                return;
            }

            var culpritCount = 0;
            foreach (var dog in m_Dogs)
            {
                if (dog != null && dog.isCulprit)
                    culpritCount++;
            }

            if (culpritCount == 0)
                Debug.LogError("[강아지탐색] 정답 강아지가 없다! " +
                    "강아지 하나의 Is Culprit 를 체크해라.", this);
            else if (culpritCount > 1)
                Debug.LogError($"[강아지탐색] 정답 강아지가 {culpritCount}마리다! " +
                    "정확히 한 마리만 체크해야 한다.", this);
            else
                Log($"강아지 {m_Dogs.Length}마리 등록됨. 정답 1마리 확인.");
        }

        void Log(string message)
        {
            if (m_VerboseLogging)
                Debug.Log($"[강아지탐색] {message}", this);
        }

        /// <summary>탐색 구간 시작. 플레이어가 골목에 도착했을 때 호출한다.</summary>
        public void BeginSearch()
        {
            if (m_Started)
                return;

            m_Started = true;
            Log("탐색 시작 — 강아지 선택 가능");

            foreach (var dog in m_Dogs)
            {
                if (dog != null && !dog.isResolved)
                    dog.SetSelectable(true);
            }

            onSearchStarted?.Invoke();
        }

        void HandleCulpritFound()
        {
            if (m_Finished)
                return;

            m_Finished = true;
            Log("정답 발견 — 나머지 강아지 선택 차단");

            // 정답을 찾았으면 남은 강아지는 더 이상 못 고르게 막는다.
            foreach (var dog in m_Dogs)
            {
                if (dog != null && !dog.isCulprit)
                    dog.SetSelectable(false);
            }

            onCulpritFound?.Invoke();
        }
    }
}
