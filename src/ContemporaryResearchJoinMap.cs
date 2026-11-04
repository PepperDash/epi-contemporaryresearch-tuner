using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;

namespace PepperDash.Essentials.Plugins.ContemporaryResearch.Tuner
{
    public class ContemporaryResearchJoinMap : SetTopBoxControllerJoinMap
    {
		[JoinName("IsOnline")]
		public JoinDataComplete IsOnline = new JoinDataComplete(
			new JoinData() 
			{ 
				JoinNumber = 49, 
				JoinSpan = 1 
			}, 
			new JoinMetadata 
			{
				Description = "IsOnline", 
				JoinCapabilities = eJoinCapabilities.ToSIMPL, 
				JoinType = eJoinType.Digital
			});
        [JoinName("PowerIsOn")]
        public JoinDataComplete PowerIsOn = new JoinDataComplete(
            new JoinData { JoinNumber = 1, JoinSpan = 1 },
            new JoinMetadata
            {
                Description = "Power On Feedback",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("Make")]
        public JoinDataComplete Make = new JoinDataComplete(
            new JoinData { JoinNumber = 2, JoinSpan = 1 },
            new JoinMetadata
            {
                Description = "Device Make",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Serial
            });

        [JoinName("Model")]
        public JoinDataComplete Model = new JoinDataComplete(
            new JoinData { JoinNumber = 3, JoinSpan = 1 },
            new JoinMetadata
            {
                Description = "Device Model",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Serial
            });

        [JoinName("IpAddress")]
        public JoinDataComplete IpAddress = new JoinDataComplete(
            new JoinData { JoinNumber = 4, JoinSpan = 1 },
            new JoinMetadata
            {
                Description = "Device IP Address",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Serial
            });

        [JoinName("MacAddress")]
        public JoinDataComplete MacAddress = new JoinDataComplete(
            new JoinData { JoinNumber = 5, JoinSpan = 1 },
            new JoinMetadata
            {
                Description = "Device MAC Address",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Serial
            });

        [JoinName("FirmwareVersion")]
        public JoinDataComplete FirmwareVersion = new JoinDataComplete(
            new JoinData { JoinNumber = 6, JoinSpan = 1 },
            new JoinMetadata
            {
                Description = "Device Firmware Version",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Serial
            });

        public ContemporaryResearchJoinMap(uint joinStart) : base(joinStart, typeof (ContemporaryResearchJoinMap))
        {

        }
    }
}