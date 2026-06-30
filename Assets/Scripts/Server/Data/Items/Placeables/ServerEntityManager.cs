using System.Collections.Generic;
using Legacy.DedicatedServer.Data.Items; // Para el ServerItemDatabase
using Legacy.DedicatedServer.Services;
using Legacy.Shared.Core;
using Legacy.Shared.Items;
using Riptide;
using UnityEngine;

namespace Legacy.DedicatedServer.Entities
{
    public class ServerEntityManager : MonoBehaviour
    {
        public static ServerEntityManager Instance { get; private set; }
        public Dictionary<ushort, ushort> entityItemIds = new Dictionary<ushort, ushort>();

        [Header("Configuración de Prefabs (Servidor)")]
        public GameObject normalBarrelPrefab;
        public GameObject explosiveBarrelPrefab;
        public GameObject lootCratePrefab;
        public GameObject airdropPrefab;
        public GameObject surprisePrefab;
        public GameObject droppedItemPrefab; // <-- AÑADIDO PARA SOLUCIONAR EL ERROR

        public Dictionary<ushort, NetworkIdentity> allEntities =
            new Dictionary<ushort, NetworkIdentity>();
        private ushort nextNetworkId = 1;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        public NetworkIdentity SpawnEntity(
            EntityType type,
            ushort itemId,
            Vector3 position,
            Quaternion rotation
        )
        {
            GameObject prefabToSpawn = null;
            switch (type)
            {
                case EntityType.NormalBarrel:
                    prefabToSpawn = normalBarrelPrefab;
                    break;
                case EntityType.ExplosiveBarrel:
                    prefabToSpawn = explosiveBarrelPrefab;
                    break;
                case EntityType.LootCrate:
                    prefabToSpawn = lootCratePrefab;
                    break;
                case EntityType.Airdrop:
                    prefabToSpawn = airdropPrefab;
                    break;
                case EntityType.SurpriseBarrel:
                    prefabToSpawn = surprisePrefab;
                    break;
                case EntityType.DroppedItem:
                    prefabToSpawn = droppedItemPrefab;
                    break; // <-- AÑADIDO
            }

            if (prefabToSpawn == null)
                return null;

            GameObject go = Instantiate(prefabToSpawn, position, rotation);
            NetworkIdentity entity = go.GetComponent<NetworkIdentity>();

            if (entity != null)
            {
                entity.ServerInitialize(nextNetworkId++, "Server_World");
                entityItemIds[entity.NetworkId] = itemId;
                BroadcastSpawnEntity(entity, itemId);
            }
            return entity;
        }

        public void RegisterEntity(NetworkIdentity entity)
        {
            if (entity != null)
                allEntities[entity.NetworkId] = entity;
        }

        public void UnregisterEntity(ushort networkId)
        {
            if (entityItemIds.ContainsKey(networkId))
                entityItemIds.Remove(networkId);
        }

        public void BroadcastSpawnEntity(NetworkIdentity identity, ushort itemId)
        {
            if (ServerServices.Network?.Server == null)
                return;
            Message msg = Message.Create(
                MessageSendMode.Reliable,
                (ushort)ServerToClientId.SpawnEntity
            );
            msg.AddUShort(identity.NetworkId);
            msg.AddUShort(itemId);
            msg.AddByte(0);
            msg.AddVector3(identity.transform.position);
            msg.AddQuaternion(identity.transform.rotation);
            ServerServices.Network.Server.SendToAll(msg);
        }

        public void BroadcastEntityHealth(ushort networkId, float currentHealth, float maxHealth)
        {
            if (ServerServices.Network?.Server == null)
                return;
            Message msg = Message.Create(
                MessageSendMode.Unreliable,
                (ushort)ServerToClientId.UpdateEntityHealth
            );
            msg.AddUShort(networkId);
            msg.AddFloat(currentHealth);
            msg.AddFloat(maxHealth);
            ServerServices.Network.Server.SendToAll(msg);
        }

        public void BroadcastEntityBreak(ushort networkId)
        {
            if (ServerServices.Network?.Server == null)
                return;
            Message msg = Message.Create(
                MessageSendMode.Reliable,
                (ushort)ServerToClientId.EntityBreak
            );
            msg.AddUShort(networkId);
            ServerServices.Network.Server.SendToAll(msg);
        }

