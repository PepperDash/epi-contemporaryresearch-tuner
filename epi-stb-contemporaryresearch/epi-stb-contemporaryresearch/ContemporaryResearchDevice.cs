using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Devices;
using PepperDash.Essentials.Devices.Common.Codec;
using PepperDash.Essentials.Devices.Common.DSP;
using System.Text.RegularExpressions;
using Crestron.SimplSharp.Reflection;
using Newtonsoft.Json;
using PepperDash.Essentials.Core.Config;
using PepperDash.Essentials.Bridges;
using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharpPro.Diagnostics;

using epi_stb_contemporaryresearch.Bridge;

namespace epi_stb_contemporaryresearch
{
    public class ContemporaryResearchDevice : ReconfigurableDevice, IBridge, ISetTopBoxControls, ICommunicationMonitor
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

        DeviceConfig _Dc;

        private Properties _props;

        public IBasicCommunication Communication { get; private set; }
        public CommunicationGather PortGather { get; private set; }
        public StatusMonitorBase CommunicationMonitor { get; private set; }

        private string UnitId { get; set; }

        public BoolFeedback PowerStatusFeedback { get; set; }
		public StringFeedback CurrentChannelFB { get; set; }
		private string _CurrentChannel { get; set; }


		private CTimer _SendKeypadTimer;
		private string _SendKeypadData;
		public string CurrentChannel
		{

			get
			{
				return _CurrentChannel;
			}
			set
			{
				_CurrentChannel = value;
				CurrentChannelFB.FireUpdate();
			}

		}
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




        public static void LoadPlugin()
        {
            DeviceFactory.AddFactoryForType("contemporaryresearch", ContemporaryResearchDevice.BuildDevice);
        }

        public static ContemporaryResearchDevice BuildDevice(DeviceConfig dc)
        {
            var comm = CommFactory.CreateCommForDevice(dc);
            var newMe = new ContemporaryResearchDevice(dc.Key, dc.Name, comm, dc);

            return newMe;
        }

        public ContemporaryResearchDevice(string key, string name, IBasicCommunication comm, DeviceConfig dc)
            : base(dc)
        {
            _Dc = dc;


            _props = JsonConvert.DeserializeObject<Properties>(dc.Properties.ToString());
            Debug.Console(0, this, "Made it to device constructor");

            UnitId = _props.unitId;

            PowerStatusFeedback = new BoolFeedback(() => PowerStatus);
			CurrentChannelFB = new StringFeedback(() => _CurrentChannel);

            Communication = comm;
            var socket = comm as ISocketStatus;
            if (socket != null)
            {
                // This instance uses IP control
                socket.ConnectionChange += new EventHandler<GenericSocketStatusChageEventArgs>(socket_ConnectionChange);
            }
            else
            {
                // This instance uses RS-232 control
            }

            PortGather = new CommunicationGather(Communication, "\x0A");
            PortGather.LineReceived += this.Port_LineReceived;


            // Custom monitoring, will check the heartbeat tracker count every 20s and reset. Heartbeat sbould be coming in every 20s if subscriptions are valid
            CommunicationMonitor = new GenericCommunicationMonitor(this, Communication, 20000, 120000, 300000, Poll);
            DeviceManager.AddDevice(CommunicationMonitor);


        }

        public override bool CustomActivate()
        {
            Communication.Connect();
            CommunicationMonitor.Start();
            return true;
        }



        private string BuildCommand(string command, string parameter)
        {
            var cmd = string.Format("{0}{1}{2}{3}\x0D", Attention, UnitId, command, parameter);
            Debug.Console(2, this, "TX : '{0}' ", cmd);
            return cmd;
        }

        private string BuildCommand(string command)
        {
            return string.Format("{0}{1}{2}\x0D", Attention, UnitId, command);
        }

        void socket_ConnectionChange(object sender, GenericSocketStatusChageEventArgs e)
        {
            Debug.Console(2, this, "Socket Status Change: {0}", e.Client.ClientStatus.ToString());

            if (e.Client.IsConnected)
            {

            }
            else if (!e.Client.IsConnected)
            {

            }
        }

        void Port_LineReceived(object dev, GenericCommMethodReceiveTextArgs args)
        {
            Debug.Console(2, this, "RX : '{0}'", args.Text);
            var header = string.Format("<1T");
            if (args.Text.Contains(header))
            {
                var message = args.Text.Substring(3, 1);
				Debug.Console(2, this, "Power Message: '{0}'", message);
                if (message.Equals("U"))
                {
                    PowerStatus = true;
                }
                else if (message.Equals("M"))
                {
                    PowerStatus = false;
                }
				CurrentChannel = args.Text.Substring(4, 3);

				Debug.Console(2, this, "Channel Message: '{0}'", args.Text.Substring(4, 3));
            }
			else if (args.Text.Contains(">0K") || args.Text.Contains(">0TU") || args.Text.Contains(">0TD"))
			{
				Poll();
			}
        }



        #region IBridge Members

