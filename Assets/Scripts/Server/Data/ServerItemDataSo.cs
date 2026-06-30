using Legacy.Shared.Items; // Usamos tus enums compartidos
using UnityEngine;

namespace Legacy.DedicatedServer.Data.Items
{
    [CreateAssetMenu(fileName = "NewServerItem", menuName = "Dedicated Server/Items/Data")]
    public class ServerItemDataSO : ScriptableObject
    {
        [Header("Datos de Red")]
        [Tooltip("ESTE ID DEBE SER EL MISMO EN EL CLIENTE")]
        public ushort itemId;
        public ItemCategory category;

        [Header("Reglas del Servidor")]
        public bool isStackable;
        public int maxStack = 100;

        // Fíjate que quitamos 'damage' de aquí. ¡Una piedra o madera no hace daño balístico!
    }
}
