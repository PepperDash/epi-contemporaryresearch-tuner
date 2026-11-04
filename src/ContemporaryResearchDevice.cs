using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;
using Newtonsoft.Json;
using PepperDash.Essentials.Core.Config;
using Crestron.SimplSharpPro.DeviceSupport;
using PepperDash.Essentials.Core.DeviceInfo;
using PepperDash.Essentials.Core.DeviceTypeInterfaces;
using PepperDash.Essentials.Core.Presets;
using PepperDash.Core.Logging;

namespace PepperDash.Essentials.Plugins.ContemporaryResearch.Tuner
{
    public class ContemporaryResearchDevice : EssentialsBridgeableDevice, ICommunicationMonitor, ISetTopBoxControls, IHasPowerControlWithFeedback, IDeviceInfoProvider, ITvPresetsProvider, IRoutingSource

    {
        #region constants
        private const string Attention = ">";

        private const string CmdPwrOn = "P1";

        private const string CmdPwrOff = "P0";

        private const string CmdChanUp = "TU";

        private const string CmdChanDn = "TD";

        private const string CmdChanLast = "TP";

        private const string CmdKeyEmu = "KK";

        private const string CmdPoll = "ST";

        private const string CmdPollIp = "IP";

        private const string CmdPollInfo = "ID";

        private const string ParamDigit0 = "=10";
        private const string ParamDigit1 = "=11";
        private const string ParamDigit2 = "=12";
        private const string ParamDigit3 = "=13";
        private const string ParamDigit4 = "=14";
        private const string ParamDigit5 = "=15";
        private const string ParamDigit6 = "=16";
        private const string ParamDigit7 = "=17";
        private const string ParamDigit8 = "=18";
        private const string ParamDigit9 = "=19";
        private const string ParamDash = "=99";
        private const string ParamKpEnter = "=21";
        private const string ParamMenu = "=29";
        private const string ParamGuide = "=63";
        private const string ParamInfo = "=100";
        private const string ParamUp = "=108";
        private const string ParamDn = "=109";
        private const string ParamLt = "=107";
        private const string ParamRt = "=106";
        private const string ParamDpEnter = "=110";
        private const string ParamExit = "=111";

        
        #endregion

        DeviceConfig _dc;

        private Properties _props;

        public IBasicCommunication Communication { get; private set; }
        public CommunicationGather PortGather { get; private set; }
        public StatusMonitorBase CommunicationMonitor { get; private set; }

        public BoolFeedback IsOnlineFeedback { get; private set; }

        public StringFeedback MakeFeedback { get; private set; }
        public StringFeedback ModelFeedback { get; private set; } 

        public readonly Dictionary<string, Action<string>> StringParse;

        private string UnitId { get; set; }

        public const string Make = "Contemporary Research";
        public string Model { get; private set; }

        public BoolFeedback PowerStatusFeedback { get; private set; }

        public BoolFeedback PowerIsOnFeedback { get { return PowerStatusFeedback; } }
        private bool _PowerStatus { get; set; }
        private bool PowerStatus
        {
            get
            {
                return _PowerStatus;
            }
            set
            {
                _PowerStatus = value;
                PowerStatusFeedback.FireUpdate();
            }
        }

