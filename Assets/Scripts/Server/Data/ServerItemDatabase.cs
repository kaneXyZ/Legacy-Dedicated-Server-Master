using System.Collections.Generic;
using Legacy.Shared.Items;
using UnityEngine;

namespace Legacy.DedicatedServer.Data.Items
{
    public class ServerItemDatabase : MonoBehaviour
    {
        public static ServerItemDatabase Instance { get; private set; }

        public List<ServerItemDataSO> allItems;
        private Dictionary<ushort, ServerItemDataSO> itemDict =
            new Dictionary<ushort, ServerItemDataSO>();

        private void Awake()
        {
            Instance = this;
            foreach (var item in allItems)
            {
                itemDict[item.itemId] = item;
            }
        }

        public ServerItemDataSO GetItem(ushort id)
        {
            if (itemDict.TryGetValue(id, out var data))
                return data;
            return null;
        }

        // Función clave para tu Anti-Cheat
        public bool IsWeapon(ushort id)
        {
            var item = GetItem(id);
            return item != null && item.category == ItemCategory.Weapon;
        }
    }
}
