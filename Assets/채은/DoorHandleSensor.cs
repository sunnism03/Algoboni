using System;
using UnityEngine;

public class DoorHandleSensor : MonoBehaviour
{
    [SerializeField] private DoorInteractable1 doorInteractable1;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    
    private void OnTriggerEnter(Collider other)
    {
        // 닿은 물체가 열쇠가 맞다면 부모 마스터 스크립트에게 알립니다!
        if (other.CompareTag("Key"))
        {
            if (doorInteractable1 != null)
            {
                // 부모의 잠금 해제 함수를 실행시키고, 부딪힌 열쇠 오브젝트를 넘겨줍니다.
                doorInteractable1.OnDoorOpen();
            }
        }
    }
}