        [MessageHandler((ushort)ClientToServerId.InteractWithEntity)]
        private static void HandlePlayerInteract(ushort fromClientId, Message message)
        {
            Vector3 origin = message.GetVector3();
            Vector3 direction = message.GetVector3();
            if (Physics.Raycast(origin, direction, out RaycastHit hit, 3.0f))
            {
                IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
                if (interactable != null)
                    interactable.Interact(fromClientId);
            }
        }

        public void SyncWorldStateToClient(ushort clientId)
        {
            if (ServerServices.Network?.Server == null)
                return;
            foreach (var kvp in allEntities)
            {
                ushort netId = kvp.Key;
                NetworkIdentity identity = kvp.Value;
                if (entityItemIds.TryGetValue(netId, out ushort itemId))
                {
                    Message msg = Message.Create(
                        MessageSendMode.Reliable,
                        (ushort)ServerToClientId.SpawnEntity
                    );
                    msg.AddUShort(netId);
                    msg.AddUShort(itemId);
                    msg.AddByte(0);
                    msg.AddVector3(identity.transform.position);
                    msg.AddQuaternion(identity.transform.rotation);
                    ServerServices.Network.Server.Send(msg, clientId);

                    if (identity.TryGetComponent(out ServerInteractableEntity interactable))
                    {
                        Message healthMsg = Message.Create(
                            MessageSendMode.Reliable,
                            (ushort)ServerToClientId.UpdateEntityHealth
                        );
                        healthMsg.AddUShort(netId);
                        healthMsg.AddFloat(interactable.Health);
                        healthMsg.AddFloat(interactable.maxHealth);
                        ServerServices.Network.Server.Send(healthMsg, clientId);
                    }
                }
            }
        }

        [MessageHandler((ushort)ClientToServerId.RequestWorldState)]
        private static void HandleRequestWorldState(ushort fromClientId, Message message)
        {
            Instance.SyncWorldStateToClient(fromClientId);
        }

        [MessageHandler((ushort)ClientToServerId.DropItem)]
        private static void HandleDropItem(ushort fromClientId, Message message)
        {
            SlotType fromType = (SlotType)message.GetByte();
            int fromSlot = message.GetInt();
            bool isFromCrate = message.GetBool();
            ushort crateId = message.GetUShort();

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
            if (player == null || player.InventoryModule == null)
                return;

            ItemDTO itemToDrop = new ItemDTO { ItemId = 0, Amount = 0 };

            if (!isFromCrate)
            {
                var list = GetPlayerListByType(player.InventoryModule, fromType);
                if (list != null && fromSlot < list.Count && list[fromSlot].ItemId != 0)
                {
                    itemToDrop = list[fromSlot];
                    list[fromSlot] = new ItemDTO { ItemId = 0, Amount = 0 };
                    player.InventoryModule.SendInventoryToClient();
                }
            }
            else
            {
                if (
                    Instance.allEntities.TryGetValue(crateId, out NetworkIdentity entity)
                    && entity.TryGetComponent(out ServerLootCrate crate)
                )
                {
                    if (fromSlot < crate.inventory.Count && crate.inventory[fromSlot].ItemId != 0)
                    {
                        itemToDrop = crate.inventory[fromSlot];
                        crate.inventory[fromSlot] = new ItemDTO { ItemId = 0, Amount = 0 };
                        crate.SendOpenLootUIToClient(fromClientId);
                    }
                }
            }

            if (itemToDrop.ItemId != 0)
            {
                Vector3 dropPos =
                    player.transform.position + (player.transform.forward * 1.5f) + Vector3.up;
                NetworkIdentity spawnedEntity = Instance.SpawnEntity(
                    EntityType.DroppedItem,
                    itemToDrop.ItemId,
                    dropPos,
                    Quaternion.identity
                );

                if (
                    spawnedEntity != null
                    && spawnedEntity.TryGetComponent(out ServerDroppedItem droppedScript)
                )
                {
                    droppedScript.InitializeDrop(itemToDrop);
                }
            }
        }

