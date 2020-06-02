This is a submission to the 2020 PACE challenge, Exact Tract: Computing treedepth decompositions of minimum width Submission by: Tom C. van der Zanden (Maastricht University)

The solver is a basic one which computes decompositions bottom-up, in a dynamic programming/positive-instance driven type approach. I.e., for increasing values of k=1,2,... we try to solve the problem of obtaining a decomposition of width at most k.
We start with a set of partial solutions which in the first iteration are just single-vertex subsets. For each partial solution we compute a lower bound by considering the width of the part decomposed so far and the size of the set separating the decomposed part from the rest of the graph. We then iteratively try to move vertices from the separator into the decomposition or merging two partial solutions together.

Compilation/running instructions

The solution is written in C#.

On Windows, the solution can be opened, compiled and ran with Visual Studio.
On Linux/Mac, the solution can be compiled and ran with Mono.