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

        public class UnsatisfiableException : ApplicationException
        {
            public UnsatisfiableException() : base() { }
        }

        public class InvalidClauseException : ApplicationException
        {
            public InvalidClauseException() : base() { }
        }

        public class ImplicationResult
        {
            public CNFClause Clause { get; private set; }
            public CNFTruth Truth { get; private set; }
            public String Variable { get; private set; }

            public ImplicationResult(String new_variable, CNFTruth new_truth, CNFClause new_clause)
            {
                Variable = new_variable;
                Clause = new_clause;
                Truth = new_truth;
            }

            public override string ToString()
            {
                String truth = Truth switch
                {
                    CNFTruth.True => "",
                    CNFTruth.False => "~",
                    CNFTruth.Unknown => "?",
                    _ => "E"
                };

                return String.Format("{0}{1} ({2})", truth, Variable, Clause);
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
                Console.WriteLine(String.Format("{0} {1}", var.Key, var.Value));
                _assignment_queue.Add(new AssignmentEntry(var.Key, CNFStateToTruth(var.Value), clause, false, 0));
            }

            _clause_db.RemoveWhere(item => item.Count == 1);
        }
        

        public ImplicationResult? GetNextImplication()
        {
            foreach (CNFClause clause in _clause_db)
            {
 
                //if (ComputeValidity(clause) == CNFTruth.False)
                //{
                //    throw new ArgumentOutOfRangeException("Clause is invalid!");
                //}

                CNFClause cause_clause = new CNFClause();
                CNFTruth new_truth = CNFTruth.Unknown;
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
                        cause_clause = clause;
                        new_var = var;
                        new_truth = CNFStateToTruth(var.Value);
                    }
                }
                if (false_vars == clause.Count - 1 && new_var != null)
                {
                    return new ImplicationResult(new_var.Value.Key, new_truth, cause_clause);
                }
                // If we know our clause to be invalid
                if (false_vars == clause.Count)
                {
                    throw new InvalidClauseException();
                }
            }

            // If we didn't find anything, return false.
            return null;
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
                    AssignmentEntry next_var = _assignment_queue[0];
                    //do {
                        _assignment_queue.RemoveAt(0);
                        _assignment_stack.Push(next_var);
                        Console.WriteLine("Pulled from queue {0}", next_var);
                        //if (_assignment_queue.Count == 0)
                        //{
                        //    break;
                        //}
                        //next_var = _assignment_queue[0];
                    //} while (next_var.Decided == false);
                }
                else
                {
                    String varname;
                    try
                    {
                        // Find the first variable we don't already have an assignment for.
                        varname = vars.First(item => !_assignment_stack.ContainsVariable(item));
                        Console.WriteLine(String.Format("Picking {0} as True", varname));
                    }
                    catch (InvalidOperationException)
                    {
                        // Quick sanity check.
                        foreach (CNFClause clause in _clause_db)
                        {
                            if (ComputeValidity(clause) == CNFTruth.False)
                            {
                                throw new NotImplementedException(String.Format("Failure! {0}", clause));
                            }
                        }

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
                    _assignment_queue.Add(next_var);
                    //_assignment_stack.Push();
                }

                // Deduce()
                ImplicationResult? result;
                Console.WriteLine("Queue: {0}", String.Join(", ", _assignment_queue));
                Console.WriteLine("Stack: {0}", _assignment_stack);
                result = GetNextImplication();
                bool conflict = false;

                while (result != null)
                {
                    Console.WriteLine(String.Format("Implication: {0} found at depth {1}", result, _assignment_stack.Peek().Depth));
                    AssignmentEntry entry = new AssignmentEntry(result.Variable, result.Truth, result.Clause, false, _assignment_stack.Peek().Depth);
                    if (_assignment_stack.ContainsVariable(entry.Variable))
                    {
                        if (_assignment_stack.GetVariable(entry.Variable) != entry.Truth)
                        {
                            throw new NotImplementedException(String.Format("Confliccvct!"));
                        }
                    }
                    // "When a new assignment is added to the stack, its implications are added byDeduceto the assignment queue"
                    _assignment_queue.Append(entry);
                    
                    /*
                    foreach (CNFClause clause in _clause_db)
                    {
                        if (ComputeValidity(clause) == CNFTruth.False)
                        {
                            conflict = true;
                            Console.WriteLine("Conflict in clause {0}. Stack: {1}", clause, _assignment_stack);
                            CNFClause conflict_clause = new CNFClause();

                            throw new NotImplementedException(String.Format("Conflict! {0}", clause));
                        }
                    }
                    */

                    conflict = false;

                    // Get the next one and see if we should continue.
                    result = GetNextImplication();
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
