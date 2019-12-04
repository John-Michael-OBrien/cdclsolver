#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace cdclsolver
{
    public class Solver
    {
        public class UnsatisfiableException : ApplicationException
        {
            public UnsatisfiableException() : base() { }
        }

        // A static placeholder that's used for all decided clauses. This avoids making a million copies of the same blank object.
        static CNFClause _decided_placeholder = new CNFClause();

        #region Instance State
        // The database of clauses
        private HashSet<CNFClause> _clause_db = new HashSet<CNFClause>();
        // The queue of assignments to make
        private List<AssignmentEntry> _assignment_queue = new List<AssignmentEntry>();
        // The stack of currently applied assignments
        private AssignmentStack _assignment_stack = new AssignmentStack();
        // The list of variables we found during setup.
        HashSet<String> _detected_variables = new HashSet<String>();
        #endregion

        #region CNFState/CNFTruth Tools
        // Checks if the truth matches the state. If they don't or the truth is unknown, this returns false.
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
        // Converts a clause state to a truth. Useful for comparing a variable against an assignment.
        public CNFTruth CNFStateToTruth(CNFStates? state)
        {
            return state switch
            {
                CNFStates.Asserted => CNFTruth.True,
                CNFStates.Negated => CNFTruth.False,
                _ => CNFTruth.Unknown
            };
        }
        #endregion

        #region Clause Handling Utilities
        // Returns the validity of a clause. If any variable is unknown, then the validity of the clause is returned as unknown.
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

        public void EnqueueEntry(AssignmentEntry entry)
        {
#if SINGLETHREAD
            if (!_assignment_queue.Any(test_entry => test_entry.Variable == entry.Variable)) {
#else
            if (!_assignment_queue.AsParallel().Any(test_entry => test_entry.Variable == entry.Variable))
            {
#endif
                _assignment_queue.Add(entry);
            }
        }

        // Adds a clause to the clause DB.
        public void AddClause(CNFClause clause)
        {
            _clause_db.Add(clause);
            // And pull all of the clause's variables in.
            foreach (String var_name in clause.Keys)
            {
                // This is a HashSet, so duplicates will be automatically culled.
                _detected_variables.Add(var_name);
            }
        }

        // Does basic clause resolution. Will throw an error if resolution isn't appropriate on the clauses.
        public CNFClause ResolveClauses(CNFClause clause1, CNFClause clause2)
        {
            bool resolved = false;
            // Prime our result with the first clause
            CNFClause result = new CNFClause(clause1);

            // Then search the second
            foreach (KeyValuePair<String, CNFStates> var in clause2)
            {
                // If we have a duplicate variable
                if (result.ContainsKey(var.Key))
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
        #endregion

        #region CDCL Routines
        // Preprocesses the clause database and plucks out any unit literals.
        private void Preprocess()
        {
            // For all clauses with just one variable in them. (unit clauses)
            foreach (CNFClause clause in _clause_db.Where(item => item.Count == 1))
            {
                KeyValuePair<String, CNFStates> var = clause.First();
#if VERBOSE
                Console.WriteLine("Found unit literal {0} {1}", var.Key, var.Value);
#endif       
                EnqueueEntry(new AssignmentEntry(var.Key, CNFStateToTruth(var.Value), clause, false, 0));
            }

            // If we're unsatisfiable with the preprocessing, bail early.
            if (_clause_db.Any(clause => ComputeValidity(clause) == CNFTruth.False)) {
                throw new UnsatisfiableException();
            }
        }


        public Dictionary<String, ImplicationResult> Deduce()
        {
            Dictionary<String, ImplicationResult> result = new Dictionary<string, ImplicationResult>();
            ConflictException? conflict=null;
            // Cache our current depth to avoid unnecessary indirection.
            int current_depth = _assignment_stack.GetDepth();

            // Parallelize the conflict detection algorithm.
#if SINGLETHREAD
            conflict = _clause_db.Select<CNFClause, ConflictException?>(clause => {
#else
            conflict = _clause_db.AsParallel().Select<CNFClause, ConflictException?>(clause => {
#endif
                CNFClause cause_clause = new CNFClause();
                CNFTruth new_truth = CNFTruth.Unknown;
                int false_vars = 0;
                KeyValuePair<String, CNFStates>? new_var = null;

                foreach (KeyValuePair<String, CNFStates> var in clause)
                {
                    CNFTruth truth;
                    if (_assignment_stack.TryGetVariableValue(var.Key, out truth)) {
                        // The assignment stack should never have an unknown in it.
                        System.Diagnostics.Debug.Assert(truth != CNFTruth.Unknown);
                        // If the value doesn't match the known one, then it's not asserting.
                        if (CompareStateToTruth(var.Value, truth) == false)
                        {
                            // Increment our counter.
                            false_vars++;
                        }
                    }
                    // If we haven't already found a clause (This reduces calls to CNFStateToTruth which shaved 4 seconds in 27 off the run time in my test routine.)
                    else if (new_truth == CNFTruth.Unknown)
                    {
                        // If we don't have the value, mark that we might have an implication about it if it's the only one.
                        cause_clause = clause;
                        new_var = var;
                        new_truth = CNFStateToTruth(var.Value);
                    }
                }

                // If there was only one unknown value
                if (false_vars == clause.Count - 1 && new_var != null)
                {
                    // Then it's implied to be asserting. Make a new implication.
                    ImplicationResult implication = new ImplicationResult(new_var.Value.Key, new_truth, cause_clause);
#if VERBOSE
                    Console.WriteLine("Implication {0} found at depth {1}", implication, current_depth);
#endif

                    // Check if we already have an implication for this variable.
                    // Lock the result structure since we're going to depend on it a lot, do non-atomic operations on it,
                    // and it's not thread safe. There is a thread safe alternative, but it's still not a good idea because
                    // of the non-atomic operations. (Specifically the ContainsKey check followed by further decisions
                    // on the results.)
                    lock (result)
                    {
                        // If we already have an implication for that variable
                        if (result.ContainsKey(implication.Variable))
                        {
                            // Grab the existing one
                            ImplicationResult test_implication = result[new_var.Value.Key];
                            // And check if it isn't the same as our new one.
                            if (test_implication.Truth != new_truth)
                            {
                                // If so it's a conflict. Return it back to be thrown.
                                return new ConflictException(test_implication.Clause, implication.Clause, implication.Variable);
                            }
                        }
                        else
                        {
                            // If it isn't already in our known implications, add it.
                            result.Add(new_var.Value.Key, implication);
                        }
                    }
                }

                return null;
            }).FirstOrDefault(conflict => conflict != null);

            // If there was a conflict
            if (conflict != null)
            {
                // Throw it back as an exception.
                throw conflict;
            }
            else
            {
                // Otherwise return whatever implications we found.
                return result;
            }
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

            List<String> vars = new List<string>(_detected_variables);
            long cycle_count=0;
            DateTime last_update = DateTime.UtcNow;

            Console.WriteLine("Preprocessing...");
            Preprocess();

#if VERBOSE
            Console.WriteLine("After Preprocessing:");
            Console.WriteLine("Queue: " + String.Join(", ", _assignment_queue));
            Console.WriteLine("Clause DB:");
            Console.WriteLine(String.Join("^", _clause_db));
#endif
            // We would pick an ordering here and shuffle the vars list to be something more ideal here.
            // This is why vars is a List instead of a HashSet; it allows for reordering.
            // PickOrdering()

            Console.WriteLine("Starting main processing loop...");
            while (true)
            {
                cycle_count++;
#if VERBOSE
                Console.WriteLine();
                Console.WriteLine("Starting Cycle {0}", cycle_count);
                Console.WriteLine("Queue: {0}", String.Join(", ", _assignment_queue));
#else
                if (DateTime.UtcNow.Subtract(last_update).TotalSeconds > 2)
                {
                    last_update = DateTime.UtcNow;
                    Console.WriteLine();
                    Console.WriteLine("Starting Cycle {0}", cycle_count);
                    Console.WriteLine("Queue Length: {0}", _assignment_queue.Count);
                    Console.WriteLine("Stack Length: {0}", _assignment_stack.Count);
                    Console.WriteLine("Stack Depth: {0}", _assignment_stack.GetDepth());
                    Console.WriteLine("Depth 0 variables: {0}", _assignment_stack.Count(entry => entry.Depth == 0));
                }
#endif

                // ChooseNextAssignment()

                if (_assignment_queue.Count > 0)
                {
                    AssignmentEntry next_var = _assignment_queue[0];
                    _assignment_queue.RemoveAt(0);
                    _assignment_stack.Push(next_var);
#if VERBOSE
                    Console.WriteLine("Pulled from queue {0}", next_var);
#endif
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
                        // Quick sanity check. Every clause should be valid.
                        System.Diagnostics.Debug.Assert(_clause_db.All(clause => ComputeValidity(clause) == CNFTruth.True), "Unknown or invalid clause detected in complete assignment!");

                        // If there aren't any matches, that means we've got all of them assigned (or there were none to assign anyway.)
                        // That means we're done.
                        return _assignment_stack;
                    }

                    // Get our depth
                    int depth = _assignment_stack.GetDepth() + 1;
                    // Make a new entry. Since we don't know what value to try, try true first.
                    AssignmentEntry next_var = new AssignmentEntry(varname, CNFTruth.True, _decided_placeholder, true, depth);
#if VERBOSE
                        Console.WriteLine("Adding Decision {0}", next_var);
#endif
                    EnqueueEntry(next_var);
                }

#if VERBOSE
                Console.WriteLine("Queue: {0}", String.Join(", ", _assignment_queue));
                Console.WriteLine("Stack: {0}", _assignment_stack);
#endif

                Dictionary<String, ImplicationResult>? result = null;
                // Initialize our continuation variable to ensure the first loop runs.
                bool conflicted = true;

                // While there's still work to do (we found a conflict
                while (conflicted && _assignment_stack.Count > 0) {
                    try
                    {
                        // Get back an array of inferred values
                        result = Deduce();
                        conflicted = false;
                    }
                    // If there was a conflict
                    catch (ConflictException conflict)
                    {
#if VERBOSE
                        Console.WriteLine("Detected conflict: {0}", conflict);
#endif
                        conflicted = true;
                        // "a conflict at level 0 results in a backtracking level of−1,which causes CDCL to report unsatisfiability"
                        if (_assignment_stack.GetDepth() == 0)
                        {
                            Console.WriteLine("Unsatisfiability conflict: {0}", conflict);
                            throw new UnsatisfiableException();
                        }

                        // Analyze the conflict and get a conflict clause
                        CNFClause conflict_clause = AnalyzeConflict(conflict, _assignment_stack.GetDepth());
                        // Add the clause to our database.
                        _clause_db.Add(conflict_clause);

#if VERBOSE
                        Console.WriteLine("Learned Conflict Clause {0}.", conflict_clause);

#endif
                        int backtrack_level = 0;

                        // If our conflict clause is a unit clause
                        if (conflict_clause.Count == 1)
                        {
                            // Add it directly to the assignment queue
                            KeyValuePair<String, CNFStates> var = conflict_clause.First();
                            AssignmentEntry entry = new AssignmentEntry(var.Key, CNFStateToTruth(var.Value), conflict_clause, false, 0);
                            EnqueueEntry(entry);
                            
                            // And backtrack to level 0.
                            backtrack_level = 0;
                        }
                        else
                        {

                            // Otherwise, look for the biggest assignment below the current depth
                            int target_depth = _assignment_stack.GetDepth();
                            backtrack_level = conflict_clause.Max(var => {
                                // Only count values that we have assignments for (there shouldn't be any we don't that matter)
                                if (!_assignment_stack.ContainsVariable(var.Key))
                                {
                                    return -1;
                                }
                                else
                                {
                                    // Cache the depth of the variable on the assignment stack
                                    int depth = _assignment_stack.GetEntryByVariable(var.Key).Depth;                                    
                                    // If it's below our taget depth, then it counts
                                    if (depth < target_depth)
                                    {
                                        return depth;
                                    }
                                    else
                                    {
                                        // Otherwise it doesn't.
                                        return -1;
                                    }
                                }
                            });
                            // Sanity check; this shouldn't happen.
                            System.Diagnostics.Debug.Assert(backtrack_level >= 0, "Backtrack level below 0 detected!");
                        }

#if VERBOSE
                        Console.WriteLine("Backtracking to depth {0}.", backtrack_level);
#endif
                        // Erase anything out of the stack that is higher depth than our target.
                        while (_assignment_stack.Count > 0 && _assignment_stack.GetDepth() > backtrack_level)
                        {
                            _assignment_stack.Pop();
                        }

                        // Erase anything out of the queue that is higher depth than our target (in case we had lots of implications.)
                        while (_assignment_queue.Count > 0 && _assignment_queue[0].Depth > backtrack_level)
                        {
                            _assignment_queue.RemoveAt(0);
                        }
                    }
                }
                
                // If there were implications in our non-conflicted pass...
                if (result != null)
                {
                    // Go through each and add them.
                    foreach (ImplicationResult implication in result.Values)
                    {
                        AssignmentEntry entry = new AssignmentEntry(implication.Variable, implication.Truth, implication.Clause, false, _assignment_stack.GetDepth());
                        // "When a new assignment is added to the stack, its implications are added by Deduce to the assignment queue"
#if VERBOSE
                        Console.WriteLine("Adding {0} to the queue", entry);
#endif
                        if (!_assignment_stack.ContainsVariable(entry.Variable))
                        {
                            EnqueueEntry(entry);
                        }
                    }
                }
            }
        }

        // Returns the list of clauses used by the solver. Useful for iterating to find alternate assignments.
        public HashSet<CNFClause> GetClauses()
        {
            HashSet<CNFClause> result = new HashSet<CNFClause>(_clause_db);
            result.UnionWith(_assignment_stack.Where(entry => entry.Depth == 0).Select<AssignmentEntry, CNFClause>(entry => entry.RelatedClause));

            return result;
        }

        // Convienence Wrapper. Takes a more complete formula and solves it directly, discarding the solver at the end.
        public static AssignmentStack Solve(CNFFormula formula)
        {
            Solver s = new Solver();

            foreach (CNFClause clause in formula)
            {
                s.AddClause(clause);
            }

            return s.Solve();
        }


        public void ParseFormula(String formula)
        {
            // Break the clauses up by spaces
            foreach (String clause_text in formula.Split(" "))
            {
                // If there's nothing in this clause after splitting, skip it. (For example if there was an extra space between the clauses.)
                if (clause_text.Length == 0)
                {
                    continue;
                }
                if (clause_text.StartsWith("#"))
                {
                    // Stop parsing if a pound is encountered (indicating an inline comment.)
                    return;
                }

                // Split up the variables inside the clause argument
                String[] vars = clause_text.Split(',');

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
                AddClause(new_clause);
#if VERBOSE
                Console.WriteLine("Added clause: {0}", new_clause.ToString());
#endif
            }
        }

        public static Solver FromFile(String filename)
        {
            Solver result=new Solver();
            System.IO.TextReader source = System.IO.File.OpenText(filename);
            String? line;
            while ((line = source.ReadLine()) != null) {
                // Wipe out any leading/trailing whitespace.
                line = line.Trim();
                // If it's blank after trimming, skip it.
                if (line.Length == 0)
                {
                    continue;
                }
                // Check if it starts with a # (indicating a comment)
                if (line.StartsWith("#"))
                {
                    continue;
                }
                result.ParseFormula(line);
            }
            return result;
        }
#endregion
    }
}
