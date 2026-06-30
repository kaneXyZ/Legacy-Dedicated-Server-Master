using Legacy.DedicatedServer.Networking;
using UnityEngine;

namespace Legacy.DedicatedServer.Players
{
    public class ServerPlayerMovement : MonoBehaviour
    {
        private ServerPlayer player;
        public CharacterController Controller;

        [Header("Físicas")]
        [SerializeField]
        private float moveSpeed = 5f;

        [SerializeField]
        private float gravity = -9.81f;

        [SerializeField]
        private float jumpHeight = 1.5f; // <-- NUEVO
        private float velocityY = 0f;

        private Vector2 latestInput;
        private Vector3 latestCamForward;
        private bool wantsToJump; // <-- NUEVO

        private void OnValidate()
        {
            if (Controller == null)
                Controller = GetComponent<CharacterController>();
        }

        public void Initialize(ServerPlayer core)
        {
            player = core;
            SetPositionRaw(new Vector3(1000f, 5, 1000f)); // Posición de spawn inicial
        }

        public void SetInputs(Vector2 inputAxis, Vector3 camForward, bool jumpPressed)
        {
            latestInput = inputAxis;
            latestCamForward = camForward;
            if (jumpPressed)
                wantsToJump = true;
        }

        private void FixedUpdate()
        {
            // Verificamos en el módulo de vida si podemos movernos
            if (!player.HealthModule.isAlive)
                return;

            // 1. Gravedad y SALTO
            if (Controller.isGrounded)
            {
                if (velocityY < 0)
                    velocityY = -2f;

                if (wantsToJump)
                {
                    // Fórmula física del salto: raíz cuadrada de (altura * -2 * gravedad)
                    velocityY = Mathf.Sqrt(jumpHeight * -2f * gravity);
                    wantsToJump = false; // Consumimos el salto
                }
            }
            velocityY += gravity * Time.fixedDeltaTime;

            // Movimiento
            latestCamForward.y = 0;
            latestCamForward.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, latestCamForward);

            Vector3 moveDirection = (latestCamForward * latestInput.y) + (right * latestInput.x);
            moveDirection *= moveSpeed;
            moveDirection.y = velocityY;

            Controller.Move(moveDirection * Time.fixedDeltaTime);

            // Caída al vacío
            if (transform.position.y < -50f)
            {
                SetPositionRaw(new Vector3(1000f, 5, 1000f));
                velocityY = 0f;
                player.HealthModule.TakeDamage(25f);
            }

            // Rotación
            Vector3 flatLookDir = latestCamForward;
            flatLookDir.y = 0;
            if (flatLookDir.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(flatLookDir);
            }

            ServerPlayerNetworkHandler.BroadcastPlayerMovement(
                player.ClientId,
                transform.position,
                transform.rotation
            );
        }

        public void SetPositionRaw(Vector3 newPos)
        {
            Controller.enabled = false;
            transform.position = newPos;
            Controller.enabled = true;
        }
    }
}
