using EFSMono.Preloading.Stats;

namespace EFSMono.Preloading
{
    public static class Preloader
    {
        public const string StatsPath = "./Preloading/Stats/stats.json";

        public static void Preload()
        {
            StatsPreloader.Preload(StatsPath);
        }

    }
}
