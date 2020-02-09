using System;
using System.IO;
using System.Text;

namespace NAudio.Midi 
{
    /// <summary>
    /// Represents a MIDI control change event
    /// </summary>
    public class ControlChangeEvent : MidiEvent 
    {
        private MidiController controller;
        private byte controllerValue;

        /// <summary>
        /// Reads a control change event from a MIDI stream
        /// </summary>
        /// <param name="br">Binary reader on the MIDI stream</param>
        public ControlChangeEvent(BinaryReader br) 
        {
            byte c = br.ReadByte();
            controllerValue = br.ReadByte();
            if((c & 0x80) != 0) 
            {
                // TODO: might be a follow-on
                throw new InvalidDataException("Invalid controller");
            }
            controller = (MidiController) c;
            if((controllerValue & 0x80) != 0) 
            {
                throw new InvalidDataException(String.Format("Invalid controllerValue {0} for controller {1}, Pos 0x{2:X}", controllerValue, controller, br.BaseStream.Position));
            }
        }

        /// <summary>
        /// Creates a control change event
        /// </summary>
        /// <param name="absoluteTime">Time</param>
        /// <param name="channel">MIDI Channel Number</param>
        /// <param name="controller">The MIDI Controller</param>
        /// <param name="controllerValue">Controller value</param>
        public ControlChangeEvent(long absoluteTime, int channel, MidiController controller, int controllerValue)
            : base(absoluteTime,channel,MidiCommandCode.ControlChange)
        {
            this.Controller = controller;
            this.ControllerValue = controllerValue;
        }
        
        /// <summary>
        /// Describes this control change event
        /// </summary>
        /// <returns>A string describing this event</returns>
        public override string ToString() 
        {
            return String.Format("{0} Controller {1} Value {2}",
                base.ToString(),
                this.controller,
                this.controllerValue);
        }

        /// <summary>
        /// <see cref="MidiEvent.GetAsShortMessage" />
        /// </summary>
        public override int GetAsShortMessage()
        {
            byte c = (byte)controller;
            return base.GetAsShortMessage() + (c << 8) + (controllerValue << 16);
        }

        /// <summary>
        /// Calls base class export first, then exports the data 
        /// specific to this event
        /// <seealso cref="MidiEvent.Export">MidiEvent.Export</seealso>
        /// </summary>
        public override void Export(ref long absoluteTime, BinaryWriter writer)
        {
            base.Export(ref absoluteTime, writer);
            writer.Write((byte)controller);
            writer.Write((byte)controllerValue);
        }

        /// <summary>
        /// The controller number
        /// </summary>
        public MidiController Controller
        {
            get
            {
                return controller;
            }
            set
            {
                if ((int) value < 0 || (int) value > 127)
                {
                    throw new ArgumentOutOfRangeException("value", "Controller number must be in the range 0-127");
                }
                controller = value;
            }
        }

        /// <summary>
        /// The controller value
        /// </summary>
        public int ControllerValue
        {
            get
            {
                return controllerValue;
            }
            set
            {
                if (value < 0 || value > 127)
                {
                    throw new ArgumentOutOfRangeException("value", "Controller Value must be in the range 0-127");
                }
                controllerValue = (byte) value;
            }
        }
    }
}