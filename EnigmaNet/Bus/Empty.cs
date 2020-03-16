using System;
using System.Collections.Generic;
using System.Text;

namespace EnigmaNet.Bus
{
    public class Empty : IEquatable<Empty>, IComparable<Empty>, IComparable
    {
        public static Empty Value = new Empty();

        public int CompareTo(Empty other)
        {
            return 0;
        }

        public int CompareTo(object obj)
        {
            return 0;
        }

        public bool Equals(Empty other)
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

        public static bool operator ==(Empty first, Empty second)
        {
            return true;
        }

        public static bool operator !=(Empty first, Empty second)
        {
            return false;
        }
    }
}
