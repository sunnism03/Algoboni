using System.Collections;
using UnityEngine;

public class Floor6Manager : MonoBehaviour
{
    private enum MissionState { Ready, NpcBoarding, NpcIdleWaiting, NpcPushed, UmbrellaGrabbed, WaitForReturn, MissionComplete }
    private MissionState currentState = MissionState.Ready;
    private Coroutine threatCoroutine;

    [Header("--- 1. Animator References ---")]
    [SerializeField] private Animator npcAnimator;
    [SerializeField] private Animator umbrellaAnimator;
    [SerializeField] private Animator panelAnimator;

    [Header("--- 2. Sound Setup ---")]
    [SerializeField] private AudioSource sfxSource;  // 단발 재생 (띵, 우산 펼쳐짐)
    [SerializeField] private AudioSource loopSource; // 반복 재생 (우산 휘적거림)
    private AudioClip elevator1, umbrella1, umbrella2;

    [Header("--- 3. XR Interaction & Player ---")]
    [SerializeField] private GameObject npcPushTarget;
    [SerializeField] private GameObject umbrellaGrabTarget;
    [SerializeField] private Transform xrOriginTransform;
    [SerializeField] private Transform npcTransform;

    void Start()
    {
        // 사운드 로드
        elevator1 = Resources.Load<AudioClip>("Sounds/elevator1");
        umbrella1 = Resources.Load<AudioClip>("Sounds/umbrella1");
        umbrella2 = Resources.Load<AudioClip>("Sounds/umbrella2");

        if (npcPushTarget != null) npcPushTarget.SetActive(false);
        if (umbrellaGrabTarget != null) umbrellaGrabTarget.SetActive(false);

        StartCoroutine(StartFloor6Mission());
    }

    private IEnumerator StartFloor6Mission()
    {
        currentState = MissionState.NpcBoarding;
        CallExternal_ElevatorOpen();

        // 띵 소리 재생
        if (elevator1) sfxSource.PlayOneShot(elevator1);

        yield return new WaitForSeconds(2.0f);

        if (npcAnimator != null) npcAnimator.SetTrigger("WalkInWithUmbrella");
        yield return new WaitForSeconds(3.0f);

        currentState = MissionState.NpcIdleWaiting;
        if (npcAnimator != null) npcAnimator.SetTrigger("NpcIdle");
        if (npcPushTarget != null) npcPushTarget.SetActive(true);
    }

    public void OnNpcPushed()
    {
        if (currentState != MissionState.NpcIdleWaiting) return;
        currentState = MissionState.NpcPushed;

        if (npcTransform != null && xrOriginTransform != null)
        {
            npcTransform.SetParent(xrOriginTransform);
            npcTransform.localPosition = new Vector3(0.4f, 0, 0.8f);
            npcTransform.localRotation = Quaternion.Euler(0, 180, 0);
        }

        if (npcPushTarget != null) npcPushTarget.SetActive(false);
        if (npcAnimator != null) npcAnimator.SetTrigger("DraggedOut");

        CallExternal_ElevatorClose();
        StartCoroutine(OutsideUmbrellaSequence());
    }

    private IEnumerator OutsideUmbrellaSequence()
    {
        yield return new WaitForSeconds(3.0f);
        if (npcTransform != null) npcTransform.SetParent(null);

        if (npcAnimator != null) npcAnimator.SetTrigger("PressPanelWithUmbrella");

        yield return new WaitForSeconds(1.2f);
        if (panelAnimator != null) panelAnimator.SetTrigger("OpenDoor");

        yield return new WaitForSeconds(1.5f);

        // 우산 휘적거리는 소리 루프 시작
        PlayUmbrellaLoop(true);

        threatCoroutine = StartCoroutine(PlayRandomThreatAnimation());
        if (umbrellaGrabTarget != null) umbrellaGrabTarget.SetActive(true);
    }

    private void PlayUmbrellaLoop(bool isPlaying)
    {
        if (isPlaying)
        {
            loopSource.clip = umbrella2;
            loopSource.loop = true;
            loopSource.Play();
        }
        else
        {
            loopSource.Stop();
        }
    }

    private IEnumerator PlayRandomThreatAnimation()
    {
        string[] threatTriggers = { "Threat_Slow", "Threat_Medium", "Threat_Fast" };
        while (currentState == MissionState.NpcPushed)
        {
            int randomIndex = Random.Range(0, threatTriggers.Length);
            if (npcAnimator != null) npcAnimator.SetTrigger(threatTriggers[randomIndex]);
            yield return new WaitForSeconds(2.0f);
        }
    }

    public void OnUmbrellaGrabbed()
    {
        if (currentState != MissionState.NpcPushed) return;
        currentState = MissionState.UmbrellaGrabbed;

        // 우산 소리 루프 정지
        PlayUmbrellaLoop(false);

        if (threatCoroutine != null) StopCoroutine(threatCoroutine);
        if (umbrellaGrabTarget != null) umbrellaGrabTarget.SetActive(false);

        // 우산 펼쳐지는 소리
        sfxSource.PlayOneShot(umbrella1);

        if (umbrellaAnimator != null) umbrellaAnimator.SetTrigger("PopOpen");
        if (npcAnimator != null) npcAnimator.SetTrigger("UmbrellaPoppedReaction");

        StartCoroutine(ReBoardingSequence());
    }

    private IEnumerator ReBoardingSequence()
    {
        yield return new WaitForSeconds(4.0f);
        currentState = MissionState.WaitForReturn;

        if (elevator1) sfxSource.PlayOneShot(elevator1);

        CallExternal_ElevatorOpen();

        yield return new WaitForSeconds(3.0f);

        if (npcAnimator != null) npcAnimator.SetTrigger("NormalBoarding");
        yield return new WaitForSeconds(2.0f);

        currentState = MissionState.MissionComplete;
        CallExternal_ElevatorClose();
    }

    #region --- External Elevator Call Methods ---
    private void CallExternal_ElevatorOpen() { Debug.Log("[6층] 문 열림"); }
    private void CallExternal_ElevatorClose() { Debug.Log("[6층] 문 닫힘"); }
    #endregion
}