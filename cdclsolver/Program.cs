using System;
using System.Collections.Generic;

namespace cdclsolver
{
    class Program
    {
        static void Main(string[] args)
        {
            Solver mysolver = new Solver();

            foreach(String arg in args)
            {
                String[] vars = arg.Split(',');

                CNFClause new_clause = new CNFClause();
                foreach (String var in vars)
                {
                    if (var.StartsWith("~"))
                    {
                        new_clause.Add(var.TrimStart('~'), CNFStates.Negated);
                    }
                    else
                    {
                        new_clause.Add(var, CNFStates.Asserted);
                    }
                }
                mysolver.AddClause(new_clause);
                Console.WriteLine("Added clause: {0}", new_clause.ToString());
            }

            try
            {
                AssignmentStack result = mysolver.Solve();
                Console.WriteLine("Final Result: {0}", result);
            }
            catch (Solver.UnsatisfiableException)
            {
                Console.WriteLine("Final Result: Unsatisfiable");
            }
            
        }
    }
}
