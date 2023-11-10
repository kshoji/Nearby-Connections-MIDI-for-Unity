using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

namespace jp.kshoji.unity.nearby.midi
{
    /// <summary>
    /// Nearby MIDI Manager, will be registered as `DontDestroyOnLoad` GameObject
    /// </summary>
    public class NearbyMidiManager : MonoBehaviour, IMidiAllEventsHandler
    {
        /// <summary>
        /// Get an instance<br />
        /// SHOULD be called by Unity's main thread.
        /// </summary>
        public static NearbyMidiManager Instance => lazyInstance.Value;

        private const string NearbyServiceId = "MIDI";

        private static bool isInitialized = false;
        private static string localEndpointName = Guid.NewGuid().ToString();
        private static Dictionary<string, NearbyMidiInputDevice> inputDevices = new Dictionary<string, NearbyMidiInputDevice>();
        private static Dictionary<string, NearbyMidiOutputDevice> outputDevices = new Dictionary<string, NearbyMidiOutputDevice>();

        public HashSet<string> DeviceIdSet
        {
            get
            {
                lock (outputDevices)
                {
                    return outputDevices.Keys.ToHashSet();
                }
            }
        }

        private static readonly Lazy<NearbyMidiManager> lazyInstance = new Lazy<NearbyMidiManager>(() =>
        {
            var instance = new GameObject("NearbyMidiManager").AddComponent<NearbyMidiManager>();
            instance.Initialize();
            return instance;
        });

        public delegate void OnMidiInputDeviceAttachedHandler(string deviceId, NearbyMidiInputDevice device);
        public delegate void OnMidiInputDeviceDetachedHandler(string deviceId);
        public delegate void OnMidiOutputDeviceAttachedHandler(string deviceId, NearbyMidiOutputDevice device);
        public delegate void OnMidiOutputDeviceDetachedHandler(string deviceId);

        public event OnMidiInputDeviceAttachedHandler OnMidiInputDeviceAttached;
        public event OnMidiInputDeviceDetachedHandler OnMidiInputDeviceDetached;
        public event OnMidiOutputDeviceAttachedHandler OnMidiOutputDeviceAttached;
        public event OnMidiOutputDeviceDetachedHandler OnMidiOutputDeviceDetached;

        public delegate void OnMidiNoteOnHandler(string deviceId, int channel, int note, int velocity);
        public delegate void OnMidiNoteOffHandler(string deviceId, int channel, int note, int velocity);
        public delegate void OnMidiPolyphonicKeyPressureHandler(string deviceId, int channel, int note, int pressure);
        public delegate void OnMidiControlChangeHandler(string deviceId, int channel, int function, int value);
        public delegate void OnMidiProgramChangeHandler(string deviceId, int channel, int program);
        public delegate void OnMidiChannelPressureHandler(string deviceId, int channel, int pressure);
        public delegate void OnMidiPitchBendChangeHandler(string deviceId, int channel, int amount);
        public delegate void OnMidiSystemExclusiveHandler(string deviceId, [ReadOnlyArray] byte[] systemExclusive);
        public delegate void OnMidiTimeCodeHandler(string deviceId, int timing);
        public delegate void OnMidiSongPositionPointerHandler(string deviceId, int position);
        public delegate void OnMidiSongSelectHandler(string deviceId, int song);
        public delegate void OnMidiTuneRequestHandler(string deviceId);
        public delegate void OnMidiTimingClockHandler(string deviceId);
        public delegate void OnMidiStartHandler(string deviceId);
        public delegate void OnMidiContinueHandler(string deviceId);
        public delegate void OnMidiStopHandler(string deviceId);
        public delegate void OnMidiActiveSensingHandler(string deviceId);
        public delegate void OnMidiSystemResetHandler(string deviceId);

