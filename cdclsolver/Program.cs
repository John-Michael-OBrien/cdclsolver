using System;
using System.Collections.Generic;

namespace cdclsolver
{
    class Program
    {
        static void Main(string[] args)
        {
            Solver mysolver = new Solver();
            mysolver.AddClause(new CNFClause()
                {
                    { "A", CNFStates.Asserted },
                    { "B", CNFStates.Asserted }
                });
            mysolver.AddClause(new CNFClause()
                {
                    { "B", CNFStates.Asserted },
                    { "C", CNFStates.Asserted }
                });
            mysolver.AddClause(new CNFClause()
                {
                    { "C", CNFStates.Asserted },
                    { "A", CNFStates.Negated }
                });
            mysolver.AddClause(new CNFClause()
                {
                    { "A", CNFStates.Asserted },
                    { "B", CNFStates.Asserted }
                });
            mysolver.AddClause(new CNFClause()
                {
                    { "A", CNFStates.Asserted }
                });
            mysolver.AddClause(new CNFClause()
                {
                    { "C", CNFStates.Negated }
                });

            mysolver.Solve();
        }
    }
}
