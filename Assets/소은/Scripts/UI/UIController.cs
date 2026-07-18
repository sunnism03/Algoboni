using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Soeun.UI
{
    /// <summary>
    /// 인스펙터에 등록하는 UI 이미지 한 장.
    /// id는 코드에서 부를 이름이고, 순서(index)로도 부를 수 있다.
    /// </summary>
    [Serializable]
    public class UIImageEntry
    {
        [Tooltip("코드에서 이 이미지를 부를 이름. 예: grandma_01, mission_floor3")]
        public string id;

        [Tooltip("표시할 UI 이미지.")]
        public Sprite sprite;

        [Tooltip("자동으로 사라지기까지의 시간(초). 0이면 Hide를 부를 때까지 유지된다.")]
        public float duration;

        [Tooltip("메모용. 게임에는 표시되지 않는다.")]
        [TextArea(1, 3)]
        public string note;
    }

    /// <summary>
    /// 게임 전역 UI 진입점. 씬이 바뀌어도 유지된다.
    ///
    /// 다른 팀원이 쓰는 방법 — 이 세 줄만 알면 된다:
    ///   UIController.instance.ShowDialogue("grandma_01");   // 대화 UI 띄우기
    ///   UIController.instance.ShowMission("floor3_step1");  // 미션 UI 띄우기
    ///   UIController.instance.HideDialogue();               // 끄기
    ///
    /// 순서대로 넘기고 싶을 때:
    ///   UIController.instance.ShowNextDialogue();           // 인스펙터 순서대로 다음 장
    /// </summary>
    [DisallowMultipleComponent]
    public class UIController : MonoBehaviour
    {
        [Header("패널")]
        [Tooltip("NPC 대화 UI 패널.")]
        [SerializeField] UIImagePanel m_DialoguePanel;

        [Tooltip("미션 UI 패널.")]
        [SerializeField] UIImagePanel m_MissionPanel;

        [Header("대화 이미지 목록")]
        [Tooltip("시나리오 순서대로 넣어두면 ShowNextDialogue()로 차례대로 넘길 수 있다.")]
        [SerializeField] UIImageEntry[] m_DialogueImages;

        [Header("미션 이미지 목록")]
        [Tooltip("층별 미션 이미지. 층마다 하나씩 넣어두면 된다.")]
        [SerializeField] UIImageEntry[] m_MissionImages;

        [Header("설정")]
        [Tooltip("씬 전환 후에도 UI를 유지한다.")]
        [SerializeField] bool m_PersistAcrossScenes = true;

        [Header("디버그")]
        [SerializeField] bool m_VerboseLogging = true;

        static UIController s_Instance;

        /// <summary>어디서든 UIController.instance 로 접근한다.</summary>
        public static UIController instance => s_Instance;

        readonly Dictionary<string, UIImageEntry> m_DialogueLookup =
            new Dictionary<string, UIImageEntry>();

        readonly Dictionary<string, UIImageEntry> m_MissionLookup =
            new Dictionary<string, UIImageEntry>();

        int m_DialogueCursor = -1;
        Coroutine m_SequenceRoutine;

        /// <summary>현재 대화 UI가 떠 있는지.</summary>
        public bool isDialogueShowing => m_DialoguePanel != null && m_DialoguePanel.isShowing;

        /// <summary>현재 미션 UI가 떠 있는지.</summary>
        public bool isMissionShowing => m_MissionPanel != null && m_MissionPanel.isShowing;

        void Awake()
        {
            if (s_Instance != null && s_Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            s_Instance = this;

            if (m_PersistAcrossScenes)
            {
                transform.SetParent(null, true);
                DontDestroyOnLoad(gameObject);
            }

            BuildLookup(m_DialogueImages, m_DialogueLookup, "대화");
            BuildLookup(m_MissionImages, m_MissionLookup, "미션");
        }

        void OnDestroy()
        {
            if (s_Instance == this)
                s_Instance = null;
        }

        void BuildLookup(UIImageEntry[] entries, Dictionary<string, UIImageEntry> lookup, string label)
        {
            lookup.Clear();

            if (entries == null)
                return;

            for (var i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];

                if (entry == null)
                    continue;

                if (string.IsNullOrWhiteSpace(entry.id))
                {
                    Debug.LogWarning($"[UI] {label} 이미지 {i}번의 id가 비어 있다. " +
                        "순서(index)로만 부를 수 있다.", this);
                    continue;
                }

                if (lookup.ContainsKey(entry.id))
                {
                    Debug.LogError($"[UI] {label} 이미지 id가 중복됐다: \"{entry.id}\" — " +
                        "먼저 등록된 것만 사용된다.", this);
                    continue;
                }

                if (entry.sprite == null)
                    Debug.LogWarning($"[UI] {label} \"{entry.id}\" 의 스프라이트가 비어 있다.", this);

                lookup[entry.id] = entry;
            }

            Log($"{label} 이미지 {lookup.Count}장 등록됨.");
        }

        void Log(string message)
        {
            if (m_VerboseLogging)
                Debug.Log($"[UI] {message}", this);
        }

        // ── 대화 UI ─────────────────────────────────────────────

        /// <summary>id로 대화 이미지를 띄운다. 가장 많이 쓰는 함수.</summary>
        public void ShowDialogue(string id, Action onHidden = null)
        {
            var entry = Find(m_DialogueLookup, id, "대화");
            if (entry == null)
            {
                onHidden?.Invoke();
                return;
            }

            m_DialogueCursor = Array.IndexOf(m_DialogueImages, entry);
            ShowEntry(m_DialoguePanel, entry, "대화", onHidden);
        }

        /// <summary>id로 띄우되 표시 시간을 직접 지정한다.</summary>
        public void ShowDialogue(string id, float duration, Action onHidden = null)
        {
            var entry = Find(m_DialogueLookup, id, "대화");
            if (entry == null)
            {
                onHidden?.Invoke();
                return;
            }

            m_DialogueCursor = Array.IndexOf(m_DialogueImages, entry);
            StopSequence();
            m_DialoguePanel?.Show(entry.sprite, duration, onHidden);
            Log($"대화 \"{entry.id}\" 표시 (지속 {duration}초)");
        }

        /// <summary>인스펙터 순서(0부터)로 대화 이미지를 띄운다.</summary>
        public void ShowDialogueAt(int index, Action onHidden = null)
        {
            if (!IsValidIndex(m_DialogueImages, index, "대화"))
            {
                onHidden?.Invoke();
                return;
            }

            m_DialogueCursor = index;
            ShowEntry(m_DialoguePanel, m_DialogueImages[index], "대화", onHidden);
        }

        /// <summary>인스펙터에 넣어둔 순서대로 다음 대화 이미지를 띄운다.</summary>
        public void ShowNextDialogue(Action onHidden = null)
        {
            var next = m_DialogueCursor + 1;

            if (m_DialogueImages == null || next >= m_DialogueImages.Length)
            {
                Log("다음 대화 이미지가 없다. 목록의 끝이다.");
                onHidden?.Invoke();
                return;
            }

            ShowDialogueAt(next, onHidden);
        }

        /// <summary>대화 순서 커서를 처음으로 되돌린다.</summary>
        public void ResetDialogueCursor() => m_DialogueCursor = -1;

        /// <summary>여러 장을 순서대로 자동 재생한다. 각 장의 duration을 사용한다.</summary>
        public void PlayDialogueSequence(string[] ids, Action onFinished = null)
        {
            if (ids == null || ids.Length == 0)
            {
                onFinished?.Invoke();
                return;
            }

            StopSequence();
            m_SequenceRoutine = StartCoroutine(SequenceRoutine(ids, onFinished));
        }

        IEnumerator SequenceRoutine(string[] ids, Action onFinished)
        {
            for (var i = 0; i < ids.Length; i++)
            {
                var entry = Find(m_DialogueLookup, ids[i], "대화");
                if (entry == null)
                    continue;

                var done = false;

                // duration이 0이면 자동으로 안 넘어가므로 최소 2초는 보여준다.
                var hold = entry.duration > 0f ? entry.duration : 2f;

                Log($"대화 시퀀스 {i + 1}/{ids.Length} — \"{entry.id}\"");
                m_DialoguePanel?.Show(entry.sprite, hold, () => done = true);

                yield return new WaitUntil(() => done);
            }

            m_SequenceRoutine = null;
            onFinished?.Invoke();
        }

        public void HideDialogue()
        {
            StopSequence();
            m_DialoguePanel?.Hide();
            Log("대화 UI 숨김");
        }

        // ── 미션 UI ─────────────────────────────────────────────

        /// <summary>id로 미션 이미지를 띄운다. 층 시작할 때 한 번 부르면 된다.</summary>
        public void ShowMission(string id)
        {
            var entry = Find(m_MissionLookup, id, "미션");
            if (entry != null)
                ShowEntry(m_MissionPanel, entry, "미션", null);
        }

        /// <summary>인스펙터 순서(0부터)로 미션 이미지를 띄운다.</summary>
        public void ShowMissionAt(int index)
        {
            if (IsValidIndex(m_MissionImages, index, "미션"))
                ShowEntry(m_MissionPanel, m_MissionImages[index], "미션", null);
        }

        public void HideMission()
        {
            m_MissionPanel?.Hide();
            Log("미션 UI 숨김");
        }

        // ── 공통 ────────────────────────────────────────────────

        /// <summary>대화와 미션 UI를 모두 끈다.</summary>
        public void HideAll()
        {
            HideDialogue();
            HideMission();
        }

        void ShowEntry(UIImagePanel panel, UIImageEntry entry, string label, Action onHidden)
        {
            if (panel == null)
            {
                Debug.LogError($"[UI] {label} 패널이 연결되지 않았다. " +
                    "UIController 인스펙터의 패널 슬롯을 확인해라.", this);
                onHidden?.Invoke();
                return;
            }

            StopSequence();
            panel.Show(entry.sprite, entry.duration, onHidden);
            Log($"{label} \"{entry.id}\" 표시" +
                (entry.duration > 0f ? $" (지속 {entry.duration}초)" : " (수동으로 꺼야 함)"));
        }

        UIImageEntry Find(Dictionary<string, UIImageEntry> lookup, string id, string label)
        {
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogError($"[UI] {label} id가 비어 있다.", this);
                return null;
            }

            if (lookup.TryGetValue(id, out var entry))
                return entry;

            Debug.LogError($"[UI] \"{id}\" 라는 {label} 이미지를 찾을 수 없다. " +
                "UIController 인스펙터의 id를 확인해라.", this);
            return null;
        }

        bool IsValidIndex(UIImageEntry[] entries, int index, string label)
        {
            if (entries != null && index >= 0 && index < entries.Length)
                return true;

            Debug.LogError($"[UI] {label} 이미지 {index}번은 범위를 벗어났다. " +
                $"(등록된 개수: {entries?.Length ?? 0})", this);
            return false;
        }

        void StopSequence()
        {
            if (m_SequenceRoutine == null)
                return;

            StopCoroutine(m_SequenceRoutine);
            m_SequenceRoutine = null;
        }
    }
}
