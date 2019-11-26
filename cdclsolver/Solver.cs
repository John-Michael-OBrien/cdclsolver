using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace cdclsolver
{
    class Solver
    {
        public struct CNFStackEntry
        {
            public String Variable;
            public CNFTruth Truth;
            public CNFClause RelatedClause;
            public bool Decided;
            public int Depth;
        }

        public struct ClauseInfo
        {
            public int UnknownAssignments;
            public CNFTruth Truth;
        }

        private HashSet<CNFClause> _clause_db;
        private List<CNFStackEntry> _assignment_stack;
        private List<String> _assignment_queue;
        private Dictionary<String, CNFTruth> _known_assignments;

        

        public ClauseInfo ComputeTruth(CNFClause clause)
        {
            CNFTruth result = CNFTruth.Unknown;
            CNFTruth var_truth;
            int count = 0;
            foreach (KeyValuePair<String, CNFStates> var in clause)
            {
                if (_known_assignments.TryGetValue(var.Key, out var_truth))
                {
                    count++;
                    if ((int) var_truth == (int) var.Value)
                    {
                        result = CNFTruth.True;                        
                    }
                }
            }

            if (count == clause.Count && result == CNFTruth.Unknown)
            {
                result = CNFTruth.False;
                throw new Exception("Unsatisfiable!");
            }

            return new ClauseInfo() {
                UnknownAssignments = clause.Count - count,
                Truth = result
            };
        }

        private void Preprocess()
        {
            bool done = false;
            while (!done)
            {
                foreach (CNFClause clause in _clause_db)
                {
                    done = true;

                    ClauseInfo info = ComputeTruth(clause);

                    // If a clause has only one element (A unit clause)
                    if (info.UnknownAssignments == 1)
                    {
                        // Then we know for sure the necessary state of this value in all satisfying assignments.
                        KeyValuePair<String, CNFStates> unit_var = clause.First();
                        String var_name = unit_var.Key;

                        // Get what value that is...
                        CNFTruth truth;
                        if (unit_var.Value == CNFStates.Asserted)
                        {
                            truth = CNFTruth.True;
                        }
                        else
                        {
                            truth = CNFTruth.False;
                        }

                        // And if we already know something about this variable
                        if (_known_assignments.ContainsKey(var_name))
                        {
                            // Check if it agrees. If it doesn't then this is a contradiction!
                            if (_known_assignments[var_name] != truth)
                            {
                                //TODO: Make this an actual thing. This is only good for debugging.
                                throw new Exception("NotSatisfiable!");
                            }
                        }
                        else
                        {
                            // Otherwise record what we know.
                            _known_assignments.Add("var_name", truth);
                        }

                        // Known values are placed at the beginning to ensure that they all are grouped together
                        _assignment_stack.Insert(0, new CNFStackEntry
                        {
                            Variable = var_name,
                            Decided = false,
                            RelatedClause = clause,
                            Truth = truth,
                            Depth = 0
                        });

                        // If the variable is still in the assignment queue, pull it. We know it's value.
                        if (_assignment_queue.Contains(var_name))
                        {
                            _assignment_queue.Remove(var_name);
                        }



                        // Remove all clauses where we see our now known variable (They're satisfied for sure and don't matter now.)
                        _clause_db.RemoveWhere(test_clause => test_clause.ContainsKey(var_name) ? test_clause[var_name] == unit_var.Value : false);
                        foreach (CNFClause test_clause in _clause_db.Where(test_clause => test_clause.ContainsKey(var_name)).ToList())
                        {

                        }

                        done = false;
                        break;
                    }
                }
            }
        }
        public void Solve(CNFFormula formula)
        {
            // Initialize the object
            _clause_db = new HashSet<CNFClause>();
            _known_assignments = new Dictionary<string, CNFTruth>();
            _assignment_queue = new List<string>();
            _assignment_stack = new List<CNFStackEntry>();

            HashSet<String> variables = new HashSet<String>();

            // Load the clauses into the clause database and get a record of all of the necessary assignments.
            foreach (CNFClause clause in formula)
            {
                _clause_db.Add(clause);
                // And pull all of the clause's variables in.
                foreach (String var_name in clause.Keys)
                {
                    variables.Add(var_name);
                }
            }

            // Mark all of the variables as needing to be done.
            // NOTE: We could just keep the HashSet as our queue, but the problem is that it doens't concern itself with order.
            // Though this implementation doesn't presently support reordering, this is an important part of such algorithms,
            // and I'd like to implement this program in a way that further optimizations don't require refactoring the whole
            // program, especially when this is only done once on the smallest amount of data.
            _assignment_queue = new List<string>(variables);

            Preprocess();
        }
    }
}
