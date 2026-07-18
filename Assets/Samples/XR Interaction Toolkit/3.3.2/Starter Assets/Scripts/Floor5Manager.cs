using System.Collections;
using UnityEngine;

public class Floor5Manager : MonoBehaviour
{
    [Header("--- 1. Target References ---")]
    [SerializeField] private Animator npcAnimator;
    [SerializeField] private Animator oppositeElevatorAnimator;

    [Header("--- 2. Screen Shader Effect ---")]
    [SerializeField] private Material redShadeMaterial;
    [SerializeField] private string shaderProperty = "_Intensity";
    private float currentShaderValue = 0.0f;

    [Header("--- 3. Sound Setup ---")]
    [SerializeField] private AudioSource sfxSource;
    private AudioClip elevator1, brakingSfx, gettingAngrySfx, elevator2Sfx;

    [Header("--- 4. Timestamps ---")]
    [SerializeField] private float time_ElevatorOpen = 2.0f;
    [SerializeField] private float time_NpcStartWalk = 4.0f;
    [SerializeField] private float time_1stBlock = 7.0f;
    [SerializeField] private float time_2ndBlock = 12.0f;
    [SerializeField] private float time_ConflictResolve = 17.0f;
    [SerializeField] private float time_ReachFinalElevator = 23.0f;

    void Start()
    {
        // 1. 셰이더 초기화
        if (redShadeMaterial != null) redShadeMaterial.SetFloat(shaderProperty, 0f);

        // 2. 사운드 로드
        elevator1 = Resources.Load<AudioClip>("Sounds/elevator1");
        brakingSfx = Resources.Load<AudioClip>("Sounds/braking");
        gettingAngrySfx = Resources.Load<AudioClip>("Sounds/gettingangry");
        elevator2Sfx = Resources.Load<AudioClip>("Sounds/elevator2");

        StartCoroutine(Floor5Timeline());
    }

    private IEnumerator Floor5Timeline()
    {
        // 1. 5층 도착 및 띵 소리
        yield return new WaitForSeconds(time_ElevatorOpen);
        if (elevator1) sfxSource.PlayOneShot(elevator1);
        Debug.Log("[5층] 문 열림 연출.");

        // 2. 행인 출발
        yield return new WaitForSeconds(time_NpcStartWalk - time_ElevatorOpen);
        if (npcAnimator != null) npcAnimator.SetTrigger("WalkOut");

        // 3. 1차 막기 (끼익 소리 2번)
        yield return new WaitForSeconds(time_1stBlock - time_NpcStartWalk);
        if (npcAnimator != null) npcAnimator.SetTrigger("Block1");
        PlayBrakingSound();
        StartCoroutine(FadeShaderValue(0.5f, 1.0f));

        // 4. 2차 막기 (끼익 소리 2번)
        yield return new WaitForSeconds(time_2ndBlock - time_1stBlock);
        if (npcAnimator != null) npcAnimator.SetTrigger("Block2");
        PlayBrakingSound();
        StartCoroutine(FadeShaderValue(1.0f, 1.0f));

        // 5. 갈등 해소 (주전자 소리 2번)
        yield return new WaitForSeconds(time_ConflictResolve - time_2ndBlock);
        if (npcAnimator != null) npcAnimator.SetTrigger("FaceTurnRed");
        PlayGettingAngrySound();
        StartCoroutine(FadeShaderValue(0.0f, 2.0f));

        // 6. 최종 도착 시퀀스 (엘베 소리 5번 후 문 열림)
        yield return new WaitForSeconds(time_ReachFinalElevator - time_ConflictResolve);
        StartCoroutine(PlayElevator2Sequence());
    }

    // --- 사운드 제어 함수 ---

    public void PlayBrakingSound() => StartCoroutine(PlaySoundRepeatedly(brakingSfx, 2, 0.5f));

    public void PlayGettingAngrySound() => StartCoroutine(PlaySoundRepeatedly(gettingAngrySfx, 2, 0.5f));

    private IEnumerator PlayElevator2Sequence()
    {
        for (int i = 0; i < 5; i++)
        {
            sfxSource.PlayOneShot(elevator2Sfx);
            yield return new WaitForSeconds(0.4f);
        }
        yield return new WaitForSeconds(0.5f);
        if (oppositeElevatorAnimator != null) oppositeElevatorAnimator.SetTrigger("FloorNumberDown");
    }

    private IEnumerator PlaySoundRepeatedly(AudioClip clip, int count, float interval)
    {
        for (int i = 0; i < count; i++)
        {
            sfxSource.PlayOneShot(clip);
            yield return new WaitForSeconds(interval);
        }
    }

    // --- 셰이더 페이드 함수 ---
    private IEnumerator FadeShaderValue(float targetValue, float duration)
    {
        if (redShadeMaterial == null) yield break;
        float startValue = currentShaderValue;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            currentShaderValue = Mathf.Lerp(startValue, targetValue, elapsed / duration);
            redShadeMaterial.SetFloat(shaderProperty, Mathf.Clamp(currentShaderValue, 0.0f, 0.5f));
            yield return null;
        }
        currentShaderValue = Mathf.Clamp(targetValue, 0.0f, 0.5f);
        redShadeMaterial.SetFloat(shaderProperty, currentShaderValue);
    }
}