        public ContemporaryResearchDevice(string key, string name, IBasicCommunication comm, DeviceConfig dc)
            : base(key, name)
        {
            _dc = dc;

            _props = JsonConvert.DeserializeObject<Properties>(dc.Properties.ToString());
            this.LogInformation("Made it to device constructor");

            UnitId = _props.unitId;

            PowerStatusFeedback = new BoolFeedback(() => PowerStatus);
            MakeFeedback = new StringFeedback(() => Make);
            ModelFeedback = new StringFeedback(() => Model);

            StringParse = new Dictionary<string, Action<string>>
            {
                {string.Format("<{0}T", UnitId), ParseStatusResponse},
                {string.Format("<{0}IP", UnitId), ParseNetworkResponse},
                {string.Format("<{0}ID", UnitId), ParseDeviceInfoResponse},
            };
			
            Communication = comm;
            var socket = comm as ISocketStatus;
            if (socket != null)
                socket.ConnectionChange += socket_ConnectionChange;
            

            PortGather = new CommunicationGather(Communication, "\x0A");
            PortGather.LineReceived += this.Port_LineReceived;
			
            // Custom monitoring, will check the heartbeat tracker count every 20s and reset. Heartbeat sbould be coming in every 20s if subscriptions are valid
            CommunicationMonitor = new GenericCommunicationMonitor(this, Communication, 20000, 120000, 300000, Poll);

            IsOnlineFeedback = CommunicationMonitor.IsOnlineFeedback;

            DeviceManager.AddDevice(CommunicationMonitor);

            AnyOut = new RoutingOutputPort(RoutingPortNames.AnyOut, eRoutingSignalType.Audio | eRoutingSignalType.Video, eRoutingPortConnectionType.None, null, this);
            OutputPorts = new RoutingPortCollection<RoutingOutputPort> { AnyOut };
		}

        protected override bool CustomActivate()
        {
			// Essentials will handle the connect method to the device
			Communication.Connect();
			// Essentials will handle starting the comms monitor
            CommunicationMonitor.Start();
			
			return base.CustomActivate();
			//return true;
        }

        private string BuildCommand(string command, string parameter)
        {
            var cmd = string.Format("{0}{1}{2}{3}\x0D", Attention, UnitId, command, parameter);
            //Debug.Console(2, this, "TX : '{0}' ", cmd);
            return cmd;
        }

        private string BuildCommand(string command)
        {
			var cmd = string.Format("{0}{1}{2}\x0D", Attention, UnitId, command);
			//Debug.Console(2, this, "TX : '{0}'", cmd);
			return cmd;
        }

        void socket_ConnectionChange(object sender, GenericSocketStatusChageEventArgs e)
        {
            this.LogVerbose("Socket Status Change: {status}", e.Client.ClientStatus.ToString());

            if (e.Client.IsConnected)
            {

            }
            else if (!e.Client.IsConnected)
            {

            }
        }

        void Port_LineReceived(object dev, GenericCommMethodReceiveTextArgs args)
        {
            var data = args.Text;
            this.LogVerbose("RX : '{data}'", data);
            if (string.IsNullOrEmpty(data)) return;

            foreach (var entry in StringParse)
            {
                if (!data.StartsWith(entry.Key, StringComparison.Ordinal)) continue;
                entry.Value.Invoke(data);
                return;
            }
        }

        private void ParseStatusResponse(string data)
        {
            // <1T U 032 ... ; the 4th character is the power state ('U' = on)
            if (data.Length > 3)
                PowerStatus = char.ToUpperInvariant(data[3]) == 'U';

            // only ask for network/device info until we have it
            if (DeviceInfo == null || string.IsNullOrEmpty(DeviceInfo.IpAddress))
                Communication.SendText(BuildCommand(CmdPollIp));
        }

        private static readonly Regex MacRegex = new Regex(@"MAC=([0-9A-Fa-f]{12})", RegexOptions.Compiled);
        private static readonly Regex IpRegex = new Regex(@"\bIP=(\d{1,3})\.(\d{1,3})\.(\d{1,3})\.(\d{1,3})", RegexOptions.Compiled);

        private void ParseNetworkResponse(string data)
        {
            // Response example: $MAC=0014C8090001 / $IP=192.168.001.231S IG=... (S/D = static/DHCP)
            var macMatch = MacRegex.Match(data);
            var ipMatch = IpRegex.Match(data);
            if (!macMatch.Success && !ipMatch.Success)
            {
                this.LogDebug("Unable to parse network response: {data}", data);
                return;
            }

            if (DeviceInfo == null) DeviceInfo = new DeviceInfo();
            if (macMatch.Success)
                DeviceInfo.MacAddress = macMatch.Groups[1].Value;
            if (ipMatch.Success)
            {
                // strip the zero padding (192.168.001.231 -> 192.168.1.231)
                DeviceInfo.IpAddress = string.Format("{0}.{1}.{2}.{3}",
                    int.Parse(ipMatch.Groups[1].Value), int.Parse(ipMatch.Groups[2].Value),
                    int.Parse(ipMatch.Groups[3].Value), int.Parse(ipMatch.Groups[4].Value));
            }
            OnDeviceInfoChanged();

            if (string.IsNullOrEmpty(DeviceInfo.FirmwareVersion))
                Communication.SendText(BuildCommand(CmdPollInfo));
        }

