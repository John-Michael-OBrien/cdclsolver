using System;
using System.Collections.Generic;
using System.Text;

namespace cdclsolver
{
    public class CNFVariable
    {
        public enum CNFStates
        {
            absent,
            present,
            negated,
        }

        public CNFStates Value { get; }
        public String Name { get; }

        public CNFVariable(String new_name, CNFStates new_value)
        {
            Name = new_name;
            Value = new_value;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

    }
}