        public void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey)
        {
            this.LinkToApiExt(trilist, joinStart, joinMapKey);
        }

        #endregion

        #region ISetTopBoxControls Members

        public void DvrList(bool pressRelease)
        {
            throw new NotImplementedException();
        }

        public bool HasDpad
        {
            get { throw new NotImplementedException(); }
        }

        public bool HasDvr
        {
            get { throw new NotImplementedException(); }
        }

        public bool HasNumeric
        {
            get { throw new NotImplementedException(); }
        }

        public bool HasPresets
        {
            get { throw new NotImplementedException(); }
        }

        public void LoadPresets(string filePath)
        {
            throw new NotImplementedException();
        }

        public PepperDash.Essentials.Core.Presets.DevicePresetsModel PresetsModel
        {
            get { throw new NotImplementedException(); }
        }

        public void Replay(bool pressRelease)
        {
            throw new NotImplementedException();
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

        public void PowerOn(bool pressRelease)
        {
            if (pressRelease)

                Communication.SendText(BuildCommand(CmdPwrOn));
        }

        public void PowerOff(bool pressRelease)
        {
            if (pressRelease)

                Communication.SendText(BuildCommand(CmdPwrOff));
        }

        public void PowerToggle(bool pressRelease)
        {
            if (pressRelease)
            {
                if (PowerStatus)
                {
                    Communication.SendText(BuildCommand(CmdPwrOff));
                }
                else
                {
                    Communication.SendText(BuildCommand(CmdPwrOff));
                }
            }

        }

        #region IColor Members

        public void Blue(bool pressRelease)
        {
            throw new NotImplementedException();
        }

        public void Green(bool pressRelease)
        {
            throw new NotImplementedException();
        }

        public void Red(bool pressRelease)
        {
            throw new NotImplementedException();
        }

        public void Yellow(bool pressRelease)
        {
            throw new NotImplementedException();
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

		public void SendKeypadButton(string key)
		{
			_SendKeypadData = _SendKeypadData + key;
			CurrentChannel = _SendKeypadData;
			if (_SendKeypadTimer == null)
			{
				_SendKeypadTimer = new CTimer(_SendKeypadChannel, 3000);
			}
			else
			{
				_SendKeypadTimer.Stop();
				_SendKeypadTimer.Reset(3000);
			}
		}

		private void _SendKeypadChannel(object notUsed)
		{
			Communication.SendText(BuildCommand(string.Format("TC={0}", _SendKeypadData)));
			_SendKeypadData = "";
		}
        #region INumericKeypad Members

        public void Digit0(bool pressRelease)
        {
            if (pressRelease)
				SendKeypadButton("0");
                
        }

        public void Digit1(bool pressRelease)
        {
			if (pressRelease)
				SendKeypadButton("1");
        }

        public void Digit2(bool pressRelease)
        {
			if (pressRelease)
				SendKeypadButton("2");
        }

        public void Digit3(bool pressRelease)
        {
			if (pressRelease)
				SendKeypadButton("3");
        }

        public void Digit4(bool pressRelease)
        {
			if (pressRelease)
				SendKeypadButton("4");
        }

        public void Digit5(bool pressRelease)
        {
			if (pressRelease)
				SendKeypadButton("5");
        }

        public void Digit6(bool pressRelease)
        {
			if (pressRelease)
				SendKeypadButton("6");
        }

        public void Digit7(bool pressRelease)
        {
			if (pressRelease)
				SendKeypadButton("7");
        }

        public void Digit8(bool pressRelease)
        {
			if (pressRelease)
				SendKeypadButton("8");
        }

        public void Digit9(bool pressRelease)
        {
			if (pressRelease)
				SendKeypadButton("9");
        }

        public bool HasKeypadAccessoryButton1
        {
            get { throw new NotImplementedException(); }
        }

        public bool HasKeypadAccessoryButton2
        {
            get { throw new NotImplementedException(); }
        }

        public void KeypadAccessoryButton1(bool pressRelease)
        {
            throw new NotImplementedException();
        }

        public string KeypadAccessoryButton1Label
        {
            get { throw new NotImplementedException(); }
        }

        public void KeypadAccessoryButton2(bool pressRelease)
        {
            throw new NotImplementedException();
        }

        public string KeypadAccessoryButton2Label
        {
            get { throw new NotImplementedException(); }
        }

        #endregion

        #region ITransport Members

        public void ChapMinus(bool pressRelease)
        {
            throw new NotImplementedException();
        }

        public void ChapPlus(bool pressRelease)
        {
            throw new NotImplementedException();
        }

        public void FFwd(bool pressRelease)
        {
            throw new NotImplementedException();
        }

        public void Pause(bool pressRelease)
        {
            throw new NotImplementedException();
        }

        public void Play(bool pressRelease)
        {
            throw new NotImplementedException();
        }

        public void Record(bool pressRelease)
        {
            throw new NotImplementedException();
        }

        public void Rewind(bool pressRelease)
        {
            throw new NotImplementedException();
        }

        public void Stop(bool pressRelease)
        {
            throw new NotImplementedException();
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
            get { throw new NotImplementedException(); }
        }

        #endregion
    }
}

