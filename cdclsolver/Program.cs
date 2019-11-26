using System;
using System.Collections.Generic;

namespace cdclsolver
{
    class Program
    {
        static void Main(string[] args)
        {
            CNFFormula formula = new CNFFormula() {
                new CNFClause()
                {
                    { "A", CNFStates.Asserted },
                    { "B", CNFStates.Asserted }
                },
                new CNFClause()
                {
                    { "B", CNFStates.Asserted },
                    { "C", CNFStates.Asserted }
                },
                new CNFClause()
                {
                    { "C", CNFStates.Asserted },
                    { "A", CNFStates.Negated }
                },
                new CNFClause()
                {
                    { "A", CNFStates.Asserted },
                    { "B", CNFStates.Asserted }
                },
                new CNFClause()
                {
                    { "A", CNFStates.Asserted }
                }
            };


            Solver mysolver = new Solver();
            mysolver.Solve(formula);

            Console.WriteLine(formula);
            foreach (CNFClause clause in formula) {
                Console.WriteLine(clause);
            }
        }
    }
}
