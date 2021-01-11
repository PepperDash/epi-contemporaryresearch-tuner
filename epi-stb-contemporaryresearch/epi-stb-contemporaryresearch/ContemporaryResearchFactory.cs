using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;
using PepperDash.Core;
using System.Collections.Generic;

namespace epi_stb_contemporaryresearch
{
    public class ContemporaryResearchFactory : EssentialsPluginDeviceFactory<ContemporaryResearchDevice>
    {
        public ContemporaryResearchFactory()
        {
            MinimumEssentialsFrameworkVersion = "1.6.7";

            TypeNames = new List<string> { "contemporaryresearch" };
        }

        public override EssentialsDevice BuildDevice(DeviceConfig dc)
        {
            Debug.Console(1, "Factory Attempting to create new Wattbox Device");

            var comms = CommFactory.CreateCommForDevice(dc);

            return new ContemporaryResearchDevice(dc.Key, dc.Name, comms, dc);
        }
    }
}