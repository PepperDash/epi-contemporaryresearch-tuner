using System;
using System.Text.RegularExpressions;

namespace PepperDash.Essentials.Plugins.ContemporaryResearch.Tuner
{
    /// <summary>
    /// Command building and response parsing for the Contemporary Research 232-ATSC 4 tuner.
    /// Has no Crestron or PepperDash dependencies so it can be unit tested on any host.
    /// Reference: docs/ContermporaryResearch_232-ATSC_Manual.pdf
    /// </summary>
    public static class ContemporaryResearchProtocol
    {
        public const string Attention = ">";

        // power
        public const string CmdPwrOn = "P1";
        public const string CmdPwrOff = "P0";
        public const string CmdPwrToggle = "PT";

        // tuning
        public const string CmdChanUp = "TU";
        public const string CmdChanDn = "TD";
        public const string CmdChanLast = "TP";
        public const string CmdTuneChannel = "TC";

        // key emulation (HD2-RC remote)
        public const string CmdKeyEmu = "KK";

        // audio
        public const string CmdVolUp = "VU";
        public const string CmdVolDn = "VD";
        public const string CmdVolStop = "VV";
        public const string CmdMuteOn = "VM";
        public const string CmdMuteOff = "VX";
        public const string CmdMuteToggle = "VT";
        public const string CmdVolLevel = "VH";

        // status requests
        public const string CmdPoll = "ST";
        public const string CmdPollAv = "SV";
        public const string CmdPollIp = "IP";
        public const string CmdPollInfo = "ID";

        public const string ParamDigit0 = "=10";
        public const string ParamDigit1 = "=11";
        public const string ParamDigit2 = "=12";
        public const string ParamDigit3 = "=13";
        public const string ParamDigit4 = "=14";
        public const string ParamDigit5 = "=15";
        public const string ParamDigit6 = "=16";
        public const string ParamDigit7 = "=17";
        public const string ParamDigit8 = "=18";
        public const string ParamDigit9 = "=19";
        public const string ParamDash = "=99";
        public const string ParamKpEnter = "=21";
        public const string ParamMenu = "=29";
        public const string ParamGuide = "=63";
        public const string ParamInfo = "=100";
        public const string ParamUp = "=108";
        public const string ParamDn = "=109";
        public const string ParamLt = "=107";
        public const string ParamRt = "=106";
        public const string ParamDpEnter = "=110";
        public const string ParamExit = "=111";

        /// <summary>The tuner defaults to unit 1 when no unit number is sent, so default to "1" when none is configured.</summary>
        public static string NormalizeUnitId(string unitId)
        {
            return string.IsNullOrWhiteSpace(unitId) ? "1" : unitId.Trim();
        }

        public static string BuildCommand(string unitId, string command)
        {
            return string.Format("{0}{1}{2}\x0D", Attention, NormalizeUnitId(unitId), command);
        }

        public static string BuildCommand(string unitId, string command, string parameter)
        {
            return string.Format("{0}{1}{2}{3}\x0D", Attention, NormalizeUnitId(unitId), command, parameter);
        }

        /// <summary>Prefix of a response line for a command group, e.g. "&lt;1T"</summary>
        public static string ResponsePrefix(string unitId, string group)
        {
            return "<" + NormalizeUnitId(unitId) + group;
        }

        // ---- volume scaling -------------------------------------------------------------------------------------

        /// <summary>Tuner volume (0-100) to the Essentials 0-65535 range</summary>
        public static int PercentToEssentials(int percent)
        {
            percent = Math.Max(0, Math.Min(100, percent));
            return (int)Math.Round(percent * 65535.0 / 100.0);
        }

        /// <summary>Essentials volume (0-65535) to the tuner's 0-100 range</summary>
        public static int EssentialsToPercent(int level)
        {
            level = Math.Max(0, Math.Min(65535, level));
            return (int)Math.Round(level * 100.0 / 65535.0);
        }

        // ---- response parsing -----------------------------------------------------------------------------------

        /// <summary>
        /// Channel/source status, e.g. "&lt;1TU032U0A0002x0": power (U on, M off), major channel, video mute, input, RF, resolution, minor channel
        /// </summary>
        public static bool TryParseStatus(string data, out bool powerOn, out string channel)
        {
            powerOn = false;
            channel = null;
            if (data == null || data.Length < 4) return false;

            powerOn = char.ToUpperInvariant(data[3]) == 'U';

            int major, minor;
            if (data.Length >= 7 && int.TryParse(data.Substring(4, 3), out major))
            {
                // the manual lists the minor channel after video mute, input, RF and resolution
                if (data.Length >= 14 && int.TryParse(data.Substring(11, 3), out minor))
                    channel = minor > 0 ? major + "-" + minor : major.ToString();
                else
                    channel = major.ToString();
            }
            return true;
        }

        /// <summary>
        /// Audio status, e.g. "&lt;1VU63Ux100": power, emulated 0-63 level, mute (M muted), n/a, actual 0-100 level
        /// </summary>
        public static bool TryParseAudio(string data, out int levelPercent, out bool muted)
        {
            levelPercent = 0;
            muted = false;
            if (data == null || data.Length < 11) return false;
            if (!int.TryParse(data.Substring(8, 3), out levelPercent)) return false;

            levelPercent = Math.Min(100, levelPercent);
            muted = char.ToUpperInvariant(data[6]) == 'M';
            return true;
        }

        private static readonly Regex MacRegex = new Regex(@"MAC=([0-9A-Fa-f]{12})", RegexOptions.Compiled);
        private static readonly Regex IpRegex = new Regex(@"\bIP=(\d{1,3})\.(\d{1,3})\.(\d{1,3})\.(\d{1,3})", RegexOptions.Compiled);

        /// <summary>
        /// Network status. The manual shows "$MAC=0014C8090001" and "$IP=192.168.001.231S IG=..." (S/D = static/DHCP).
        /// Either line, with or without a "&lt;1IP" prefix, is accepted; zero padding is removed from the address.
        /// </summary>
        public static bool TryParseNetwork(string data, out string ipAddress, out string macAddress)
        {
            ipAddress = null;
            macAddress = null;
            if (string.IsNullOrEmpty(data)) return false;

            var macMatch = MacRegex.Match(data);
            if (macMatch.Success)
                macAddress = macMatch.Groups[1].Value.ToUpperInvariant();

            var ipMatch = IpRegex.Match(data);
            if (ipMatch.Success)
            {
                ipAddress = string.Format("{0}.{1}.{2}.{3}",
                    int.Parse(ipMatch.Groups[1].Value), int.Parse(ipMatch.Groups[2].Value),
                    int.Parse(ipMatch.Groups[3].Value), int.Parse(ipMatch.Groups[4].Value));
            }
            return macMatch.Success || ipMatch.Success;
        }

        /// <summary>
        /// Product ID: "&lt;1ID" followed by "model firmware". The manual doesn't give the response format; this is an assumption.
        /// </summary>
        public static bool TryParseInfo(string data, string unitId, out string model, out string firmware)
        {
            model = null;
            firmware = null;
            var prefix = ResponsePrefix(unitId, "ID");
            if (data == null || !data.StartsWith(prefix, StringComparison.Ordinal)) return false;

            var chunks = data.Substring(prefix.Length).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (chunks.Length == 0) return false;

            model = chunks[0].Trim();
            if (chunks.Length > 1) firmware = chunks[1].Trim();
            return true;
        }
    }
}
