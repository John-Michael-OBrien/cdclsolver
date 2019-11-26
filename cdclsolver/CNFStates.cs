using System;
using System.Collections.Generic;
using System.Text;

namespace cdclsolver
{
    public enum CNFStates
    {
        Asserted=-1,
        Negated=0,
    }
    public enum CNFTruth
    {
        Unknown=1,
        True=-1,
        False=0
    }

}
