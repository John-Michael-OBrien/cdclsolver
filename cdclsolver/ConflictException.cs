using System;
using System.Collections.Generic;
using System.Text;

namespace cdclsolver
{
    public class ConflictException : ApplicationException
    {
        public CNFClause Clause1 { get; private set; }
        public CNFClause Clause2 { get; private set; }
        public String Variable { get; private set; }

        public ConflictException(CNFClause clause1, CNFClause clause2, String variable) : base()
        {
            Clause1 = clause1;
            Clause2 = clause2;
            Variable = variable;
        }

        public override string ToString()
        {
            return String.Format("Conflict between {0}^{1} on variable {2}.", Clause1, Clause2, Variable);
        }
    }
}
