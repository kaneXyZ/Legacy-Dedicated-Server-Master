using System.Collections.Generic;
using Legacy.DedicatedServer.Data.Loot;
using Legacy.DedicatedServer.Services;
using Legacy.Shared.Core;
using Legacy.Shared.Items;
using Riptide;
using UnityEngine;

namespace Legacy.DedicatedServer.Entities
{
    public class ServerLootCrate : ServerInteractableEntity
    {
        [Header("Sistema de Botín")]
        public LootTableSO lootTable;
        private bool hasGeneratedLoot = false;

        public List<ItemDTO> inventory = new List<ItemDTO>();

        // Responde a la función Die() obligatoria del padre
        protected override void Die()
        {
            if (inventory.Count > 0 || (!hasGeneratedLoot && lootTable != null))
            {
                Debug.Log($"[Servidor] La caja {networkId} se rompió y soltó loot al piso.");
                // Instanciar modelos 3D en el piso...
            }

            ServerEntityManager.Instance.BroadcastEntityBreak(networkId);
            ServerEntityManager.Instance.UnregisterEntity(networkId);
            Destroy(gameObject);
        }

        // Responde a la interacción del jugador
        public override void Interact(ushort fromClientId)
        {
            if (!hasGeneratedLoot && lootTable != null)
            {
                inventory = lootTable.GenerateLoot();
                hasGeneratedLoot = true;
            }

            if (inventory.Count == 0)
            {
                Die();
                return;
            }

            SendOpenLootUIToClient(fromClientId);
        }

        public void SendOpenLootUIToClient(ushort clientId)
        {
            if (ServerServices.Network?.Server == null)
                return;

            Message msg = Message.Create(
                MessageSendMode.Reliable,
                (ushort)ServerToClientId.OpenLootUI
            );
            msg.AddUShort(networkId);
            msg.AddInt(inventory.Count);

            foreach (var item in inventory)
            {
                msg.AddUShort(item.ItemId);
                msg.AddInt(item.Amount);
            }

            ServerServices.Network.Server.Send(msg, clientId);
        }
    }
}
