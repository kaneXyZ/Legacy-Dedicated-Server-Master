// Archivo: Assets/Scripts/Systems/Players/PlayerNetworkHandler.cs
using Legacy.DedicatedServer.Services;
using Legacy.Shared.Core;
using Riptide;
using UnityEngine;

namespace Legacy.DedicatedServer.Players
{
    public static class PlayerNetworkHandler
    {
        // Envía la info de un jugador a TODOS los clientes conectados
        public static void BroadcastPlayerSpawned(ServerPlayer player)
        {
            // LOG DE DEBUG PARA EL SERVIDOR
            ServerServices.Logger.LogInfo(
                LogCategory.Network,
                $"[Server] Broadcast Spawn -> Cliente: {player.ClientId} | Posición: {player.transform.position}"
            );
            Message msg = Message.Create(MessageSendMode.Reliable, ServerToClientId.PlayerSpawned);
            msg.AddUShort(player.ClientId);
            msg.AddString(player.AccountId);
            msg.AddVector3(player.transform.position);

            if (ServerServices.Network != null && ServerServices.Network.Server != null)
            {
                ServerServices.Network.Server.SendToAll(msg);
            }
        }

        // Envía la info de un jugador a UN SOLO cliente (útil cuando alguien se conecta y necesita ver a los que ya estaban)
        public static void SendPlayerSpawned(ServerPlayer player, ushort toClientId)
        {
            // LOG DE DEBUG PARA EL SERVIDOR
            ServerServices.Logger.LogInfo(
                LogCategory.Network,
                $"[Server] Send Spawn a {toClientId} -> Cliente: {player.ClientId} | Posición: {player.transform.position}"
            );
            Message msg = Message.Create(MessageSendMode.Reliable, ServerToClientId.PlayerSpawned);
            msg.AddUShort(player.ClientId);
            msg.AddString(player.AccountId);
            msg.AddVector3(player.transform.position);

            if (ServerServices.Network != null && ServerServices.Network.Server != null)
            {
                ServerServices.Network.Server.Send(msg, toClientId);
            }
        }

        public static void BroadcastPlayerDespawned(ushort clientId)
        {
            // Creamos el mensaje con el ID 3 (o el nombre de tu Enum para despawn)
            Message msg = Message.Create(
                MessageSendMode.Reliable,
                ServerToClientId.PlayerDespawned
            );
            msg.AddUShort(clientId); // Solo necesitamos enviarle la ID del jugador que se fue

            // Lo enviamos a todos los clientes que siguen conectados
            if (ServerServices.Network != null && ServerServices.Network.Server != null)
            {
                ServerServices.Network.Server.SendToAll(msg);
            }
        }
    }
}
