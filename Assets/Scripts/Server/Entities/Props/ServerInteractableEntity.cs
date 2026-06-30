using Legacy.DedicatedServer.Services;
using Legacy.Shared.Core;
using UnityEngine;

namespace Legacy.DedicatedServer.Entities
{
    // Hacemos la clase abstracta. Define las reglas para todas las entidades.
    public abstract class ServerInteractableEntity
        : MonoBehaviour,
            IDamageable,
            IInteractable,
            INetworkComponent
    {
        [Header("Identidad en Red")]
        public ushort networkId;
        public EntityType entityType;

        [Header("Salud y Daño")]
        public float maxHealth = 100f;
        public float currentHealth = 100f;
        public float Health => currentHealth;

        #region INTERFAZ DE RED
        public virtual void OnNetworkSpawn(ushort netId, string ownerId)
        {
            this.networkId = netId;
            this.currentHealth = this.maxHealth;
        }
        #endregion

        #region SISTEMA DE DAÑO (IDamageable)
        public virtual void TakeDamage(float damage)
        {
            currentHealth -= damage;
            ServerEntityManager.Instance.BroadcastEntityHealth(networkId, currentHealth, maxHealth);

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        // Cada hijo decidirá cómo muere (explotando, soltando loot, etc.)
        protected abstract void Die();
        #endregion

        // Cada hijo decidirá qué pasa cuando el jugador presiona la tecla de interactuar
        public abstract void Interact(ushort fromClientId);
    }
}
