namespace Legacy.DedicatedServer.Master.Dtos
{
    public class MasterServerRegisterRequest
    {
        public string id;
        public string nombre;
        public string ip;
        public int puerto;
        public string region;
        public int jugadores_actuales;
        public int jugadores_max;
        public string mapa;
        public string version;
        public string platform;
    }
}
