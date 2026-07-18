using UnityEngine;
using UnityEngine.InputSystem;

namespace Soeun.Delivery
{
    /// <summary>
    /// 엘리베이터 담당자 작업이 끝나기 전에 에디터에서 혼자 테스트하기 위한 임시 스크립트.
    /// Game 뷰의 버튼을 누르거나 O / P 키로 각 단계를 강제 실행한다.
    /// 실제 빌드에는 넣지 않는다.
    /// </summary>
    public class DeliveryDebugTrigger : MonoBehaviour
    {
        [SerializeField] DeliveryManController m_DeliveryMan;

        [Tooltip("Game 뷰에 테스트 버튼을 표시한다. 키보드 포커스와 무관하게 동작한다.")]
        [SerializeField] bool m_ShowOnScreenButtons = true;

        bool m_WarnedNoKeyboard;

        void Start()
        {
            if (m_DeliveryMan == null)
            {
                Debug.LogError("[디버그] Delivery Man 슬롯이 비어 있다. " +
                    "인스펙터에서 DeliveryMan 오브젝트를 드래그해라.", this);
                return;
            }

            Debug.Log("[디버그] 준비 완료 — Game 뷰의 버튼을 누르거나 O/P 키를 눌러라.", this);
        }

        void Update()
        {
            if (m_DeliveryMan == null)
                return;

            var kb = Keyboard.current;
            if (kb == null)
            {
                if (!m_WarnedNoKeyboard)
                {
                    m_WarnedNoKeyboard = true;
                    Debug.LogWarning("[디버그] 키보드 장치를 찾을 수 없다. " +
                        "Game 뷰의 버튼을 사용해라.", this);
                }
                return;
            }

            if (kb.oKey.wasPressedThisFrame)
                FireDoorOpened();

            if (kb.pKey.wasPressedThisFrame)
                FirePlayerEntered();
        }

        void OnGUI()
        {
            if (!m_ShowOnScreenButtons || m_DeliveryMan == null)
                return;

            var style = new GUIStyle(GUI.skin.button) { fontSize = 20 };

            GUILayout.BeginArea(new Rect(20f, 20f, 320f, 220f));

            GUILayout.Label($"상태: {m_DeliveryMan.currentState}",
                new GUIStyle(GUI.skin.label) { fontSize = 20 });

            if (GUILayout.Button("① 문 열림 (O)", style, GUILayout.Height(50f)))
                FireDoorOpened();

            if (GUILayout.Button("② 플레이어 진입 (P)", style, GUILayout.Height(50f)))
                FirePlayerEntered();

            GUILayout.EndArea();
        }

        void FireDoorOpened()
        {
            Debug.Log("[디버그] → OnDoorOpened() 호출", this);
            m_DeliveryMan.OnDoorOpened();
        }

        void FirePlayerEntered()
        {
            Debug.Log("[디버그] → OnPlayerEnteredElevator() 호출", this);
            m_DeliveryMan.OnPlayerEnteredElevator();
        }
    }
}
