using System;
using System.Collections.Generic;
using System.Text;

namespace cdclsolver
{
    public class CNFClause : Dictionary<String, CNFStates>
    {                
        public override int GetHashCode()
        {
            int hash=0;
            foreach (KeyValuePair<String, CNFStates> var in this)
            {
                hash ^= var.Key.GetHashCode();
                hash ^= var.Value.GetHashCode();
            }
            return hash;
        }

        public override bool Equals(object obj)
        {
            // This isn't nothing.
            if (obj is null)            
            {
                return false;
            }
            // We can only be equal to things we can assign to CNFClauses.
            if (! typeof(CNFClause).IsAssignableFrom(obj.GetType()))
            {
                return false;
            }

            // Cast it to a Clause.
            CNFClause other_clause = (CNFClause) obj;

            // We can't be equal if we're different lengths.
            if (other_clause.Count != this.Count)
            {
                return false;
            }

            // For each variable in the clause
            foreach (KeyValuePair<String, CNFStates> var in this)
            {
                // If they don't have one of our variables, then we can't be the same.
                if (! other_clause.ContainsKey(var.Key))
                {
                    return false;
                }

                // If they have our variable, it's state has to be the same to be the same clause.
                if (other_clause[var.Key] != var.Value)
                {
                    return false;
                }
            }

            // If all of those conditions are true, then we're the same.
            return true;
        }

        public override string ToString()
        {
            List<String> vals = new List<String>();

            foreach (KeyValuePair<String, CNFStates> var in this)
            {
                if (var.Value == CNFStates.Asserted)
                {
                    vals.Add(var.Key);
                }
                else
                { 
                    vals.Add("-" + var.Key);
                }
            }
            return String.Join("v", vals);
        }
    }
}
