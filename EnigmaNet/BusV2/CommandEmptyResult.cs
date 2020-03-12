using System;
using System.Collections.Generic;
using System.Text;

namespace EnigmaNet.BusV2
{
    public struct CommandEmptyResult : IEquatable<CommandEmptyResult>, IComparable<CommandEmptyResult>, IComparable
    {
        public static CommandEmptyResult Value = new CommandEmptyResult();

        public int CompareTo(CommandEmptyResult other)
        {
            return 0;
        }

        public int CompareTo(object obj)
        {
            return 0;
        }

        public bool Equals(CommandEmptyResult other)
        {
            return true;
        }

        public override bool Equals(object obj)
        {
            return true;
        }

        public override int GetHashCode()
        {
            return 0;
        }

        public static bool operator ==(CommandEmptyResult first, CommandEmptyResult second)
        {
            return true;
        }

        public static bool operator !=(CommandEmptyResult first, CommandEmptyResult second)
        {
            return false;
        }
    }
}
