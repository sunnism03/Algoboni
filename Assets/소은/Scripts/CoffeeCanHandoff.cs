using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Soeun.Delivery
{
    /// <summary>
    /// 아저씨 손에 붙어 있던 캔커피를 플레이어가 잡는 순간 손에서 떼어낸다.
    /// 잡히기 전에는 물리를 꺼서 손에 붙어 있게 하고, 잡히면 일반 물체가 된다.
    /// XRGrabInteractable 과 같은 오브젝트에 붙인다.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    public class CoffeeCanHandoff : MonoBehaviour
    {
        [Header("물리")]
        [Tooltip("잡히기 전까지 물리를 끈다. 이걸 끄면 잡기도 전에 캔이 바닥으로 떨어진다.")]
        [SerializeField] bool m_FreezeUntilGrabbed = true;

        [Tooltip("잡은 뒤 놓았을 때 바닥으로 떨어지게 할지. " +
                 "해제하면 놓아도 그 자리에 그대로 떠 있는다.")]
        [SerializeField] bool m_EnableGravityAfterGrab = true;

        [Header("디버그")]
        [Tooltip("체크하면 진행 상황을 Console에 출력한다.")]
        [SerializeField] bool m_VerboseLogging = true;

        [Header("이벤트")]
        [Tooltip("플레이어가 커피를 처음 잡은 순간. 여기에 씬 종료 처리를 연결한다.")]
        public UnityEvent onFirstGrabbed;

        [Tooltip("커피를 놓은 순간.")]
        public UnityEvent onReleased;

        XRGrabInteractable m_Grab;
        Rigidbody m_Rigidbody;
        bool m_Initialized;
        bool m_HasBeenGrabbed;

        public bool hasBeenGrabbed => m_HasBeenGrabbed;

        /// <summary>
        /// DeliveryManController 의 Awake() 에서 먼저 호출될 수 있으므로 초기화를 지연 실행한다.
        /// (유니티는 오브젝트 간 Awake 순서를 보장하지 않는다.)
        /// </summary>
        void EnsureInitialized()
        {
            if (m_Initialized)
                return;

            m_Initialized = true;

            m_Grab = GetComponent<XRGrabInteractable>();
            m_Rigidbody = GetComponent<Rigidbody>();

            // 아저씨 손에 붙어 있는 동안에는 물리를 꺼둔다.
            if (m_FreezeUntilGrabbed && m_Rigidbody != null)
            {
                m_Rigidbody.isKinematic = true;
                m_Rigidbody.useGravity = false;
            }
        }

        void Awake() => EnsureInitialized();

        void OnEnable()
        {
            EnsureInitialized();
            m_Grab.selectEntered.AddListener(HandleSelectEntered);
            m_Grab.selectExited.AddListener(HandleSelectExited);
        }

        void OnDisable()
        {
            if (m_Grab == null)
                return;

            m_Grab.selectEntered.RemoveListener(HandleSelectEntered);
            m_Grab.selectExited.RemoveListener(HandleSelectExited);
        }

        void Log(string message)
        {
            if (m_VerboseLogging)
                Debug.Log($"[캔커피] {message}", this);
        }

        /// <summary>커피를 잡을 수 있는 상태인지 설정한다.</summary>
        public void SetGrabbable(bool grabbable)
        {
            EnsureInitialized();
            m_Grab.enabled = grabbable;

            // 콜라이더도 같이 껐다 켜서 레이캐스트 하이라이트가 미리 뜨지 않게 한다.
            var colliders = GetComponentsInChildren<Collider>();
            foreach (var col in colliders)
                col.enabled = grabbable;

            Log($"잡기 {(grabbable ? "허용" : "차단")} (콜라이더 {colliders.Length}개 {(grabbable ? "ON" : "OFF")})");
        }

        void HandleSelectEntered(SelectEnterEventArgs args)
        {
            var grabberName = args.interactorObject?.transform != null
                ? args.interactorObject.transform.name
                : "알 수 없음";

            if (m_HasBeenGrabbed)
            {
                Log($"다시 잡힘 (잡은 주체: {grabberName})");
                return;
            }

            m_HasBeenGrabbed = true;
            Log($"처음 잡힘! 잡은 주체 = {grabberName}");

            // 아저씨 손에서 분리. worldPositionStays = true 로 위치 유지.
            if (transform.parent != null)
            {
                Log($"부모 '{transform.parent.name}' 에서 분리.");
                transform.SetParent(null, true);
            }

            if (m_EnableGravityAfterGrab && m_Rigidbody != null)
                m_Rigidbody.useGravity = true;

            onFirstGrabbed?.Invoke();
        }

        void HandleSelectExited(SelectExitEventArgs args)
        {
            Log("놓음");
            onReleased?.Invoke();
        }
    }
}
