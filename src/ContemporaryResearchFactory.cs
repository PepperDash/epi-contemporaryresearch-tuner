using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;
using PepperDash.Core;
using System;
using System.Collections.Generic;

namespace PepperDash.Essentials.Plugins.Stb.ContemporaryResearch
{
    public class ContemporaryResearchFactory : EssentialsPluginDeviceFactory<ContemporaryResearchDevice>
    {
        public ContemporaryResearchFactory()
        {
            MinimumEssentialsFrameworkVersion = "3.0.0";

            TypeNames = new List<string> { "contemporaryresearch" };
        }

        public override EssentialsDevice BuildDevice(DeviceConfig dc)
        {
            Debug.LogDebug("Factory Attempting to create new Contemporary Research device");

            var comms = CommFactory.CreateCommForDevice(dc);
            var device = new ContemporaryResearchDevice(dc.Key, dc.Name, comms, dc);

            var listName = dc.Properties.Value<string>("presetsList");
            if (listName != null)
                device.LoadPresets(listName);

            return device;
        }
    }
}
