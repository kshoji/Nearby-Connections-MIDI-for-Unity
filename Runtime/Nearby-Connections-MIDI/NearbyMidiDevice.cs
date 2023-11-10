using System.IO;

namespace jp.kshoji.unity.nearby.midi
{
    /// <summary>
    /// Nearby MIDI Input Device
    /// </summary>
    public class NearbyMidiInputDevice
    {
        private Stream stream;
        byte[] buffer = new byte[1024];
        private MidiParser midiParser;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="stream"></param>
        /// <param name="eventsHandler"></param>
        public NearbyMidiInputDevice(string endpointId, Stream stream, IMidiAllEventsHandler eventsHandler)
        {
            this.stream = stream;
            midiParser = new MidiParser(endpointId, eventsHandler);
        }

        public delegate void DeviceDisconnected();
        public event DeviceDisconnected OnDeviceDisconnected;

        /// <summary>
        /// Close the device
        /// </summary>
        public void Close()
        {
            if (stream == null)
            {
                return;
            }

            stream.Close();
            stream = null;
            OnDeviceDisconnected?.Invoke();
        }

        /// <summary>
        /// Check stream updates.
        /// Called every frames
        /// </summary>
        public void OnUpdate()
        {
            if (stream == null)
            {
                return;
            }

            var read = stream.Read(buffer);
            if (read > 0)
            {
                midiParser.Parse(buffer, read);
            }
            else if (read == -1)
            {
                stream.Close();
                stream = null;
                OnDeviceDisconnected?.Invoke();
            }
        }
    }

    /// <summary>
    /// Nearby MIDI Output Device
    /// </summary>
    public class NearbyMidiOutputDevice
    {
        private Stream stream;
        byte[] buffer = new byte[3];

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="stream">the Nearby stream</param>
        public NearbyMidiOutputDevice(Stream stream)
        {
            this.stream = stream;
        }

        public delegate void DeviceDisconnected();
        public event DeviceDisconnected OnDeviceDisconnected;

        /// <summary>
        /// Close the device
        /// </summary>
        public void Close()
        {
            if (stream == null)
            {
                return;
            }

            stream.Close();
            stream = null;
            OnDeviceDisconnected?.Invoke();
        }

        private void SendMidiData(byte[] data, int count)
        {
            if (stream == null)
            {
                return;
            }

            if (stream.CanWrite == false)
            {
                Close();
                return;
            }

            stream.Write(data, 0, count);
        }

        /// <summary>
        /// Sends a Note On message
        /// </summary>
        /// <param name="channel">0-15</param>
        /// <param name="note">0-127</param>
        /// <param name="velocity">0-127</param>
        public void SendMidiNoteOn(int channel, int note, int velocity)
        {
            buffer[0] = (byte)((channel & 0x0f) | 0x90);
            buffer[1] = (byte)(note & 0x7f);
            buffer[2] = (byte)(velocity & 0x7f);
            SendMidiData(buffer, 3);
        }

        /// <summary>
        /// Sends a Note Off message
        /// </summary>
        /// <param name="channel">0-15</param>
        /// <param name="note">0-127</param>
        /// <param name="velocity">0-127</param>
        public void SendMidiNoteOff(int channel, int note, int velocity)
        {
            buffer[0] = (byte)((channel & 0x0f) | 0x80);
            buffer[1] = (byte)(note & 0x7f);
            buffer[2] = (byte)(velocity & 0x7f);
            SendMidiData(buffer, 3);
        }

        /// <summary>
        /// Sends a Polyphonic Aftertouch message
        /// </summary>
        /// <param name="channel">0-15</param>
        /// <param name="note">0-127</param>
        /// <param name="pressure">0-127</param>
        public void SendMidiPolyphonicAftertouch(int channel, int note, int pressure)
        {
            buffer[0] = (byte)((channel & 0x0f) | 0xa0);
            buffer[1] = (byte)(note & 0x7f);
            buffer[2] = (byte)(pressure & 0x7f);
            SendMidiData(buffer, 3);
        }

        /// <summary>
        /// Sends a Control Change message
        /// </summary>
        /// <param name="channel">0-15</param>
        /// <param name="controlFunction">0-127</param>
        /// <param name="controlValue">0-127</param>
        public void SendMidiControlChange(int channel, int controlFunction, int controlValue)
        {
            buffer[0] = (byte)((channel & 0x0f) | 0xb0);
            buffer[1] = (byte)(controlFunction & 0x7f);
            buffer[2] = (byte)(controlValue & 0x7f);
            SendMidiData(buffer, 3);
        }

