using System;
using System.Collections.Generic;

namespace cdclsolver
{
    class Program
    {
        static void Main(string[] args)
        {
            Solver mysolver = new Solver();

            Console.WriteLine("Reading arguments from the command line...");

            // Grab all of our arguments.
            foreach(String arg in args)
            {
                // If they ask for help, give it.
                if (arg.ToLower() == "--help")
                {
                    String executable = System.IO.Path.GetFileNameWithoutExtension(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
                    Console.WriteLine("Usage: {0} <clause>[,<clause>,[<clause>,[...]]]", executable);
                    Console.WriteLine("Clause Format: [~]<variable name>,[[~]<variable name>, [[~]<variable name>, [...]]]");
                    Console.WriteLine("~ indicates negation.");
                    Console.WriteLine();
                    Console.WriteLine("Example: {0} ~a,~b,c a,~b,c ~c,d ~c,~d ~a,c,d ~a,b,~d b,c,~d a,b,d", executable);

                    // And end the program
                    return;
                }

                // Split up the variables inside the clause argument
                String[] vars = arg.Split(',');

                CNFClause new_clause = new CNFClause();
                foreach (String var in vars)
                {                    
                    // If the clause is negated...
                    if (var.StartsWith("~"))
                    {
                        // Add a negative clause, trimming the negation symbol.
                        new_clause.Add(var.TrimStart('~'), CNFStates.Negated);
                    }
                    else
                    {
                        // Otherwise add the clause as positive.
                        new_clause.Add(var, CNFStates.Asserted);
                    }
                }
                // Add our newly created clause
                mysolver.AddClause(new_clause);
                Console.WriteLine("Added clause: {0}", new_clause.ToString());
            }

            // Try solving for an assignment.
            try
            {
                Console.WriteLine("Starting solver...");
                AssignmentStack result = mysolver.Solve();
                // Write out the final assignment.
                Console.WriteLine();
                Console.WriteLine("Final Result: {0}", result);
            }
            // If we crash and burn as unsatisfiable
            catch (Solver.UnsatisfiableException)
            {
                // Tell the user.
                Console.WriteLine();
                Console.WriteLine("Final Result: Unsatisfiable");
            }
            
        }
    }
}
