using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Soeun.Floor3
{
    /// <summary>
    /// 컨트롤러로 선택할 수 있는 강아지 한 마리(+주인).
    /// 정답이면 들어올려지고 목걸이가 열리며, 오답이면 주인이 하소연하고 자리를 떠난다.
    /// XRSimpleInteractable 과 같은 오브젝트에 붙인다.
    /// </summary>
    [RequireComponent(typeof(XRSimpleInteractable))]
    public class SelectableDog : MonoBehaviour
    {
        [Header("정답 여부")]
        [Tooltip("체크하면 이 강아지가 똥을 싼 범인이다. 씬에서 정확히 한 마리만 체크한다.")]
        [SerializeField] bool m_IsCulprit;

        [Header("애니메이션")]
        [SerializeField] Animator m_DogAnimator;
        [SerializeField] Animator m_OwnerAnimator;

        [Tooltip("정답일 때 강아지가 들리는 트리거 파라미터")]
        [SerializeField] string m_PickedUpTrigger = "PickedUp";

        [Tooltip("정답일 때 목걸이가 열리는 트리거 파라미터")]
        [SerializeField] string m_CollarOpenTrigger = "CollarOpen";

        [Tooltip("오답일 때 주인이 하소연하는 트리거 파라미터")]
        [SerializeField] string m_ComplainTrigger = "Complain";

        [Tooltip("오답일 때 떠나는 트리거 파라미터 (강아지/주인 공통)")]
        [SerializeField] string m_LeaveTrigger = "Leave";

        [Header("연출 시간")]
        [SerializeField] float m_ComplainDuration = 3f;
        [SerializeField] float m_LeaveDuration = 4f;

        [Header("떠나기 이동")]
        [Tooltip("오답일 때 이 위치로 걸어간다. 비워두면 뒤쪽으로 직선 이동한다.")]
        [SerializeField] Transform m_LeaveDestination;

        [Tooltip("주인+강아지를 묶은 부모. 비워두면 이 오브젝트가 이동한다.")]
        [SerializeField] Transform m_MoveRoot;

        [Header("디버그")]
        [SerializeField] bool m_VerboseLogging = true;

        [Header("이벤트")]
        [Tooltip("정답 강아지를 골랐을 때. 여기에 대화 UI 호출을 연결한다.")]
        public UnityEvent onCorrectSelected;

        [Tooltip("오답 강아지를 골랐을 때. 주인 하소연 대사를 여기에 연결한다.")]
        public UnityEvent onWrongSelected;

        [Tooltip("오답 강아지+주인이 자리를 떠난 뒤.")]
        public UnityEvent onLeft;

        XRSimpleInteractable m_Interactable;
        bool m_Initialized;
        bool m_Resolved;

        public bool isCulprit => m_IsCulprit;
        public bool isResolved => m_Resolved;

        /// <summary>
        /// DogSearchManager 의 Awake() 에서 먼저 호출될 수 있으므로 초기화를 지연 실행한다.
        /// (유니티는 오브젝트 간 Awake 순서를 보장하지 않는다.)
        /// </summary>
        void EnsureInitialized()
        {
            if (m_Initialized)
                return;

            m_Initialized = true;

            m_Interactable = GetComponent<XRSimpleInteractable>();

            if (m_MoveRoot == null)
                m_MoveRoot = transform;
        }

        void Awake() => EnsureInitialized();

        void OnEnable()
        {
            EnsureInitialized();
            m_Interactable.selectEntered.AddListener(HandleSelected);
        }

        void OnDisable()
        {
            if (m_Interactable == null)
                return;

            m_Interactable.selectEntered.RemoveListener(HandleSelected);
        }

        void Log(string message)
        {
            if (m_VerboseLogging)
                Debug.Log($"[강아지:{name}] {message}", this);
        }

        /// <summary>선택 가능 여부를 설정한다. 탐색 단계 전에는 꺼둔다.</summary>
        public void SetSelectable(bool selectable)
        {
            EnsureInitialized();
            m_Interactable.enabled = selectable;

            foreach (var col in GetComponentsInChildren<Collider>())
                col.enabled = selectable;

            Log($"선택 {(selectable ? "가능" : "불가")}");
        }

        void HandleSelected(SelectEnterEventArgs args)
        {
            if (m_Resolved)
                return;

            m_Resolved = true;
            SetSelectable(false);

            if (m_IsCulprit)
            {
                Log("정답! 범인 강아지를 찾았다.");
                StartCoroutine(CulpritRoutine());
            }
            else
            {
                Log("오답. 주인이 하소연한다.");
                StartCoroutine(WrongRoutine());
            }
        }

        IEnumerator CulpritRoutine()
        {
            // 강아지가 들어올려지는 연출
            if (m_DogAnimator != null && !string.IsNullOrEmpty(m_PickedUpTrigger))
                m_DogAnimator.SetTrigger(m_PickedUpTrigger);

            yield return new WaitForSeconds(1.5f);

            // 목걸이 열림
            if (m_DogAnimator != null && !string.IsNullOrEmpty(m_CollarOpenTrigger))
                m_DogAnimator.SetTrigger(m_CollarOpenTrigger);

            Log("목걸이 열림 연출 재생");
            onCorrectSelected?.Invoke();
        }

        IEnumerator WrongRoutine()
        {
            if (m_OwnerAnimator != null && !string.IsNullOrEmpty(m_ComplainTrigger))
                m_OwnerAnimator.SetTrigger(m_ComplainTrigger);

            onWrongSelected?.Invoke();

            yield return new WaitForSeconds(m_ComplainDuration);

            // 떠나기
            if (m_OwnerAnimator != null && !string.IsNullOrEmpty(m_LeaveTrigger))
                m_OwnerAnimator.SetTrigger(m_LeaveTrigger);

            if (m_DogAnimator != null && !string.IsNullOrEmpty(m_LeaveTrigger))
                m_DogAnimator.SetTrigger(m_LeaveTrigger);

            Log("떠나는 중");

            var start = m_MoveRoot.position;
            var end = m_LeaveDestination != null
                ? m_LeaveDestination.position
                : start + m_MoveRoot.forward * -8f;

            // 걸어가는 방향을 바라보게 회전
            var direction = end - start;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f)
                m_MoveRoot.rotation = Quaternion.LookRotation(direction, Vector3.up);

            var elapsed = 0f;
            while (elapsed < m_LeaveDuration)
            {
                elapsed += Time.deltaTime;
                m_MoveRoot.position = Vector3.Lerp(start, end, elapsed / m_LeaveDuration);
                yield return null;
            }

            Log("퇴장 완료");
            onLeft?.Invoke();
            m_MoveRoot.gameObject.SetActive(false);
        }
    }
}
