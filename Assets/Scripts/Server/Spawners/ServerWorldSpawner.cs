// Archivo: Assets/Scripts/Server/Spawners/ServerWorldSpawner.cs
using System.Collections;
using Legacy.DedicatedServer.Entities;
using Legacy.Shared.Core; // Asumo que aquí está tu Enum EntityType
using UnityEngine;

namespace Legacy.DedicatedServer.Spawners
{
    public class ServerWorldSpawner : MonoBehaviour
    {
        [System.Serializable]
        public class SpawnPointData
        {
            [Tooltip("Punto exacto (Crea un GameObject vacío como hijo y arrástralo aquí)")]
            public Transform spawnPoint;

            [Tooltip("El ID visual que el cliente debe dibujar (Ej: 50 para Barril normal)")]
            public ushort itemIDToSpawn = 50;

            [Tooltip("Qué comportamiento tendrá en el servidor")]
            public EntityType entityType = EntityType.NormalBarrel;

            // Variables ocultas para el control interno del servidor
            [HideInInspector]
            public bool isOccupied;

            [HideInInspector]
            public NetworkIdentity currentEntity;
        }

        [Header("Configuración del Radtown")]
        [Tooltip("Tiempo en segundos para que reaparezca el botín (Ej: 300 = 5 min)")]
        public float respawnTimeSeconds = 300f;
        public float gizmoRadius = 0.5f;

        [Header("Puntos de Botín / Barriles")]
        [SerializeField]
        private SpawnPointData[] spawnPoints;

        private void Start()
        {
            // Al arrancar el servidor, generamos todos los barriles del Radtown
            ExecuteSpawns();
        }

        private void ExecuteSpawns()
        {
            if (spawnPoints == null)
                return;

            foreach (var point in spawnPoints)
            {
                if (point.spawnPoint != null && !point.isOccupied)
                {
                    SpawnAtPoint(point);
                }
            }
        }

        private void SpawnAtPoint(SpawnPointData point)
        {
            Vector3 finalPosition = point.spawnPoint.position;
            // Forzamos a que el barril esté siempre derecho (rotación X y Z en 0) pero mantenga la rotación Y que le diste
            Quaternion finalRotation = Quaternion.Euler(0f, point.spawnPoint.eulerAngles.y, 0);

            // Disparamos un rayo desde 1 metro arriba del punto de spawn hacia abajo
            Vector3 rayStart = point.spawnPoint.position + (Vector3.up * 1f);

            // Si el rayo choca con algo (el piso) a menos de 5 metros de distancia
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 5f))
            {
                finalPosition = new Vector3(hit.point.x, hit.point.y, hit.point.z); // Pegamos el barril exactamente donde el rayo tocó el piso
            }
            else
            {
                Debug.LogWarning(
                    $"[Spawner] No se encontró piso debajo del punto de spawn en {finalPosition}. Revisa la altura."
                );
            }

            // Ahora sí, spawneamos con la posición corregida
            NetworkIdentity identity = ServerEntityManager.Instance.SpawnEntity(
                point.entityType,
                point.itemIDToSpawn,
                finalPosition,
                finalRotation
            );

            if (identity != null)
            {
                point.isOccupied = true;
                point.currentEntity = identity;

                // Iniciamos la vigilancia para el respawn
                StartCoroutine(MonitorSpawnPoint(point));
            }
        }

        /// <summary>
        /// Esta corrutina vigila el barril. Si el objeto es destruido (por daño o looteo),
        /// espera el tiempo designado y vuelve a hacer aparecer un barril nuevo.
        /// </summary>
        private IEnumerator MonitorSpawnPoint(SpawnPointData point)
        {
            // 1. Mientras la entidad exista (no sea null), esperamos tranquilamente
            while (point.currentEntity != null)
            {
                yield return new WaitForSeconds(2f); // Revisamos cada 2 segundos para ahorrar recursos
            }

            // 2. Si salimos del ciclo While, significa que el jugador destruyó o looteó la entidad
            point.isOccupied = false;

            // 3. Empieza el temporizador de respawn
            yield return new WaitForSeconds(respawnTimeSeconds);

            // 4. ¡Volvemos a generar el barril!
            SpawnAtPoint(point);
        }

        // Dibuja esferas de posición en la pestaña Scene para posicionarlos visualmente
        private void OnDrawGizmos()
        {
            if (spawnPoints == null)
                return;

            foreach (var point in spawnPoints)
            {
                if (point.spawnPoint != null)
                {
                    // Esfera amarilla para la posición
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireSphere(point.spawnPoint.position, gizmoRadius);

                    // Línea roja para saber hacia dónde está mirando el barril/caja
                    Gizmos.color = Color.red;
                    Gizmos.DrawRay(
                        point.spawnPoint.position,
                        point.spawnPoint.forward * gizmoRadius
                    );
                }
            }
        }
    }
}
