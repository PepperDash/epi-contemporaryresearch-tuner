using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;

namespace epi_stb_contemporaryresearch.Bridge.JoinMap
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
        public ContemporaryResearchJoinMap(uint joinStart) : base(joinStart, typeof (ContemporaryResearchJoinMap))
        {

        }
    }
}