using UnityEngine;
using UnityEngine.Events;

namespace Soeun.Delivery
{
    /// <summary>
    /// 엘리베이터 내부 판정 영역. 플레이어(XR Origin)가 들어오면 이벤트를 쏜다.
    /// BoxCollider(Is Trigger)와 함께 사용한다.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ElevatorTriggerZone : MonoBehaviour
    {
        [Tooltip("플레이어로 인정할 레이어. XR Origin의 캐릭터 컨트롤러가 속한 레이어를 지정한다.")]
        [SerializeField] LayerMask m_PlayerLayers = ~0;

        [Tooltip("체크하면 태그도 함께 검사한다.")]
        [SerializeField] bool m_RequireTag = true;
        [SerializeField] string m_PlayerTag = "Player";

        [Tooltip("한 번만 발동시킬지 여부.")]
        [SerializeField] bool m_TriggerOnce = true;

        [Header("디버그")]
        [Tooltip("체크하면 진입 판정 결과를 Console에 출력한다.")]
        [SerializeField] bool m_VerboseLogging = true;

        [Space]
        public UnityEvent onPlayerEntered;
        public UnityEvent onPlayerExited;

        bool m_Fired;

        void Reset()
        {
            var col = GetComponent<Collider>();
            col.isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            if (m_TriggerOnce && m_Fired)
                return;

            if (!IsPlayer(other))
            {
                if (m_VerboseLogging)
                    Debug.Log($"[엘리베이터존] '{other.name}' 진입했지만 플레이어가 아님. " +
                        $"(Layer={LayerMask.LayerToName(other.gameObject.layer)}, Tag={other.tag})", this);
                return;
            }

            m_Fired = true;

            if (m_VerboseLogging)
                Debug.Log($"[엘리베이터존] 플레이어 '{other.name}' 진입 감지!", this);

            onPlayerEntered?.Invoke();
        }

        void OnTriggerExit(Collider other)
        {
            if (!IsPlayer(other))
                return;

            onPlayerExited?.Invoke();
        }

        bool IsPlayer(Collider other)
        {
            if ((m_PlayerLayers.value & (1 << other.gameObject.layer)) == 0)
                return false;

            if (m_RequireTag && !other.CompareTag(m_PlayerTag))
                return false;

            return true;
        }
    }
}
