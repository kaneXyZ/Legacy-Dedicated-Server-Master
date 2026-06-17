using UnityEngine;

namespace Legacy.DedicatedServer.Bootstrap
{
    [CreateAssetMenu(fileName = "ServerRuntimeConfig", menuName = "Legacy/Server/Runtime Config")]
    public class ServerRuntimeConfig : ScriptableObject
    {
        [Header("Network Defaults")]
        public int port = 7777;
        public int maxClientCount = 32;
        public int tickRate = 50;
        public string bindAddress = "0.0.0.0";
        public string advertisedAddress = "127.0.0.1";

        [Header("Console & Performance")]
        public string consoleTitle = "Legacy Dedicated Server";
        public bool clearConsoleOnBoot = true;
        public bool runInBackground = true;
        public bool verboseLogs = true;
        public bool showTimestamps = true;
        public bool autoStartOnAwake = true;

        [Header("Auth Client Policies")]
        public bool disconnectPreviousSessionOnDuplicateLogin = true;
        public float unauthenticatedKickDelay = 10f;
    }
}
