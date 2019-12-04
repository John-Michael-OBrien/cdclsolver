# CDCL Solver
By John-Michael O'Brien, ECEN 5139

## Usage:
`cdclsolver <clause>[,<clause>,[<clause>,[...]]]`

The binary can be found in `cdclsolver\cdclsolver\bin\Debug\netcoreapp3.0\`

## Clause format:
`[~]<variable name>,[[~]<variable name>, [[~]<variable name>, [...]]]`

`~` indicates that the clause requires the variable to be negative.

For example:
The formula `(¬a∨¬b∨c)∧(a∨¬b∨c)∧(¬c∨d)∧(¬c∨¬d)∧(¬a∨c∨d)∧(¬a∨b∨¬d)∧(b∨c∨¬d)∧(a∨b∨d)` can be tested by using the
following command:

`cdclsolver ~a,~b,c a,~b,c ~c,d ~c,~d ~a,c,d ~a,b,~d b,c,~d a,b,d`

The returned result will be the assignment stack's value and associated clauses, or the literal `Unsatisfiable`.

The goal with this was to implement a multithreaded CDCL solver. The deduction can be done using multithreaded testing. Doing this
would allow for an optimization on modern, multithreaded systems, potentially greatly increasing the computational speed of the
process. In doing this, I selected C# to allow for a good blend of memory management features, a broad palette of multithreading
tools, as well as high code performance. C#'s tendancy to hiccup as a result of garbage collection won't be an issue here, as there
is no user component to this process.

Overall, I was able to duplicate, from scratch, the CDCL solution methodology using the documentation from the class notes. There
were several challenges in doing so, not the least of which being my own penchant for premature optimization. That said, there was
a simple optimization that was done to the assignment stack that vastly improved the speed. Since variable lookups (and stack
entry lookups) are exceedingly common, the AssignmentStack object keeps a variable name keyed index. This can be used to do rapid
lookups of entries based on variable name (in `O(1)` time). This significantly speeds up the process of analyzing the data.

With that said, a great deal of optimization could be done to this program. First of all, there's a lot of scrubbing through the
variables. Many parts of this algorithm operate in `O(n)` time. Caching would *greatly* improve the speed of this algorithm;
often times only one variable has changed. Checking every clause to see if they need to be validated is wildly inefficient. To that
same end, there is significant value in considering the application of bloom filters. It's not an issue on the tiny test cases that
I've been working with; but applied to a larger problem with millions of variables, a bloom filter, selecting on relevant
variables, would allow for the program to work from a much smaller list of data.

As a final note, as this project stands right now, it does not use multi-threading. The intention is to add this functionality to
the program in the coming 24 hours, but the time constraints require that I make my immediate submission. I will ammend that
submission tomorrow where possible.