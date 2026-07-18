using UnityEngine;

namespace Soeun.UI
{
    /// <summary>
    /// 캔버스를 카메라 앞 고정 위치에 붙인다. (화면 하단 HUD)
    /// 카메라를 직접 부모로 삼지 않고 매 프레임 따라가므로,
    /// 씬이 바뀌어 카메라가 교체돼도 알아서 다시 붙는다.
    /// </summary>
    [DisallowMultipleComponent]
    public class HudCanvasAnchor : MonoBehaviour
    {
        [Header("배치")]
        [Tooltip("카메라로부터의 거리(m). VR에서는 1.2~2.0m가 눈이 편하다.")]
        [SerializeField] float m_Distance = 1.5f;

        [Tooltip("눈높이 기준 아래로 내리는 정도(m). 음수가 아래쪽이다.")]
        [SerializeField] float m_VerticalOffset = -0.45f;

        [Tooltip("좌우 오프셋(m). 0이면 정중앙.")]
        [SerializeField] float m_HorizontalOffset;

        [Header("따라오기")]
        [Tooltip("0이면 머리에 완전히 고정(진짜 HUD). 값을 주면 살짝 늦게 따라와 멀미가 줄어든다.")]
        [Range(0f, 20f)]
        [SerializeField] float m_FollowSpeed = 10f;

        [Tooltip("체크하면 고개를 위아래로 들 때 UI가 따라 올라가지 않는다. (권장)")]
        [SerializeField] bool m_IgnorePitch = true;

        [Tooltip("비워두면 Camera.main 을 자동으로 찾는다.")]
        [SerializeField] Transform m_Head;

        void LateUpdate()
        {
            if (m_Head == null)
            {
                if (Camera.main == null)
                    return;

                m_Head = Camera.main.transform;
            }

            var target = DesiredPosition();
            var lookRotation = Quaternion.LookRotation(target - m_Head.position, Vector3.up);

            if (m_FollowSpeed <= 0f)
            {
                transform.SetPositionAndRotation(target, lookRotation);
                return;
            }

            var t = 1f - Mathf.Exp(-m_FollowSpeed * Time.deltaTime);
            transform.SetPositionAndRotation(
                Vector3.Lerp(transform.position, target, t),
                Quaternion.Slerp(transform.rotation, lookRotation, t));
        }

        Vector3 DesiredPosition()
        {
            var forward = m_Head.forward;
            var right = m_Head.right;

            if (m_IgnorePitch)
            {
                // 수평 성분만 사용해 고개를 들어도 UI가 하늘로 따라 올라가지 않게 한다.
                forward.y = 0f;
                right.y = 0f;

                if (forward.sqrMagnitude < 0.0001f)
                    forward = Vector3.forward;
            }

            forward.Normalize();
            right.Normalize();

            return m_Head.position
                   + forward * m_Distance
                   + right * m_HorizontalOffset
                   + Vector3.up * m_VerticalOffset;
        }

        /// <summary>즉시 제자리로 옮긴다. (켜지는 순간 날아오는 것 방지)</summary>
        public void SnapIntoPlace()
        {
            if (m_Head == null && Camera.main != null)
                m_Head = Camera.main.transform;

            if (m_Head == null)
                return;

            var target = DesiredPosition();
            transform.SetPositionAndRotation(
                target, Quaternion.LookRotation(target - m_Head.position, Vector3.up));
        }
    }
}
