using System.Collections;
using Soeun.UI;
using UnityEngine;

namespace Soeun.Floor3
{
    /// <summary>
    /// 3층 UI 재생 순서를 담당한다.
    /// 대화 UI를 띄우고 → 사라지면 → 시스템 UI를 띄우는 한 쌍을 단계마다 재생한다.
    ///
    /// 재생 시점
    ///   1번 : 넘어졌다가 일어날 때
    ///   2~4번: 오답 강아지를 고른 순서대로 (어떤 강아지를 먼저 고르든 2 → 3 → 4)
    ///   5번 : 정답 강아지를 들어올릴 때
    /// </summary>
    public class Floor3UISequencer : MonoBehaviour
    {
        [Header("연결")]
        [Tooltip("비워두면 씬에서 자동으로 찾는다.")]
        [SerializeField] SlipFallSequence m_FallSequence;

        [Tooltip("비워두면 씬에서 자동으로 모은다.")]
        [SerializeField] SelectableDog[] m_Dogs;

        [Header("UI id — UIRoot에 등록한 id와 같아야 한다")]
        [Tooltip("대화 UI id를 1번부터 순서대로 넣는다.")]
        [SerializeField] string[] m_DialogueIds = { "1", "2", "3", "4", "5" };

        [Tooltip("시스템 UI id를 1번부터 순서대로 넣는다.")]
        [SerializeField] string[] m_SystemIds = { "1", "2", "3", "4", "5" };

        [Header("타이밍")]
        [Tooltip("각 UI가 화면에 떠 있는 시간(초).")]
        [SerializeField] float m_HoldDuration = 2f;

        [Tooltip("대화 UI가 사라지고 시스템 UI가 뜨기까지의 간격(초). 0이면 바로 이어진다.")]
        [SerializeField] float m_GapDuration;

        [Header("디버그")]
        [SerializeField] bool m_VerboseLogging = true;

        /// <summary>오답을 고른 횟수. 2번부터 시작하므로 +1 해서 단계 번호가 된다.</summary>
        int m_WrongCount;

        Coroutine m_Routine;

        void Awake()
        {
            if (m_FallSequence == null)
                m_FallSequence = FindFirstObjectByType<SlipFallSequence>();

            if (m_Dogs == null || m_Dogs.Length == 0)
                m_Dogs = FindObjectsByType<SelectableDog>(FindObjectsSortMode.None);
        }

        void OnEnable()
        {
            if (m_FallSequence != null)
                m_FallSequence.onStoodUp.AddListener(HandleStoodUp);

            foreach (var dog in m_Dogs)
            {
                if (dog == null)
                    continue;

                dog.onWrongSelected.AddListener(HandleWrongDog);
                dog.onCorrectSelected.AddListener(HandleCorrectDog);
            }
        }

        void OnDisable()
        {
            if (m_FallSequence != null)
                m_FallSequence.onStoodUp.RemoveListener(HandleStoodUp);

            foreach (var dog in m_Dogs)
            {
                if (dog == null)
                    continue;

                dog.onWrongSelected.RemoveListener(HandleWrongDog);
                dog.onCorrectSelected.RemoveListener(HandleCorrectDog);
            }
        }

        void Start()
        {
            // 연결 상태를 한눈에 확인할 수 있게 시작할 때 보고한다.
            var dogCount = 0;
            foreach (var dog in m_Dogs)
            {
                if (dog != null)
                    dogCount++;
            }

            Log($"준비 완료 — 넘어짐 연결={(m_FallSequence != null ? "O" : "X")}, " +
                $"강아지 {dogCount}마리 연결, " +
                $"UIController={(UIController.instance != null ? "O" : "X")}");

            if (m_FallSequence == null)
                Debug.LogError("[3층UI] SlipFallSequence를 찾을 수 없다. " +
                    "씬에 FallSequence 오브젝트가 있는지 확인해라.", this);

            if (dogCount == 0)
                Debug.LogError("[3층UI] 강아지를 하나도 찾지 못했다. " +
                    "SelectableDog가 붙은 오브젝트가 씬에 있는지 확인해라.", this);

            if (UIController.instance == null)
                Debug.LogError("[3층UI] UIController를 찾을 수 없다. " +
                    "씬에 UIRoot 프리팹이 있는지 확인해라.", this);
        }

        void Log(string message)
        {
            if (m_VerboseLogging)
                Debug.Log($"[3층UI] {message}", this);
        }

        // ── 재생 시점 ───────────────────────────────────────────

        void HandleStoodUp()
        {
            Log("일어남 → 1번 재생");
            PlayStep(1);
        }

        void HandleWrongDog()
        {
            m_WrongCount++;

            // 첫 번째 오답 → 2번, 두 번째 → 3번, 세 번째 → 4번
            var step = m_WrongCount + 1;

            if (step > 4)
            {
                Log($"오답 {m_WrongCount}번째 — 준비된 UI(4번)를 넘어서 재생하지 않는다.");
                return;
            }

            Log($"오답 {m_WrongCount}번째 → {step}번 재생");
            PlayStep(step);
        }

        void HandleCorrectDog()
        {
            Log("정답 → 5번 재생");
            PlayStep(5);
        }

        // ── 재생 ────────────────────────────────────────────────

        /// <summary>단계 번호(1~5)로 대화 → 시스템 UI를 이어서 재생한다.</summary>
        public void PlayStep(int step)
        {
            var index = step - 1;

            if (index < 0 || index >= m_DialogueIds.Length || index >= m_SystemIds.Length)
            {
                Debug.LogError($"[3층UI] {step}번 단계의 id가 등록되지 않았다. " +
                    $"(대화 {m_DialogueIds.Length}개 / 시스템 {m_SystemIds.Length}개)", this);
                return;
            }

            if (UIController.instance == null)
            {
                Debug.LogWarning("[3층UI] UIController를 찾을 수 없다. " +
                    "씬에 UIRoot 프리팹이 있는지 확인해라.", this);
                return;
            }

            if (m_Routine != null)
                StopCoroutine(m_Routine);

            m_Routine = StartCoroutine(PlayRoutine(m_DialogueIds[index], m_SystemIds[index], step));
        }

        IEnumerator PlayRoutine(string dialogueId, string systemId, int step)
        {
            var ui = UIController.instance;

            // ① 대화 UI
            Log($"{step}번 대화 UI \"{dialogueId}\" 표시");
            ui.ShowDialogue(dialogueId, m_HoldDuration);
            yield return new WaitForSeconds(m_HoldDuration);

            if (m_GapDuration > 0f)
                yield return new WaitForSeconds(m_GapDuration);

            // ② 시스템 UI
            Log($"{step}번 시스템 UI \"{systemId}\" 표시");
            ui.ShowMission(systemId, m_HoldDuration);
            yield return new WaitForSeconds(m_HoldDuration);

            Log($"{step}번 재생 완료");
            m_Routine = null;
        }
    }
}
