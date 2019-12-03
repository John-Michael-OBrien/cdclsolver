using System;
using System.Collections.Generic;
using System.Text;

namespace cdclsolver
{
    public class AssignmentEntry : object
    {
        public String Variable { get; private set; }
        public CNFTruth Truth { get; private set; }
        public CNFClause RelatedClause { get; private set; }
        public bool Decided { get; private set; }
        public int Depth { get; private set; }

        public AssignmentEntry(String variable, CNFTruth truth, CNFClause clause, bool decided, int depth)
        {
            Variable = variable;
            Truth = truth;
            RelatedClause = clause;
            Decided = decided;
            Depth = depth;
        }
    }
}
