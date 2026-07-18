using UnityEngine;
using UnityEngine.InputSystem;

namespace Soeun.Floor3
{
    /// <summary>
    /// 3층 씬 테스트용 임시 패널.
    /// 에디터에서는 Game 뷰 버튼과 숫자키, 헤드셋에서는 오른손 A/B 버튼으로
    /// 각 단계를 건너뛰며 확인할 수 있다. 실제 빌드에는 넣지 않는다.
    /// </summary>
    public class Floor3DebugPanel : MonoBehaviour
    {
        [SerializeField] Floor3Director m_Director;
        [SerializeField] DogSearchManager m_DogSearch;

        [Tooltip("Game 뷰에 버튼을 표시한다. 헤드셋에서는 보이지만 누를 수 없다.")]
        [SerializeField] bool m_ShowOnScreenButtons = true;

        [Tooltip("씬 재생 직후 자동으로 1단계(문 열림)를 실행한다.")]
        [SerializeField] bool m_AutoStart;

        [SerializeField] float m_AutoStartDelay = 2f;

        InputAction m_Step1Action;   // A 버튼 / 1키 — 문 열림
        InputAction m_Step2Action;   // B 버튼 / 2키 — 강아지 구역 도착

        void Awake()
        {
            m_Step1Action = new InputAction("Debug_DoorOpen", InputActionType.Button);
            m_Step1Action.AddBinding("<XRController>{RightHand}/primaryButton");
            m_Step1Action.AddBinding("<Keyboard>/1");

            m_Step2Action = new InputAction("Debug_ReachDogs", InputActionType.Button);
            m_Step2Action.AddBinding("<XRController>{RightHand}/secondaryButton");
            m_Step2Action.AddBinding("<Keyboard>/2");
        }

        void OnEnable()
        {
            m_Step1Action.performed += _ => FireDoorOpened();
            m_Step2Action.performed += _ => FireReachedDogs();
            m_Step1Action.Enable();
            m_Step2Action.Enable();
        }

        void OnDisable()
        {
            m_Step1Action.Disable();
            m_Step2Action.Disable();
        }

        void Start()
        {
            if (m_Director == null)
            {
                Debug.LogError("[3층디버그] Director 슬롯이 비어 있다.", this);
                return;
            }

            Debug.Log("[3층디버그] 준비 완료 — A(또는 1): 문 열림 / B(또는 2): 강아지 구역 도착", this);

            if (m_AutoStart)
                Invoke(nameof(FireDoorOpened), m_AutoStartDelay);
        }

        void OnGUI()
        {
            if (!m_ShowOnScreenButtons || m_Director == null)
                return;

            var buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 18 };
            var labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 20 };

            GUILayout.BeginArea(new Rect(20f, 20f, 340f, 260f));

            GUILayout.Label($"단계: {m_Director.currentStage}", labelStyle);

            if (m_DogSearch != null)
                GUILayout.Label($"강아지 탐색 완료: {m_DogSearch.isFinished}", labelStyle);

            if (GUILayout.Button("① 엘리베이터 문 열림 (A / 1)", buttonStyle, GUILayout.Height(46f)))
                FireDoorOpened();

            if (GUILayout.Button("② 강아지 구역 도착 (B / 2)", buttonStyle, GUILayout.Height(46f)))
                FireReachedDogs();

            GUILayout.EndArea();
        }

        void FireDoorOpened()
        {
            if (m_Director == null)
                return;

            Debug.Log("[3층디버그] → OnElevatorDoorOpened()", this);
            m_Director.OnElevatorDoorOpened();
        }

        void FireReachedDogs()
        {
            if (m_Director == null)
                return;

            Debug.Log("[3층디버그] → OnReachedDogArea()", this);
            m_Director.OnReachedDogArea();
        }
    }
}
