using System.Collections.Generic;
using Newtonsoft.Json;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;

namespace PepperDash.Essentials.Plugins.ContemporaryResearch.Tuner
{
    /// <summary>Extra properties for the mock type. No <c>control</c> object is needed.</summary>
    public class ContemporaryResearchMockProperties : Properties
    {
        /// <summary>Start unreachable, so the communication monitor reports offline</summary>
        [JsonProperty("simulateOffline")]
        public bool SimulateOffline { get; set; }

        /// <summary>Channels the simulated tuner steps through, e.g. ["2-1","4-1"]</summary>
        [JsonProperty("channels")]
        public List<string> Channels { get; set; }
    }

    /// <summary>
    /// Builds the real <see cref="ContemporaryResearchDevice"/> on top of a simulated tuner, for bench testing rooms, Mobile
    /// Control and EISC bridges without hardware. Switch a device to the mock by changing its config <c>type</c> to
    /// <c>contemporaryresearchmock</c>. Not for production use.
    /// </summary>
    public class ContemporaryResearchMockFactory : EssentialsPluginDeviceFactory<ContemporaryResearchDevice>
    {
        public ContemporaryResearchMockFactory()
        {
            MinimumEssentialsFrameworkVersion = "3.0.0";

            TypeNames = new List<string> { "contemporaryresearchmock" };
        }

        public override EssentialsDevice BuildDevice(DeviceConfig dc)
        {
            var props = dc.Properties.ToObject<ContemporaryResearchMockProperties>() ?? new ContemporaryResearchMockProperties();

            var tuner = new TunerSimulator(props.unitId, props.Channels);
            var comms = new TunerSimulatorComms(dc.Key + "-comms", tuner, props.SimulateOffline);
            DeviceManager.AddDevice(comms);

            Debug.LogInformation("Creating mock Contemporary Research tuner {key}", dc.Key);
            return new ContemporaryResearchDevice(dc.Key, dc.Name, comms, dc);
        }
    }
}
