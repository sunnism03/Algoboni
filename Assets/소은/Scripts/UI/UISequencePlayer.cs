using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Soeun.UI
{
    /// <summary>어느 패널에 띄울지.</summary>
    public enum UIPanelKind
    {
        [InspectorName("대화")] Dialogue,
        [InspectorName("시스템")] System,
    }

    /// <summary>UI 한 장을 띄우는 한 단계.</summary>
    [Serializable]
    public class UISequenceStep
    {
        [Tooltip("대화 패널에 띄울지, 시스템 패널에 띄울지.")]
        public UIPanelKind panel = UIPanelKind.Dialogue;

        [Tooltip("UIRoot에 등록한 id.")]
        public string id;

        [Tooltip("화면에 떠 있는 시간(초).")]
        public float holdDuration = 2f;

        [Tooltip("이 단계가 끝나고 다음 단계까지 쉬는 시간(초).")]
        public float gapAfter;
    }

    /// <summary>
    /// UI를 순서대로 재생하는 범용 컴포넌트.
    /// 인스펙터에 단계를 나열해두고 Play() 를 부르면 차례대로 띄운다.
    ///
    /// 씬 시작과 동시에 재생하려면 Play On Start 를 체크하고,
    /// 특정 순간에 재생하려면 다른 스크립트의 UnityEvent에 Play() 를 연결한다.
    /// </summary>
    public class UISequencePlayer : MonoBehaviour
    {
        [Header("재생할 순서")]
        [Tooltip("위에서부터 차례대로 재생된다.")]
        [SerializeField] UISequenceStep[] m_Steps;

        [Header("재생 시점")]
        [Tooltip("씬이 시작되면 자동으로 재생한다.")]
        [SerializeField] bool m_PlayOnStart;

        [Tooltip("자동 재생일 때 시작까지 기다리는 시간(초).")]
        [SerializeField] float m_StartDelay;

        [Header("디버그")]
        [SerializeField] bool m_VerboseLogging = true;

        [Space]
        [Tooltip("모든 단계가 끝났을 때.")]
        public UnityEvent onSequenceFinished;

        Coroutine m_Routine;

        public bool isPlaying => m_Routine != null;

        void Start()
        {
            if (UIController.instance == null)
                Debug.LogError("[UI순서] UIController를 찾을 수 없다. " +
                    "씬에 UIRoot 프리팹이 있는지 확인해라.", this);

            if (m_Steps == null || m_Steps.Length == 0)
            {
                Debug.LogWarning("[UI순서] 재생할 단계가 하나도 없다.", this);
                return;
            }

            Log($"준비 완료 — {m_Steps.Length}단계 등록됨" +
                (m_PlayOnStart ? $", {m_StartDelay}초 뒤 자동 재생" : ""));

            if (m_PlayOnStart)
                Play();
        }

        void Log(string message)
        {
            if (m_VerboseLogging)
                Debug.Log($"[UI순서] {message}", this);
        }

        /// <summary>처음부터 순서대로 재생한다. 이미 재생 중이면 다시 시작한다.</summary>
        public void Play()
        {
            if (m_Steps == null || m_Steps.Length == 0)
                return;

            if (UIController.instance == null)
            {
                Debug.LogWarning("[UI순서] UIController가 없어서 재생할 수 없다.", this);
                return;
            }

            if (m_Routine != null)
                StopCoroutine(m_Routine);

            m_Routine = StartCoroutine(PlayRoutine());
        }

        /// <summary>재생을 멈추고 UI를 끈다.</summary>
        public void Stop()
        {
            if (m_Routine != null)
            {
                StopCoroutine(m_Routine);
                m_Routine = null;
            }

            UIController.instance?.HideAll();
            Log("재생 중단");
        }

        IEnumerator PlayRoutine()
        {
            if (m_StartDelay > 0f)
                yield return new WaitForSeconds(m_StartDelay);

            var ui = UIController.instance;

            for (var i = 0; i < m_Steps.Length; i++)
            {
                var step = m_Steps[i];

                if (step == null || string.IsNullOrEmpty(step.id))
                {
                    Debug.LogWarning($"[UI순서] {i + 1}번째 단계의 id가 비어 있어 건너뛴다.", this);
                    continue;
                }

                var kindLabel = step.panel == UIPanelKind.Dialogue ? "대화" : "시스템";
                Log($"{i + 1}/{m_Steps.Length} — {kindLabel} \"{step.id}\" ({step.holdDuration}초)");

                if (step.panel == UIPanelKind.Dialogue)
                    ui.ShowDialogue(step.id, step.holdDuration);
                else
                    ui.ShowMission(step.id, step.holdDuration);

                yield return new WaitForSeconds(step.holdDuration);

                if (step.gapAfter > 0f)
                    yield return new WaitForSeconds(step.gapAfter);
            }

            Log("재생 완료");
            m_Routine = null;
            onSequenceFinished?.Invoke();
        }
    }
}
