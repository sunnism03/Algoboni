using UnityEngine;
using UnityEngine.InputSystem;

namespace Soeun.Delivery
{
    /// <summary>
    /// 엘리베이터 담당자 작업이 끝나기 전에 혼자 테스트하기 위한 임시 스크립트.
    /// 에디터에서는 Game 뷰 버튼 / O,P 키,
    /// 헤드셋에서는 오른손 컨트롤러 A,B 버튼으로 각 단계를 강제 실행한다.
    /// 실제 빌드에는 넣지 않는다.
    /// </summary>
    public class DeliveryDebugTrigger : MonoBehaviour
    {
        [SerializeField] DeliveryManController m_DeliveryMan;

        [Tooltip("Game 뷰에 테스트 버튼을 표시한다. 에디터 전용 — VR 헤드셋에서는 누를 수 없다.")]
        [SerializeField] bool m_ShowOnScreenButtons = true;

        [Header("자동 진행 (헤드셋 테스트용)")]
        [Tooltip("체크하면 재생 직후 아래 시간에 맞춰 자동으로 ①②를 실행한다. " +
                 "컨트롤러 버튼을 못 찾을 때 대비용.")]
        [SerializeField] bool m_AutoAdvance;
        [SerializeField] float m_AutoDoorOpenDelay = 3f;
        [SerializeField] float m_AutoPlayerEnterDelay = 6f;

        // 오른손 컨트롤러: A = primaryButton, B = secondaryButton
        InputAction m_DoorAction;
        InputAction m_EnterAction;

        bool m_AutoDoorFired;
        bool m_AutoEnterFired;
        float m_Timer;

        void Awake()
        {
            m_DoorAction = new InputAction("DebugDoorOpen", InputActionType.Button);
            m_DoorAction.AddBinding("<XRController>{RightHand}/primaryButton");
            m_DoorAction.AddBinding("<Keyboard>/o");

            m_EnterAction = new InputAction("DebugPlayerEnter", InputActionType.Button);
            m_EnterAction.AddBinding("<XRController>{RightHand}/secondaryButton");
            m_EnterAction.AddBinding("<Keyboard>/p");
        }

        void OnEnable()
        {
            m_DoorAction.performed += _ => FireDoorOpened();
            m_EnterAction.performed += _ => FirePlayerEntered();
            m_DoorAction.Enable();
            m_EnterAction.Enable();
        }

        void OnDisable()
        {
            m_DoorAction.Disable();
            m_EnterAction.Disable();
        }

        void Start()
        {
            if (m_DeliveryMan == null)
            {
                Debug.LogError("[디버그] Delivery Man 슬롯이 비어 있다. " +
                    "인스펙터에서 DeliveryMan 오브젝트를 드래그해라.", this);
                return;
            }

            Debug.Log("[디버그] 준비 완료 — 컨트롤러 A(①) / B(②), 또는 O·P 키, " +
                      "또는 Game 뷰 버튼.", this);
        }

        void Update()
        {
            if (!m_AutoAdvance || m_DeliveryMan == null)
                return;

            m_Timer += Time.deltaTime;

            if (!m_AutoDoorFired && m_Timer >= m_AutoDoorOpenDelay)
            {
                m_AutoDoorFired = true;
                Debug.Log("[디버그] 자동 진행 — 문 열림", this);
                FireDoorOpened();
            }

            if (!m_AutoEnterFired && m_Timer >= m_AutoPlayerEnterDelay)
            {
                m_AutoEnterFired = true;
                Debug.Log("[디버그] 자동 진행 — 플레이어 진입", this);
                FirePlayerEntered();
            }
        }

        void OnGUI()
        {
            if (!m_ShowOnScreenButtons || m_DeliveryMan == null)
                return;

            var style = new GUIStyle(GUI.skin.button) { fontSize = 20 };

            GUILayout.BeginArea(new Rect(20f, 20f, 320f, 220f));

            GUILayout.Label($"상태: {m_DeliveryMan.currentState}",
                new GUIStyle(GUI.skin.label) { fontSize = 20 });

            if (GUILayout.Button("① 문 열림 (O / A버튼)", style, GUILayout.Height(50f)))
                FireDoorOpened();

            if (GUILayout.Button("② 플레이어 진입 (P / B버튼)", style, GUILayout.Height(50f)))
                FirePlayerEntered();

            GUILayout.EndArea();
        }

        void FireDoorOpened()
        {
            if (m_DeliveryMan == null)
                return;

            Debug.Log("[디버그] → OnDoorOpened() 호출", this);
            m_DeliveryMan.OnDoorOpened();
        }

        void FirePlayerEntered()
        {
            if (m_DeliveryMan == null)
                return;

            Debug.Log("[디버그] → OnPlayerEnteredElevator() 호출", this);
            m_DeliveryMan.OnPlayerEnteredElevator();
        }
    }
}