        public event OnMidiNoteOnHandler OnMidiNoteOn;
        public event OnMidiNoteOffHandler OnMidiNoteOff;
        public event OnMidiPolyphonicKeyPressureHandler OnMidiPolyphonicKeyPressure;
        public event OnMidiControlChangeHandler OnMidiControlChange;
        public event OnMidiProgramChangeHandler OnMidiProgramChange;
        public event OnMidiChannelPressureHandler OnMidiChannelPressure;
        public event OnMidiPitchBendChangeHandler OnMidiPitchBendChange;
        public event OnMidiSystemExclusiveHandler OnMidiSystemExclusive;
        public event OnMidiTimeCodeHandler OnMidiTimeCode;
        public event OnMidiSongPositionPointerHandler OnMidiSongPositionPointer;
        public event OnMidiSongSelectHandler OnMidiSongSelect;
        public event OnMidiTuneRequestHandler OnMidiTuneRequest;
        public event OnMidiTimingClockHandler OnMidiTimingClock;
        public event OnMidiStartHandler OnMidiStart;
        public event OnMidiContinueHandler OnMidiContinue;
        public event OnMidiStopHandler OnMidiStop;
        public event OnMidiActiveSensingHandler OnMidiActiveSensing;
        public event OnMidiSystemResetHandler OnMidiSystemReset;

        private void Initialize()
        {
            if (!isInitialized)
            {
                NearbyConnectionsManager.Instance.OnEndpointDiscovered += endpointId =>
                {
                    NearbyConnectionsManager.Instance.Connect(localEndpointName, endpointId);
                };
                NearbyConnectionsManager.Instance.OnConnectionInitiated += (endpointId, endpointName, connection) =>
                {
                    // auto accept connection
                    NearbyConnectionsManager.Instance.AcceptConnection(endpointId);
                };
                NearbyConnectionsManager.Instance.OnEndpointConnected += endpointId =>
                {
                    void PrepareOutputDevice()
                    {
                        var stream = NearbyConnectionsManager.Instance.StartSendStream(endpointId);
                        var outputDevice = new NearbyMidiOutputDevice(stream);
                        NearbyMidiOutputDevice.DeviceDisconnected outputDeviceOnOnDeviceDisconnected = null;
                        outputDeviceOnOnDeviceDisconnected = () =>
                        {
                            outputDevice.OnDeviceDisconnected -= outputDeviceOnOnDeviceDisconnected;
                            outputDevice.Close();
                            OnMidiOutputDeviceDetached?.Invoke(endpointId);
                            lock (outputDevices)
                            {
                                outputDevices.Remove(endpointId);
                            }
                            // auto reconnect if endpoint still connected
                            if (NearbyConnectionsManager.Instance.GetEstablishedConnections().Contains(endpointId))
                            {
                                PrepareOutputDevice();
                            }
                        };
                        outputDevice.OnDeviceDisconnected += outputDeviceOnOnDeviceDisconnected;

                        lock (outputDevices)
                        {
                            if (outputDevices.ContainsKey(endpointId))
                            {
                                outputDevices.Remove(endpointId);
                            }

                            outputDevices.Add(endpointId, outputDevice);
                        }
                        OnMidiOutputDeviceAttached?.Invoke(endpointId, outputDevice);
                    }

                    PrepareOutputDevice();
                };

                NearbyConnectionsManager.Instance.OnReceiveStream += (endpointId, payloadId, stream) =>
                {
                    void PrepareInputDevice()
                    {
                        var inputDevice = new NearbyMidiInputDevice(endpointId, stream, this);
                        NearbyMidiInputDevice.DeviceDisconnected inputDeviceOnOnDeviceDisconnected = null;
                        inputDeviceOnOnDeviceDisconnected = () =>
                        {
                            inputDevice.OnDeviceDisconnected -= inputDeviceOnOnDeviceDisconnected;
                            inputDevice.Close();
                            OnMidiInputDeviceDetached?.Invoke(endpointId);
                            lock (inputDevices)
                            {
                                inputDevices.Remove(endpointId);
                            }
                            // auto reconnect if endpoint still connected
                            if (NearbyConnectionsManager.Instance.GetEstablishedConnections().Contains(endpointId))
                            {
                                PrepareInputDevice();
                            }
                        };

                        inputDevice.OnDeviceDisconnected += inputDeviceOnOnDeviceDisconnected;

                        lock (inputDevices)
                        {
                            if (inputDevices.ContainsKey(endpointId))
                            {
                                inputDevices.Remove(endpointId);
                            }

                            inputDevices.Add(endpointId, inputDevice);
                        }
                        OnMidiInputDeviceAttached?.Invoke(endpointId, inputDevice);
                    }

                    PrepareInputDevice();
                };
            }

            isInitialized = false;
            NearbyConnectionsManager.Instance.Initialize(() => { isInitialized = true; });
        }

