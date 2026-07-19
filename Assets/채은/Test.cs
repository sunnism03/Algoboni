using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI; 
using UnityEngine.SceneManagement;

public class Test : MonoBehaviour
{
    [System.Serializable]
    public struct FloorButtonMapping
    {
        public string floorSceneName; // 이동할 씬 이름 (예: Floor 7)
        public Button targetButton;   // 클릭할 UI 버튼
    }

    [Header("🛗 베이스가 될 엘리베이터 메인 씬 이름")]
    [SerializeField] private string elevatorSceneName = "Elevator"; // 본인의 엘리베이터 씬 이름으로 변경하세요!

    [Header("🏢 층별 버튼 매핑 리스트")]
    [SerializeField] private List<FloorButtonMapping> floorButtons = new List<FloorButtonMapping>();

    [Header("현재 테스트로 띄워진 층 (내부 추적용)")]
    [SerializeField] private string activeFloorSceneName = "";

    private void Start()
    {
        // 인스펙터에 등록된 모든 버튼에 씬 전환 이벤트를 자동으로 바인딩합니다.
        foreach (var mapping in floorButtons)
        {
            if (mapping.targetButton != null)
            {
                string sceneName = mapping.floorSceneName;
                mapping.targetButton.onClick.AddListener(() => StartActualGame(sceneName));
            }
        }
    }

    /// <summary>
    /// 버튼을 누르면 호출되는 실제 게임 진입 루틴
    /// </summary>
    public void StartActualGame(string targetFloorName)
    {
        StartCoroutine(TestToGameTransitionRoutine(targetFloorName));
    }

    private IEnumerator TestToGameTransitionRoutine(string targetFloorName)
    {
        Debug.Log($"🛠️ [Test 씬] {targetFloorName} 및 엘리베이터 씬 동시 로드를 시작합니다.");

        // 1. 베이스가 되는 엘리베이터 씬을 추가(Additive) 모드로 로드합니다.
        AsyncOperation loadElevatorOp = SceneManager.LoadSceneAsync(elevatorSceneName, LoadSceneMode.Additive);
        while (!loadElevatorOp.isDone) yield return null;
        Debug.Log($"🛗 엘리베이터 베이스 씬({elevatorSceneName}) 로드 완료.");

        // 2. 선택한 특정 복도 층 씬을 추가(Additive) 모드로 로드합니다.
        AsyncOperation loadFloorOp = SceneManager.LoadSceneAsync(targetFloorName, LoadSceneMode.Additive);
        while (!loadFloorOp.isDone) yield return null;
        Debug.Log($"🏢 복도 층 씬({targetFloorName}) 로드 완료.");

        // 3. 새로 불러온 복도 층을 액티브(Active) 씬으로 지정합니다.
        Scene nextScene = SceneManager.GetSceneByName(targetFloorName);
        if (nextScene.isLoaded)
        {
            SceneManager.SetActiveScene(nextScene);
        }

        // 4. 💡 [중요] 게임 환경이 모두 구축되었으므로, 현재 켜져 있는 'Test' 씬을 언로드(비로드)합니다.
        // 이 스크립트가 붙은 오브젝트가 포함된 씬을 안전하게 닫아줍니다.
        string currentTestSceneName = SceneManager.GetActiveScene().name; 
        
        // 만약 액티브 씬 변경 때문에 이름이 꼬이는 걸 방지하기 위해 
        // 그냥 이 스크립트(오브젝트)가 현재 물리적으로 속해 있는 씬을 타겟으로 잡습니다.
        Scene myScene = gameObject.scene; 
        
        Debug.Log($"🧹 작업 완료. 현재 테스트 씬({myScene.name})을 비로드합니다.");
        SceneManager.UnloadSceneAsync(myScene);
    }

    private void OnDestroy()
    {
        // 메모리 누수 방지용 이벤트 해제
        foreach (var mapping in floorButtons)
        {
            if (mapping.targetButton != null)
            {
                mapping.targetButton.onClick.RemoveAllListeners();
            }
        }
    }
}