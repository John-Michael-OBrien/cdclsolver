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
                    new CNFVariable("A", CNFVariable.CNFStates.present),
                    new CNFVariable("B", CNFVariable.CNFStates.present)
                },
                new CNFClause()
                {
                    new CNFVariable("B", CNFVariable.CNFStates.present),
                    new CNFVariable("C", CNFVariable.CNFStates.present)
                },
                new CNFClause()
                {
                    new CNFVariable("C", CNFVariable.CNFStates.present),
                    new CNFVariable("A", CNFVariable.CNFStates.negated)
                }
            };
        }
    }
}
