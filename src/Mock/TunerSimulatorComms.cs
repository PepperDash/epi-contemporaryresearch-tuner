using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PepperDash.Core;

namespace PepperDash.Essentials.Plugins.ContemporaryResearch.Tuner
{
    /// <summary>
    /// <see cref="IBasicCommunication"/> that answers like a 232-ATSC 4, so the real <see cref="ContemporaryResearchDevice"/>
    /// (and its Mobile Control messengers and bridge joins) can be exercised with no hardware. Registered as a device so
    /// <c>devjson</c> can drive it, e.g. <c>{"deviceKey":"tuner01-comms","methodName":"SetOffline","params":[true]}</c>
    /// or <c>{"deviceKey":"tuner01-comms","methodName":"SimulateLine","params":["&lt;1TM002U0A0002x0"]}</c>.
    /// </summary>
    public class TunerSimulatorComms : Device, IBasicCommunication
    {
        /// <summary>The tuner delays its status responses by about 125 ms; replies are also queued so they never re-enter the caller.</summary>
        private const int ResponseDelayMs = 125;
        private const int RampIntervalMs = 250;

        private readonly TunerSimulator _tuner;
        private readonly object _sync = new object();
        private Timer _rampTimer;
        private bool _offline;
        private bool _connected;

        public event EventHandler<GenericCommMethodReceiveBytesArgs> BytesReceived;
        public event EventHandler<GenericCommMethodReceiveTextArgs> TextReceived;

        public TunerSimulatorComms(string key, TunerSimulator tuner, bool offline = false)
            : base(key)
        {
            _tuner = tuner;
            _offline = offline;
        }

        public bool IsConnected
        {
            get { return _connected && !_offline; }
        }

        public void Connect()
        {
            _connected = true;
            _rampTimer = new Timer(OnRampTick, null, RampIntervalMs, RampIntervalMs);
        }

        public void Disconnect()
        {
            _connected = false;
            if (_rampTimer != null) _rampTimer.Dispose();
            _rampTimer = null;
        }

        public void SendText(string text)
        {
            if (_offline || string.IsNullOrEmpty(text)) return;

            IList<string> responses;
            lock (_sync)
            {
                // a command string may carry several commands, each ending in CR
                var all = new List<string>();
                foreach (var command in text.Split(new[] { '\r' }, StringSplitOptions.RemoveEmptyEntries))
                    foreach (var response in _tuner.Process(command))
                        all.Add(response);
                responses = all;
            }
            Emit(responses);
        }

        public void SendBytes(byte[] bytes)
        {
            SendText(Encoding.ASCII.GetString(bytes));
        }

        /// <summary>Console: drop all commands and stop replying (or resume), as if the unit were unreachable</summary>
        public void SetOffline(bool offline)
        {
            _offline = offline;
        }

        /// <summary>Console: deliver a raw line to the plugin, e.g. to mimic a front panel change</summary>
        public void SimulateLine(string line)
        {
            Emit(new[] { line });
        }

        private void OnRampTick(object state)
        {
            string audio;
            lock (_sync) audio = _tuner.Tick();
            if (audio != null && !_offline) Emit(new[] { audio });
        }

        private void Emit(IList<string> lines)
        {
            if (lines.Count == 0) return;
            Task.Delay(ResponseDelayMs).ContinueWith(_ =>
            {
                var handler = TextReceived;
                if (handler == null) return;
                foreach (var line in lines)
                    handler(this, new GenericCommMethodReceiveTextArgs(line + "\r\n"));
            });
        }
    }
}
