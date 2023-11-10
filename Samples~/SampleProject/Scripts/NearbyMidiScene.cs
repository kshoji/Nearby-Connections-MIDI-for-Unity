using System;
using System.Collections.Generic;
using System.Linq;
using jp.kshoji.unity.nearby.midi;
using UnityEngine;

public class NearbyMidiScene : MonoBehaviour
{
    private float guiScale;

    private const int MaxNumberOfReceiverMidiMessages = 50;
    private readonly List<string> receivedMidiMessages = new List<string>();

    private const int SendMidiWindow = 0;
    private const int ReceiveMidiWindow = 1;
    private const int MidiConnectionWindow = 3;

    private Rect midiConnectionWindowRect = new Rect(0, 0, 400, 400);
    private Rect sendMidiWindowRect = new Rect(25, 25, 400, 400);
    private Rect receiveMidiWindowRect = new Rect(50, 50, 400, 400);

    private int deviceIdIndex;
    private float channel;
    private float noteNumber = 64f;
    private float velocity = 100f;
    private float program;
    private float controlFunction;
    private float controlValue;
    private Vector2 receiveMidiWindowScrollPosition;

    private void Awake()
    {
        guiScale = (Screen.width > Screen.height) ? Screen.width / 1024f : Screen.height / 1024f;

        NearbyMidiManager.Instance.OnMidiNoteOn += OnMidiNoteOn;
        NearbyMidiManager.Instance.OnMidiNoteOff += OnMidiNoteOff;
        NearbyMidiManager.Instance.OnMidiProgramChange += OnMidiProgramChange;
        NearbyMidiManager.Instance.OnMidiControlChange += OnMidiControlChange;

        NearbyMidiManager.Instance.OnMidiPolyphonicKeyPressure += OnMidiPolyphonicAftertouch;
        NearbyMidiManager.Instance.OnMidiChannelPressure += OnMidiChannelAftertouch;
        NearbyMidiManager.Instance.OnMidiPitchBendChange += OnMidiPitchWheel;
        NearbyMidiManager.Instance.OnMidiSystemExclusive += OnMidiSystemExclusive;
        NearbyMidiManager.Instance.OnMidiTimeCode += OnMidiTimeCodeQuarterFrame;
        NearbyMidiManager.Instance.OnMidiSongPositionPointer += OnMidiSongPositionPointer;
        NearbyMidiManager.Instance.OnMidiSongSelect += OnMidiSongSelect;
        NearbyMidiManager.Instance.OnMidiTuneRequest += OnMidiTuneRequest;
        NearbyMidiManager.Instance.OnMidiTimingClock += OnMidiTimingClock;
        NearbyMidiManager.Instance.OnMidiStart += OnMidiStart;
        NearbyMidiManager.Instance.OnMidiContinue += OnMidiContinue;
        NearbyMidiManager.Instance.OnMidiStop += OnMidiStop;
        NearbyMidiManager.Instance.OnMidiActiveSensing += OnMidiActiveSensing;
        NearbyMidiManager.Instance.OnMidiSystemReset += OnMidiReset;

        NearbyMidiManager.Instance.OnMidiOutputDeviceAttached += OnMidiOutputDeviceAttached;
        NearbyMidiManager.Instance.OnMidiOutputDeviceDetached += OnMidiOutputDeviceDetached;
        NearbyMidiManager.Instance.OnMidiInputDeviceAttached += OnMidiInputDeviceAttached;
        NearbyMidiManager.Instance.OnMidiInputDeviceDetached += OnMidiInputDeviceDetached;
    }

    private void OnApplicationQuit()
    {
        NearbyMidiManager.Instance.OnMidiNoteOn -= OnMidiNoteOn;
        NearbyMidiManager.Instance.OnMidiNoteOff -= OnMidiNoteOff;
        NearbyMidiManager.Instance.OnMidiControlChange -= OnMidiControlChange;
        NearbyMidiManager.Instance.OnMidiProgramChange -= OnMidiProgramChange;

        NearbyMidiManager.Instance.OnMidiPolyphonicKeyPressure -= OnMidiPolyphonicAftertouch;
        NearbyMidiManager.Instance.OnMidiChannelPressure -= OnMidiChannelAftertouch;
        NearbyMidiManager.Instance.OnMidiPitchBendChange -= OnMidiPitchWheel;
        NearbyMidiManager.Instance.OnMidiSystemExclusive -= OnMidiSystemExclusive;
        NearbyMidiManager.Instance.OnMidiTimeCode -= OnMidiTimeCodeQuarterFrame;
        NearbyMidiManager.Instance.OnMidiSongPositionPointer -= OnMidiSongPositionPointer;
        NearbyMidiManager.Instance.OnMidiSongSelect -= OnMidiSongSelect;
        NearbyMidiManager.Instance.OnMidiTuneRequest -= OnMidiTuneRequest;
        NearbyMidiManager.Instance.OnMidiTimingClock -= OnMidiTimingClock;
        NearbyMidiManager.Instance.OnMidiStart -= OnMidiStart;
        NearbyMidiManager.Instance.OnMidiContinue -= OnMidiContinue;
        NearbyMidiManager.Instance.OnMidiStop -= OnMidiStop;
        NearbyMidiManager.Instance.OnMidiActiveSensing -= OnMidiActiveSensing;
        NearbyMidiManager.Instance.OnMidiSystemReset -= OnMidiReset;
        
        NearbyMidiManager.Instance.OnMidiOutputDeviceAttached -= OnMidiOutputDeviceAttached;
        NearbyMidiManager.Instance.OnMidiOutputDeviceDetached -= OnMidiOutputDeviceDetached;
        NearbyMidiManager.Instance.OnMidiInputDeviceAttached -= OnMidiInputDeviceAttached;
        NearbyMidiManager.Instance.OnMidiInputDeviceDetached -= OnMidiInputDeviceDetached;
    }

