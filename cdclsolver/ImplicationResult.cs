using System;
using System.Collections.Generic;
using System.Text;

namespace cdclsolver
{
    public class ImplicationResult
    {
        public CNFClause Clause { get; private set; }
        public CNFTruth Truth { get; private set; }
        public String Variable { get; private set; }

        public ImplicationResult(String new_variable, CNFTruth new_truth, CNFClause new_clause)
        {
            Variable = new_variable;
            Clause = new_clause;
            Truth = new_truth;
        }

        public override string ToString()
        {
            String truth = Truth switch
            {
                CNFTruth.True => "",
                CNFTruth.False => "~",
                CNFTruth.Unknown => "?",
                _ => "E"
            };

            return String.Format("{0}{1} implied by {2}", truth, Variable, Clause);
        }
    }
}
