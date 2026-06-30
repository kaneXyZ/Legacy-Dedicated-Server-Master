using Legacy.Shared.Items;
using UnityEngine;

namespace Legacy.DedicatedServer.Data.Items
{
    [CreateAssetMenu(fileName = "NewWeapon", menuName = "Dedicated Server/Items/Weapon")]
    public class ServerWeaponDataSO : ServerItemDataSO // <-- ¡HEREDA DE TU BASE!
    {
        [Header("Weapon Stats (Exclusivo de Armas)")]
        public float damage = 25f;
        public float fireRate = 0.1f;
        public float range = 100f;
        [Header("Munición")]
        public int maxAmmo = 30; // Tamaño del cargador
        public ushort ammoItemId; // ID del ítem de la bala (Ej: 15 para balas 9mm)

        private void OnValidate()
        {
            // Forzamos a que, si creas un arma, su categoría siempre sea Weapon y no se pueda stackear
            category = ItemCategory.Weapon;
            isStackable = false;
            maxStack = 1;
        }
    }
}