using UnityEngine;

namespace Legacy.DedicatedServer.Auth
{
    public abstract class ServerAuthValidator : ScriptableObject
    {
        public abstract bool TryValidate(
            string token,
            out string accountId,
            out string displayName,
            out string failureReason
        );
    }
}
