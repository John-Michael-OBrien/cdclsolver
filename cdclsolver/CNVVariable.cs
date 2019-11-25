using System;
using System.Collections.Generic;
using System.Text;

namespace cdclsolver
{
    class CNFVariable
    {
        public enum CNFStates
        {
            absent,
            present,
            negated,
        }

        public CNFStates Value { get; }
        public String Name { get; }


        CNFVariable(String new_name, CNFStates new_value)
        {
            Name = new_name;
            Value = new_value;
        }
    }
}
