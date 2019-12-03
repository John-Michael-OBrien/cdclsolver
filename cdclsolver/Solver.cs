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

        public class ImplicationResult
        {
            public bool Conflict { get; private set; }
            public String Variable { get; private set; }

            public ImplicationResult(String new_variable, bool new_conflict)
            {
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
        

        public List<ImplicationResult> GetImplications()
        {
            List<ImplicationResult> result = new List<ImplicationResult>();
            foreach (CNFClause clause in _clause_db)
            {
                bool conflict = false;
                int false_vars = 0;
                KeyValuePair<String, CNFStates>? new_var = null;

                foreach (KeyValuePair<String, CNFStates> var in clause)
                {
                    if (_assignment_stack.ContainsVariable(var.Key))
                    {
                        CNFTruth truth = _assignment_stack.GetVariable(var.Key);
                        // The assignment stack should never have an unknown in it.
                        System.Diagnostics.Debug.Assert(truth != CNFTruth.Unknown);
                        if (CompareStateToTruth(var.Value, truth)==false)
                        {
                            false_vars++;
                        }
                    }
                    else
                    {
                        conflict = false;
                        new_var = var;
                    }
                }
                if (false_vars == clause.Count - 1 && new_var != null)
                {
                    result.Add(new ImplicationResult(new_var.Value.Key, conflict));
                }
            }

            // If we didn't find anything, return false.
            return result;
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
            List<string> queue_text = _assignment_queue.ConvertAll<String>(item => item.ToString());
            Console.WriteLine("Queue: " + String.Join(",", queue_text));
            Console.WriteLine("Clause DB:");
            Console.WriteLine(String.Join("\n", _clause_db));

            // We would pick an ordering here and shuffle the vars list to be something more ideal here.
            // PickOrdering()

            while (true)
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
                    String varname;
                    try
                    {
                        // Find the first variable we don't already have an assignment for.
                        varname = vars.First(item => !_assignment_stack.ContainsVariable(item));
                    }
                    catch (InvalidOperationException)
                    {
                        // If there aren't any matches, that means we've got all of them assigned (or there were none to assign anyway.)
                        // That means we're done.
                        return _assignment_stack;
                    }

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

                // Deduce()
                List<ImplicationResult> result = GetImplications();
                foreach (ImplicationResult implication in result)
                {

                }
            }
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
