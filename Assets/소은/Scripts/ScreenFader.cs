using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Soeun.Floor3
{
    /// <summary>
    /// VR용 화면 페이드. 카메라 바로 앞에 검은 판을 붙여서 알파를 조절한다.
    /// 씬에 빈 오브젝트 하나 만들어서 붙여두면 나머지는 자동으로 생성된다.
    /// </summary>
    public class ScreenFader : MonoBehaviour
    {
        [Tooltip("비워두면 Camera.main 을 사용한다.")]
        [SerializeField] Camera m_TargetCamera;

        [Tooltip("페이드 판을 카메라 앞 몇 미터에 둘지. 너무 멀면 다른 물체가 뚫고 나온다.")]
        [SerializeField] float m_PlaneDistance = 0.25f;

        [SerializeField] Color m_FadeColor = Color.black;

        static ScreenFader s_Instance;
        public static ScreenFader instance => s_Instance;

        Image m_Image;
        Coroutine m_Routine;

        void Awake()
        {
            if (s_Instance != null && s_Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            s_Instance = this;
            BuildOverlay();
        }

        void OnDestroy()
        {
            if (s_Instance == this)
                s_Instance = null;
        }

        void BuildOverlay()
        {
            if (m_TargetCamera == null)
                m_TargetCamera = Camera.main;

            if (m_TargetCamera == null)
            {
                Debug.LogError("[페이더] 카메라를 찾을 수 없다. XR Origin의 카메라 Tag가 MainCamera인지 확인해라.", this);
                return;
            }

            var canvasGo = new GameObject("FadeCanvas");
            canvasGo.transform.SetParent(m_TargetCamera.transform, false);
            canvasGo.transform.localPosition = new Vector3(0f, 0f, m_PlaneDistance);
            canvasGo.transform.localRotation = Quaternion.identity;

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            // 다른 월드 UI보다 항상 앞에 오도록.
            canvas.sortingOrder = 32767;

            var rect = canvas.GetComponent<RectTransform>();
            // 카메라와 매우 가까우므로 2m x 2m 이면 시야를 충분히 덮는다.
            rect.sizeDelta = new Vector2(2f, 2f);
            rect.localScale = Vector3.one;

            var imageGo = new GameObject("FadeImage");
            imageGo.transform.SetParent(canvasGo.transform, false);

            m_Image = imageGo.AddComponent<Image>();
            m_Image.color = new Color(m_FadeColor.r, m_FadeColor.g, m_FadeColor.b, 0f);
            m_Image.raycastTarget = false;

            var imageRect = m_Image.GetComponent<RectTransform>();
            imageRect.anchorMin = Vector2.zero;
            imageRect.anchorMax = Vector2.one;
            imageRect.sizeDelta = Vector2.zero;

            SetAlpha(0f);
        }

        void SetAlpha(float alpha)
        {
            if (m_Image == null)
                return;

            var c = m_Image.color;
            c.a = Mathf.Clamp01(alpha);
            m_Image.color = c;
            // 완전히 투명하면 렌더링 자체를 끈다.
            m_Image.enabled = c.a > 0.001f;
        }

        /// <summary>검은 화면으로 전환.</summary>
        public void FadeOut(float duration, Action onComplete = null) =>
            StartFade(1f, duration, onComplete);

        /// <summary>화면을 다시 보이게.</summary>
        public void FadeIn(float duration, Action onComplete = null) =>
            StartFade(0f, duration, onComplete);

        void StartFade(float targetAlpha, float duration, Action onComplete)
        {
            if (m_Image == null)
            {
                onComplete?.Invoke();
                return;
            }

            if (m_Routine != null)
                StopCoroutine(m_Routine);

            m_Routine = StartCoroutine(FadeRoutine(targetAlpha, duration, onComplete));
        }

        IEnumerator FadeRoutine(float targetAlpha, float duration, Action onComplete)
        {
            var startAlpha = m_Image.color.a;

            if (duration <= 0f)
            {
                SetAlpha(targetAlpha);
            }
            else
            {
                var elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    SetAlpha(Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration));
                    yield return null;
                }
                SetAlpha(targetAlpha);
            }

            m_Routine = null;
            onComplete?.Invoke();
        }
    }
}
