# CDCL Solver
By John-Michael O'Brien, ECEN 5139

## Usage
### Running the Program
The program can be run using a commandline clause list with the following syntax:

`cdclsolver <clause>[,<clause>,[<clause>,[...]]]`

To run with a source file:

`cdclsolver --file <filename>`

The binary can be found in `cdclsolver\cdclsolver\bin\Debug\netcoreapp3.0\`

### Clause Format
`[~]<variable name>,[[~]<variable name>, [[~]<variable name>, [...]]]`

`~` indicates that the clause requires the variable to be negative. Variable names cannot start with the character `#` as it will
be treated as a comment, or `~` as it will be treated as a negation. However, both of these characters can be used inside a
variable name (i.e. `x#45` and `prefix~postfix` are both valid variable names, but `#a` is not). Variable names are case-sensitive,
and can include any printable UTF-8 codepage 0 character. Others may work, but C#'s handling of Unicode is odd and the program
was not written with emoji and foreign characters in mind.

For example:
The formula `(¬a∨¬b∨c)∧(a∨¬b∨c)∧(¬c∨d)∧(¬c∨¬d)∧(¬a∨c∨d)∧(¬a∨b∨¬d)∧(b∨c∨¬d)∧(a∨b∨d)` can be tested by using the
following command:

`cdclsolver ~a,~b,c a,~b,c ~c,d ~c,~d ~a,c,d ~a,b,~d b,c,~d a,b,d`

### File Format

Input files will read from each line of a multi-line text file. Any line whose first non-whitespace charater is `#` will be treated
as a comment. Any amount of whitespace can be placed before, between, or after the clauses. If a whitespace is followed by a `#`
in a line, everything after that will be treated as a comment.

The example below will be imported as `(¬a∨¬b∨c)∧(a∨¬b∨c)∧(¬c∨d)∧(¬c∨¬d)∧(¬a∨c∨d)∧(¬a∨b∨¬d)∧(b∨c∨¬d)∧(a∨b∨d)∧(E#~)`:
```
# Test file, by JM. Derived from the classrom example.
~a,~b,c a,~b,c
        

# Some dummy whitespace above this
~a,c,d          ~a,b,~d b,c,~d # inline comment example, also, lots of inline whitespace
      a,b,d # Prefixing whitespace and inline whitespace
# Tons of trailing whitespace below this (Also, the conflicting pair. Comment out the second
# one to make this satisfiable.)
~c,d                  
~c,~d

# Dummy unit literal with # and ~ in it. Can't start with either, but no reason you can't
# include it elsewhere, though I'd recommend against it for readability reasons.
E#~
```

The returned result will be the assignment stack's value and associated clauses, or the literal `Unsatisfiable`.

## Implementation and Observations

The goal with this was to implement a multithreaded CDCL solver. The deduction can be done using multithreaded testing since the
clauses are all tested for implications independantly of each other. Doing this allows for an optimization on modern, multi-core
systems, greatly increasing the computational speed of the process. To accomplish this goal, I selected C# to allow for a good
blend of memory management features, a broad palette of multithreading tools, as well as high code performance. C#'s tendancy to
hiccup as a result of garbage collection won't be an issue here, as there is no user component to this process, nor is there a
latency requirement.

Overall, I was able to duplicate, from scratch, the CDCL solution methodology using the documentation from the class notes. There
were several challenges in doing so, not the least of which being my own penchant for premature optimization. That said, there was
a simple optimization that was done to the assignment stack that vastly improved the speed. Since variable lookups (and stack
entry lookups) are exceedingly common, the AssignmentStack object keeps a variable name keyed index. This can be used to do rapid
lookups of entries based on variable name (in `O(1)` time). This significantly speeds up the process of analyzing the data.

With that said, a great deal of optimization could be done to this program, and a few have. First of all, there's a lot of
scrubbing through the variables. Many parts of this algorithm operate in `O(n)` time. Caching would *greatly* improve the speed of
this algorithm; often times only one variable has changed. Checking every clause to see if they need to be validated is wildly
inefficient. To that same end, there is significant value in considering the application of bloom filters. It's not an issue on the
iny test cases that I've been working with; but applied to a larger problem with millions of variables, a bloom filter, selecting
on relevant variables, would allow for the program to work from a much smaller list of data.

That said, the most notable optimization is multithreading, which was done and caused a substantial improvement in performance.
When dealing with large datasets (1,000 variables, 10,000 clauses) the speed was increased by a factor of over 300%. This
improvement scales mostly with the size of the clause database and does not scale with the variable count. The reason for this is
that the parallelization of the implication search is done along the clause database, instead of along the variable list. There
is an open question if sweeping the variable list across the clause list and searching for a variable within the clause might
be more efficient than sweeping the clauses and checking them against the variable list, but as of present this has not been
explored. Similalry, most optimizations were done based on hot spots identified via profiling. Larger architectural analysis
is warranted; the implementation is naive, and almost assuredly could save itself a good deal of effort with more thought applied
to the methodology's implementation details.