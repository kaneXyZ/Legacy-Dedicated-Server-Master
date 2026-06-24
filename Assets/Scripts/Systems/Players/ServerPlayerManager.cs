// Archivo: Assets/Scripts/Systems/Players/ServerPlayerManager.cs
using System.Collections.Generic;
using Legacy.DedicatedServer.Services;
using UnityEngine;

namespace Legacy.DedicatedServer.Players
{
    public class ServerPlayerManager : MonoBehaviour
    {
        public static ServerPlayerManager Instance { get; private set; }

        [Header("Configuración")]
        [Tooltip("El prefab vacío o cápsula que representa al jugador en el servidor")]
        [SerializeField]
        private GameObject playerPrefab;

        // Diccionario para rastrear a todos los jugadores activos
        public Dictionary<ushort, ServerPlayer> players = new Dictionary<ushort, ServerPlayer>();

        private void Awake()
        {
            Instance = this;
        }

        public void SpawnPlayerInWorld(ushort clientId, string accountId)
        {
            ServerServices.Logger.LogInfo(
                LogCategory.Player,
                $"Iniciando spawn para Cliente {clientId} (UID: {accountId})."
            );

            // 1. Instanciar el objeto en el servidor
            GameObject go = Instantiate(playerPrefab);
            go.name = $"Player_{clientId}_{accountId}";

            ServerPlayer newPlayer = go.GetComponent<ServerPlayer>();
            newPlayer.Initialize(clientId, accountId);

            // 2. Avisar al NUEVO jugador de todos los jugadores que YA estaban en el servidor
            foreach (ServerPlayer existingPlayer in players.Values)
            {
                PlayerNetworkHandler.SendPlayerSpawned(existingPlayer, clientId);
            }

            // 3. Agregar al nuevo jugador a nuestra lista local
            players.Add(clientId, newPlayer);

            // 4. Avisar a TODOS (incluyendo al nuevo) que este jugador acaba de hacer spawn
            PlayerNetworkHandler.BroadcastPlayerSpawned(newPlayer);

            ServerServices.Logger.LogInfo(
                LogCategory.Player,
                $"Spawn completado para Cliente {clientId}."
            );
        }

        public void RemovePlayer(ushort clientId)
        {
            if (players.TryGetValue(clientId, out ServerPlayer playerToRemove))
            {
                // 1. Destruimos el GameObject físico del servidor
                Destroy(playerToRemove.gameObject);

                // 2. Lo sacamos del diccionario para liberar la memoria y la ID
                players.Remove(clientId);

                // 3. ¡Llamamos al Handler de Red estático para que le avise a los clientes!
                PlayerNetworkHandler.BroadcastPlayerDespawned(clientId);

                ServerServices.Logger.LogInfo(
                    LogCategory.Player,
                    $"Jugador {clientId} eliminado de la memoria y la escena del servidor."
                );
            }
        }
    }
}
