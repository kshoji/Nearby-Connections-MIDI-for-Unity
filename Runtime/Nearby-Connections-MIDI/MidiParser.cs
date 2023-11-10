using System.IO;

namespace jp.kshoji.unity.nearby.midi
{
    public class MidiParser
    {
        // MIDI event message
        private int midiEventKind;
        private int midiEventNote;

        // for SysEx messages
        private readonly object systemExclusiveLock = new object();
        private MemoryStream systemExclusiveStream;

        // states
        enum MidiState
        {
            Wait,
            Signal2Of2Bytes,
            Signal2Of3Bytes,
            Signal3Of3Bytes,
            Sysex,
        }
        private MidiState midiState;

        private string sender;
        private IMidiAllEventsHandler midiInputEventListener;

        public MidiParser(string endpointId, IMidiAllEventsHandler midiAllEventsHandler)
        {
            sender = endpointId;
            midiInputEventListener = midiAllEventsHandler;
        }

        /**
         * Parses MIDI events
         *
         * @param eventData the event byte
         */
        private void ParseMidiEvent(byte midiEvent)
        {
            if (midiState == MidiState.Wait)
            {
                switch (midiEvent & 0xf0)
                {
                    case 0xf0:
                    {
                        switch (midiEvent)
                        {
                            case 0xf0:
                                lock (systemExclusiveLock)
                                {
                                    systemExclusiveStream = new MemoryStream();
                                    systemExclusiveStream.WriteByte(midiEvent);
                                }

                                midiState = MidiState.Sysex;
                                break;

                            case 0xf1:
                            case 0xf3:
                                // 0xf1 MIDI Time Code Quarter Frame. : 2bytes
                                // 0xf3 Song Select. : 2bytes
                                midiEventKind = midiEvent;
                                midiState = MidiState.Signal2Of2Bytes;
                                break;

                            case 0xf2:
                                // 0xf2 Song Position Pointer. : 3bytes
                                midiEventKind = midiEvent;
                                midiState = MidiState.Signal2Of3Bytes;
                                break;

                            case 0xf6:
                                // 0xf6 Tune Request : 1byte
                                midiInputEventListener?.OnMidiTuneRequest(sender);
                                midiState = MidiState.Wait;
                                break;
                            case 0xf8:
                                // 0xf8 Timing Clock : 1byte
                                midiInputEventListener?.OnMidiTimingClock(sender);
                                midiState = MidiState.Wait;
                                break;
                            case 0xfa:
                                // 0xfa Start : 1byte
                                midiInputEventListener?.OnMidiStart(sender);
                                midiState = MidiState.Wait;
                                break;
                            case 0xfb:
                                // 0xfb Continue : 1byte
                                midiInputEventListener?.OnMidiContinue(sender);
                                midiState = MidiState.Wait;
                                break;
                            case 0xfc:
                                // 0xfc Stop : 1byte
                                midiInputEventListener?.OnMidiStop(sender);
                                midiState = MidiState.Wait;
                                break;
                            case 0xfe:
                                // 0xfe Active Sensing : 1byte
                                midiInputEventListener?.OnMidiActiveSensing(sender);
                                midiState = MidiState.Wait;
                                break;
                            case 0xff:
                                // 0xff Reset : 1byte
                                midiInputEventListener?.OnMidiReset(sender);
                                midiState = MidiState.Wait;
                                break;
                        }
                    }
                        break;
                    case 0x80:
                    case 0x90:
                    case 0xa0:
                    case 0xb0:
                    case 0xe0:
                        // 3bytes pattern
                        midiEventKind = midiEvent;
                        midiState = MidiState.Signal2Of3Bytes;
                        break;
                    case 0xc0: // program change
                    case 0xd0: // channel after-touch
                        // 2bytes pattern
                        midiEventKind = midiEvent;
                        midiState = MidiState.Signal2Of2Bytes;
                        break;
                    default:
                        // 0x00 - 0x70: running status
                        if ((midiEventKind & 0xf0) != 0xf0)
                        {
                            // previous event kind is multi-bytes pattern
                            midiEventNote = midiEvent;
                            midiState = MidiState.Signal3Of3Bytes;
                        }

                        break;
                }
            }
            else if (midiState == MidiState.Signal2Of2Bytes)
            {
                switch (midiEventKind & 0xf0)
                {
                    // 2bytes pattern
                    case 0xc0: // program change
                        midiEventNote = midiEvent;
                        midiInputEventListener?.OnMidiProgramChange(sender, midiEventKind & 0xf, midiEventNote);
                        midiState = MidiState.Wait;
                        break;
                    case 0xd0: // channel after-touch
                        midiEventNote = midiEvent;
                        midiInputEventListener?.OnMidiChannelAftertouch(sender, midiEventKind & 0xf, midiEventNote);
                        midiState = MidiState.Wait;
                        break;
                    case 0xf0:
                    {
                        switch (midiEventKind)
                        {
                            case 0xf1:
                                // 0xf1 MIDI Time Code Quarter Frame. : 2bytes
                                midiEventNote = midiEvent;
                                midiInputEventListener?.OnMidiTimeCodeQuarterFrame(sender, midiEventNote);
                                midiState = MidiState.Wait;
                                break;
                            case 0xf3:
                                // 0xf3 Song Select. : 2bytes
                                midiEventNote = midiEvent;
                                midiInputEventListener?.OnMidiSongSelect(sender, midiEventNote);
                                midiState = MidiState.Wait;
                                break;
                            default:
                                // illegal state
                                midiState = MidiState.Wait;
                                break;
                        }
                    }
                        break;
                    default:
                        // illegal state
                        midiState = MidiState.Wait;
                        break;
                }
            }
            else if (midiState == MidiState.Signal2Of3Bytes)
            {
                switch (midiEventKind & 0xf0)
                {
                    case 0x80:
                    case 0x90:
                    case 0xa0:
                    case 0xb0:
                    case 0xe0:
                    case 0xf0:
                        // 3bytes pattern
                        midiEventNote = midiEvent;
                        midiState = MidiState.Signal3Of3Bytes;
                        break;
                    default:
                        // illegal state
                        midiState = MidiState.Wait;
                        break;
                }
            }
            else if (midiState == MidiState.Signal3Of3Bytes)
            {
                switch (midiEventKind & 0xf0)
                {
                    // 3bytes pattern
                    case 0x80: // note off
                        midiInputEventListener?.OnMidiNoteOff(sender, midiEventKind & 0xf, midiEventNote, midiEvent);
                        midiState = MidiState.Wait;
                        break;
                    case 0x90: // note on
                        if (midiEvent == 0)
                        {
                            midiInputEventListener?.OnMidiNoteOff(sender, midiEventKind & 0xf, midiEventNote, midiEvent);
                        }
                        else
                        {
                            midiInputEventListener?.OnMidiNoteOn(sender, midiEventKind & 0xf, midiEventNote, midiEvent);
                        }

                        midiState = MidiState.Wait;
                        break;
                    case 0xa0: // control polyphonic key pressure
                        midiInputEventListener?.OnMidiPolyphonicAftertouch(sender, midiEventKind & 0xf, midiEventNote, midiEvent);
                        midiState = MidiState.Wait;
                        break;
                    case 0xb0: // control change
                        midiInputEventListener?.OnMidiControlChange(sender, midiEventKind & 0xf, midiEventNote, midiEvent);
                        midiState = MidiState.Wait;
                        break;
                    case 0xe0: // pitch bend
                        midiInputEventListener?.OnMidiPitchWheel(sender, midiEventKind & 0xf, (midiEventNote & 0x7f) | ((midiEvent & 0x7f) << 7));
                        midiState = MidiState.Wait;
                        break;
                    case 0xf0: // Song Position Pointer.
                        midiInputEventListener?.OnMidiSongPositionPointer(sender, (midiEventNote & 0x7f) | ((midiEvent & 0x7f) << 7));
                        midiState = MidiState.Wait;
                        break;
                    default:
                        // illegal state
                        midiState = MidiState.Wait;
                        break;
                }
            }
            else if (midiState == MidiState.Sysex)
            {
                if (midiEvent == 0xf7)
                {
                    // the end of message
                    lock (systemExclusiveLock)
                    {
                        midiInputEventListener?.OnMidiSystemExclusive(sender, systemExclusiveStream.ToArray());
                        systemExclusiveStream.Dispose();
                        systemExclusiveStream = null;
                    }

                    midiState = MidiState.Wait;
                }
                else
                {
                    lock (systemExclusiveLock)
                    {
                        systemExclusiveStream.WriteByte(midiEvent);
                    }
                }
            }
        }

        /**
         * Updates incoming data
         *
         * @param data incoming data
         */
        public void Parse(byte[] data, int length)
        {
            if (data == null || length <= 0)
            {
                return;
            }

            for (var i = 0; i < length; i++)
            {
                ParseMidiEvent(data[i]);
            }
        }
    }
}