        private void ParseDeviceInfoResponse(string data)
        {
            // strip the "<{unit}ID" prefix, remainder is "<model> <firmware>"
            var prefix = string.Format("<{0}ID", UnitId);
            var payload = data.Length > prefix.Length ? data.Substring(prefix.Length).Trim() : string.Empty;
            var infoChunks = payload.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (infoChunks.Length == 0) return;

            Model = infoChunks[0];
            ModelFeedback.FireUpdate();
            MakeFeedback.FireUpdate();
            if (infoChunks.Length <= 1) return;
            if (DeviceInfo == null) DeviceInfo = new DeviceInfo();
            DeviceInfo.FirmwareVersion = infoChunks[1];
            OnDeviceInfoChanged();
        }

        void OnDeviceInfoChanged()
        {
            var handler = DeviceInfoChanged;
            if (handler == null) return;
            handler.Invoke(this, (new DeviceInfoEventArgs(DeviceInfo)));
        }
		

        #region IBridge Members

        public override void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
        {
            {
                var joinMap = new ContemporaryResearchJoinMap(joinStart);
				// This adds the join map to the collection on the bridge
				if (bridge != null)
				{
					bridge.AddJoinMap(Key, joinMap);
				}

				var joinMapSerialized = JoinMapHelper.TryGetJoinMapAdvancedForDevice(joinMapKey);
				if (joinMapSerialized != null)
				{
					joinMap.SetCustomJoinData(joinMapSerialized);
				}	
				
                this.LogDebug("Linking to Trilist '{trilistId}'", trilist.ID.ToString("X"));
				this.LogInformation("Linking to SetTopBox: {name}", Name);

                CommunicationMonitor.IsOnlineFeedback.LinkInputSig(trilist.BooleanInput[joinMap.IsOnline.JoinNumber]);
	
                trilist.StringInput[joinMap.Name.JoinNumber].StringValue = Name;

                PowerStatusFeedback.LinkInputSig(trilist.BooleanInput[joinMap.PowerIsOn.JoinNumber]);
                MakeFeedback.LinkInputSig(trilist.StringInput[joinMap.Make.JoinNumber]);
                ModelFeedback.LinkInputSig(trilist.StringInput[joinMap.Model.JoinNumber]);

                // capability flags and device info are pushed on link, on device-info change and whenever the EISC comes online
                Action updateStatic = () =>
                {
                    trilist.BooleanInput[joinMap.HasDpad.JoinNumber].BoolValue = HasDpad;
                    trilist.BooleanInput[joinMap.HasNumeric.JoinNumber].BoolValue = HasNumeric;
                    trilist.BooleanInput[joinMap.HasDvr.JoinNumber].BoolValue = HasDvr;
                    trilist.BooleanInput[joinMap.HasPresets.JoinNumber].BoolValue = HasPresets;
                    trilist.StringInput[joinMap.IpAddress.JoinNumber].StringValue = DeviceInfo != null ? DeviceInfo.IpAddress ?? string.Empty : string.Empty;
                    trilist.StringInput[joinMap.MacAddress.JoinNumber].StringValue = DeviceInfo != null ? DeviceInfo.MacAddress ?? string.Empty : string.Empty;
                    trilist.StringInput[joinMap.FirmwareVersion.JoinNumber].StringValue = DeviceInfo != null ? DeviceInfo.FirmwareVersion ?? string.Empty : string.Empty;
                };
                updateStatic();
                DeviceInfoChanged += (d, a) => updateStatic();
                trilist.OnlineStatusChange += (d, a) =>
                {
                    if (!a.DeviceOnLine) return;
                    updateStatic();
                    trilist.BooleanInput[joinMap.PowerIsOn.JoinNumber].BoolValue = PowerStatus;
                    trilist.StringInput[joinMap.Make.JoinNumber].StringValue = Make;
                    trilist.StringInput[joinMap.Model.JoinNumber].StringValue = Model ?? string.Empty;
                };

                trilist.SetBoolSigAction(joinMap.PowerOn.JoinNumber, PowerOn);
                trilist.SetBoolSigAction(joinMap.PowerOff.JoinNumber, PowerOff);
                trilist.SetBoolSigAction(joinMap.PowerToggle.JoinNumber, PowerToggle);

                trilist.SetBoolSigAction(joinMap.Up.JoinNumber, Up);
                trilist.SetBoolSigAction(joinMap.Down.JoinNumber, Down);
                trilist.SetBoolSigAction(joinMap.Left.JoinNumber, Left);
                trilist.SetBoolSigAction(joinMap.Right.JoinNumber, Right);
                trilist.SetBoolSigAction(joinMap.Select.JoinNumber, Select);
                trilist.SetBoolSigAction(joinMap.Menu.JoinNumber, Menu);
                trilist.SetBoolSigAction(joinMap.Exit.JoinNumber, Exit);

                trilist.SetBoolSigAction(joinMap.ChannelUp.JoinNumber, ChannelUp);
                trilist.SetBoolSigAction(joinMap.ChannelDown.JoinNumber, ChannelDown);
                trilist.SetBoolSigAction(joinMap.LastChannel.JoinNumber, LastChannel);
                trilist.SetBoolSigAction(joinMap.Guide.JoinNumber, Guide);
                trilist.SetBoolSigAction(joinMap.Info.JoinNumber, Info);
                trilist.SetBoolSigAction(joinMap.Exit.JoinNumber, Exit);

                trilist.SetBoolSigAction(joinMap.Digit0.JoinNumber, Digit0);
                trilist.SetBoolSigAction(joinMap.Digit1.JoinNumber, Digit1);
                trilist.SetBoolSigAction(joinMap.Digit2.JoinNumber, Digit2);
                trilist.SetBoolSigAction(joinMap.Digit3.JoinNumber, Digit3);
                trilist.SetBoolSigAction(joinMap.Digit4.JoinNumber, Digit4);
                trilist.SetBoolSigAction(joinMap.Digit5.JoinNumber, Digit5);
                trilist.SetBoolSigAction(joinMap.Digit6.JoinNumber, Digit6);
                trilist.SetBoolSigAction(joinMap.Digit7.JoinNumber, Digit7);
                trilist.SetBoolSigAction(joinMap.Digit8.JoinNumber, Digit8);
                trilist.SetBoolSigAction(joinMap.Digit9.JoinNumber, Digit9);
                trilist.SetBoolSigAction(joinMap.Dash.JoinNumber, Dash);
                trilist.SetBoolSigAction(joinMap.KeypadEnter.JoinNumber, KeypadEnter);
            }
        }

