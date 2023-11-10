using UnityEngine.EventSystems;

namespace jp.kshoji.unity.nearby.midi
{
    /// <summary>
    /// MIDI Note On event handler
    /// </summary>
    public interface IMidiNoteOnEventHandler : IEventSystemHandler
    {
        void OnMidiNoteOn(string deviceId, int channel, int note, int velocity);
    }

    /// <summary>
    /// MIDI Note Off event handler
    /// </summary>
    public interface IMidiNoteOffEventHandler : IEventSystemHandler
    {
        void OnMidiNoteOff(string deviceId, int channel, int note, int velocity);
    }

    /// <summary>
    /// MIDI Polyphonic Aftertouch event handler
    /// </summary>
    public interface IMidiPolyphonicAftertouchEventHandler : IEventSystemHandler
    {
        void OnMidiPolyphonicAftertouch(string deviceId, int channel, int note, int pressure);
    }

    /// <summary>
    /// MIDI Control Change event handler
    /// </summary>
    public interface IMidiControlChangeEventHandler : IEventSystemHandler
    {
        void OnMidiControlChange(string deviceId, int channel, int function, int value);
    }

    /// <summary>
    /// MIDI Program Change event handler
    /// </summary>
    public interface IMidiProgramChangeEventHandler : IEventSystemHandler
    {
        void OnMidiProgramChange(string deviceId, int channel, int program);
    }

    /// <summary>
    /// MIDI Channel Aftertouch event handler
    /// </summary>
    public interface IMidiChannelAftertouchEventHandler : IEventSystemHandler
    {
        void OnMidiChannelAftertouch(string deviceId, int channel, int pressure);
    }

    /// <summary>
    /// MIDI Pitch Wheel event handler
    /// </summary>
    public interface IMidiPitchWheelEventHandler : IEventSystemHandler
    {
        void OnMidiPitchWheel(string deviceId, int channel, int amount);
    }

    /// <summary>
    /// MIDI System Exclusive event handler
    /// </summary>
    public interface IMidiSystemExclusiveEventHandler : IEventSystemHandler
    {
        void OnMidiSystemExclusive(string deviceId, byte[] systemExclusive);
    }

    /// <summary>
    /// MIDI Time Code Quarter Frame event handler
    /// </summary>
    public interface IMidiTimeCodeQuarterFrameEventHandler : IEventSystemHandler
    {
        void OnMidiTimeCodeQuarterFrame(string deviceId, int timing);
    }

    /// <summary>
    /// MIDI Song Select event handler
    /// </summary>
    public interface IMidiSongSelectEventHandler : IEventSystemHandler
    {
        void OnMidiSongSelect(string deviceId, int song);
    }

    /// <summary>
    /// MIDI Song Position Pointer event handler
    /// </summary>
    public interface IMidiSongPositionPointerEventHandler : IEventSystemHandler
    {
        void OnMidiSongPositionPointer(string deviceId, int position);
    }

    /// <summary>
    /// MIDI Tune Request event handler
    /// </summary>
    public interface IMidiTuneRequestEventHandler : IEventSystemHandler
    {
        void OnMidiTuneRequest(string deviceId);
    }

    /// <summary>
    /// MIDI Timing Clock event handler
    /// </summary>
    public interface IMidiTimingClockEventHandler : IEventSystemHandler
    {
        void OnMidiTimingClock(string deviceId);
    }

    /// <summary>
    /// MIDI Start event handler
    /// </summary>
    public interface IMidiStartEventHandler : IEventSystemHandler
    {
        void OnMidiStart(string deviceId);
    }

    /// <summary>
    /// MIDI Continue event handler
    /// </summary>
    public interface IMidiContinueEventHandler : IEventSystemHandler
    {
        void OnMidiContinue(string deviceId);
    }

    /// <summary>
    /// MIDI Stop event handler
    /// </summary>
    public interface IMidiStopEventHandler : IEventSystemHandler
    {
        void OnMidiStop(string deviceId);
    }

    /// <summary>
    /// MIDI Active Sensing event handler
    /// </summary>
    public interface IMidiActiveSensingEventHandler : IEventSystemHandler
    {
        void OnMidiActiveSensing(string deviceId);
    }

    /// <summary>
    /// MIDI Reset event handler
    /// </summary>
    public interface IMidiResetEventHandler : IEventSystemHandler
    {
        void OnMidiReset(string deviceId);
    }

    /// <summary>
    /// MIDI Playing events handler
    /// </summary>
    public interface IMidiPlayingEventsHandler : IMidiNoteOnEventHandler, IMidiNoteOffEventHandler,
        IMidiChannelAftertouchEventHandler, IMidiPitchWheelEventHandler, IMidiPolyphonicAftertouchEventHandler,
        IMidiProgramChangeEventHandler, IMidiControlChangeEventHandler
    {
    }

    /// <summary>
    /// MIDI System events handler
    /// </summary>
    public interface IMidiSystemEventsHandler : IMidiContinueEventHandler, IMidiResetEventHandler,
        IMidiStartEventHandler, IMidiStopEventHandler, IMidiActiveSensingEventHandler, IMidiSongSelectEventHandler,
        IMidiSongPositionPointerEventHandler, IMidiSystemExclusiveEventHandler, IMidiTimeCodeQuarterFrameEventHandler,
        IMidiTimingClockEventHandler, IMidiTuneRequestEventHandler
    {
    }

    /// <summary>
    /// MIDI All events handler
    /// </summary>
    public interface IMidiAllEventsHandler : IMidiPlayingEventsHandler, IMidiSystemEventsHandler
    {
    }
}