using UnityEngine;

namespace Legacy.DedicatedServer.Players
{
    [RequireComponent(typeof(ServerPlayerMovement))]
    [RequireComponent(typeof(ServerPlayerHealth))]
    [RequireComponent(typeof(ServerPlayerInventory))]
    [RequireComponent(typeof(ServerPlayerEquipment))] // <-- NUEVO
    public class ServerPlayer : MonoBehaviour
    {
        public ushort ClientId { get; private set; }
        public string AccountId { get; private set; }

        // Agregamos "Module" al final para evitar choques con variables antiguas
        public ServerPlayerMovement MovementModule { get; private set; }
        public ServerPlayerHealth HealthModule { get; private set; }
        public ServerPlayerInventory InventoryModule { get; private set; }
        public ServerPlayerEquipment EquipmentModule { get; private set; } // <-- NUEVO

        private void Awake()
        {
            MovementModule = GetComponent<ServerPlayerMovement>();
            HealthModule = GetComponent<ServerPlayerHealth>();
            InventoryModule = GetComponent<ServerPlayerInventory>();
            EquipmentModule = GetComponent<ServerPlayerEquipment>(); // <-- NUEVO
        }

        public void Initialize(ushort clientId, string accountId)
        {
            ClientId = clientId;
            AccountId = accountId;
            name = $"ServerPlayer_{clientId} ({accountId})";

            HealthModule.Initialize(this);
            MovementModule.Initialize(this);
            InventoryModule.Initialize(this);
            EquipmentModule.Initialize(this); // <-- NUEVO
        }
    }
}
