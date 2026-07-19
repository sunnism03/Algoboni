using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Video;
using Soeun.UI;

public class Floor7Manager : MonoBehaviour
{
    [Header("[컷씬 1] 암전 효과 UI")]
    [SerializeField] private Image fadeImage;          // FadeImage 등록
    [SerializeField] private float fadeDuration = 3.0f; // 화면이 밝아지는 데 걸리는 시간

    [Header("[연출 2] 벨소리 설정")]
    [SerializeField] private AudioSource phoneAudio;

    [Header("[컷씬 2] 상사 통화 연출 에셋")]
    [SerializeField] private AudioSource bossCallAudio; // 상사 목소리 오디오
    [SerializeField] private VideoPlayer phoneVideo;    // 휴대폰 위 AI 영상 플레이어

    [Header("[플레이] 분위기 전환 연출용")]
    [SerializeField] private GameObject officeLights;   // 문 열고 나갈 때 어둡게 바꿀 불빛들
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (fadeImage == null)
        {
            GameObject foundObject = GameObject.Find("FadeImage");
            if (foundObject != null)
            {
                fadeImage = foundObject.GetComponent<Image>();
            }
        }

        // 검증 후 연출 루틴 작동
        if (fadeImage != null)
        {
            StartCoroutine(OpeningSequenceRoutine());
        }
    }

    private IEnumerator OpeningSequenceRoutine()
    {
        // ==========================================
        // 0. 준비 단계: 플레이어 조작 잠금 및 화면 암전
        // ==========================================
        /*if (VRPlayerController.Instance != null)
        {
            VRPlayerController.Instance.SetPlayerMovement(false);
        }*/
        
        // 시작 시 화면은 완전 검은색 상태
        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 1f;
            fadeImage.color = c;
            fadeImage.gameObject.SetActive(true);
        }

        // 벨소리 울리기
        if (phoneAudio != null)
        {
            Debug.Log("🔊 [시작] 따르릉! 완전 암전 상태에서 벨소리가 먼저 시작됩니다.");
            phoneAudio.Play();
        }

        yield return new WaitForSeconds(1.0f); // 암전 상태로 1초 대기 (유저 호흡 고르기)

        // ==========================================
        // 1. Fade In 단계: 서서히 밝아짐
        // ==========================================
        Debug.Log("오프닝 시작: 화면이 서서히 밝아집니다.");
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            if (fadeImage != null)
            {
                Color c = fadeImage.color;
                // 알파 값을 1(검은색)에서 0(투명)으로 서서히 줄임
                c.a = Mathf.Lerp(1f, 0f, timer / fadeDuration);
                fadeImage.color = c;
            }
            yield return null;
        }
        
        if (fadeImage != null) fadeImage.gameObject.SetActive(false); // 완전히 밝아지면 캔버스 끄기

        yield return new WaitForSeconds(1.0f); // 밝아진 후 잠시 상황 인지 시간 주기

        UIController.instance.PlayDialogueSequence(
            new[] { "me1", "boss1", "me2", "system1" });

        // ==========================================
        // 2. 통화 단계: 상사의 불호령 & 휴대폰 AI 영상 재생
        // ==========================================
        Debug.Log("상사 통화 시작");
        if (bossCallAudio != null) bossCallAudio.Play();
        if (phoneVideo != null) phoneVideo.Play();

        // 상사 대사와 영상 오디오가 완전히 끝날 때까지 대기
        // (정확히 오디오 길이에 맞추려면 bossCallAudio.clip.length 만큼 대기해도 됩니다)
        if (bossCallAudio != null)
        {
            yield return new WaitForSeconds(bossCallAudio.clip.length);
        }
        else
        {
            yield return new WaitForSeconds(10.0f); // 오디오가 없을 때를 대비한 기본 10초 대기
        }

        Debug.Log("상사 통화 종료. 자유 행동으로 전환합니다.");

        // ==========================================
        // 3. 구현 플레이 단계: 조작 해제 및 복도 연출 준비
        // ==========================================
        /*if (VRPlayerController.Instance != null)
        {
            VRPlayerController.Instance.SetPlayerMovement(true); // 이제 유저가 문 열고 이동 가능!
        }*/

        // 사무실 내부 불을 어둡게 전환하여 복도 끝 엘리베이터로 가도록 시각적 유도
        if (officeLights != null)
        {
            officeLights.SetActive(false);
        }
        
        Debug.Log("✔ 미션 조건 충족 대기 중... 복도 끝 엘리베이터로 향하세요.");
    }
}
