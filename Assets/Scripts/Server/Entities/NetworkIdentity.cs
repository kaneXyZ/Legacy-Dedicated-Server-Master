// Archivo: Assets/Scripts/Server/Entities/NetworkIdentity.cs
using Legacy.Shared.Core;
using UnityEngine;

namespace Legacy.DedicatedServer.Entities
{
    [DisallowMultipleComponent]
    public class NetworkIdentity : MonoBehaviour
    {
        public ushort NetworkId { get; private set; }
        public string OwnerId { get; private set; }

        private INetworkComponent[] _networkComponents;

        /// <summary>
        /// Llamado por el servidor cuando instancia este objeto en el mundo.
        /// </summary>
        public void ServerInitialize(ushort netId, string owner)
        {
            NetworkId = netId;
            OwnerId = owner;

            // Escaneamos este GameObject y TODOS sus hijos
            // buscando cualquier script que herede de INetworkComponent
            _networkComponents = GetComponentsInChildren<INetworkComponent>(true);

            // Le avisamos a cada componente que la entidad ya es oficial en la red
            foreach (var component in _networkComponents)
            {
                if (component != null)
                {
                    component.OnNetworkSpawn(NetworkId, OwnerId);
                }
            }

            // Registramos la identidad en el diccionario global del servidor
            if (ServerEntityManager.Instance != null)
            {
                ServerEntityManager.Instance.RegisterEntity(this);
            }
            else
            {
                Debug.LogError(
                    "[NetworkIdentity] ServerEntityManager Instance is null! Ensure it is instantiated in the scene."
                );
            }
        }

        private void OnDestroy()
        {
            if (ServerEntityManager.Instance != null)
            {
                ServerEntityManager.Instance.UnregisterEntity(NetworkId);
            }
        }
    }
}
