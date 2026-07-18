using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Soeun.UI
{
    /// <summary>
    /// 이미지 한 장을 띄우는 패널. 대화 UI와 미션 UI가 각각 하나씩 사용한다.
    /// 실제 호출은 UIController를 통해서만 한다.
    /// </summary>
    public class UIImagePanel : MonoBehaviour
    {
        [Header("참조")]
        [Tooltip("패널 루트. 평소에는 꺼져 있다.")]
        [SerializeField] GameObject m_Root;

        [Tooltip("이미지를 표시할 Image 컴포넌트.")]
        [SerializeField] Image m_Image;

        [Header("연출")]
        [Tooltip("나타나고 사라지는 시간(초). 0이면 즉시 전환.")]
        [SerializeField] float m_FadeDuration = 0.2f;

        [Tooltip("체크하면 스프라이트 원본 비율에 맞춰 크기를 자동 조정한다.")]
        [SerializeField] bool m_PreserveAspect = true;

        CanvasGroup m_CanvasGroup;
        HudCanvasAnchor m_Anchor;
        Coroutine m_Routine;
        Action m_OnHidden;

        public bool isShowing => m_Root != null && m_Root.activeSelf;
        public Sprite currentSprite => m_Image != null ? m_Image.sprite : null;

        void Awake()
        {
            m_Anchor = GetComponentInParent<HudCanvasAnchor>();

            if (m_Root == null)
            {
                Debug.LogError("[UI패널] Root 슬롯이 비어 있다.", this);
                return;
            }

            m_CanvasGroup = m_Root.GetComponent<CanvasGroup>();
            if (m_CanvasGroup == null)
                m_CanvasGroup = m_Root.AddComponent<CanvasGroup>();

            if (m_Image != null)
                m_Image.preserveAspect = m_PreserveAspect;

            m_CanvasGroup.alpha = 0f;
            m_Root.SetActive(false);
        }

        /// <summary>
        /// 이미지를 띄운다.
        /// duration이 0 이하면 Hide()를 부를 때까지 유지된다.
        /// </summary>
        public void Show(Sprite sprite, float duration, Action onHidden = null)
        {
            if (m_Root == null || m_Image == null)
            {
                Debug.LogError("[UI패널] Root 또는 Image 슬롯이 비어 있다.", this);
                onHidden?.Invoke();
                return;
            }

            if (sprite == null)
            {
                Debug.LogWarning("[UI패널] 스프라이트가 비어 있다.", this);
                onHidden?.Invoke();
                return;
            }

            StopRoutine();

            m_OnHidden = onHidden;
            m_Image.sprite = sprite;
            m_Image.preserveAspect = m_PreserveAspect;

            m_Root.SetActive(true);

            // 켜지는 순간 카메라 앞 제자리에 놓는다.
            m_Anchor?.SnapIntoPlace();

            m_Routine = StartCoroutine(ShowRoutine(duration));
        }

        IEnumerator ShowRoutine(float duration)
        {
            yield return Fade(1f);

            if (duration > 0f)
            {
                yield return new WaitForSeconds(duration);
                yield return Fade(0f);
                m_Root.SetActive(false);
            }

            m_Routine = null;

            var callback = m_OnHidden;
            m_OnHidden = null;
            callback?.Invoke();
        }

        public void Hide()
        {
            if (m_Root == null || !m_Root.activeSelf)
                return;

            StopRoutine();
            m_Routine = StartCoroutine(HideRoutine());
        }

        IEnumerator HideRoutine()
        {
            yield return Fade(0f);
            m_Root.SetActive(false);
            m_Routine = null;

            var callback = m_OnHidden;
            m_OnHidden = null;
            callback?.Invoke();
        }

        IEnumerator Fade(float targetAlpha)
        {
            if (m_CanvasGroup == null)
                yield break;

            if (m_FadeDuration <= 0f)
            {
                m_CanvasGroup.alpha = targetAlpha;
                yield break;
            }

            var start = m_CanvasGroup.alpha;
            var elapsed = 0f;

            while (elapsed < m_FadeDuration)
            {
                elapsed += Time.deltaTime;
                m_CanvasGroup.alpha = Mathf.Lerp(start, targetAlpha, elapsed / m_FadeDuration);
                yield return null;
            }

            m_CanvasGroup.alpha = targetAlpha;
        }

        void StopRoutine()
        {
            if (m_Routine == null)
                return;

            StopCoroutine(m_Routine);
            m_Routine = null;
        }
    }
}
