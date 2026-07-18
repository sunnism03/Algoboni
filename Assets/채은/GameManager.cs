using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("현재 로드된 층 씬 이름")]
    [SerializeField] private string currentFloorSceneName = "Floor 7";

    [Header("엘리베이터 상태 관리")]
    public bool isMissionCleared = false;   // 미션 성공 여부
    private bool isTransitioning = false;   // 층 전환(로딩) 중인지 여부
    private bool isPlayerExitedElevator = false;

    [Header("[엘리베이터 연출 - 사운드]")]
    [SerializeField] private AudioSource elevatorBellAudio;

    [Header("[엘리베이터 연출 - 문 이동 설정]")]
    [SerializeField] private Transform leftDoor;          // 왼쪽 문 오브젝트
    [SerializeField] private Transform rightDoor;         // 오른쪽 문 오브젝트
    [SerializeField] private float openDistance = 1.2f;    // 문이 양옆으로 열릴 거리 (미터 단위)
    [SerializeField] private float doorSpeed = 1.5f;       // 문이 열리고 닫히는 속도

    [Header("[층 전환 딜레이 설정]")]
    [SerializeField] private float elevatorMoveDuration = 3.0f; // 💡 문이 닫힌 후 다음 층으로 이동하는 체감 시간 (3초)

    // 문의 원래 위치(닫힌 상태)와 열린 상태의 위치를 기억할 변수
    private Vector3 leftDoorClosedPos;
    private Vector3 leftDoorOpenPos;
    private Vector3 rightDoorClosedPos;
    private Vector3 rightDoorOpenPos;

    private Coroutine doorCoroutine; // 중복 실행 방지용 코루틴 참조

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (leftDoor != null && rightDoor != null)
        {
            leftDoorClosedPos = leftDoor.localPosition;
            rightDoorClosedPos = rightDoor.localPosition;

            leftDoorOpenPos = leftDoorClosedPos + new Vector3(-openDistance, 0, 0);
            rightDoorOpenPos = rightDoorClosedPos + new Vector3(openDistance, 0, 0);
        }
    }

    private void Start()
    {
        // 게임 시작 시 초기화 층 씬 로드
        StartCoroutine(LoadInitialFloor());
    }

    private IEnumerator LoadInitialFloor()
    {
        AsyncOperation loadOp = SceneManager.LoadSceneAsync(currentFloorSceneName, LoadSceneMode.Additive);
        yield return loadOp;
        
        Scene loadedScene = SceneManager.GetSceneByName(currentFloorSceneName);
        SceneManager.SetActiveScene(loadedScene);
    }

    public void CompleteMission()
    {
        if (isMissionCleared) return; 
        
        isMissionCleared = true;
        Debug.Log("🎯 [GameManager] 미션 성공 신호 수신! 엘리베이터 문을 엽니다.");

        OpenElevatorDoor();
    }

    private void OpenElevatorDoor()
    {
        if (elevatorBellAudio != null)
        {
            elevatorBellAudio.Play();
        }

        if (doorCoroutine != null) StopCoroutine(doorCoroutine);
        doorCoroutine = StartCoroutine(MoveDoorsRoutine(true));
    }

    private IEnumerator MoveDoorsRoutine(bool isOpen)
    {
        float timeElapsed = 0f;

        Vector3 leftStart = isOpen ? leftDoorClosedPos : leftDoorOpenPos;
        Vector3 leftTarget = isOpen ? leftDoorOpenPos : leftDoorClosedPos;

        Vector3 rightStart = isOpen ? rightDoorClosedPos : rightDoorOpenPos;
        Vector3 rightTarget = isOpen ? rightDoorOpenPos : rightDoorClosedPos;

        while (timeElapsed < 1f)
        {
            timeElapsed += Time.deltaTime * doorSpeed;

            float easeT = timeElapsed < 0.5f 
                ? 4f * timeElapsed * timeElapsed * timeElapsed 
                : 1f - Mathf.Pow(-2f * timeElapsed + 2f, 3f) / 2f;

            if (leftDoor != null) leftDoor.localPosition = Vector3.Lerp(leftStart, leftTarget, easeT);
            if (rightDoor != null) rightDoor.localPosition = Vector3.Lerp(rightStart, rightTarget, easeT);

            yield return null;
        }

        if (leftDoor != null) leftDoor.localPosition = leftTarget;
        if (rightDoor != null) rightDoor.localPosition = rightTarget;
    }

    public void OnPlayerExitedElevator()
    {
        if (isPlayerExitedElevator) return; 
        isPlayerExitedElevator = true;

        if (doorCoroutine != null) StopCoroutine(doorCoroutine);
        doorCoroutine = StartCoroutine(MoveDoorsRoutine(false));
    }

    public void OnPlayerEnteredElevator(string nextFloorName)
    {
        if (!isMissionCleared || isTransitioning) return;
        StartCoroutine(FloorTransitionRoutine(nextFloorName));
    }

    public void ChangeFloor(string nextFloorName)
    {
        if (isTransitioning) return;
        StartCoroutine(FloorTransitionRoutine(nextFloorName));
    }

    /// <summary>
    /// 💡 암전 없이 엘리베이터 내부 시야를 유지한 채 씬만 교체하는 정밀 루틴
    /// </summary>
    private IEnumerator FloorTransitionRoutine(string nextFloorName)
    {
        isTransitioning = true;

        // [단계 1] 엘리베이터 문이 부드럽게 스르륵 닫힙니다.
        if (doorCoroutine != null) StopCoroutine(doorCoroutine);
        yield return StartCoroutine(MoveDoorsRoutine(false)); 
        
        yield return new WaitForSeconds(0.5f); // 문이 완전히 닫힌 후 정적

        // [단계 2] 💡 엘리베이터가 우웅~ 하고 이동하는 시간 동안 대기합니다.
        // 유저는 닫힌 엘리베이터 문을 보며 대기하므로 속도감이 급작스럽지 않게 느껴집니다.
        Debug.Log($"🛗 엘리베이터 이동 연출 중... ({elevatorMoveDuration}초 대기)");
        yield return new WaitForSeconds(elevatorMoveDuration);

        // [단계 3] 문 뒤에서 조용히 구형 층 씬을 언로드합니다.
        AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(currentFloorSceneName);
        while (!unloadOp.isDone)
        {
            yield return null; 
        }

        // [단계 4] 문 뒤에서 새로운 층 씬을 비동기로 로드합니다.
        AsyncOperation loadOp = SceneManager.LoadSceneAsync(nextFloorName, LoadSceneMode.Additive);
        while (!loadOp.isDone)
        {
            yield return null; 
        }

        // [단계 5] 새로운 씬을 액티브 상태로 고정하고 데이터 상태 리셋
        currentFloorSceneName = nextFloorName;
        Scene nextScene = SceneManager.GetSceneByName(currentFloorSceneName);
        SceneManager.SetActiveScene(nextScene);

        isMissionCleared = false;
        isPlayerExitedElevator = false;

        yield return new WaitForSeconds(0.8f); // 새로운 층 도착 직전의 미세한 버퍼 시간

        // [단계 6] 도착했으므로 "띵동" 사운드와 함께 새로운 층의 문을 활짝 엽니다!
        OpenElevatorDoor();

        isTransitioning = false;
    }
}