using System;
using System.Collections.Generic;
using System.Text;

namespace cdclsolver
{
    public class AssignmentStackEntry : object
    {
        public String Variable;
        public CNFTruth Truth;
        public CNFClause RelatedClause;
        public bool Decided;
        public int Depth;
    }

    public class AssignmentStack : List<AssignmentStackEntry>
    {
        private Dictionary<string, AssignmentStackEntry> _assignment_index = new Dictionary<string, AssignmentStackEntry>();

        public override string ToString()
        {
            StringBuilder output = new StringBuilder();
            for (int index= this.Count-1; index >= 0; index--)
            {
                AssignmentStackEntry item = this[index];
                String truth;
                String decided;

                truth = item.Truth switch
                {
                    CNFTruth.True => "T",
                    CNFTruth.False => "F",
                    CNFTruth.Unknown => "U",
                    _ => "E"
                };

                decided = item.Decided switch
                {
                    true => "D",
                    false => "I"
                };

                output.AppendLine(String.Format("{0}={1}: {2}, {3}", item.Variable, truth, item.Depth, decided));
            }

            return output.ToString();
        }

        public bool Push(String var_name, bool decided, CNFClause clause, CNFTruth truth, int depth)
        {
            // If we already have a known assignment for this variable...
            if (_assignment_index.ContainsKey(var_name))
            {
                // Return true if it matches.
                return _assignment_index[var_name].Truth == truth;
            }

            AssignmentStackEntry entry;
            if (depth == 0)
            {
                entry = new AssignmentStackEntry
                {
                    Variable = var_name,
                    Decided = decided,
                    RelatedClause = clause,
                    Truth = truth,
                    Depth = depth
                };

                // Known values are placed at the beginning to ensure that they all are grouped together
                this.Insert(0, entry);
            }
            else
            {
                entry = new AssignmentStackEntry
                {
                    Variable = var_name,
                    Decided = decided,
                    RelatedClause = clause,
                    Truth = truth,
                    Depth = depth
                };

                // Known values are placed at the beginning to ensure that they all are grouped together
                this.Add(entry);
            }
            _assignment_index.Add(var_name, entry);
            return true;
        }

        public AssignmentStackEntry Pop()
        {
            int index = this.Count - 1;

            if (index < 0)
            {
                throw new IndexOutOfRangeException("Tried to pop on an empty stack.");
            }
            AssignmentStackEntry item = this[index];

            if (item.Depth == 0)
            {
                throw new IndexOutOfRangeException("Tried to pop on a stack with no poppable members.");
            }

            // Pull the variable from the index.
            _assignment_index.Remove(item.Variable);
            // And pull the item out of the list. This should always be the last element, so this should go fast.
            this.RemoveAt(index);

            return item;
        }

        public AssignmentStackEntry Peek()
        {
            int index = this.Count - 1;

            if (index < 0)
            {
                throw new IndexOutOfRangeException("Tried to peek on an empty stack.");
            }

            return this[index];
        }

        public bool AddKnownVariable(String var_name, CNFTruth truth, CNFClause clause)
        {
            return Push(var_name, false, clause, truth, 0);
        }

        public bool ContainsVariable(String var_name)
        {
            return _assignment_index.ContainsKey(var_name);
        }

        public CNFTruth GetVariable(String var_name)
        {
            return _assignment_index[var_name].Truth;
        }

        public void RemoveToDepth(int depth)
        {
            if (depth < 0)
            {
                throw new ArgumentException("Depth should be a number >= 0.");
            }

            // As long as we're not empty and we're still deeper than we want to remove
            while (this.Count > 0 && Peek().Depth > depth)
            {
                Pop();
            }
        }
    }
}
