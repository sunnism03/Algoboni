using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class Floor4Manager : MonoBehaviour
{
    [Header("--- NPC & UI ---")]
    [SerializeField] private Animator npcAnimator;
    [SerializeField] private GameObject zonePlayerEnter; // 복도 진출 감지 트리거

    [Header("--- Mission Trash ---")]
    [SerializeField] private XRGrabInteractable trashGrabInteractable;
    private Rigidbody trashRigidbody;
    private bool isPlayerInside = false;
    private bool isTrashDropped = false;
    private bool isTrackingCollision = false;

    [Header("--- 4층 사운드 ---")]
    [SerializeField] private AudioSource sfxSource; // Inspector에 AudioSource 할당
    private AudioClip elevator1;

    void Start()
    {
        elevator1 = Resources.Load<AudioClip>("Sounds/elevator1");

        if (npcAnimator != null) npcAnimator.SetTrigger("PlayDismiss");

        if (trashGrabInteractable != null)
        {
            trashRigidbody = trashGrabInteractable.GetComponent<Rigidbody>();
            trashGrabInteractable.selectExited.AddListener(OnTrashReleased);
        }

        if (zonePlayerEnter != null) zonePlayerEnter.SetActive(true);
    }

    void OnDestroy()
    {
        if (trashGrabInteractable != null)
            trashGrabInteractable.selectExited.RemoveListener(OnTrashReleased);
    }

    public void OnPlayerEnterHallway()
    {
        if (isPlayerInside) return;
        isPlayerInside = true;
        Debug.Log("[4층] 유저 복도 진입 완료. 미션 시작.");
        if (zonePlayerEnter != null) zonePlayerEnter.SetActive(false);
    }

    private void OnTrashReleased(SelectExitEventArgs args)
    {
        if (isTrashDropped || isTrackingCollision) return;
        isTrackingCollision = true;
    }

    void FixedUpdate()
    {
        if (isTrackingCollision && !isTrashDropped)
        {
            if (trashRigidbody != null && Mathf.Abs(trashRigidbody.linearVelocity.y) < 0.05f)
            {
                Collider[] hitColliders = Physics.OverlapSphere(trashRigidbody.position, 0.15f);
                foreach (var col in hitColliders)
                {
                    if (col.CompareTag("Ground"))
                    {
                        OnTrashHitGround();
                        break;
                    }
                }
            }
        }
    }

    private void OnTrashHitGround()
    {
        isTrackingCollision = false;
        isTrashDropped = true;

        // [사운드 재생] 쓰레기 버리기 성공 시 띵 소리 (2초 후 멈춤)
        if (sfxSource != null && elevator1 != null)
        {
            sfxSource.PlayOneShot(elevator1);
            StartCoroutine(StopAudioAfterDelay(sfxSource, 2.0f));
        }

        Debug.Log("[4층] 쓰레기 버리기 성공! 미션 종료.");
        // ElevatorManager.Instance.OpenDoor();
    }

    // 2초 뒤 소리 정지 코루틴
    private IEnumerator StopAudioAfterDelay(AudioSource source, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (source != null && source.isPlaying)
        {
            source.Stop();
        }
    }
}