        #endregion

        #region ISetTopBoxControls Members

        private void LogUnsupported(string command)
        {
            this.LogDebug("{command} is not supported by the Contemporary Research tuner", command);
        }

        public void DvrList(bool pressRelease)
        {
            LogUnsupported("DvrList");
        }

        public bool HasDpad
        {
            get { return true; }
        }

        public bool HasDvr
        {
            get { return false; }
        }

        public bool HasNumeric
        {
            get { return true; }
        }

        public bool HasPresets
        {
            get { return TvPresets != null; }
        }

        public PepperDash.Essentials.Core.Presets.DevicePresetsModel PresetsModel
        {
            get { return TvPresets; }
        }

        public void Replay(bool pressRelease)
        {
            LogUnsupported("Replay");
        }

        #endregion

        #region IChannel Members

        public void ChannelDown(bool pressRelease)
        {
            if (pressRelease)
                Communication.SendText(BuildCommand(CmdChanDn));
        }

        public void ChannelUp(bool pressRelease)
        {
			if (pressRelease)

				Communication.SendText(BuildCommand(CmdChanUp));
        }

        public void Exit(bool pressRelease)
        {
            if (pressRelease)

                Communication.SendText(BuildCommand(CmdKeyEmu, ParamExit));
        }

        public void Guide(bool pressRelease)
        {
            if (pressRelease)

                Communication.SendText(BuildCommand(CmdKeyEmu, ParamGuide));
        }

