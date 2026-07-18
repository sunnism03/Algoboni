using System.Collections.Generic;
using UnityEngine;

namespace Soeun.Delivery
{
    /// <summary>
    /// 이 오브젝트를 씬 전환 후에도 살려서 다음 씬으로 가져간다.
    /// 캔커피 프리팹에 붙이고, 플레이어가 잡는 순간 MarkAsCarried()를 호출한다.
    /// </summary>
    public class PersistentCarriedItem : MonoBehaviour
    {
        [Tooltip("아이템 고유 ID. 예: CanCoffee")]
        [SerializeField] string m_ItemId = "CanCoffee";

        [Tooltip("중복 생성을 막는다. 같은 ID가 이미 씬에 있으면 자기 자신을 파괴한다.")]
        [SerializeField] bool m_PreventDuplicates = true;

        static readonly Dictionary<string, PersistentCarriedItem> s_Instances =
            new Dictionary<string, PersistentCarriedItem>();

        public string itemId => m_ItemId;

        bool m_Carried;

        void Awake()
        {
            if (m_PreventDuplicates &&
                s_Instances.TryGetValue(m_ItemId, out var existing) &&
                existing != null && existing != this)
            {
                Destroy(gameObject);
                return;
            }

            s_Instances[m_ItemId] = this;
        }

        void OnDestroy()
        {
            if (s_Instances.TryGetValue(m_ItemId, out var existing) && existing == this)
                s_Instances.Remove(m_ItemId);
        }

        /// <summary>플레이어가 이 아이템을 처음 잡았을 때 호출.</summary>
        public void MarkAsCarried()
        {
            if (m_Carried)
                return;

            m_Carried = true;
            CarriedItemRegistry.Acquire(m_ItemId);

            // 루트 오브젝트만 DontDestroyOnLoad 대상이 될 수 있다.
            transform.SetParent(null, true);
            DontDestroyOnLoad(gameObject);
        }
    }
}
