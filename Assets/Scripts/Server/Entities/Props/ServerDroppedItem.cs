using Legacy.DedicatedServer.Services;
using Legacy.Shared.Items;
using UnityEngine;

namespace Legacy.DedicatedServer.Entities
{
    public class ServerDroppedItem : ServerInteractableEntity
    {
        [Header("Datos del Ítem Tirado")]
        public ItemDTO containedItem;

        public void InitializeDrop(ItemDTO item)
        {
            containedItem = item;
            entityType = Shared.Core.EntityType.DroppedItem; // ¡Asegúrate que exista en tu Enum de Shared!
            maxHealth = 25f; // La bolsita tiene 25 de vida
            currentHealth = maxHealth;
        }

        public override void Interact(ushort fromClientId)
        {
            var allPlayers = FindObjectsOfType<Legacy.DedicatedServer.Players.ServerPlayer>();
            Legacy.DedicatedServer.Players.ServerPlayer player = null;
            foreach (var p in allPlayers)
            {
                if (p.ClientId == fromClientId)
                {
                    player = p;
                    break;
                }
            }

            if (player == null)
                return;

            // Busca un espacio vacío en la mochila
            bool pickedUp = false;
            for (int i = 0; i < player.InventoryModule.bagInventory.Count; i++)
            {
                if (player.InventoryModule.bagInventory[i].ItemId == 0)
                {
                    player.InventoryModule.bagInventory[i] = containedItem;
                    player.InventoryModule.SendInventoryToClient();
                    pickedUp = true;
                    break;
                }
            }

            if (pickedUp)
            {
                Debug.Log(
                    $"[Servidor] Jugador {fromClientId} recogió el ítem {containedItem.ItemId} del suelo."
                );
                Die(); // Se destruye la bolsa del piso
            }
        }

        protected override void Die()
        {
            ServerEntityManager.Instance.BroadcastEntityBreak(networkId);
            ServerEntityManager.Instance.UnregisterEntity(networkId);
            Destroy(gameObject);
        }
    }
}
