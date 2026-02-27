using System.Collections.Generic;

namespace InfoOverload.Readouts
{
    public static class Readouts
    {
        public static List<Readout> CurrentReadouts => SceneUtil.GetCurrent(WorldReadouts, BuildReadouts);

        public static readonly List<Readout> WorldReadouts = new List<Readout>
        {
            new ActiveCheats(),
            new RocketInfo(),
            new PlanetInfo(),
            new AtmoInfo(),
            new PartCount(),
            new ResourceCounts(),
            new MiscInfo(),
        };

        public static readonly List<Readout> BuildReadouts = new List<Readout>
        {
            new ActiveCheats(),
            new BuildInfo(),
            new SelectedPartsInfo(),
            new PartCount(),
            new ResourceCounts(),
            new MiscInfo(),
        };

        public static void RegisterSettings()
        {
            foreach (Readout function in WorldReadouts)
            {
                Settings.Settings.Register_World(function);
            }
            foreach (Readout function in BuildReadouts)
            {
                Settings.Settings.Register_Build(function);
            }
        }
    }
}