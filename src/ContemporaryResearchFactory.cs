using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;
using PepperDash.Core;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace epi_stb_contemporaryresearch
{
    public class ContemporaryResearchFactory : EssentialsPluginDeviceFactory<ContemporaryResearchDevice>
    {
        public ContemporaryResearchFactory()
        {
            MinimumEssentialsFrameworkVersion = "1.7.6";

            TypeNames = new List<string> { "contemporaryresearch" };
        }

        public override EssentialsDevice BuildDevice(DeviceConfig dc)
        {
            Debug.Console(1, "Factory Attempting to create new Contemporary Research device");

            var comms = CreateCommForDeviceCompat(dc);
            var device = new ContemporaryResearchDevice(dc.Key, dc.Name, comms, dc);

            var listName = dc.Properties.Value<string>("presetsList");
            if (listName != null)
                device.LoadPresets(listName);

            return device;
        }

        private static IBasicCommunication CreateCommForDeviceCompat(DeviceConfig dc)
        {
            var method = typeof(CommFactory).GetMethod("CreateCommForDevice", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(DeviceConfig) }, null);
            if (method == null)
            {
                throw new MissingMethodException("CommFactory.CreateCommForDevice(DeviceConfig) was not found.");
            }

            var commObj = method.Invoke(null, new object[] { dc });
            var comm = commObj as IBasicCommunication;
            if (commObj != null && comm == null)
            {
                throw new InvalidCastException(string.Format("CommFactory.CreateCommForDevice returned '{0}', which does not implement IBasicCommunication.", commObj.GetType().FullName));
            }

            return comm;
        }
    }
}