#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace cdclsolver
{
    class Solver
    {
        static CNFClause _decided_placeholder = new CNFClause();

        public class UnsatisfiableException : Exception
        {
            public UnsatisfiableException(string message) : base(message) { }
        }

        public class ImplicationResult
        {
            public bool Found { get; private set; }
            public bool Conflict { get; private set; }
            public String Variable { get; private set; }

            public ImplicationResult(bool new_found, String new_variable, bool new_conflict)
            {
                Found = new_found;
                Variable = new_variable;
                Conflict = new_conflict;
            }
        }

        private HashSet<CNFClause> _clause_db = new HashSet<CNFClause>();
        private List<AssignmentEntry> _assignment_queue = new List<AssignmentEntry>();
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

        public KeyValuePair<String, CNFStates>? GetNewKnownVariable(CNFClause clause)
        {
            int unknown_vars = clause.Count;
            KeyValuePair<String, CNFStates>? new_var = null;

            foreach(KeyValuePair<String, CNFStates> var in clause)
            {
                if (_assignment_stack.ContainsVariable(var.Key))
                {
                    if (CompareStateToTruth(var.Value, _assignment_stack.GetVariable(var.Key))) {
                        unknown_vars--;
                    }
                    else
                    {
                        new_var = var;
                    }
                }
                else
                {
                    new_var = var;
                }
            }
            if (unknown_vars == 1)
            {
                return new_var;
            }
            else
            {
                return null;
            }
        }

        public CNFTruth CNFStateToTruth(CNFStates? state)
        {
            return state switch
            {
                CNFStates.Asserted => CNFTruth.True,
                CNFStates.Negated => CNFTruth.False,
                _ => CNFTruth.Unknown
            };
        }

        private void Preprocess()
        {
            // For all clauses with just one variable in them.
            foreach (CNFClause clause in _clause_db.Where(item => item.Count == 1))
            {
                KeyValuePair<String, CNFStates> var = clause.First();
                _assignment_queue.Add(new AssignmentEntry(var.Key, CNFStateToTruth(var.Value), clause, false, 0));
            }

            _clause_db.RemoveWhere(item => item.Count == 1);
        }

        public ImplicationResult DetermineImplications(int depth)
        {
            foreach (CNFClause clause in _clause_db)
            {
                KeyValuePair<String, CNFStates>? new_var = GetNewKnownVariable(clause);
                CNFTruth truth;

                // This should be inside the if, but the compliler worries that "new_var?.value" might change the null state,
                // so we just do a nullable operation out here, and then do an explicit null check and go from there.
                truth = CNFStateToTruth(new_var?.Value);

                if (new_var != null)
                {
                    _assignment_queue.Add(new AssignmentEntry(new_var.Value.Key, truth, clause, false, depth));

                    return new ImplicationResult(true, new_var.Value.Key, false);
                }
            }

            return new ImplicationResult(false, "", false);
        }
        public AssignmentStack Solve()
        {
            // Mark all of the variables as needing to be done.
            // NOTE: We could just keep the HashSet as our queue, but the problem is that it doens't concern itself with order.
            // Though this implementation doesn't presently support reordering, this is an important part of such algorithms,
            // and I'd like to implement this program in a way that further optimizations don't require refactoring the whole
            // program, especially when this is only done once on the smallest amount of data.
            _assignment_queue = new List<AssignmentEntry>();

            List<String> vars = new List<string>(_detected_variables);

            Preprocess();
            
            Console.WriteLine("After Preprocessing:");
            Console.WriteLine("Queue: " + String.Join(",", _assignment_queue.ConvertAll(item => item.Variable)));
            Console.WriteLine("Stack:");
            Console.WriteLine(_assignment_stack);
            Console.WriteLine("Clause DB:");
            Console.WriteLine(String.Join("\n", _clause_db));

            // We would pick an ordering here and shuffle the vars list to be something more ideal here.
            // PickOrdering()

            while (vars.Count > 0)
            {

                // ChooseNextAssignment()
                if (_assignment_queue.Count > 0)
                {
                    // Find the first variable we don't have an assignment for already. (Start at the back for efficiency in removal!)
                    AssignmentEntry next_var = _assignment_queue[_assignment_queue.Count - 1];
                    _assignment_queue.RemoveAt(_assignment_queue.Count - 1);

                    _assignment_stack.Push(next_var);
                }
                else
                {
                    String varname = vars.First(item => !_assignment_stack.ContainsVariable(item));
                    // Assume depth 1
                    int depth = 1;
                    // But if we already have something on the stack,
                    if (_assignment_stack.Count > 0)
                    {
                        // Pick one more than the highest one.
                        depth = _assignment_stack.Peek().Depth + 1;
                    }
                    // Make a new entry. Since we don't know what value to try, try true first.
                    AssignmentEntry next_var = new AssignmentEntry(varname, CNFTruth.True, _decided_placeholder, true, depth);
                    _assignment_stack.Push(next_var);
                }


            }
            return _assignment_stack;
        }

        // Convienence Wrapper
        public AssignmentStack Solve(CNFFormula formula)
        {
            foreach (CNFClause clause in formula)
            {
                AddClause(clause);
            }

            return Solve();
        }
    }
}
