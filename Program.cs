using System;
using System.Collections.Generic;
using System.Linq;

namespace PacketMarkup
{
    class Packet
    {
        public enum Unit
        {
            Bit, Byte
        }

        public interface IPacketField
        {
            Unit InUnit { get; }
            string Name { get; }
            int Size { get; }

            void AddChild(IPacketField child);
            void DrawAscii();
        }

        public abstract class Field : IPacketField
        {
            protected string _name;
            protected int _size;
            protected Unit _unit;
            
            protected List<IPacketField> children = new List<IPacketField>();
                        
            string IPacketField.Name => _name;
            int IPacketField.Size => _size;
            Unit IPacketField.InUnit => _unit;

            public Field(string name, int size)
            {
                _name = name;
                _size = size;
                _unit = Unit.Byte;
            }

            public abstract void AddChild(IPacketField child);
            public abstract void DrawAscii();

            static public void CenteredText(string txt, int area)
            {
                int padding = (area * 8 - txt.Length) / 2 - 2;
                for (int i = 0; i < padding; i++)
                    Console.Write(".");
                Console.Write(" " + txt + " ");
                for (int i = 0; i < padding; i++)
                    Console.Write(".");
            }
        }

        public class SimpleField : Field
        {
            public SimpleField(string name, int size) : base(name, size) { }

            public override void AddChild(IPacketField child) =>
                  throw new InvalidOperationException();

            public override void DrawAscii()
            {
                Console.Write("| ");
                Field.CenteredText(_name, _size);
            }
        }

        public class ArrayField : Field
        {
            public ArrayField(string name, int size) : base(name, size) { }

            public override void AddChild(IPacketField child) =>
                children.Add(child);

            public override void DrawAscii()
            {
                Console.Write("| ");

                int i = children.Sum(x => x.Size) - 1;
                foreach (IPacketField item in children)
                {
                    Console.Write("|");
                    string content = _name + "[" + i + "] = " + item.Name;
                    Field.CenteredText(content, item.Size);
                    i -= item.Size;
                }
            }
        }

        public class FlagField : Field
        {
            public FlagField(string name, int size) : base(name, size) =>
                _unit = Unit.Bit;

            public override void AddChild(IPacketField child) =>
                children.Add(child);

            public int TotalLengthInBytes()
            {
                int sum = children.Sum(x => x.Size);
                return sum / 8 + ((sum % 8 > 0) ? 1 : 0); 
            }

            public override void DrawAscii()
            {
                Console.Write("| ");

                foreach (IPacketField item in children)
                    Console.Write(item.Name + " ");
            }

        }
    }

    class Program
    {
        enum State
        {
            Ready,
            SubReady,
            ReadName,
            ReadString,
            Append, 
            ReadNumber,
            ReadFlags,
            ReadLength
        }

        static void Cout(string msg, ConsoleColor cc)
        {
            Console.ForegroundColor = cc;
            Console.Write(" " + msg);
            Console.ResetColor();
        }

        static void Main(string[] args)
        {
            string raw = "ID:\"SLLCPv\"+Version+0 OpCode Manufacturer[13]+0 ModelName[14]+0 Flags:{Re[2] DL[2] HW HE DC[2]} Interfaces:{DmxIn[4] DmxOut[4] MidiIn[4] MidiOut[4] LaserOut[4] StripOut[4]}";
            string tmpStr = string.Empty;

            State state = State.Ready;
            bool isSub = false;

            foreach (char c in raw)
            {
                switch (state)
                {
                    case State.Ready:
                    {
                        tmpStr = string.Empty;
                        if (char.IsLetter(c))
                        {
                            state = State.ReadName;
                            tmpStr += c;
                        }
                        else if (char.IsDigit(c))
                        {
                            state = State.ReadNumber;
                            tmpStr += c;
                        }
                        else if (c == '{')
                            state = State.SubReady;
                        else if (c == '"')
                            state = State.ReadString;
                        else if (c == '+')
                            state = State.Append;
                        isSub = false;
                        break;
                    }

                    case State.SubReady:
                    {
                        tmpStr = string.Empty;
                        if (char.IsLetter(c))
                        {
                            state = State.ReadFlags;
                            tmpStr += c;
                        }
                        else if (c == '[')
                            state = State.ReadLength;
                        else if (c == '}')
                            state = State.Ready;
                        isSub = true;
                        break;
                    }

                    case State.ReadName:
                    {
                        if (char.IsLetter(c))
                            tmpStr += c;
                        else
                        {
                            Cout(tmpStr, ConsoleColor.Cyan);
                            tmpStr = string.Empty;
                            if (c == '[')
                                state = State.ReadLength;
                            else
                                state = State.Ready;
                        }
                        break;
                    }

                    case State.ReadString:
                    {
                        if (c != '"')
                            tmpStr += c;
                        else
                        {
                            Cout(tmpStr, ConsoleColor.Green);
                            state = State.Ready;
                        }
                        break;
                    }

                    case State.Append:
                    {
                        state = State.Ready;
                        break;
                    }

                    case State.ReadNumber:
                    {
                        if (char.IsDigit(c))
                            tmpStr += c;
                        else
                        {
                            int num = int.Parse(tmpStr);
                            Cout("0x" + num.ToString("X"), ConsoleColor.Yellow);
                            state = State.Ready;
                        }
                        break;
                    }

                    case State.ReadFlags:
                    {
                        if (char.IsLetter(c))
                            tmpStr += c;
                        else
                        {
                            Cout(tmpStr, ConsoleColor.DarkCyan);
                            if (c == '[')
                            {
                                tmpStr = string.Empty;
                                state = State.ReadLength;
                            }
                            else
                                state = State.SubReady;
                        }
                        break;
                    }

                    case State.ReadLength:
                    {
                        if (char.IsDigit(c))
                            tmpStr += c;
                        else
                        {
                            Cout(tmpStr, ConsoleColor.White);
                            state = isSub ? State.SubReady : State.Ready;
                        }
                        break;
                    }
                }
            }
        }
    }
}
