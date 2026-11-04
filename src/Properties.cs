using PepperDash.Essentials.Core;

namespace PepperDash.Essentials.Plugins.ContemporaryResearch.Tuner
{
    public class Properties
    {
        public EssentialsControlPropertiesConfig Control { get; set; }

        public string unitId { get; set; }

        /// <summary>Presets list file, relative to /html/presets/lists/ (optional)</summary>
        public string presetsList { get; set; }
    }
}