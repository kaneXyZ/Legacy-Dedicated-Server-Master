using UnityEngine;

namespace Legacy.DedicatedServer.Entities
{
    public class ServerAirdrop : ServerLootCrate // ¡Magia! Puede heredar de ServerLootCrate si también guarda ítems
    {
        private bool isHacked = false;

        public override void Interact(ushort fromClientId)
        {
            if (!isHacked)
            {
                Debug.Log(
                    $"[Servidor] Jugador {fromClientId} empezó a hackear el Airdrop {networkId}. Temporizador 10 min..."
                );
                isHacked = true;
                // Iniciar corrutina de tiempo...
            }
            else
            {
                // Si ya pasaron los 10 min, ejecuta la lógica de loot normal
                base.Interact(fromClientId);
            }
        }
    }
}
