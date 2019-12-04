using System;
using System.Collections.Generic;

namespace cdclsolver
{
    class Program
    {
        static void ShowHelp()
        {
            String executable = System.IO.Path.GetFileNameWithoutExtension(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            Console.WriteLine("Usage: {0} <clause>[,<clause>,[<clause>,[...]]] | --file <source filename>", executable);
            Console.WriteLine("Clause Format: [~]<variable name>,[[~]<variable name>, [[~]<variable name>, [...]]]");
            Console.WriteLine("~ indicates negation.");
            Console.WriteLine();
            Console.WriteLine("Direct Clause Example: {0} ~a,~b,c a,~b,c ~c,d ~c,~d ~a,c,d ~a,b,~d b,c,~d a,b,d", executable);
            Console.WriteLine("File Example: {0} --file clauses.txt", executable);
        }

        static void Main(string[] args)
        {
            Solver mysolver = new Solver();

            Console.WriteLine("Reading arguments from the command line...");

            // Grab all of our arguments.
            for (int i = 0; i < args.Length; i++)
            {
                // Fetch the argument
                String arg = args[i];
                try
                {
                    if (arg.StartsWith("--"))
                    {
                        // If they ask for help, give it.
                        if (arg.ToLower() == "--help")
                        {
                            ShowHelp();
                            // And end the program
                            return;
                        }
                        else if (arg.ToLower() == "--file")
                        {
                            // Move to the filename itself
                            i++;
                            // Make sure that's an actual argument
                            if (i >= args.Length)
                            {
                                Console.WriteLine("No file argument provided.");
                                ShowHelp();
                                return;
                            }
                            // Get a loaded solver
                            mysolver = Solver.FromFile(args[i]);
                            // And then exit the argument processor.
                            break;
                        }
                    }
                    else
                    {
                        // Otherwise it's a clause. Parse it as such.
                        mysolver.ParseFormula(arg);
                    }

                }
                catch(Exception e)
                {
                    Console.WriteLine("Unable to parse command line argument {0}. Reason: {1}", arg, e);
                    ShowHelp();
                    return;
                }
            }


            // Try solving for an assignment.
            try
            {
                Console.WriteLine("Starting solver...");
                DateTime start_time = DateTime.UtcNow;
                AssignmentStack result = mysolver.Solve();
                DateTime end_time = DateTime.UtcNow;
                // Write out the final assignment.
                Console.WriteLine();
                Console.WriteLine("Final Result: {0}", result);
                Console.WriteLine("Solving time: {0}", end_time - start_time);
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
