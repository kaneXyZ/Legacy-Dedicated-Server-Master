using Legacy.DedicatedServer.Data.Items;
using Legacy.DedicatedServer.Networking;
using Legacy.Shared.Items;
using Riptide;
using UnityEngine;

namespace Legacy.DedicatedServer.Players
{
    public class ServerPlayerEquipment : MonoBehaviour
    {
        private ServerPlayer player;
        public ServerWeaponDataSO activeWeapon = null;
        public int currentWeaponSlot = -1;
        private float lastFireTime = 0f;

        public void Initialize(ServerPlayer core)
        {
            player = core;
            UnequipWeapon();
        }

        public void SetActiveWeaponSlot(int slotIndex)
        {
            Debug.Log(
                $"<color=yellow>[SERVER-EQUIPO]</color> Jugador {player.ClientId} solicitó equipar el slot {slotIndex}."
            );

            if (slotIndex < 0 || slotIndex > 1)
            {
                Debug.Log(
                    $"<color=yellow>[SERVER-EQUIPO]</color> Desequipando arma del jugador {player.ClientId}."
                );
                UnequipWeapon();
                return;
            }

            ItemDTO itemDTO = player.InventoryModule.PrimaryAndSecondaryInventory[slotIndex];

            if (itemDTO.ItemId == 0)
            {
                Debug.LogWarning(
                    $"<color=orange>[SERVER-EQUIPO]</color> Jugador {player.ClientId} intentó sacar un arma, pero el servidor dice que su slot está VACÍO."
                );
                UnequipWeapon();
                return;
            }

            var itemData = ServerItemDatabase.Instance.GetItem(itemDTO.ItemId);
            if (itemData is ServerWeaponDataSO weaponData)
            {
                activeWeapon = weaponData;
                currentWeaponSlot = slotIndex;
                Debug.Log(
                    $"<color=green>[SERVER-EQUIPO]</color> ¡APROBADO! Jugador {player.ClientId} sacó un(a) {activeWeapon.name} (Daño: {activeWeapon.damage})"
                );

                // Opcional: Mandar broadcast para que los demás vean su arma en 3ra persona
            }
            else
            {
                Debug.LogWarning(
                    $"<color=red>[SERVER-EQUIPO]</color> Jugador {player.ClientId} intentó equipar el ID {itemDTO.ItemId}, pero NO es un ServerWeaponDataSO."
                );
            }
        }

        private void UnequipWeapon()
        {
            activeWeapon = null;
            currentWeaponSlot = -1;
        }

        public void FireWeapon(Vector3 origin, Vector3 direction)
        {
            if (activeWeapon == null || !player.HealthModule.isAlive)
                return;
            if (Time.time < lastFireTime + activeWeapon.fireRate)
                return;

            // 1. VERIFICAR Y GASTAR MUNICIÓN
            ItemDTO currentWeaponStruct = player.InventoryModule.PrimaryAndSecondaryInventory[
                currentWeaponSlot
            ];

            if (currentWeaponStruct.AmmoLoaded <= 0)
            {
                Debug.Log(
                    $"<color=yellow>[SERVER-COMBATE]</color> CLICK! El arma de {player.ClientId} no tiene balas."
                );
                return; // Arma vacía, no dispara
            }

            // Restamos 1 bala. OJO: Como es un struct, debemos reasignarlo al array para que se guarde.
            currentWeaponStruct.AmmoLoaded--;
            player.InventoryModule.PrimaryAndSecondaryInventory[currentWeaponSlot] =
                currentWeaponStruct;
            player.InventoryModule.SendInventoryToClient(); // Sincronizamos la UI del cliente

            lastFireTime = Time.time;

            // 2. DISPARO REAL
            if (Physics.Raycast(origin, direction, out RaycastHit hit, activeWeapon.range))
            {
                var damageable =
                    hit.collider.GetComponentInParent<Legacy.Shared.Core.IDamageable>();
                if (damageable != null)
                    damageable.TakeDamage(activeWeapon.damage);
            }
        }

        // ==========================================
        // SISTEMA DE RECARGA
        // ==========================================
        public void ReloadWeapon()
        {
            if (activeWeapon == null || !player.HealthModule.isAlive)
                return;

            ItemDTO weaponStruct = player.InventoryModule.PrimaryAndSecondaryInventory[
                currentWeaponSlot
            ];

            // ¿Cuántas balas le faltan al cargador para estar lleno?
            int bulletsNeeded = activeWeapon.maxAmmo - weaponStruct.AmmoLoaded;
            if (bulletsNeeded <= 0)
                return; // Ya está lleno

            int bulletsFound = 0;
            var bag = player.InventoryModule.bagInventory;

            // Buscamos en la mochila el ítem de munición correspondiente
            for (int i = 0; i < bag.Count; i++)
            {
                if (bag[i].ItemId == activeWeapon.ammoItemId)
                {
                    int takeAmount = Mathf.Min(bulletsNeeded - bulletsFound, bag[i].Amount);

                    // Restamos las balas del stack de la mochila
                    ItemDTO ammoStack = bag[i];
                    ammoStack.Amount -= takeAmount;

                    // Si el stack se vació, lo borramos (ID = 0)
                    if (ammoStack.Amount <= 0)
                        ammoStack = new ItemDTO { ItemId = 0, Amount = 0 };

                    bag[i] = ammoStack; // Guardamos el cambio en la mochila
                    bulletsFound += takeAmount;

                    if (bulletsFound >= bulletsNeeded)
                        break; // Ya llenamos el cargador
                }
            }

            if (bulletsFound > 0)
            {
                weaponStruct.AmmoLoaded += (ushort)bulletsFound;
                player.InventoryModule.PrimaryAndSecondaryInventory[currentWeaponSlot] =
                    weaponStruct;
                player.InventoryModule.SendInventoryToClient();
                Debug.Log(
                    $"<color=cyan>[SERVER-RECARGA]</color> Jugador {player.ClientId} recargó {bulletsFound} balas."
                );

                // TODO: Enviar broadcast Riptide para que los demás escuchen el sonido de recarga
            }
            else
            {
                Debug.Log(
                    $"<color=orange>[SERVER-RECARGA]</color> Jugador {player.ClientId} no tiene balas compatibles (ID: {activeWeapon.ammoItemId}) en la mochila."
                );
            }
        }
    }
}
