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
                    { "A", CNFStates.Negated },
                    { "B", CNFStates.Negated },
                    { "C", CNFStates.Asserted }
                });
            mysolver.AddClause(new CNFClause()
                {
                    { "A", CNFStates.Asserted },
                    { "B", CNFStates.Negated },
                    { "C", CNFStates.Asserted }
                });
            mysolver.AddClause(new CNFClause()
                {
                    { "C", CNFStates.Negated },
                    { "D", CNFStates.Asserted }
                });
            mysolver.AddClause(new CNFClause()
                {
                    { "C", CNFStates.Negated },
                    { "D", CNFStates.Negated }
                });
            mysolver.AddClause(new CNFClause()
                {
                    { "A", CNFStates.Negated },
                    { "C", CNFStates.Asserted },
                    { "D", CNFStates.Asserted }
                });
            mysolver.AddClause(new CNFClause()
                {
                    { "A", CNFStates.Negated },
                    { "B", CNFStates.Asserted },
                    { "D", CNFStates.Negated }
                });
            mysolver.AddClause(new CNFClause()
                {
                    { "B", CNFStates.Asserted },
                    { "C", CNFStates.Asserted },
                    { "D", CNFStates.Negated }
                });
            mysolver.AddClause(new CNFClause()
                {
                    { "A", CNFStates.Asserted },
                    { "B", CNFStates.Asserted },
                    { "D", CNFStates.Asserted }
                });
            //mysolver.AddClause(new CNFClause()
            //    {
            //        { "C", CNFStates.Negated }
            //    });

            Console.WriteLine(mysolver.Solve());
        }
    }
}
