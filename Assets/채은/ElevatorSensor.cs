using UnityEngine;

public class ElevatorSensor : MonoBehaviour
{
    [Header("도착할 다음 층 씬 이름")]
    [SerializeField] private string targetFloorSceneName = "Scene_Floor_01"; // 7층에서 탈 때는 1층 입력

    // 무언가 이 투명한 공간 영역(Trigger) 안으로 들어왔을 때 실행되는 유니티 내장 함수
    private void OnTriggerEnter(Collider other)
    {
        // 들어온 오브젝트가 플레이어(XR Origin 등)인지 태그나 컴포넌트로 검사합니다.
        if (other.CompareTag("Player") || other.GetComponentInChildren<Camera>() != null)
        {
            // 게임 매니저에게 플레이어가 들어왔다고 통보합니다.
            GameManager.Instance.OnPlayerEnteredElevator();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // 내린 존재가 플레이어가 맞다면
        if (other.CompareTag("Player"))
        {
            // GameManager에게 플레이어가 내렸음을 알립니다.
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPlayerExitedElevator();
                
                // 💡 문이 한 번 닫힌 후에는 이 센서가 다시 켜질 일이 없으므로 오브젝트를 비활성화하거나 파괴하여 중복 작동을 막습니다.
                //gameObject.SetActive(false); 
            }
        }
    }
}