        private void Update()
        {
            lock (inputDevices)
            {
                foreach (var inputDevice in inputDevices.Values)
                {
                    inputDevice.OnUpdate();
                }
            }
        }

        private void OnApplicationQuit()
        {
            NearbyConnectionsManager.Instance.Terminate();
        }

        /// <summary>
        /// Sends a Note On message
        /// </summary>
        /// <param name="deviceId">the Device Id</param>
        /// <param name="channel">0-15</param>
        /// <param name="note">0-127</param>
        /// <param name="velocity">0-127</param>
        public void SendMidiNoteOn(string deviceId, int channel, int note, int velocity)
        {
            lock (outputDevices)
            {
                if (outputDevices.TryGetValue(deviceId, out var outputDevice))
                {
                    outputDevice.SendMidiNoteOn(channel, note, velocity);
                }
            }
        }

        /// <summary>
        /// Sends a Note Off message
        /// </summary>
        /// <param name="deviceId">the Device Id</param>
        /// <param name="channel">0-15</param>
        /// <param name="note">0-127</param>
        /// <param name="velocity">0-127</param>
        public void SendMidiNoteOff(string deviceId, int channel, int note, int velocity)
        {
            lock (outputDevices)
            {
                if (outputDevices.TryGetValue(deviceId, out var outputDevice))
                {
                    outputDevice.SendMidiNoteOff(channel, note, velocity);
                }
            }
        }

        /// <summary>
        /// Sends a Polyphonic Aftertouch message
        /// </summary>
        /// <param name="deviceId">the Device Id</param>
        /// <param name="channel">0-15</param>
        /// <param name="note">0-127</param>
        /// <param name="pressure">0-127</param>
        public void SendMidiPolyphonicAftertouch(string deviceId, int channel, int note, int pressure)
        {
            lock (outputDevices)
            {
                if (outputDevices.TryGetValue(deviceId, out var outputDevice))
                {
                    outputDevice.SendMidiPolyphonicAftertouch(channel, note, pressure);
                }
            }
        }

        /// <summary>
        /// Sends a Control Change message
        /// </summary>
        /// <param name="deviceId">the Device Id</param>
        /// <param name="channel">0-15</param>
        /// <param name="function">0-127</param>
        /// <param name="value">0-127</param>
        public void SendMidiControlChange(string deviceId, int channel, int function, int value)
        {
            lock (outputDevices)
            {
                if (outputDevices.TryGetValue(deviceId, out var outputDevice))
                {
                    outputDevice.SendMidiControlChange(channel, function, value);
                }
            }
        }

        /// <summary>
        /// Sends a Program Change message
        /// </summary>
        /// <param name="deviceId">the Device Id</param>
        /// <param name="channel">0-15</param>
        /// <param name="program">0-127</param>
        public void SendMidiProgramChange(string deviceId, int channel, int program)
        {
            lock (outputDevices)
            {
                if (outputDevices.TryGetValue(deviceId, out var outputDevice))
                {
                    outputDevice.SendMidiProgramChange(channel, program);
                }
            }
        }

        /// <summary>
        /// Sends a Channel Aftertouch message
        /// </summary>
        /// <param name="deviceId">the Device Id</param>
        /// <param name="channel">0-15</param>
        /// <param name="pressure">0-127</param>
        public void SendMidiChannelAftertouch(string deviceId, int channel, int pressure)
        {
            lock (outputDevices)
            {
                if (outputDevices.TryGetValue(deviceId, out var outputDevice))
                {
                    outputDevice.SendMidiChannelAftertouch(channel, pressure);
                }
            }
        }

        /// <summary>
        /// Sends a Pitch Wheel message
        /// </summary>
        /// <param name="deviceId">the Device Id</param>
        /// <param name="channel">0-15</param>
        /// <param name="amount">0-16383</param>
        public void SendMidiPitchWheel(string deviceId, int channel, int amount)
        {
            lock (outputDevices)
            {
                if (outputDevices.TryGetValue(deviceId, out var outputDevice))
                {
                    outputDevice.SendMidiPitchWheel(channel, amount);
                }
            }
        }

        /// <summary>
        /// Sends a System Exclusive message
        /// </summary>
        /// <param name="deviceId">the Device Id</param>
        /// <param name="sysEx">byte array starts with F0, ends with F7</param>
        public void SendMidiSystemExclusive(string deviceId, byte[] sysEx)
        {
            lock (outputDevices)
            {
                if (outputDevices.TryGetValue(deviceId, out var outputDevice))
                {
                    outputDevice.SendMidiSystemExclusive(sysEx);
                }
            }
        }

