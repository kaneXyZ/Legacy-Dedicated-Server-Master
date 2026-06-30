using System.Collections.Generic;
using UnityEngine;

namespace Legacy.DedicatedServer.Data.Loot
{
    [CreateAssetMenu(fileName = "NewLootTable", menuName = "Dedicated Server/Loot/Barrel")]
    public class LootTableSO : ScriptableObject
    {
        [System.Serializable]
        public struct LootEntry
        {
            public Items.ServerItemDataSO item; // Referencia al SO del Servidor
            public int minAmount;
            public int maxAmount;

            [Range(0f, 100f)]
            public float dropChance;
        }

        public int minItemsToGenerate = 1;
        public int maxItemsToGenerate = 4;
        public List<LootEntry> possibleLoot;

        // Devuelve una lista de DTOs puros listos para enviar por red
        public List<Shared.Items.ItemDTO> GenerateLoot()
        {
            List<Shared.Items.ItemDTO> generated = new List<Shared.Items.ItemDTO>();
            int itemsToGen = Random.Range(minItemsToGenerate, maxItemsToGenerate + 1);

            for (int i = 0; i < itemsToGen; i++)
            {
                foreach (var entry in possibleLoot)
                {
                    if (Random.Range(0f, 100f) <= entry.dropChance)
                    {
                        int amount = Random.Range(entry.minAmount, entry.maxAmount + 1);

                        generated.Add(
                            new Shared.Items.ItemDTO
                            {
                                ItemId = entry.item.itemId,
                                Amount = amount,
                                Durability = 100, // Durabilidad al 100%
                            }
                        );
                        break;
                    }
                }
            }
            return generated;
        }
    }
}