        [MessageHandler((ushort)ClientToServerId.MoveLootItem)]
        private static void HandleMoveLootItem(ushort fromClientId, Message message)
        {
            ushort crateId = message.GetUShort();
            bool fromCrate = message.GetBool();
            int fromSlot = message.GetInt();
            SlotType fromSlotType = (SlotType)message.GetByte();
            bool toCrate = message.GetBool();
            int toSlot = message.GetInt();
            SlotType toSlotType = (SlotType)message.GetByte();

            Legacy.DedicatedServer.Players.ServerPlayer player = null;
            var allPlayers = FindObjectsOfType<Legacy.DedicatedServer.Players.ServerPlayer>();
            foreach (var p in allPlayers)
            {
                if (p.ClientId == fromClientId)
                {
                    player = p;
                    break;
                }
            }
            if (player == null || player.InventoryModule == null)
                return;
            var playerInv = player.InventoryModule;

            if (toCrate)
            {
                playerInv.SendInventoryToClient();
                return;
            }

            ItemDTO itemBeingMoved = new ItemDTO { ItemId = 0, Amount = 0 };
            if (fromCrate)
            {
                if (
                    Instance.allEntities.TryGetValue(crateId, out NetworkIdentity e)
                    && e.TryGetComponent(out ServerLootCrate c)
                )
                    itemBeingMoved = c.inventory[fromSlot];
            }
            else
            {
                var sourceList = GetPlayerListByType(playerInv, fromSlotType);
                if (sourceList != null)
                    itemBeingMoved = sourceList[fromSlot];
            }

            // ANTI-CHEAT DE ARMAS
            if (itemBeingMoved.ItemId != 0 && ServerItemDatabase.Instance != null)
            {
                bool isWeapon = ServerItemDatabase.Instance.IsWeapon(itemBeingMoved.ItemId);
                if (toSlotType == SlotType.WeaponSlot && !isWeapon)
                {
                    playerInv.SendInventoryToClient();
                    return;
                }
                if (toSlotType == SlotType.HotbarSlot && isWeapon)
                {
                    playerInv.SendInventoryToClient();
                    return;
                }
            }

            if (!fromCrate && !toCrate)
            {
                List<ItemDTO> sourceList = GetPlayerListByType(playerInv, fromSlotType);
                List<ItemDTO> destList = GetPlayerListByType(playerInv, toSlotType);

                if (sourceList == null || destList == null)
                    return;
                var itemToMove = sourceList[fromSlot];
                if (itemToMove.ItemId == 0)
                    return;

                var itemInDest = destList[toSlot];
                destList[toSlot] = itemToMove;
                sourceList[fromSlot] = itemInDest;

                playerInv.SendInventoryToClient();
                return;
            }

            if (fromCrate && !toCrate)
            {
                if (!Instance.allEntities.TryGetValue(crateId, out NetworkIdentity entity))
                    return;
                if (!entity.TryGetComponent(out ServerLootCrate crate))
                    return;

                List<ItemDTO> destList = GetPlayerListByType(playerInv, toSlotType);
                if (destList == null)
                    return;
                var itemToMove = crate.inventory[fromSlot];
                if (itemToMove.ItemId == 0)
                    return;

                if (destList[toSlot].ItemId == 0)
                {
                    destList[toSlot] = itemToMove;
                    crate.inventory[fromSlot] = new ItemDTO { ItemId = 0, Amount = 0 };
                }
                playerInv.SendInventoryToClient();
                crate.SendOpenLootUIToClient(fromClientId);
            }
        }

        private static List<ItemDTO> GetPlayerListByType(
            Legacy.DedicatedServer.Players.ServerPlayerInventory inv,
            SlotType type
        )
        {
            switch (type)
            {
                case SlotType.BagSlot:
                    return inv.bagInventory;
                case SlotType.HotbarSlot:
                    return inv.hotbarInventory;
                case SlotType.WeaponSlot:
                    return inv.PrimaryAndSecondaryInventory;
                case SlotType.VestimentSlot:
                    return inv.ArmorInventory;
                default:
                    return null;
            }
        }
    }
}
