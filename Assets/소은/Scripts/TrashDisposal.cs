using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Soeun.Floor3
{
    /// <summary>
    /// 쓰레기통. 지정한 오브젝트(똥)가 들어오면 성공 처리한다.
    /// XRSocketInteractor 와 같은 오브젝트에 붙인다.
    /// </summary>
    [RequireComponent(typeof(XRSocketInteractor))]
    public class TrashDisposal : MonoBehaviour
    {
        [Header("받을 물건")]
        [Tooltip("이 오브젝트만 인정한다. 비워두면 아무 XRGrabInteractable 이나 받는다.")]
        [SerializeField] XRGrabInteractable m_ExpectedItem;

        [Header("연출")]
        [Tooltip("넣은 뒤 오브젝트를 숨길지 여부.")]
        [SerializeField] bool m_HideAfterDisposal = true;

        [Tooltip("숨기기까지의 지연 시간(초).")]
        [SerializeField] float m_HideDelay = 0.5f;

        [Header("사운드 (선택)")]
        [SerializeField] AudioSource m_AudioSource;
        [SerializeField] AudioClip m_DisposalClip;

        [Header("디버그")]
        [SerializeField] bool m_VerboseLogging = true;

        [Space]
        [Tooltip("똥을 넣는 데 성공했을 때. 여기에 엘리베이터 문 열기를 연결한다.")]
        public UnityEvent onDisposed;

        XRSocketInteractor m_Socket;
        bool m_Disposed;

        public bool isDisposed => m_Disposed;

        void Awake()
        {
            m_Socket = GetComponent<XRSocketInteractor>();
        }

        void OnEnable()
        {
            m_Socket.selectEntered.AddListener(HandleItemPlaced);
        }

        void OnDisable()
        {
            m_Socket.selectEntered.RemoveListener(HandleItemPlaced);
        }

        void Log(string message)
        {
            if (m_VerboseLogging)
                Debug.Log($"[쓰레기통] {message}", this);
        }

        void HandleItemPlaced(SelectEnterEventArgs args)
        {
            if (m_Disposed)
                return;

            var placed = args.interactableObject as XRGrabInteractable;

            if (m_ExpectedItem != null && placed != m_ExpectedItem)
            {
                Log($"'{placed?.name}' 은(는) 받지 않는 물건이다.");
                return;
            }

            m_Disposed = true;
            Log($"'{placed?.name}' 투입 완료!");

            if (m_AudioSource != null && m_DisposalClip != null)
                m_AudioSource.PlayOneShot(m_DisposalClip);

            if (m_HideAfterDisposal && placed != null)
                StartCoroutine(HideRoutine(placed.gameObject));

            onDisposed?.Invoke();
        }

        System.Collections.IEnumerator HideRoutine(GameObject item)
        {
            yield return new WaitForSeconds(m_HideDelay);

            // 소켓에서 먼저 빼내야 XRI 내부 상태가 꼬이지 않는다.
            if (m_Socket != null && m_Socket.hasSelection)
                m_Socket.interactionManager.CancelInteractableSelection(
                    (IXRSelectInteractable)m_Socket.firstInteractableSelected);

            item.SetActive(false);
            Log("똥 오브젝트 숨김 처리");
        }
    }
}
