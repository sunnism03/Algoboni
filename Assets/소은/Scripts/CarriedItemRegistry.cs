using System.Collections.Generic;

namespace Soeun.Delivery
{
    /// <summary>
    /// 씬이 바뀌어도 유지되는 "플레이어가 들고 있는 물건" 저장소.
    /// 캔커피처럼 다음 씬에서 계속 써야 하는 아이템의 소지 여부를 여기서 관리한다.
    /// 실제 오브젝트 유지는 <see cref="PersistentCarriedItem"/> 가 담당한다.
    /// </summary>
    public static class CarriedItemRegistry
    {
        static readonly HashSet<string> s_Owned = new HashSet<string>();

        /// <summary>플레이어가 해당 아이템을 획득했다고 기록한다.</summary>
        public static void Acquire(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
                return;

            s_Owned.Add(itemId);
        }

        public static bool Has(string itemId) => s_Owned.Contains(itemId);

        public static void Release(string itemId) => s_Owned.Remove(itemId);

        /// <summary>새 게임을 시작할 때 초기화.</summary>
        public static void Clear() => s_Owned.Clear();
    }
}
