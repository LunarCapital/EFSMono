namespace EFSMono.Preloading.Stats
{
    public class DefaultStat
    {
        public int id { get; private set; }
        public string name { get; private set; }
        public float defaultVal { get; private set; }

        public DefaultStat(int id, string name, float defaultVal)
        {
            this.id = id;
            this.name = name;
            this.defaultVal = defaultVal;
        }
    }

}
