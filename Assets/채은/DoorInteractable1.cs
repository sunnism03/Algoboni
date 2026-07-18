using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables; // Unity 6 XRI 네임스페이스

public class DoorInteractable1 : MonoBehaviour
{
    [Header("문 회전 설정 (부모 축 기준)")]
    [SerializeField] private float openAngle = 90f;      // 문 끝 경첩 기준으로 열릴 각도 (안쪽은 -90f)
    [SerializeField] private float openSpeed = 2f;       // 문이 열리는 속도 (높을수록 빠름)

    //private XRSimpleInteractable simpleInteractable;
    private bool isOpened = false;
    
    private Quaternion startRotation;
    private Quaternion targetRotation;

    private void Awake()
    {
        // 💡 실제 콜라이더와 인터랙터가 달린 자식 오브젝트(문 모델)에서 컴포넌트를 찾아옵니다.
        //simpleInteractable = GetComponentInChildren<XRSimpleInteractable>();
        
        // 스크립트가 붙은 부모(Door_Pivot)의 시작 회전값과 최종 목표 회전값을 계산합니다.
        startRotation = transform.localRotation;
        targetRotation = startRotation * Quaternion.Euler(0, openAngle, 0);
    }


    public void OnDoorOpen()
    {
        if (isOpened) return; 
        isOpened = true;

        StartCoroutine(RotateDoorRoutine());
    }

    /// <summary>
    /// 이징(Ease-In-Out) 곡선이 적용된 부드러운 경첩 회전 코루틴
    /// </summary>
    private IEnumerator RotateDoorRoutine()
    {
        float timeElapsed = 0f;

        while (timeElapsed < 1f)
        {
            timeElapsed += Time.deltaTime * openSpeed;
            
            // 📈 Ease-In-Out Cubic 곡선 수학식 (처음과 끝에 부드럽게 감속)
            float easeT = timeElapsed < 0.5f 
                ? 4f * timeElapsed * timeElapsed * timeElapsed 
                : 1f - Mathf.Pow(-2f * timeElapsed + 2f, 3f) / 2f;

            // 자식 메쉬가 아닌 이 스크립트가 붙은 부모(Pivot) 자체를 회전시킵니다.
            transform.localRotation = Quaternion.Slerp(startRotation, targetRotation, easeT);
            yield return null;
        }

        transform.localRotation = targetRotation; // 미세 오차 보정 고정
        Debug.Log("✔ 문 경첩 축을 기준으로 완벽하게 열렸습니다.");
    }

    // --- VR 헤드셋 없을 때 에디터 컴포넌트 우클릭으로 강제 테스트용 ---
    [ContextMenu("★ 테스트: 문 강제로 열기")]
    public void TestOpenDoor()
    {
        OnDoorOpen();
    }
}