    private void OnGUI()
    {
        if (Event.current.type != EventType.Layout)
        {
            return;
        }

        GUIUtility.ScaleAroundPivot(new Vector2(guiScale, guiScale), Vector2.zero);

        sendMidiWindowRect = GUILayout.Window(SendMidiWindow, sendMidiWindowRect, OnGUIWindow, "Send MIDI");
        receiveMidiWindowRect = GUILayout.Window(ReceiveMidiWindow, receiveMidiWindowRect, OnGUIWindow, "Receive MIDI");
        midiConnectionWindowRect = GUILayout.Window(MidiConnectionWindow, midiConnectionWindowRect, OnGUIWindow, "MIDI Connections");
    }

    private void OnGUIWindow(int id)
    {
        switch (id)
        {
            case SendMidiWindow:
                GUILayout.Label("Device: ");
                var deviceIds = NearbyMidiManager.Instance.DeviceIdSet.ToArray();
                if (deviceIds.Length == 0)
                {
                    GUILayout.Label("No devices connected");
                }
                else
                {
                    // get device name for device ID
                    deviceIdIndex = GUILayout.SelectionGrid(deviceIdIndex, deviceIds, 1);

                    GUILayout.Label($"Channel: {(int)channel}");
                    channel = GUILayout.HorizontalSlider(channel, 0, 16.9f);
                    GUILayout.Label($"Note: {(int)noteNumber}");
                    noteNumber = GUILayout.HorizontalSlider(noteNumber, 0, 127.9f);
                    GUILayout.Label($"Velocity: {(int)velocity}");
                    velocity = GUILayout.HorizontalSlider(velocity, 0, 127.9f);

                    if (GUILayout.Button("NoteOn"))
                    {
                        NearbyMidiManager.Instance.SendMidiNoteOn(deviceIds[deviceIdIndex], (int)channel, (int)noteNumber, (int)velocity);
                    }
                    if (GUILayout.Button("NoteOff"))
                    {
                        NearbyMidiManager.Instance.SendMidiNoteOff(deviceIds[deviceIdIndex], (int)channel, (int)noteNumber, (int)velocity);
                    }
                
                    GUILayout.Label($"Program: {(int)program}");
                    program = GUILayout.HorizontalSlider(program, 0, 127.9f);
                    if (GUILayout.Button("ProgramChange"))
                    {
                        NearbyMidiManager.Instance.SendMidiProgramChange(deviceIds[deviceIdIndex], (int)channel, (int)program);
                    }

                    GUILayout.Label($"Control Function: {(int)controlFunction}");
                    controlFunction = GUILayout.HorizontalSlider(controlFunction, 0, 127.9f);
                    GUILayout.Label($"Control Value: {(int)controlValue}");
                    controlValue = GUILayout.HorizontalSlider(controlValue, 0, 127.9f);
                    if (GUILayout.Button("ControlChange"))
                    {
                        NearbyMidiManager.Instance.SendMidiControlChange(deviceIds[deviceIdIndex], (int)channel, (int)controlFunction, (int)controlValue);
                    }
                }
                break;

            case ReceiveMidiWindow:
                receiveMidiWindowScrollPosition = GUILayout.BeginScrollView(receiveMidiWindowScrollPosition);
                GUILayout.Label("Midi messages: ");
                if (receivedMidiMessages.Count > MaxNumberOfReceiverMidiMessages)
                {
                    receivedMidiMessages.RemoveRange(0, receivedMidiMessages.Count - MaxNumberOfReceiverMidiMessages);
                }
                foreach (var message in receivedMidiMessages.AsReadOnly().Reverse())
                {
                    GUILayout.Label(message);
                }
                GUILayout.EndScrollView();
                break;
            
                case MidiConnectionWindow:
                    if (GUILayout.Button("Discover MIDI devices"))
                    {
                        NearbyMidiManager.Instance.StartDiscovering();
                    }
                    if (GUILayout.Button("Stop discovering"))
                    {
                        NearbyMidiManager.Instance.StopDiscovering();
                    }
                    GUILayout.Space(20f);

                    if (GUILayout.Button("Advertise as MIDI device"))
                    {
                        NearbyMidiManager.Instance.StartAdvertising();
                    }
                    if (GUILayout.Button("Stop advertising"))
                    {
                        NearbyMidiManager.Instance.StopAdvertising();
                    }
                    GUILayout.Space(20f);
                    break;
        }
        GUI.DragWindow();
    }

