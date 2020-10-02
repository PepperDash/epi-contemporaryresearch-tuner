using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro.DeviceSupport;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Devices.Common;
using PepperDash.Essentials.Bridges;
using epi_stb_contemporaryresearch.Bridge.JoinMap;

using Newtonsoft.Json;


namespace epi_stb_contemporaryresearch.Bridge
{
    public static class ContemporaryResearchApiExtensions
    {
        public static void LinkToApiExt(this ContemporaryResearchDevice stbDevice, BasicTriList trilist, uint joinStart, string joinMapKey)
        {
            ContemporaryResearchJoinMap joinMap = new ContemporaryResearchJoinMap();
            var joinMapSerialized = JoinMapHelper.GetJoinMapForDevice(joinMapKey);

            if (!string.IsNullOrEmpty(joinMapSerialized))
                joinMap = JsonConvert.DeserializeObject<ContemporaryResearchJoinMap>(joinMapSerialized);

            joinMap.OffsetJoinNumbers(joinStart);

            Debug.Console(1, "Linking to Trilist '{0}'", trilist.ID.ToString("X"));
            Debug.Console(0, "Linking to SetTopBox: {0}", stbDevice.Name);

            var commMonitor = stbDevice as ICommunicationMonitor;
            if (commMonitor != null)
            {
                commMonitor.CommunicationMonitor.IsOnlineFeedback.LinkInputSig(trilist.BooleanInput[joinMap.IsOnline]);
            }

            trilist.StringInput[joinMap.Name].StringValue = stbDevice.Name;

            trilist.SetBoolSigAction(joinMap.PowerOn, (b) => stbDevice.PowerOn(b));
            trilist.SetBoolSigAction(joinMap.PowerOff, (b) => stbDevice.PowerOff(b));
            trilist.SetBoolSigAction(joinMap.PowerToggle, (b) => stbDevice.PowerToggle(b));

            trilist.SetBoolSigAction(joinMap.Up, (b) => stbDevice.Up(b));
            trilist.SetBoolSigAction(joinMap.Down, (b) => stbDevice.Down(b));
            trilist.SetBoolSigAction(joinMap.Left, (b) => stbDevice.Left(b));
            trilist.SetBoolSigAction(joinMap.Right, (b) => stbDevice.Right(b));
            trilist.SetBoolSigAction(joinMap.Select, (b) => stbDevice.Select(b));
            trilist.SetBoolSigAction(joinMap.Menu, (b) => stbDevice.Menu(b));
            trilist.SetBoolSigAction(joinMap.Exit, (b) => stbDevice.Exit(b));

            trilist.SetBoolSigAction(joinMap.ChannelUp, (b) => stbDevice.ChannelUp(b));
            trilist.SetBoolSigAction(joinMap.ChannelDown, (b) => stbDevice.ChannelDown(b));
            trilist.SetBoolSigAction(joinMap.LastChannel, (b) => stbDevice.LastChannel(b));
            trilist.SetBoolSigAction(joinMap.Guide, (b) => stbDevice.Guide(b));
            trilist.SetBoolSigAction(joinMap.Info, (b) => stbDevice.Info(b));
            trilist.SetBoolSigAction(joinMap.Exit, (b) => stbDevice.Exit(b));


			trilist.SetSigTrueAction(joinMap.Digit0, () => stbDevice.SendKeypadButton("0"));
			trilist.SetSigTrueAction(joinMap.Digit1, () => stbDevice.SendKeypadButton("1"));
			trilist.SetSigTrueAction(joinMap.Digit2, () => stbDevice.SendKeypadButton("2"));
			trilist.SetSigTrueAction(joinMap.Digit3, () => stbDevice.SendKeypadButton("3"));
			trilist.SetSigTrueAction(joinMap.Digit4, () => stbDevice.SendKeypadButton("4"));
			trilist.SetSigTrueAction(joinMap.Digit5, () => stbDevice.SendKeypadButton("5"));
			trilist.SetSigTrueAction(joinMap.Digit6, () => stbDevice.SendKeypadButton("6"));
			trilist.SetSigTrueAction(joinMap.Digit7, () => stbDevice.SendKeypadButton("7"));
			trilist.SetSigTrueAction(joinMap.Digit8, () => stbDevice.SendKeypadButton("8"));
			trilist.SetSigTrueAction(joinMap.Digit9, () => stbDevice.SendKeypadButton("9"));
			trilist.SetSigTrueAction(joinMap.Dash, () => stbDevice.SendKeypadButton("-"));
			/*
            trilist.SetBoolSigAction(joinMap.Digit0, (b) => stbDevice.Digit0(b));
            trilist.SetBoolSigAction(joinMap.Digit1, (b) => stbDevice.Digit1(b));
            trilist.SetBoolSigAction(joinMap.Digit2, (b) => stbDevice.Digit2(b));
            trilist.SetBoolSigAction(joinMap.Digit3, (b) => stbDevice.Digit3(b));
            trilist.SetBoolSigAction(joinMap.Digit4, (b) => stbDevice.Digit4(b));
            trilist.SetBoolSigAction(joinMap.Digit5, (b) => stbDevice.Digit5(b));
            trilist.SetBoolSigAction(joinMap.Digit6, (b) => stbDevice.Digit6(b));
            trilist.SetBoolSigAction(joinMap.Digit7, (b) => stbDevice.Digit7(b));
            trilist.SetBoolSigAction(joinMap.Digit8, (b) => stbDevice.Digit8(b));
            trilist.SetBoolSigAction(joinMap.Digit9, (b) => stbDevice.Digit9(b));
            trilist.SetBoolSigAction(joinMap.Dash, (b) => stbDevice.Dash(b));
			 */

            trilist.SetBoolSigAction(joinMap.KeypadEnter, (b) => stbDevice.KeypadEnter(b));

			stbDevice.CurrentChannelFB.LinkInputSig(trilist.StringInput[joinMap.CurrentChannel]);
			stbDevice.PowerStatusFeedback.LinkInputSig(trilist.BooleanInput[joinMap.PowerOn]);
			stbDevice.PowerStatusFeedback.LinkComplementInputSig(trilist.BooleanInput[joinMap.PowerOff]);

        }
    }
}