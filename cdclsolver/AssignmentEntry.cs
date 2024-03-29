﻿using System;
using System.Collections.Generic;
using System.Text;

namespace cdclsolver
{
    // Represents a single entry in the assignment stack and queue.
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

        public override string ToString()
        {
            String truth = Truth switch
            {
                CNFTruth.True => "",
                CNFTruth.False => "~",
                CNFTruth.Unknown => "?",
                _ => "E"
            };
            if (Decided)
            {
                return String.Format("{0}{1}@{2} D", truth, Variable, Depth);
            }
            else
            {
                return String.Format("{0}{1}@{2} {3}", truth, Variable, Depth, RelatedClause);
            }
            
        }
    }
}