        /// <summary>
        /// Sends a Program Change message
        /// </summary>
        /// <param name="channel">0-15</param>
        /// <param name="program">0-127</param>
        public void SendMidiProgramChange(int channel, int program)
        {
            buffer[0] = (byte)((channel & 0x0f) | 0xc0);
            buffer[1] = (byte)(program & 0x7f);
            SendMidiData(buffer, 2);
        }

        /// <summary>
        /// Sends a Channel Aftertouch message
        /// </summary>
        /// <param name="channel">0-15</param>
        /// <param name="pressure">0-127</param>
        public void SendMidiChannelAftertouch(int channel, int pressure)
        {
            buffer[0] = (byte)((channel & 0x0f) | 0xd0);
            buffer[1] = (byte)(pressure & 0x7f);
            SendMidiData(buffer, 2);
        }

        /// <summary>
        /// Sends a Pitch Wheel message
        /// </summary>
        /// <param name="channel">0-15</param>
        /// <param name="amount">0-16383</param>
        public void SendMidiPitchWheel(int channel, int amount)
        {
            buffer[0] = (byte)((channel & 0x0f) | 0xe0);
            buffer[1] = (byte)(amount & 0x7f);
            buffer[2] = (byte)((amount >> 7) & 0x7f);
            SendMidiData(buffer, 3);
        }

        /// <summary>
        /// Sends a System Exclusive message
        /// </summary>
        /// <param name="sysEx">byte array starts with F0, ends with F7</param>
        public void SendMidiSystemExclusive(byte[] sysEx)
        {
            SendMidiData(sysEx, sysEx.Length);
        }

        /// <summary>
        /// Sends a Time Code Quarter Frame message
        /// </summary>
        /// <param name="timing">0-127</param>
        public void SendMidiTimeCodeQuarterFrame(int timing)
        {
            buffer[0] = 0xf1;
            buffer[1] = (byte)(timing & 0x7f);
            SendMidiData(buffer, 2);
        }

        /// <summary>
        /// Sends a Song Select message
        /// </summary>
        /// <param name="song">0-127</param>
        public void SendMidiSongSelect(int song)
        {
            buffer[0] = 0xf3;
            buffer[1] = (byte)(song & 0x7f);
            SendMidiData(buffer, 2);
        }

        /// <summary>
        /// Sends a Song Position Pointer message
        /// </summary>
        /// <param name="position">0-16383</param>
        public void SendMidiSongPositionPointer(int position)
        {
            buffer[0] = 0xf2;
            buffer[1] = (byte)(position & 0x7f);
            buffer[2] = (byte)((position >> 7) & 0x7f);
            SendMidiData(buffer, 3);
        }

        /// <summary>
        /// Sends a Tune Request message
        /// </summary>
        public void SendMidiTuneRequest()
        {
            buffer[0] = 0xf6;
            SendMidiData(buffer, 1);
        }

        /// <summary>
        /// Sends a Timing Clock message
        /// </summary>
        public void SendMidiTimingClock()
        {
            buffer[0] = 0xf8;
            SendMidiData(buffer, 1);
        }

        /// <summary>
        /// Sends a Start message
        /// </summary>
        public void SendMidiStart()
        {
            buffer[0] = 0xfa;
            SendMidiData(buffer, 1);
        }

        /// <summary>
        /// Sends a Continue message
        /// </summary>
        public void SendMidiContinue()
        {
            buffer[0] = 0xfb;
            SendMidiData(buffer, 1);
        }

        /// <summary>
        /// Sends a Stop message
        /// </summary>
        public void SendMidiStop()
        {
            buffer[0] = 0xfc;
            SendMidiData(buffer, 1);
        }

        /// <summary>
        /// Sends an Active Sensing message
        /// </summary>
        public void SendMidiActiveSensing()
        {
            buffer[0] = 0xfe;
            SendMidiData(buffer, 1);
        }

        /// <summary>
        /// Sends a Reset message
        /// </summary>
        public void SendMidiReset()
        {
            buffer[0] = 0xff;
            SendMidiData(buffer, 1);
        }
    }
}