        public void Info(bool pressRelease)
        {
            if (pressRelease)

                Communication.SendText(BuildCommand(CmdKeyEmu, ParamInfo));
        }

        public void LastChannel(bool pressRelease)
        {
            if (pressRelease)

                Communication.SendText(BuildCommand(CmdChanLast));
        }

        #endregion

        public void PowerOn()
        {
            Communication.SendText(BuildCommand(CmdPwrOn));
        }

        public void PowerOff()
        {
            Communication.SendText(BuildCommand(CmdPwrOff));
        }

        public void PowerToggle()
        {
            if (PowerStatus)
                PowerOff();
            else
                PowerOn();
        }

        public void PowerOn(bool pressRelease)
        {
            if (pressRelease) PowerOn();
        }

        public void PowerOff(bool pressRelease)
        {
            if (pressRelease) PowerOff();
        }

        public void PowerToggle(bool pressRelease)
        {
            if (pressRelease) PowerToggle();
        }

        #region IColor Members

        public void Blue(bool pressRelease)
        {
            LogUnsupported("Blue");
        }

        public void Green(bool pressRelease)
        {
            LogUnsupported("Green");
        }

        public void Red(bool pressRelease)
        {
            LogUnsupported("Red");
        }

        public void Yellow(bool pressRelease)
        {
            LogUnsupported("Yellow");
        }

        #endregion

        #region IDPad Members

        public void Down(bool pressRelease)
        {
            if (pressRelease)

                Communication.SendText(BuildCommand(CmdKeyEmu, ParamDn));
        }

        public void Left(bool pressRelease)
        {
            if (pressRelease)

                Communication.SendText(BuildCommand(CmdKeyEmu, ParamLt));
        }

        public void Menu(bool pressRelease)
        {
            if (pressRelease)

                Communication.SendText(BuildCommand(CmdKeyEmu, ParamMenu));
        }

        public void Right(bool pressRelease)
        {
            Communication.SendText(BuildCommand(CmdKeyEmu, ParamRt));
        }

        public void Select(bool pressRelease)
        {
            if (pressRelease)

                Communication.SendText(BuildCommand(CmdKeyEmu, ParamDpEnter));
        }

        public void Up(bool pressRelease)
        {
            if (pressRelease)

                Communication.SendText(BuildCommand(CmdKeyEmu, ParamUp));
        }

        #endregion

        #region ISetTopBoxNumericKeypad Members

        public void Dash(bool pressRelease)
        {
            if (pressRelease)

                Communication.SendText(BuildCommand(CmdKeyEmu, ParamDash));
        }

        public void KeypadEnter(bool pressRelease)
        {
            if (pressRelease)

                Communication.SendText(BuildCommand(CmdKeyEmu, ParamKpEnter));
        }

        #endregion

        #region INumericKeypad Members

        public void Digit0(bool pressRelease)
        {
            if (pressRelease)

                Communication.SendText(BuildCommand(CmdKeyEmu, ParamDigit0));
        }

        public void Digit1(bool pressRelease)
        {
            if (pressRelease)

                Communication.SendText(BuildCommand(CmdKeyEmu, ParamDigit1));
        }

        public void Digit2(bool pressRelease)
        {
            if (pressRelease)

                Communication.SendText(BuildCommand(CmdKeyEmu, ParamDigit2));
        }