        /// <summary>
        /// Sends a Time Code Quarter Frame message
        /// </summary>
        /// <param name="deviceId">the Device Id</param>
        /// <param name="timing">0-127</param>
        public void SendMidiTimeCodeQuarterFrame(string deviceId, int timing)
        {
            lock (outputDevices)
            {
                if (outputDevices.TryGetValue(deviceId, out var outputDevice))
                {
                    outputDevice.SendMidiTimeCodeQuarterFrame(timing);
                }
            }
        }

        /// <summary>
        /// Sends a Song Select message
        /// </summary>
        /// <param name="deviceId">the Device Id</param>
        /// <param name="song">0-127</param>
        public void SendMidiSongSelect(string deviceId, int song)
        {
            lock (outputDevices)
            {
                if (outputDevices.TryGetValue(deviceId, out var outputDevice))
                {
                    outputDevice.SendMidiSongSelect(song);
                }
            }
        }

        /// <summary>
        /// Sends a Song Position Pointer message
        /// </summary>
        /// <param name="deviceId">the Device Id</param>
        /// <param name="position">0-16383</param>
        public void SendMidiSongPositionPointer(string deviceId, int position)
        {
            lock (outputDevices)
            {
                if (outputDevices.TryGetValue(deviceId, out var outputDevice))
                {
                    outputDevice.SendMidiSongPositionPointer(position);
                }
            }
        }

        /// <summary>
        /// Sends a Tune Request message
        /// </summary>
        /// <param name="deviceId">the Device Id</param>
        public void SendMidiTuneRequest(string deviceId)
        {
            lock (outputDevices)
            {
                if (outputDevices.TryGetValue(deviceId, out var outputDevice))
                {
                    outputDevice.SendMidiTuneRequest();
                }
            }
        }

        /// <summary>
        /// Sends a Timing Clock message
        /// </summary>
        /// <param name="deviceId">the Device Id</param>
        public void SendMidiTimingClock(string deviceId)
        {
            lock (outputDevices)
            {
                if (outputDevices.TryGetValue(deviceId, out var outputDevice))
                {
                    outputDevice.SendMidiTimingClock();
                }
            }
        }

        /// <summary>
        /// Sends a Start message
        /// </summary>
        /// <param name="deviceId">the Device Id</param>
        public void SendMidiStart(string deviceId)
        {
            lock (outputDevices)
            {
                if (outputDevices.TryGetValue(deviceId, out var outputDevice))
                {
                    outputDevice.SendMidiStart();
                }
            }
        }

        /// <summary>
        /// Sends a Continue message
        /// </summary>
        /// <param name="deviceId">the Device Id</param>
        public void SendMidiContinue(string deviceId)
        {
            lock (outputDevices)
            {
                if (outputDevices.TryGetValue(deviceId, out var outputDevice))
                {
                    outputDevice.SendMidiContinue();
                }
            }
        }

        /// <summary>
        /// Sends a Stop message
        /// </summary>
        /// <param name="deviceId">the Device Id</param>
        public void SendMidiStop(string deviceId)
        {
            lock (outputDevices)
            {
                if (outputDevices.TryGetValue(deviceId, out var outputDevice))
                {
                    outputDevice.SendMidiStop();
                }
            }
        }

        /// <summary>
        /// Sends an Active Sensing message
        /// </summary>
        /// <param name="deviceId">the Device Id</param>
        public void SendMidiActiveSensing(string deviceId)
        {
            lock (outputDevices)
            {
                if (outputDevices.TryGetValue(deviceId, out var outputDevice))
                {
                    outputDevice.SendMidiActiveSensing();
                }
            }
        }

        /// <summary>
        /// Sends a Reset message
        /// </summary>
        /// <param name="deviceId">the Device Id</param>
        public void SendMidiReset(string deviceId)
        {
            lock (outputDevices)
            {
                if (outputDevices.TryGetValue(deviceId, out var outputDevice))
                {
                    outputDevice.SendMidiReset();
                }
            }
        }

