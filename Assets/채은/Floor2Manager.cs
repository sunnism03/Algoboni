using System.Collections; // 💡 IEnumerator(코루틴) 사용을 위해 필수!
using Soeun.UI;
using UnityEngine;

public class Floor2Manager : MonoBehaviour
{
    [Header("미션 UI 출력 대기 시간 (초)")]
    [SerializeField] private float showUIDelay = 2.0f; // 💡 2초 뒤에 띄우도록 설정

    void Start()
    {
        // 씬이 시작되자마자 바로 띄우지 않고, 몇 초 후에 실행하도록 코루틴을 시작합니다.
        StartCoroutine(ShowMissionWithDelayRoutine());
    }

    /// <summary>
    /// 지정한 시간만큼 기다린 후에 미션 UI를 표시하는 코루틴
    /// </summary>
    private IEnumerator ShowMissionWithDelayRoutine()
    {
        // 인스펙터에 설정한 showUIDelay(2초)만큼 얌전히 기다립니다.
        yield return new WaitForSeconds(showUIDelay);

        // 기다림이 끝나면 미션 UI 출력!
        if (UIController.instance != null)
        {
            UIController.instance.ShowDialogue("system1");
            
        }
        else
        {
            Debug.LogWarning("🚨 UIController.instance를 찾을 수 없습니다!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {

        // 2. 부딪힌 존재가 플레이어이고, 아직 문 앞 UI를 띄운 적이 없다면 실행
        if (other.CompareTag("Player"))
        {

            if (UIController.instance != null)
            {
                UIController.instance.PlayDialogueSequence(
                    new[] { "me1", "me2", "grandma1","system2" });
            }
        }

    }
}