        public void Digit3(bool pressRelease)
        {
            if (pressRelease)

                Communication.SendText(BuildCommand(CmdKeyEmu, ParamDigit3));
        }

        public void Digit4(bool pressRelease)
        {
            if (pressRelease)

                Communication.SendText(BuildCommand(CmdKeyEmu, ParamDigit4));
        }

        public void Digit5(bool pressRelease)
        {
            if (pressRelease)

                Communication.SendText(BuildCommand(CmdKeyEmu, ParamDigit5));
        }

        public void Digit6(bool pressRelease)
        {
            if (pressRelease)

                Communication.SendText(BuildCommand(CmdKeyEmu, ParamDigit6));
        }

        public void Digit7(bool pressRelease)
        {
            if (pressRelease)

                Communication.SendText(BuildCommand(CmdKeyEmu, ParamDigit7));
        }

        public void Digit8(bool pressRelease)
        {
            if (pressRelease)

                Communication.SendText(BuildCommand(CmdKeyEmu, ParamDigit8));
        }

        public void Digit9(bool pressRelease)
        {
            if (pressRelease)

                Communication.SendText(BuildCommand(CmdKeyEmu, ParamDigit9));
        }

        public bool HasKeypadAccessoryButton1
        {
            get { return false; }
        }

        public bool HasKeypadAccessoryButton2
        {
            get { return false; }
        }

        public void KeypadAccessoryButton1(bool pressRelease)
        {
            LogUnsupported("KeypadAccessoryButton1");
        }

        public string KeypadAccessoryButton1Label
        {
            get { return string.Empty; }
        }

        public void KeypadAccessoryButton2(bool pressRelease)
        {
            LogUnsupported("KeypadAccessoryButton2");
        }

        public string KeypadAccessoryButton2Label
        {
            get { return string.Empty; }
        }

        #endregion

        #region ITransport Members

        public void ChapMinus(bool pressRelease)
        {
            LogUnsupported("ChapMinus");
        }

        public void ChapPlus(bool pressRelease)
        {
            LogUnsupported("ChapPlus");
        }

        public void FFwd(bool pressRelease)
        {
            LogUnsupported("FFwd");
        }

        public void Pause(bool pressRelease)
        {
            LogUnsupported("Pause");
        }

        public void Play(bool pressRelease)
        {
            LogUnsupported("Play");
        }

        public void Record(bool pressRelease)
        {
            LogUnsupported("Record");
        }

        public void Rewind(bool pressRelease)
        {
            LogUnsupported("Rewind");
        }

        public void Stop(bool pressRelease)
        {
            LogUnsupported("Stop");
        }

        #endregion

        #region Poll
        public void Poll()
        {
            Communication.SendText(BuildCommand(CmdPoll));
        }

        #endregion

        #region IUiDisplayInfo Members

        public uint DisplayUiType
        {
            get { return DisplayUiConstants.TypeDirecTv; }
        }

        #endregion

		#region ISetTopBoxControls Members


        public DevicePresetsModel TvPresets { get; private set; }
        public void LoadPresets(string filePath)
        {
            TvPresets = new DevicePresetsModel(Key + "-presets", this, filePath);
            
            DeviceManager.AddDevice(TvPresets);

            foreach (var item in TvPresets.PresetsList)
            {
                this.LogInformation("Preset Name = {presetName}", item.Name);
            }
        }

        #endregion

        #region IDeviceInfoProvider Members

        public DeviceInfo DeviceInfo { get; private set; }


        public event DeviceInfoChangeHandler DeviceInfoChanged;

        public void UpdateDeviceInfo()
        {
            // request network info (the response chain also requests model/firmware)
            Communication.SendText(BuildCommand(CmdPollIp));
            Communication.SendText(BuildCommand(CmdPollInfo));
        }

        #endregion

        #region IRoutingSource Members

        #region IRoutingOutputs Members
        public RoutingOutputPort AnyOut { get; private set; }
        public RoutingPortCollection<RoutingOutputPort> OutputPorts { get; private set; }

        #endregion
        #endregion
    }
}