    private void OnMidiNoteOn(string deviceId, int channel, int note, int velocity)
    {
        receivedMidiMessages.Add($"OnMidiNoteOn from: {deviceId}, channel: {channel}, note: {note}, velocity: {velocity}");
    }

    private void OnMidiNoteOff(string deviceId, int channel, int note, int velocity)
    {
        receivedMidiMessages.Add($"OnMidiNoteOff from: {deviceId}, channel: {channel}, note: {note}, velocity: {velocity}");
    }

    private void OnMidiChannelAftertouch(string deviceId, int channel, int pressure)
    {
        receivedMidiMessages.Add($"OnMidiChannelAftertouch from: {deviceId}, channel: {channel}, pressure: {pressure}");
    }

    private void OnMidiPitchWheel(string deviceId, int channel, int amount)
    {
        receivedMidiMessages.Add($"OnMidiPitchWheel from: {deviceId}, channel: {channel}, amount: {amount}");
    }

    private void OnMidiPolyphonicAftertouch(string deviceId, int channel, int note, int pressure)
    {
        receivedMidiMessages.Add($"OnMidiPolyphonicAftertouch from: {deviceId}, channel: {channel}, note: {note}, pressure: {pressure}");
    }

    private void OnMidiProgramChange(string deviceId, int channel, int program)
    {
        receivedMidiMessages.Add($"OnMidiProgramChange from: {deviceId}, channel: {channel}, program: {program}");
    }

    private void OnMidiControlChange(string deviceId, int channel, int function, int value)
    {
        receivedMidiMessages.Add($"OnMidiControlChange from: {deviceId}, channel: {channel}, function: {function}, value: {value}");
    }

    private void OnMidiContinue(string deviceId)
    {
        receivedMidiMessages.Add($"OnMidiContinue from: {deviceId}");
    }

    private void OnMidiReset(string deviceId)
    {
        receivedMidiMessages.Add($"OnMidiReset from: {deviceId}");
    }

    private void OnMidiStart(string deviceId)
    {
        receivedMidiMessages.Add($"OnMidiStart from: {deviceId}");
    }

    private void OnMidiStop(string deviceId)
    {
        receivedMidiMessages.Add($"OnMidiStop from: {deviceId}");
    }

    private void OnMidiActiveSensing(string deviceId)
    {
        // too many events received, so commented out
        // receivedMidiMessages.Add("OnMidiActiveSensing");
    }

    private void OnMidiSongSelect(string deviceId, int song)
    {
        receivedMidiMessages.Add($"OnMidiSongSelect from: {deviceId}, song: {song}");
    }

    private void OnMidiSongPositionPointer(string deviceId, int position)
    {
        receivedMidiMessages.Add($"OnMidiSongPositionPointer from: {deviceId}, song: {position}");
    }

    private void OnMidiSystemExclusive(string deviceId, byte[] systemExclusive)
    {
        receivedMidiMessages.Add($"OnMidiSystemExclusive from: {deviceId}, systemExclusive: {BitConverter.ToString(systemExclusive).Replace("-", " ")}");
    }

    private void OnMidiTimeCodeQuarterFrame(string deviceId, int timing)
    {
        receivedMidiMessages.Add($"OnMidiTimeCodeQuarterFrame from: {deviceId}, timing: {timing}");
    }

    private void OnMidiTimingClock(string deviceId)
    {
        receivedMidiMessages.Add($"OnMidiTimingClock from: {deviceId}");
    }

    private void OnMidiTuneRequest(string deviceId)
    {
        receivedMidiMessages.Add($"OnMidiTuneRequest from: {deviceId}");
    }

    private void OnMidiInputDeviceAttached(string deviceId, NearbyMidiInputDevice inputDevice)
    {
        receivedMidiMessages.Add($"MIDI Input device attached. deviceId: {deviceId}");
    }

    private void OnMidiOutputDeviceAttached(string deviceId, NearbyMidiOutputDevice outputDevice)
    {
        receivedMidiMessages.Add($"MIDI Output device attached. deviceId: {deviceId}");
    }

    private void OnMidiInputDeviceDetached(string deviceId)
    {
        receivedMidiMessages.Add($"MIDI Input device detached. deviceId: {deviceId}");
    }

    private void OnMidiOutputDeviceDetached(string deviceId)
    {
        receivedMidiMessages.Add($"MIDI Output device detached. deviceId: {deviceId}");
    }
}
