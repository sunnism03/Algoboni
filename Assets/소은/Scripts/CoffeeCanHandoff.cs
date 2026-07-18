using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Soeun.Delivery
{
    /// <summary>
    /// 아저씨 손에 붙어 있던 캔커피를 플레이어가 잡는 순간 손에서 떼어내고,
    /// 이후에는 일반 물체처럼 잡고 놓을 수 있게 만든다.
    /// XRGrabInteractable 과 같은 오브젝트에 붙인다.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    public class CoffeeCanHandoff : MonoBehaviour
    {
        [Tooltip("씬 전환 후에도 유지시킬지 여부. 체크하면 처음 잡는 순간 DontDestroyOnLoad 처리된다.")]
        [SerializeField] bool m_PersistAcrossScenes = true;

        [Header("디버그")]
        [Tooltip("체크하면 진행 상황을 Console에 출력한다.")]
        [SerializeField] bool m_VerboseLogging = true;

        [Space]
        public UnityEvent onFirstGrabbed;
        public UnityEvent onReleased;

        XRGrabInteractable m_Grab;
        Rigidbody m_Rigidbody;
        PersistentCarriedItem m_Persistent;
        bool m_Initialized;
        bool m_HasBeenGrabbed;

        /// <summary>
        /// 다른 오브젝트의 Awake()에서 먼저 호출될 수 있으므로 초기화를 지연 실행한다.
        /// (유니티는 오브젝트 간 Awake 순서를 보장하지 않는다.)
        /// </summary>
        void EnsureInitialized()
        {
            if (m_Initialized)
                return;

            m_Initialized = true;

            m_Grab = GetComponent<XRGrabInteractable>();
            m_Rigidbody = GetComponent<Rigidbody>();
            m_Persistent = GetComponent<PersistentCarriedItem>();

            // 아저씨 손에 붙어 있는 동안에는 물리를 끈다.
            if (m_Rigidbody != null)
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

            if (m_Rigidbody != null)
                m_Rigidbody.useGravity = true;

            if (m_PersistAcrossScenes && m_Persistent != null)
            {
                m_Persistent.MarkAsCarried();
                Log($"씬 전환 대비 등록 완료 (ID: {m_Persistent.itemId})");
            }
            else if (m_PersistAcrossScenes)
            {
                Debug.LogWarning("[캔커피] Persist Across Scenes가 켜져 있지만 " +
                    "PersistentCarriedItem 컴포넌트가 없다. 씬 전환 시 사라진다.", this);
            }

            onFirstGrabbed?.Invoke();
        }

        void HandleSelectExited(SelectExitEventArgs args)
        {
            Log("놓음 → 중력 작용 시작.");
            onReleased?.Invoke();
        }
    }
}