        /// <summary>
        /// Start to scan MIDI devices
        /// </summary>
        public void StartDiscovering()
        {
            if (!isInitialized)
            {
                Debug.LogError("NearbyConnectionsManager initialization is not finished.");
                return;
            }
            NearbyConnectionsManager.Instance.StartDiscovering(NearbyServiceId, NearbyConnectionsManager.Strategy.P2P_CLUSTER);
        }

        /// <summary>
        /// Start to advertise MIDI device
        /// </summary>
        public void StartAdvertising()
        {
            if (!isInitialized)
            {
                Debug.LogError("NearbyConnectionsManager initialization is not finished.");
                return;
            }
            NearbyConnectionsManager.Instance.StartAdvertising(localEndpointName, NearbyServiceId, NearbyConnectionsManager.Strategy.P2P_CLUSTER);
        }

        /// <summary>
        /// Stop to scan MIDI devices
        /// </summary>
        public void StopDiscovering()
        {
            if (!isInitialized)
            {
                Debug.LogError("NearbyConnectionsManager initialization is not finished.");
                return;
            }
            NearbyConnectionsManager.Instance.StopDiscovering();
        }

        /// <summary>
        /// Stop to advertise MIDI device
        /// </summary>
        public void StopAdvertising()
        {
            if (!isInitialized)
            {
                Debug.LogError("NearbyConnectionsManager initialization is not finished.");
            }
            NearbyConnectionsManager.Instance.StopAdvertising();
        }

        void IMidiNoteOnEventHandler.OnMidiNoteOn(string deviceId, int channel, int note, int velocity)
            => OnMidiNoteOn?.Invoke(deviceId, channel, note, velocity);

        void IMidiNoteOffEventHandler.OnMidiNoteOff(string deviceId, int channel, int note, int velocity)
            => OnMidiNoteOff?.Invoke(deviceId, channel, note, velocity);

        public void OnMidiChannelAftertouch(string deviceId, int channel, int pressure)
            => OnMidiChannelPressure?.Invoke(deviceId, channel, pressure);

        public void OnMidiPitchWheel(string deviceId, int channel, int amount)
            => OnMidiPitchBendChange?.Invoke(deviceId, channel, amount);

        public void OnMidiPolyphonicAftertouch(string deviceId, int channel, int note, int pressure)
            => OnMidiPolyphonicKeyPressure?.Invoke(deviceId, channel, note, pressure);

        void IMidiProgramChangeEventHandler.OnMidiProgramChange(string deviceId, int channel, int program)
            => OnMidiProgramChange?.Invoke(deviceId, channel, program);

        void IMidiControlChangeEventHandler.OnMidiControlChange(string deviceId, int channel, int function, int value)
            => OnMidiControlChange?.Invoke(deviceId, channel, function, value);

        void IMidiContinueEventHandler.OnMidiContinue(string deviceId)
            => OnMidiContinue?.Invoke(deviceId);

        public void OnMidiReset(string deviceId) => OnMidiSystemReset?.Invoke(deviceId);

        void IMidiStartEventHandler.OnMidiStart(string deviceId) => OnMidiStart?.Invoke(deviceId);

        void IMidiStopEventHandler.OnMidiStop(string deviceId) => OnMidiStop?.Invoke(deviceId);

        void IMidiActiveSensingEventHandler.OnMidiActiveSensing(string deviceId) => OnMidiActiveSensing?.Invoke(deviceId);

        void IMidiSongSelectEventHandler.OnMidiSongSelect(string deviceId, int song) => OnMidiSongSelect?.Invoke(deviceId, song);

        void IMidiSongPositionPointerEventHandler.OnMidiSongPositionPointer(string deviceId, int position)
        {
            OnMidiSongPositionPointer?.Invoke(deviceId, position);
        }

        void IMidiSystemExclusiveEventHandler.OnMidiSystemExclusive(string deviceId, byte[] systemExclusive)
            => OnMidiSystemExclusive?.Invoke(deviceId, systemExclusive);

        public void OnMidiTimeCodeQuarterFrame(string deviceId, int timing) => OnMidiTimeCode?.Invoke(deviceId, timing);

        void IMidiTimingClockEventHandler.OnMidiTimingClock(string deviceId) => OnMidiTimingClock?.Invoke(deviceId);

        void IMidiTuneRequestEventHandler.OnMidiTuneRequest(string deviceId) => OnMidiTuneRequest?.Invoke(deviceId);
    }
}