// Archivo: Assets/Scripts/Systems/Players/ServerPlayer.cs
using Legacy.DedicatedServer.Networking; // Asegúrate de importar el namespace de tu handler de red
using Legacy.Shared.Core;
using Riptide;
using UnityEngine;

namespace Legacy.DedicatedServer.Players
{
    [RequireComponent(typeof(CharacterController))]
    public class ServerPlayer : MonoBehaviour
    {
        public ushort ClientId { get; private set; }
        public string AccountId { get; private set; }

        [Header("Físicas")]
        public CharacterController Controller;

        [Header("Movimiento Servidor")]
        [SerializeField]
        private float moveSpeed = 5f;

        [SerializeField]
        private float gravity = -9.81f;
        private float velocityY = 0f;

        [Header("Estados y Stats en Tiempo Real (Debug)")]
        [SerializeField]
        private float maxHealth = 100f;

        [SerializeField]
        private float currentHealth = 100f;

        [SerializeField]
        private float currentFood = 100f;

        [SerializeField]
        private bool isAlive = true;

        [SerializeField]
        private bool isWounded = false;

        // Propiedades públicas de solo lectura
        public float Health => currentHealth;
        public float Food => currentFood;
        public bool IsAlive => isAlive;
        public bool IsWounded => isWounded;

        // Variables donde almacenamos la intención del jugador
        private Vector2 latestInput;
        private Vector3 latestCamForward;

        // Temporizador para enviar estadísticas (Ej. cada 0.5 segundos para no saturar la red, o puedes hacerlo inmediato ante un cambio)
        private float statSyncTimer = 0f;
        private const float STAT_SYNC_INTERVAL = 0.5f;

        private void OnValidate()
        {
            if (Controller == null)
                Controller = GetComponent<CharacterController>();
        }

        public void Initialize(ushort clientId, string accountId)
        {
            ClientId = clientId;
            AccountId = accountId;
            name = $"ServerPlayer_{clientId} ({accountId})";

            currentHealth = maxHealth;
            isAlive = true;
            isWounded = false;

            SetPositionRaw(new Vector3(0, 10, 0));
        }

        public void SetInputs(Vector2 inputAxis, Vector3 camForward)
        {
            latestInput = inputAxis;
            latestCamForward = camForward;
        }

        private void FixedUpdate()
        {
            // Si el jugador está muerto, no se mueve ni procesa físicas de movimiento
            if (!isAlive)
                return;

            // 1. Gravedad
            if (Controller.isGrounded && velocityY < 0)
            {
                velocityY = -2f; // Mantiene al personaje pegado al piso sin acumular fuerza infinita
            }
            velocityY += gravity * Time.fixedDeltaTime;

            // 2. Cálculo de movimiento en base a la cámara del cliente
            latestCamForward.y = 0;
            latestCamForward.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, latestCamForward);

            Vector3 moveDirection = (latestCamForward * latestInput.y) + (right * latestInput.x);
            moveDirection *= moveSpeed;
            moveDirection.y = velocityY;

            // 3. Movemos el CharacterController
            Controller.Move(moveDirection * Time.fixedDeltaTime);

            // 4. Caída al vacío de seguridad
            if (transform.position.y < -50f)
            {
                Debug.LogWarning(
                    $"[ServerPlayer] El jugador {ClientId} cayó al vacío. Reseteando posición."
                );
                SetPositionRaw(new Vector3(0, 10, 0));
                velocityY = 0f;

                // Opcional: Quitar vida por caer al vacío
                TakeDamage(25f);
            }

            // 5. Sincronización periódica de estadísticas hacia el cliente
            statSyncTimer += Time.fixedDeltaTime;
            if (statSyncTimer >= STAT_SYNC_INTERVAL)
            {
                ServerPlayerNetworkHandler.SendStatsToClient(ClientId, currentHealth, currentFood);
                statSyncTimer = 0f;
            }

            Vector3 flatLookDir = latestCamForward;
            flatLookDir.y = 0;

            if (flatLookDir.sqrMagnitude > 0.001f)
            {
                // Rotamos el cuerpo físico del servidor hacia donde el cliente está mirando
                transform.rotation = Quaternion.LookRotation(flatLookDir);
            }

            // (Opcional) Llamar aquí al Broadcast de movimiento de tu red
            ServerPlayerNetworkHandler.BroadcastPlayerMovement(
                ClientId,
                transform.position,
                transform.rotation
            );
        }

        public void TakeDamage(float amount)
        {
            if (!isAlive)
                return;

            currentHealth -= amount;

            // Actualizamos el estado de herido
            if (currentHealth < 30f)
                isWounded = true;

            if (currentHealth <= 0f)
            {
                currentHealth = 0f;
                Die();
            }
            else
            {
                // Enviamos la actualización de vida de forma inmediata si recibe daño
                ServerPlayerNetworkHandler.SendStatsToClient(ClientId, currentHealth, currentFood);
            }
        }

        public void Heal(float amount)
        {
            if (!isAlive)
                return;

            currentHealth += amount;
            if (currentHealth > maxHealth)
                currentHealth = maxHealth;

            if (currentHealth >= 30f)
                isWounded = false;

            ServerPlayerNetworkHandler.SendStatsToClient(ClientId, currentHealth, currentFood);
        }

        private void Die()
        {
            isAlive = false;
            Debug.Log($"[ServerPlayer] El jugador {ClientId} ha muerto.");

            // Enviar evento de muerte al cliente (puedes crear un método para esto)
            ServerPlayerNetworkHandler.SendStatsToClient(ClientId, currentHealth, currentFood);
        }

        private void SetPositionRaw(Vector3 newPos)
        {
            Controller.enabled = false;
            transform.position = newPos;
            Controller.enabled = true;
        }

        private void OnDrawGizmos()
        {
            // Dibuja un rayo Azul en la consola del servidor hacia donde el servidor CREE que miras
            Gizmos.color = Color.blue;

            // Usamos latestCamForward para ver exactamente la inclinación (pitch) recibida de la red
            Vector3 eyePosition = transform.position + Vector3.up * 1.6f; // Altura de los ojos aprox.
            Gizmos.DrawRay(eyePosition, latestCamForward * 5f);
            Gizmos.DrawWireSphere(eyePosition + latestCamForward * 5f, 0.2f);
        }
    }
}
