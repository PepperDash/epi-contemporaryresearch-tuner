using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace PepperDash.Essentials.Plugins.ContemporaryResearch.Tuner
{
    /// <summary>
    /// In-memory model of a 232-ATSC 4 tuner. Feed it command lines and it returns the response lines the manual
    /// describes. Synchronous and free of Crestron dependencies, so it is used both by the mock comms on a processor
    /// and by the unit tests. Responses follow the manual's formats; the ID response is an assumption.
    /// </summary>
    public class TunerSimulator
    {
        private const int VolumeRampStep = 5;

        private readonly string _unitId;
        private readonly List<string> _channels;
        private string _entry = string.Empty;

        public TunerSimulator(string unitId, IEnumerable<string> channels = null)
        {
            _unitId = ContemporaryResearchProtocol.NormalizeUnitId(unitId);
            _channels = new List<string>(channels ?? new[] { "2-1", "4-1", "5-1", "7-1", "9-1", "11-1", "13-1" });
            if (_channels.Count == 0) _channels.Add("2-1");
            Channel = _channels[0];
            PreviousChannel = Channel;
            Volume = 50;
        }

        public string Model = "232-ATSC4";
        public string Firmware = "1.00";
        public string MacAddress = "0014C8090001";
        public string IpAddress = "192.168.001.231";

        public bool PowerOn { get; private set; }
        public string Channel { get; private set; }
        public string PreviousChannel { get; private set; }
        public int Volume { get; private set; }
        public bool Muted { get; private set; }

        /// <summary>1 = ramping up, -1 = ramping down, 0 = stopped</summary>
        public int Ramp { get; private set; }

        /// <summary>
        /// Handles one command line (leading '&gt;', optional trailing CR/LF) and returns the response lines, without terminators.
        /// Malformed commands and commands for other units return nothing.
        /// </summary>
        public IList<string> Process(string line)
        {
            var responses = new List<string>();
            if (string.IsNullOrEmpty(line)) return responses;

            line = line.Trim('\r', '\n', ' ');
            if (!line.StartsWith(ContemporaryResearchProtocol.Attention, StringComparison.Ordinal)) return responses;

            var body = line.Substring(1);
            var unit = _unitId;
            if (body.Length > 0 && char.IsDigit(body[0]))
            {
                unit = body.Substring(0, 1);
                body = body.Substring(1);
            }
            if (unit != _unitId && unit != "0") return responses;
            if (body.Length < 2) return responses;

            var cmd = body.Substring(0, 2).ToUpperInvariant();
            var param = body.Substring(2);
            if (param.StartsWith("=", StringComparison.Ordinal)) param = param.Substring(1);

            switch (cmd)
            {
                case ContemporaryResearchProtocol.CmdPwrOn: PowerOn = true; responses.Add(Status()); break;
                case ContemporaryResearchProtocol.CmdPwrOff: PowerOn = false; Ramp = 0; responses.Add(Status()); break;
                case ContemporaryResearchProtocol.CmdPwrToggle: PowerOn = !PowerOn; responses.Add(Status()); break;

                case ContemporaryResearchProtocol.CmdChanUp: Step(1); responses.Add(Status()); break;
                case ContemporaryResearchProtocol.CmdChanDn: Step(-1); responses.Add(Status()); break;
                case ContemporaryResearchProtocol.CmdChanLast: Tune(PreviousChannel); responses.Add(Status()); break;
                case ContemporaryResearchProtocol.CmdTuneChannel: Tune(NormalizeChannel(param)); responses.Add(Status()); break;
                case ContemporaryResearchProtocol.CmdKeyEmu: Key(param); responses.Add(Status()); break;

                case ContemporaryResearchProtocol.CmdVolUp: Ramp = 1; responses.Add(Audio()); break;
                case ContemporaryResearchProtocol.CmdVolDn: Ramp = -1; responses.Add(Audio()); break;
                case ContemporaryResearchProtocol.CmdVolStop: Ramp = 0; responses.Add(Audio()); break;
                case ContemporaryResearchProtocol.CmdMuteOn: Muted = true; responses.Add(Audio()); break;
                case ContemporaryResearchProtocol.CmdMuteOff: Muted = false; responses.Add(Audio()); break;
                case ContemporaryResearchProtocol.CmdMuteToggle: Muted = !Muted; responses.Add(Audio()); break;
                case ContemporaryResearchProtocol.CmdVolLevel:
                    int level;
                    if (int.TryParse(param, NumberStyles.Integer, CultureInfo.InvariantCulture, out level))
                    {
                        Volume = Math.Max(0, Math.Min(100, level));
                        responses.Add(Audio());
                    }
                    break;

                case ContemporaryResearchProtocol.CmdPoll: responses.Add(Status()); break;
                case ContemporaryResearchProtocol.CmdPollAv: responses.Add(Audio()); break;
                case ContemporaryResearchProtocol.CmdPollIp:
                    responses.Add("$MAC=" + MacAddress);
                    responses.Add("$IP=" + IpAddress + "S IG=000.000.000.000 IM=255.255.255.000 IY=1");
                    break;
                case ContemporaryResearchProtocol.CmdPollInfo:
                    // ASSUMPTION: the manual doesn't document the ID response format
                    responses.Add(ContemporaryResearchProtocol.ResponsePrefix(_unitId, "ID") + " " + Model + " " + Firmware);
                    break;
            }
            return responses;
        }

        /// <summary>Advances a running volume ramp one step. Returns the audio status line, or null if not ramping.</summary>
        public string Tick()
        {
            if (Ramp == 0 || !PowerOn) return null;
            Volume = Math.Max(0, Math.Min(100, Volume + Ramp * VolumeRampStep));
            if (Volume == 0 || Volume == 100) Ramp = 0;
            return Audio();
        }

        /// <summary>Channel/source status line, e.g. "&lt;1TU032U0A0002x0"</summary>
        public string Status()
        {
            int major, minor;
            SplitChannel(Channel, out major, out minor);
            return string.Format("{0}{1}{2:000}U0A0{3:000}x0",
                ContemporaryResearchProtocol.ResponsePrefix(_unitId, "T"), PowerOn ? "U" : "M", major, minor);
        }

        /// <summary>Audio status line, e.g. "&lt;1VU63Ux100"</summary>
        public string Audio()
        {
            var emulated = (int)Math.Round(Volume * 63.0 / 100.0);
            return string.Format("{0}{1}{2:00}{3}x{4:000}",
                ContemporaryResearchProtocol.ResponsePrefix(_unitId, "V"), PowerOn ? "U" : "M", emulated, Muted ? "M" : "U", Volume);
        }

        private void Key(string param)
        {
            int key;
            if (!int.TryParse(param, NumberStyles.Integer, CultureInfo.InvariantCulture, out key)) return;

            if (key >= 10 && key <= 19)
                _entry += (key - 10).ToString(CultureInfo.InvariantCulture);
            else if (key == 99)
                _entry += "-";
            else if (key == 21 || key == 110)
            {
                if (_entry.Length > 0) Tune(NormalizeChannel(_entry));
                _entry = string.Empty;
            }
            else if (key == 22 || key == 101) { if (key == 22) Step(1); else Tune(PreviousChannel); }
            else if (key == 23) Step(-1);
            else if (key == 111) _entry = string.Empty;
        }

        private void Step(int direction)
        {
            var index = _channels.IndexOf(Channel);
            if (index < 0) index = 0;
            index = (index + direction + _channels.Count) % _channels.Count;
            Tune(_channels[index]);
        }

        private void Tune(string channel)
        {
            if (string.IsNullOrEmpty(channel) || channel == Channel) return;
            PreviousChannel = Channel;
            Channel = channel;
        }

        private static string NormalizeChannel(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            value = value.Trim().Replace(':', '-');
            var parts = value.Split('-');
            int major, minor = 1;
            if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out major)) return null;
            if (parts.Length > 1 && !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out minor)) return null;
            return major + "-" + minor;
        }

        private static void SplitChannel(string channel, out int major, out int minor)
        {
            var parts = channel.Split('-');
            major = int.Parse(parts[0], CultureInfo.InvariantCulture);
            minor = parts.Length > 1 ? int.Parse(parts[1], CultureInfo.InvariantCulture) : 0;
        }
    }
}
