using UnityEngine;

namespace Legacy.DedicatedServer.Entities
{
    public class ServerExplosiveBarrel : ServerInteractableEntity
    {
        protected override void Die()
        {
            Debug.Log($"[Servidor] ¡BOOM! El barril {networkId} explotó.");
            // TODO: Physics.OverlapSphere para dañar jugadores cercanos

            ServerEntityManager.Instance.BroadcastEntityBreak(networkId);
            ServerEntityManager.Instance.UnregisterEntity(networkId);
            Destroy(gameObject);
        }

        public override void Interact(ushort fromClientId)
        {
            // Los barriles explosivos no se lootean. Ignoramos o mandamos mensaje al cliente.
            Debug.Log($"[Servidor] Jugador {fromClientId} tocó un barril explosivo.");
        }
    }
}
