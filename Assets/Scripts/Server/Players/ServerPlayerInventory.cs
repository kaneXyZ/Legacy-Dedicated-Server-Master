using System.Collections.Generic;
using Legacy.DedicatedServer.Services;
using Legacy.Shared.Core;
using Legacy.Shared.Items;
using Riptide;
using UnityEngine;

namespace Legacy.DedicatedServer.Players
{
    public class ServerPlayerInventory : MonoBehaviour
    {
        public ServerPlayer Player { get; private set; }

        [Header("Configuración de Tamaños")]
        public const int BAG_SIZE = 18;
        public const int HOTBAR_SIZE = 4;
        public const int WEAPONS_SIZE = 2;
        public const int ARMOR_SIZE = 4;

        // Inventarios lógicos en memoria RAM del Servidor
        public List<ItemDTO> bagInventory;
        public List<ItemDTO> hotbarInventory;
        public List<ItemDTO> PrimaryAndSecondaryInventory;
        public List<ItemDTO> ArmorInventory;

        // ==========================================
        // INICIALIZACIÓN (Llamado por ServerPlayer.cs)
        // ==========================================
        public void Initialize(ServerPlayer core)
        {
            Player = core;

            InitializeEmptyLists();

            // Cuando el jugador se conecta e inicializa, cargamos sus datos
            LoadInventoryFromDatabase();
        }

        private void InitializeEmptyLists()
        {
            // Instanciamos las listas con su capacidad exacta para no desperdiciar RAM
            bagInventory = new List<ItemDTO>(BAG_SIZE);
            hotbarInventory = new List<ItemDTO>(HOTBAR_SIZE);
            PrimaryAndSecondaryInventory = new List<ItemDTO>(WEAPONS_SIZE);
            ArmorInventory = new List<ItemDTO>(ARMOR_SIZE);

            // Llenamos de slots vacíos (ID 0)
            for (int i = 0; i < BAG_SIZE; i++)
                bagInventory.Add(new ItemDTO { ItemId = 0, Amount = 0 });
            for (int i = 0; i < HOTBAR_SIZE; i++)
                hotbarInventory.Add(new ItemDTO { ItemId = 0, Amount = 0 });
            for (int i = 0; i < WEAPONS_SIZE; i++)
                PrimaryAndSecondaryInventory.Add(new ItemDTO { ItemId = 0, Amount = 0 });
            for (int i = 0; i < ARMOR_SIZE; i++)
                ArmorInventory.Add(new ItemDTO { ItemId = 0, Amount = 0 });
        }

        // ==========================================
        // SISTEMA DE BASE DE DATOS (SQLITE READY)
        // ==========================================
        public void LoadInventoryFromDatabase()
        {
            // TODO: Aquí conectarás SQLite.
            // Ejemplo de cómo se verá tu código futuro:
            // var dbData = DatabaseManager.Instance.GetPlayerInventory(Player.AccountId);
            // si (dbData != null) { sobreescribir las listas con dbData } else { dar loot inicial de nuevo jugador }

            // FAKE DATA TEMPORAL: Le damos una piedra inicial
            hotbarInventory[0] = new ItemDTO { ItemId = 1, Amount = 1 };

            // Después de cargar (o crear) los datos en el server, actualizamos la pantalla del cliente
            SendInventoryToClient();
        }

        public void SaveInventoryToDatabase()
        {
            if (string.IsNullOrEmpty(Player.AccountId))
                return;

            // TODO: Aquí guardarás en SQLite cuando el jugador se desconecte o haya un autoguardado.
            // Ejemplo:
            // DatabaseManager.Instance.SavePlayerInventory(Player.AccountId, bagInventory, hotbarInventory...);

            Debug.Log($"[ServerPlayerInventory] Inventario de {Player.AccountId} guardado en BD.");
        }

        // ==========================================
        // RED: ENVIAR DATOS AL CLIENTE (RIPTIDE)
        // ==========================================
        public void SendInventoryToClient()
        {
            if (ServerServices.Network?.Server == null)
                return;

            Message msg = Message.Create(
                MessageSendMode.Reliable,
                (ushort)ServerToClientId.SyncPlayerInventory
            );

            // Usamos un método auxiliar para empacar todas las listas de forma limpia
            WriteInventoryToMessage(msg, bagInventory);
            WriteInventoryToMessage(msg, hotbarInventory);
            WriteInventoryToMessage(msg, PrimaryAndSecondaryInventory);
            WriteInventoryToMessage(msg, ArmorInventory);

            ServerServices.Network.Server.Send(msg, Player.ClientId);
            Debug.Log(
                $"[ServerPlayerInventory] Inventario sincronizado al Cliente {Player.ClientId}"
            );
        }

        [MessageHandler((ushort)ClientToServerId.RequestInventorySync)]
        private static void HandleRequestInventorySync(ushort fromClientId, Message message)
        {
            var allPlayers = FindObjectsOfType<ServerPlayer>();
            foreach (var p in allPlayers)
            {
                if (p.ClientId == fromClientId && p.InventoryModule != null)
                {
                    p.InventoryModule.SendInventoryToClient();
                    break;
                }
            }
        }

        // Método auxiliar para no repetir código al empacar listas en la red
        private void WriteInventoryToMessage(Message msg, List<ItemDTO> inventoryList)
        {
            msg.AddInt(inventoryList.Count);
            for (int i = 0; i < inventoryList.Count; i++)
            {
                msg.AddUShort(inventoryList[i].ItemId);
                msg.AddInt(inventoryList[i].Amount);
                msg.AddInt(i); // El index
            }
        }
    }
}
