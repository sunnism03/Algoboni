using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("층수 설정 (7층부터 1층까지 순서대로 다운)")]
    [SerializeField] private int currentFloorNumber = 7;

    [Header("엘리베이터 상태 관리")]
    public bool isMissionCleared = false;   // 미션 성공 여부
    private bool isTransitioning = false;   // 층 전환(로딩) 중인지 여부
    private bool isPlayerExitedElevator = false;

    [Header("[엘리베이터 연출 - 사운드]")]
    [SerializeField] private AudioSource elevatorBellAudio;

    [Header("[엘리베이터 연출 - 문 이동 설정]")]
    [SerializeField] private Transform slidingDoor;        // 💡 단일 문 오브젝트 (안에서 본 기준)
    [SerializeField] private float openDistance = 1.2f;    // 문이 왼쪽으로 열릴 거리 (미터 단위)
    [SerializeField] private float doorSpeed = 1.5f;       // 문이 열리고 닫히는 속도

    [Header("[층 전환 딜레이 설정]")]
    [SerializeField] private float elevatorMoveDuration = 3.0f; // 문이 닫힌 후 다음 층으로 이동하는 체감 시간 (3초)

    // 문의 원래 위치(닫힌 상태)와 열린 상태의 위치를 기억할 변수
    private Vector3 doorClosedPos;
    private Vector3 doorOpenPos;

    private Coroutine doorCoroutine; // 중복 실행 방지용 코루틴 참조
    private string currentFloorSceneName;

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

        currentFloorSceneName = $"Floor{currentFloorNumber}";

        // 💡 단일 도어 기준 초기 위치 계산
        if (slidingDoor != null)
        {
            doorClosedPos = slidingDoor.localPosition;
            // 안에서 본 기준 왼쪽으로 열려야 하므로 -X 방향으로 이동
            doorOpenPos = doorClosedPos + new Vector3(-openDistance, 0, 0);
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

    /// <summary>
    /// 단일 문을 부드러운 이징 곡선으로 보간 이동시키는 코루틴
    /// </summary>
    private IEnumerator MoveDoorsRoutine(bool isOpen)
    {
        float timeElapsed = 0f;

        // 💡 단일 도어 상태에 따른 보간 시작/목표 지점 세팅
        Vector3 doorStart = isOpen ? doorClosedPos : doorOpenPos;
        Vector3 doorTarget = isOpen ? doorOpenPos : doorClosedPos;

        while (timeElapsed < 1f)
        {
            timeElapsed += Time.deltaTime * doorSpeed;

            float easeT = timeElapsed < 0.5f 
                ? 4f * timeElapsed * timeElapsed * timeElapsed 
                : 1f - Mathf.Pow(-2f * timeElapsed + 2f, 3f) / 2f;

            // 💡 단일 도어 오브젝트의 localPosition만 부드럽게 보간
            if (slidingDoor != null) slidingDoor.localPosition = Vector3.Lerp(doorStart, doorTarget, easeT);

            yield return null;
        }

        if (slidingDoor != null) slidingDoor.localPosition = doorTarget;
    }

    public void OnPlayerExitedElevator()
    {
        if (isPlayerExitedElevator) return; 
        isPlayerExitedElevator = true;

        if (doorCoroutine != null) StopCoroutine(doorCoroutine);
        doorCoroutine = StartCoroutine(MoveDoorsRoutine(false));
    }

    public void OnPlayerEnteredElevator()
    {
        if (!isMissionCleared || isTransitioning) return;
        StartCoroutine(FloorTransitionRoutine());
    }

    public void ChangeFloor()
    {
        if (isTransitioning) return;
        StartCoroutine(FloorTransitionRoutine());
    }

    private IEnumerator FloorTransitionRoutine()
    {
        isTransitioning = true;

        // [단계 1] 엘리베이터 문이 부드럽게 스르륵 닫힙니다.
        if (doorCoroutine != null) StopCoroutine(doorCoroutine);
        yield return StartCoroutine(MoveDoorsRoutine(false)); 
        
        yield return new WaitForSeconds(0.5f); // 문이 완전히 닫힌 후 정적

        // [단계 2] 엘리베이터가 우웅~ 하고 이동하는 시간 동안 대기
        Debug.Log($"🛗 엘리베이터 이동 연출 중... ({elevatorMoveDuration}초 대기)");
        yield return new WaitForSeconds(elevatorMoveDuration);

        string oldFloorSceneName = currentFloorSceneName;

        currentFloorNumber--; // 7층 -> 6층 -> ... -> 1층 순으로 감소
        if (currentFloorNumber < 1)
        {
            currentFloorNumber = 1; 
            Debug.LogWarning("🚨 이미 최하층(1층)입니다!");
        }

        currentFloorSceneName = $"Floor{currentFloorNumber}";

        // [단계 3] 구형 층 씬 언로드
        Debug.Log($"구형 씬 언로드: {oldFloorSceneName}");
        AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(oldFloorSceneName);
        while (!unloadOp.isDone)
        {
            yield return null; 
        }

        // [단계 4] 새로운 층 씬 비동기 로드
        Debug.Log($"신형 씬 비동기 로드: {currentFloorSceneName}");
        AsyncOperation loadOp = SceneManager.LoadSceneAsync(currentFloorSceneName, LoadSceneMode.Additive);
        while (!loadOp.isDone)
        {
            yield return null; 
        }

        // [단계 5] 새로운 씬 활성화 및 리셋
        Scene nextScene = SceneManager.GetSceneByName(currentFloorSceneName);
        SceneManager.SetActiveScene(nextScene);

        isMissionCleared = false;
        isPlayerExitedElevator = false;

        yield return new WaitForSeconds(0.8f); // 씬 변경 후 안정 버퍼 시간

        // [단계 6] 도착 사운드와 함께 외도어 열기
        OpenElevatorDoor();

        isTransitioning = false;
    }
}