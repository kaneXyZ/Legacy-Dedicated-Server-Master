using Legacy.DedicatedServer.Networking;
using Legacy.Shared.Core; // Para IDamageable
using UnityEngine;

namespace Legacy.DedicatedServer.Players
{
    public class ServerPlayerHealth : MonoBehaviour, IDamageable
    {
        private ServerPlayer player;

        [Header("Stats")]
        public float maxHealth = 100f;
        public float currentHealth = 100f;
        public float currentFood = 100f;

        public bool isAlive = true;
        public bool isWounded = false;

        // Propiedad obligatoria de IDamageable
        public float Health => currentHealth;

        private float statSyncTimer = 0f;
        private const float STAT_SYNC_INTERVAL = 0.5f;

        public void Initialize(ServerPlayer core)
        {
            player = core;
            currentHealth = maxHealth;
            isAlive = true;
            isWounded = false;
        }

        private void FixedUpdate()
        {
            if (!isAlive)
                return;

            // Sincronización periódica de estadísticas
            statSyncTimer += Time.fixedDeltaTime;
            if (statSyncTimer >= STAT_SYNC_INTERVAL)
            {
                ServerPlayerNetworkHandler.SendStatsToClient(
                    player.ClientId,
                    currentHealth,
                    currentFood
                );
                statSyncTimer = 0f;
            }
        }

        // Método de la interfaz IDamageable
        public void TakeDamage(float amount)
        {
            if (!isAlive)
                return;

            currentHealth -= amount;
            if (currentHealth < 30f)
                isWounded = true;

            if (currentHealth <= 0f)
            {
                currentHealth = 0f;
                Die();
            }
            else
            {
                ServerPlayerNetworkHandler.SendStatsToClient(
                    player.ClientId,
                    currentHealth,
                    currentFood
                );
            }
        }

        public void Heal(float amount)
        {
            if (!isAlive)
                return;
            currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
            if (currentHealth >= 30f)
                isWounded = false;

            ServerPlayerNetworkHandler.SendStatsToClient(
                player.ClientId,
                currentHealth,
                currentFood
            );
        }

        private void Die()
        {
            isAlive = false;
            Debug.Log($"[ServerPlayer] El jugador {player.ClientId} ha muerto.");
            ServerPlayerNetworkHandler.SendStatsToClient(
                player.ClientId,
                currentHealth,
                currentFood
            );

            // TODO: Podrías llamar al player.Inventory.DropAllItems() aquí
        }

        // ==========================================
        public void OnNetworkSpawn(ushort netId, string ownerId)
        {
            // Como el jugador ya se inicializa a través de ServerPlayer.Initialize(),
            // podemos dejar este método vacío o usarlo para lógica futura.
            Debug.Log($"[ServerPlayerHealth] Componente de red de daño listo para ID: {netId}");
        }
    }
}
