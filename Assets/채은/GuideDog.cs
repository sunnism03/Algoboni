using System.Collections;
using UnityEngine;

public class GuideDog : MonoBehaviour
{
    [Header("목적지 (202호 앞 오브젝트)")]
    [SerializeField] private Transform targetDestination;
    [SerializeField] private float moveSpeed = 3f;

    [Header("강아지 효과음")]
    [SerializeField] private AudioSource barkAudio;

    private bool isMoving = false;
    private bool isBarkingRoutineRunning = false;

    private void Start()
    {
        // 2층 씬이 로드되고 조금 뒤에 강아지가 출발하도록 설정
        Invoke(nameof(StartGuiding), 1.5f);
    }

    private void StartGuiding()
    {
        isMoving = true;
        StartCoroutine(RepeatBarkingRoutine());
        Debug.Log("강아지가 202호 앞으로 안내를 시작합니다.");
    }

    private void Update()
    {
        if (!isMoving || targetDestination == null) return;

        // 목적지로 부드럽게 이동
        transform.position = Vector3.MoveTowards(transform.position, targetDestination.position, moveSpeed * Time.deltaTime);

        // 목적지 방향 바라보기
        Vector3 direction = (targetDestination.position - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        }

        // 도착하면 정지
        if (Vector3.Distance(transform.position, targetDestination.position) < 0.1f)
        {
            isMoving = false;
            // TODO: 여기서 가만히 서서 짖는 애니메이션 무한 반복 켜기
            Debug.Log("강아지가 202호 앞에 도착하여 유저를 기다립니다.");
        }
    }

    private IEnumerator RepeatBarkingRoutine()
    {
        isBarkingRoutineRunning = true;

        // 이 스크립트가 켜져 있는 동안 무한 반복합니다.
        while (isBarkingRoutineRunning)
        {
            if (barkAudio != null)
            {
                barkAudio.Play(); // 멍!
            }

            // 💡 정직하게 똑같은 간격으로 짖으면 기계 같으므로, 지정한 범위 내에서 랜덤한 시간만큼 쉬게 합니다.
            float randomInterval = Random.Range(0.5f, 1.5f);
            yield return new WaitForSeconds(randomInterval);
        }
    }
}