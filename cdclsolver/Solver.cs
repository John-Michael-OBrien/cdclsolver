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

        public class ConflictException : ApplicationException
        {
            public CNFClause Clause1 { get; private set; }
            public CNFClause Clause2 { get; private set; }
            public String Variable { get; private set; }

            public ConflictException(CNFClause clause1, CNFClause clause2, String variable) : base() {
                Clause1 = clause1;
                Clause2 = clause2;
                Variable = variable;
            }

            public override string ToString()
            {
                return String.Format("Conflict between ({0})^({1}) on variable {2}.", Clause1, Clause2, Variable);
            }
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
                    if (CompareStateToTruth(var.Value, _assignment_stack.GetVariableValue(var.Key)))
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
                Console.WriteLine("Found unit literal {0} {1}", var.Key, var.Value);
                _assignment_queue.Add(new AssignmentEntry(var.Key, CNFStateToTruth(var.Value), clause, false, 0));
            }

            _clause_db.RemoveWhere(item => item.Count == 1);
        }


        public Dictionary<String, ImplicationResult> GetImplications()
        {
            Dictionary<String, ImplicationResult> result = new Dictionary<string, ImplicationResult>();

            foreach (CNFClause clause in _clause_db)
            {
                CNFClause cause_clause = new CNFClause();
                CNFTruth new_truth = CNFTruth.Unknown;
                int false_vars = 0;
                KeyValuePair<String, CNFStates>? new_var = null;

                foreach (KeyValuePair<String, CNFStates> var in clause)
                {
                    if (_assignment_stack.ContainsVariable(var.Key))
                    {
                        CNFTruth truth = _assignment_stack.GetVariableValue(var.Key);
                        // The assignment stack should never have an unknown in it.
                        System.Diagnostics.Debug.Assert(truth != CNFTruth.Unknown);
                        if (CompareStateToTruth(var.Value, truth) == false)
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
                    ImplicationResult implication = new ImplicationResult(new_var.Value.Key, new_truth, cause_clause);
                    Console.WriteLine(String.Format("Implication: {0} found at depth {1}", implication, _assignment_stack.Peek().Depth));
                    if (result.ContainsKey(implication.Variable))
                    {
                        ImplicationResult test_implication = result[new_var.Value.Key];
                        if (test_implication.Truth != new_truth)
                        {
                            throw new ConflictException(test_implication.Clause, implication.Clause, implication.Variable);
                        }
                    }
                    else
                    {
                        result.Add(new_var.Value.Key, implication);
                    }                        
                }
            }

            // Return whatever we found.
            return result;
        }

        public CNFClause ResolveClauses(CNFClause clause1, CNFClause clause2)
        {
            bool resolved = false;
            // Prime our result with the first clause
            CNFClause result = new CNFClause(clause1);

            // Then search the second
            foreach (KeyValuePair<String, CNFStates> var in clause2)
            {
                // If we have a duplicate variable
                if(result.ContainsKey(var.Key))
                {
                    // And it's different
                    if (result[var.Key] != var.Value)
                    {
                        // Remove it (This will also remove the resolution variable)
                        result.Remove(var.Key);
                        resolved = true;
                    }
                }
                else
                {
                    // Otherwise add the new variable to the resolved clause
                    result.Add(var.Key, var.Value);
                }
            }

            if (!resolved)
            {
                throw new ArgumentException("The clauses could not be resolved; they do not have a resolution variable in common.");
            }
            return result;
        }

        CNFClause AnalyzeConflict(ConflictException conflict, int depth)
        {
            HashSet<String> analyzed_vars = new HashSet<String>();
            CNFClause result = ResolveClauses(conflict.Clause1, conflict.Clause2);
            analyzed_vars.Add(conflict.Variable);

            do
            {
                int count = 0;
                foreach (KeyValuePair<String, CNFStates> var in result)
                {
                    if (_assignment_stack.ContainsVariable(var.Key) && _assignment_stack.GetEntryByVariable(var.Key).Depth == depth)
                    {
                        count++;
                    }
                }
                if (count == 1)
                {
                    return result;
                }
                
                foreach (KeyValuePair<String, CNFStates> var in result)
                {
                    if (!analyzed_vars.Contains(var.Key) && _assignment_stack.ContainsVariable(var.Key)) {
                        AssignmentEntry graph_entry = _assignment_stack.GetEntryByVariable(var.Key);
                        if (graph_entry.Depth == depth && graph_entry.Decided == false)
                        {
                            result = ResolveClauses(result, graph_entry.RelatedClause);
                        }
                        analyzed_vars.Add(var.Key);
                    }
                }                
            } while (true);
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
                Console.WriteLine();
                Console.WriteLine("Starting Cycle.");
                Console.WriteLine("Queue: {0}", String.Join(", ", _assignment_queue));

                if (_assignment_queue.Count > 0)
                {
                    AssignmentEntry next_var = _assignment_queue[0];
                    _assignment_queue.RemoveAt(0);
                    _assignment_stack.Push(next_var);
                    Console.WriteLine("Pulled from queue {0}", next_var);
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
                    AssignmentEntry next_var = new AssignmentEntry(varname, CNFTruth.False, _decided_placeholder, true, depth);
                    _assignment_queue.Add(next_var);
                }

                Console.WriteLine("Stack: {0}", _assignment_stack);

                // Deduce()
                Dictionary<String, ImplicationResult>? result = null;

                bool wasnt_conflicted = false;

                while (!wasnt_conflicted && _assignment_stack.Count > 0) {
                    try
                    {
                        result = GetImplications();
                        wasnt_conflicted = true;
                    }
                    catch (ConflictException conflict)
                    {
                        wasnt_conflicted = false;
                        // "a conflict at level 0 results in a backtracking level of−1,which causes CDCL to report unsatisfiability"
                        if (_assignment_stack.Peek().Depth == 0)
                        {
                            throw new UnsatisfiableException();
                        }
                        CNFClause conflict_clause = AnalyzeConflict(conflict, _assignment_stack.Peek().Depth);

                        _clause_db.Add(conflict_clause);
                        Console.WriteLine("Learned Conflict Clause ({0}).", conflict_clause);

                        int backtrack_level = 0;

                        if (conflict_clause.Count == 1)
                        {
                            KeyValuePair<String, CNFStates> var = conflict_clause.First();
                            _assignment_queue.Add(new AssignmentEntry(var.Key, CNFStateToTruth(var.Value), conflict_clause, false, 0));
                            backtrack_level = 0;
                        }
                        else
                        {
                            int target_depth = _assignment_stack.Peek().Depth;
                            backtrack_level = conflict_clause.Max(var => {
                                if (!_assignment_stack.ContainsVariable(var.Key))
                                {
                                    return -1;
                                }
                                else
                                {
                                    int depth = _assignment_stack.GetEntryByVariable(var.Key).Depth;                                    
                                    if (depth < target_depth)
                                    {
                                        return depth;
                                    }
                                    else
                                    {
                                        return -1;
                                    }
                                }
                            });
                        }

                        Console.WriteLine("Backtracking to depth {0}.", backtrack_level);
                        // Erase anything out of the stack that is higher depth than our target.
                        while (_assignment_stack.Count > 0 && _assignment_stack.Peek().Depth > backtrack_level)
                        {
                            _assignment_stack.Pop();
                        }

                        // Erase anything out of the queue that is higher depth than our target (in case we had lots of implications.)
                        while (_assignment_queue.Count > 0 && _assignment_queue[0].Depth > backtrack_level)
                        {
                            _assignment_queue.RemoveAt(0);
                        }

                        //throw new Exception(conflict.ToString());
                    }
                }
                
                if (result != null)
                {
                    foreach (ImplicationResult implication in result.Values)
                    {
                        AssignmentEntry entry = new AssignmentEntry(implication.Variable, implication.Truth, implication.Clause, false, _assignment_stack.Peek().Depth);
                        // "When a new assignment is added to the stack, its implications are added by Deduce to the assignment queue"
                        Console.WriteLine("Adding {0} to the queue", entry);
                        _assignment_queue.Add(entry);
                    }
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
