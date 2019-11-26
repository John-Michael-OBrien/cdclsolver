using System;
using System.Collections.Generic;
using System.Text;

namespace cdclsolver
{
    public class CNFFormula : HashSet<CNFClause>
    {
        public override string ToString()
        {
            return "(" + String.Join(")^(", this) + ")";
        }
    }
}
