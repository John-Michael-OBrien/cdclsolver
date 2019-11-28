using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace cdclsolver
{
    class Solver
    {
        class UnsatisfiableException : Exception
        {
            public UnsatisfiableException(string message) : base(message) { }
        }

        private HashSet<CNFClause> _clause_db = new HashSet<CNFClause>();
        private List<String> _assignment_queue = new List<string>();
        private AssignmentStack _assignment_stack = new AssignmentStack();
        HashSet<String> _detected_variables = new HashSet<String>();

        private bool CompareStateToTruth(CNFStates state, CNFTruth truth)
        {
            if ((state == CNFStates.Asserted && truth == CNFTruth.True) || state == CNFStates.Negated && truth == CNFTruth.False)
            {
                return true;
            }
            else
            {
                return false;
            }
        }


        private CNFTruth ComputeValidity(CNFClause clause)
        {
            int count = 0;
            // Check each variable in the clause
            foreach (KeyValuePair<String, CNFStates> var in clause)
            {
                // If we find one,
                if (_assignment_stack.ContainsVariable(var.Key))
                {
                    // Mark that we found it.
                    count++;
                    // If we match, then the clause as a whole is true.
                    if (CompareStateToTruth(var.Value, _assignment_stack.GetVariable(var.Key)))
                    {
                        return CNFTruth.True;
                    }
                }
            }

            // If we failed to match EVERY clause, then we know we're false (Unsatisfiable, to be detected upstream of us.)
            if (count == clause.Count)
            {
                return CNFTruth.False;
            }

            // If we didn't find a true assignment, but a few variables were of unknown state, then we're also unknow.
            return CNFTruth.Unknown;
        }

        public void AddClause(CNFClause clause)
        {
            _clause_db.Add(clause);
            // And pull all of the clause's variables in.
            foreach (String var_name in clause.Keys)
            {
                _detected_variables.Add(var_name);
            }
        }

        private void Preprocess()
        {
            foreach (CNFClause clause in _clause_db)
            {
                if (clause.Count == 1)
                {
                    CNFTruth truth;

                    // Get the only variable
                    KeyValuePair<String, CNFStates> unit_var = clause.First();

                    // 
                    if (unit_var.Value == CNFStates.Asserted)
                    {
                        truth = CNFTruth.True;
                    }
                    else
                    {
                        truth = CNFTruth.False;
                    }
                    // Add the variable to the stack
                    if (!_assignment_stack.AddKnownVariable(unit_var.Key, truth, clause))
                    {
                        throw new UnsatisfiableException("Unable to satisfy clauses");
                    }
                    // And pull it out of the assignment queue.
                    _assignment_queue.Remove(unit_var.Key);
                }
            }

            // Remove any clauses that are KNOWN resolved.
            foreach (AssignmentStackEntry entry in _assignment_stack.Where(item => item.Depth == 0))
            {
                _clause_db.Remove(entry.RelatedClause);
            }
        }

        public void Solve()
        {
            // Mark all of the variables as needing to be done.
            // NOTE: We could just keep the HashSet as our queue, but the problem is that it doens't concern itself with order.
            // Though this implementation doesn't presently support reordering, this is an important part of such algorithms,
            // and I'd like to implement this program in a way that further optimizations don't require refactoring the whole
            // program, especially when this is only done once on the smallest amount of data.
            _assignment_queue = new List<string>(_detected_variables);

            Preprocess();
            Console.WriteLine("After Preprocessing:");
            Console.WriteLine("Queue: " + String.Join(",", _assignment_queue));
            Console.WriteLine("Stack:");
            Console.WriteLine(_assignment_stack);
            Console.WriteLine("Clause DB:");
            Console.WriteLine(String.Join("\n", _clause_db));
        }

        // Convienence Wrapper
        public void Solve(CNFFormula formula)
        {
            foreach (CNFClause clause in formula)
            {
                AddClause(clause);
            }

            Solve();
        }
    }
}
