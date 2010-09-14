using System;
using System.Collections.Generic;
using System.Diagnostics;
using AdamMil.AI.Search;
using AC=AdamMil.Collections;

namespace AdamMil.AI.ConstraintSatisfaction
{

#region Problem definitions
#region IFiniteDomainCSP
/// <summary>Represents a binary constraint satisfaction problem (CSP) with variables that have finite domains.</summary>
/// <include file="documentation.xml" path="/AI/CSP/typeparam[@name='VarType']"/>
/// <remarks>
/// A CSP is a general problem representation consisting of variables, domains from which values for the
/// variables can be chosen, and a set of constraints on the variables. This interface represents a restricted form of
/// CSP, namely a binary CSP with finite domains. This means that constraints can only occur between two variables, and
/// that the domains for the variables must be finite. The first restriction is minor because any non-binary CSP can be
/// converted into a binary CSP with the addition of extra variables.
/// </remarks>
public interface IFiniteDomainCSP<VarType>
{
  /// <summary>Determines whether two variables with the given values conflict with the constraints.</summary>
  /// <param name="var1">The index of the first variable as returned by <see cref="GetVariables"/>.</param>
  /// <param name="value1">The index of the first variable's value within its domain.</param>
  /// <param name="var2">The index of the second variable as returned by <see cref="GetVariables"/>.</param>
  /// <param name="value2">The index of the second variable's value within its domain.</param>
  /// <returns>Returns true if the values of the variables conflict with the constraints in the CSP, and false if they
  /// do not.
  /// </returns>
  bool Conflicts(int var1, int value1, int var2, int value2);

  // TODO: should we add a GetRandomAssignment() method and use it in the local search?

  /// <summary>Retrieves the variables in the CSP, as well as the indices of the variables connected to each other by
  /// constraints.
  /// </summary>
  /// <param name="variables">Receives an array of the variables in the CSP.</param>
  /// <param name="neighbors">Receives an <see cref="INeighborList"/> holding the indices of the variables connected to
  /// each variable by constraints. The list should be the same length as <paramref name="variables"/>, and the
  /// collection at index <c>i</c> should contain the indices of the variables connected to the variable at index
  /// <c>i</c> by constraints, where the variable indices are as they are in <paramref name="variables"/>.
  /// </param>
  void GetVariables(out SimpleVariable<VarType>[] variables, out INeighborList neighbors);
}
#endregion

#region INeighborList
/// <summary>An interface for a class that maps a variable index to the indices of the other variables connected to it
/// by constraints. These are called the neighbors of a variable.
/// </summary>
public interface INeighborList
{
  /// <include file="documentation.xml" path="/AI/CSP/INeighborList/Indexer/*"/>
  AC.IReadOnlyCollection<int> this[int variable] { get; }
}
#endregion

#region BinaryConstraint
/// <summary>Represents a constraint between two variables.</summary>
/// <include file="documentation.xml" path="/AI/CSP/typeparam[@name='VarType']"/>
/// <param name="value1">The value of the first variable.</param>
/// <param name="value2">The value of the second variable.</param>
/// <returns>Returns true if the values are consistent (ie, they don't violate the constraint), and false if are not.</returns>
public delegate bool BinaryConstraint<VarType>(VarType value1, VarType value2);
#endregion

#region ArrayDomain
/// <summary>Represents the domain of a finite variable with values taken from an array.</summary>
/// <include file="documentation.xml" path="/AI/CSP/typeparam[@name='VarType']"/>
/// <remarks>The domain for a variable represents the set of values that can be assigned to that variable.</remarks>
public sealed class ArrayDomain<VarType> : FiniteDomain<VarType>
{
  /// <summary>Initializes the domain from the values in the given array.</summary>
  public ArrayDomain(params VarType[] array) : base(array == null ? 0 : array.Length)
  {
    if(array == null) throw new ArgumentNullException();
    this.array = (VarType[])array.Clone();
  }

  /// <summary>Gets the value from the domain's original values at the given index.</summary>
  public override VarType this[int index]
  {
    get { return array[index]; }
  }

  readonly VarType[] array;
}
#endregion

#region FiniteDomain
/// <summary>Represents the domain of a finite variable.</summary>
/// <include file="documentation.xml" path="/AI/CSP/typeparam[@name='VarType']"/>
/// <remarks>The domain for a variable represents the set of values that can be assigned to that variable.</remarks>
public abstract class FiniteDomain<VarType>
{
  /// <summary>Initializes the finite domain with the given number of values.</summary>
  /// <param name="valueCount">The number of values in the domain. There must be at least one value.</param>
  protected FiniteDomain(int valueCount)
  {
    if(valueCount < 1) throw new ArgumentOutOfRangeException();
    this.count = valueCount;
  }

  /// <summary>Gets the value from the domain's original values at the given index.</summary>
  public abstract VarType this[int index]
  {
    get;
  }

  /// <summary>Gets the number of original values in the domain.</summary>
  public int Count
  {
    get { return count; }
  }

  /// <summary>Converts the domain to a human-readable string containing the list of values.</summary>
  public override string ToString()
  {
    // concatenate all the values together, separated by spaces
    System.Text.StringBuilder sb = new System.Text.StringBuilder();
    for(int i=0; i<count; i++)
    {
      if(sb.Length != 0) sb.Append(' ');
      sb.Append(Convert.ToString(this[i]));
    }
    return sb.ToString();
  }

  /// <summary>The number of values in the domain.</summary>
  readonly int count;
}
#endregion

#region IntPair
/// <summary>Represents a pair of small integers suitable for use as a key in a dictionary.</summary>
struct IntPair
{
  /// <summary>Initializes the <see cref="IntPair"/> with the given integers.</summary>
  public IntPair(int var1, int var2)
  {
    Value1 = var1;
    Value2 = var2;
  }

  /// <summary>Determines whether this <see cref="IntPair"/> is equal to another <see cref="IntPair"/>.</summary>
  /// <param name="obj">An <see cref="IntPair"/> to compare against.</param>
  public override bool Equals(object obj)
  {
    IntPair other = (IntPair)obj;
    return Value1 == other.Value1 && Value2 == other.Value2;
  }

  /// <summary>Gets the hash code for this <see cref="IntPair"/>.</summary>
  public override int GetHashCode()
  {
    // this assumes that the integer values are relatively small (< 64k), so that it's better to shift Value1 over
    return (Value1 << 16) ^ Value2;
  }

  /// <summary>Converts this <see cref="IntPair"/> into a string.</summary>
  public override string ToString()
  {
    return Value1.ToString() + " " + Value2.ToString();
  }

  /// <summary>The values in the <see cref="IntPair"/>.</summary>
  public readonly int Value1, Value2;
}
#endregion

#region NeighborArrayList
/// <summary>Implements a neighbor list that represents the mapping between a variable and its neighbors as a simple
/// array of arrays.
/// </summary>
public sealed class NeighborArrayList : INeighborList
{
  /// <summary>Initializes the neighbor list with an array of arrays.</summary>
  /// <param name="neighbors">An array of arrays that represents the neighbors for each variable. The array at index
  /// <c>i</c> holds the indices of the neighbors of the variable at index <c>i</c>.
  /// </param>
  public NeighborArrayList(int[][] neighbors)
  {
    if(neighbors == null) throw new ArgumentNullException();
    this.neighbors = new AC.ReadOnlyCollectionWrapper<int>[neighbors.Length];
    for(int i=0; i<neighbors.Length; i++) this.neighbors[i] = new AC.ReadOnlyCollectionWrapper<int>(neighbors[i]);
  }

  /// <include file="documentation.xml" path="/AI/CSP/INeighborList/Indexer/*"/>
  public AC.IReadOnlyCollection<int> this[int index]
  {
    get { return neighbors[index]; }
  }

  readonly AC.ReadOnlyCollectionWrapper<int>[] neighbors;
}
#endregion

#region SimpleVariable
/// <summary>Represents a variable with a finite domain.</summary>
/// <typeparam name="VarType">The .NET type representing the values of the variable.</typeparam>
public struct SimpleVariable<VarType>
{
  /// <summary>Initializes the variable with the given name and domain.</summary>
  /// <param name="name">An optional name given to the variable.</param>
  /// <param name="domain">The variable's domain, which contains the values that the variable can have.</param>
  public SimpleVariable(string name, FiniteDomain<VarType> domain)
  {
    if(domain == null) throw new ArgumentNullException();
    Name   = name;
    Domain = domain;
  }

  /// <summary>Returns the name of the variable.</summary>
  public override string ToString() { return Name; }

  /// <summary>The variable's domain.</summary>
  public readonly FiniteDomain<VarType> Domain;

  /// <summary>The variable's name.</summary>
  public readonly string Name;
}
#endregion

#region FiniteDomainCSP
/// <summary>Represents a configurable binary CSP with finite domains.</summary>
/// <include file="documentation.xml" path="/AI/CSP/typeparam[@name='VarType']"/>
/// <remarks>This is a simple implementation of <see cref="IFiniteDomainCSP{T}"/> that allows you to easily add
/// variables and constraints to create a CSP. To use this class, first add your variables with
/// <see cref="AddVariable"/>. Then add the constraints with <see cref="AddConstraint"/>. You can solve the CSP with
/// any of the finite domain CSP solvers, such as <see cref="BacktrackingSolver{T}"/> or
/// <see cref="LocalSearchSolver{T}"/>. Finally, you can convert the solution returned from the solver into a simpler
/// form using <see cref="ConvertSolution"/>.
/// </remarks>
public class FiniteDomainCSP<VarType> : IFiniteDomainCSP<VarType>
{
  /// <summary>Adds a new variable to the CSP.</summary>
  /// <param name="name">The name of the variable.</param>
  /// <param name="domain">The variable's domain.</param>
  /// <remarks>The variable must have a unique name, but it may share a domain with other variables.</remarks>
  public void AddVariable(string name, FiniteDomain<VarType> domain)
  {
    if(name == null || domain == null) throw new ArgumentNullException();
    if(GetVariableIndex(name) != -1) throw new ArgumentException("A variable named '"+name+"' already exists.");
    variables.Add(new SimpleVariable<VarType>(name, domain));
    OnCSPChanged();
  }

  /// <summary>Adds a new constraint to the CSP.</summary>
  /// <param name="var1">The first variable in the constraint.</param>
  /// <param name="var2">The second variable in the constraint.</param>
  /// <param name="constraint">A delegate that will be called with the values of the two variables, and which must
  /// expected determine whether the values are consistent. The first argument will be the value of
  /// <paramref name="var1"/> and the second argument will be the value of <paramref name="var2"/>.
  /// </param>
  /// <remarks>You can only add one constraint per variable, and since constraints are symmetrical, if you've added a
  /// constraint for variables A and B, you cannot add another constraint for variables B and A. This should not be
  /// much of a restriction though, since you can always combine multiple constraints into a single constraint.
  /// </remarks>
  public void AddConstraint(string var1, string var2, BinaryConstraint<VarType> constraint)
  {
    if(var1 == null || var2 == null || constraint == null) throw new ArgumentNullException();
    if(string.Equals(var1, var2, StringComparison.Ordinal))
    {
      throw new ArgumentException("The two variables cannot be equal.");
    }

    int varIndex1 = GetVariableIndex(var1), varIndex2 = GetVariableIndex(var2);
    if(varIndex1 == -1) throw new ArgumentException("The variable named '"+var1+"' could not be found.");
    if(varIndex2 == -1) throw new ArgumentException("The variable named '"+var2+"' could not be found.");

    // make sure there's no constraint for these two variables already (being sure to check (v2,v1) as well as (v1,v2))
    IntPair pair = new IntPair(varIndex1, varIndex2);
    if(constraints.ContainsKey(pair) || constraints.ContainsKey(new IntPair(varIndex2, varIndex1)))
    {
      throw new ArgumentException("A constraint already exists between variables '"+var1+"' and '"+var2+"'.");
    }

    constraints.Add(pair, constraint);
    OnCSPChanged();
  }

  /// <summary>Given a solution from one of the CSP solvers, returns a dictionary mapping variable names to their
  /// values.
  /// </summary>
  /// <remarks>The CSP solvers don't care about or use the names or values of the variables, only the indices of the
  /// variables and the indices of the values with the domain. The solution returned by a solver (a mapping from
  /// variable index to value index) reflects this. This method converts such a solution into a dictionary that maps
  /// variable names to variable values.
  /// </remarks>
  public Dictionary<string,VarType> ConvertSolution(Assignment solution)
  {
    if(solution == null) throw new ArgumentNullException();

    Dictionary<string,VarType> newSolution = new Dictionary<string,VarType>(solution.AssignedCount);
    for(int i=0; i<solution.TotalCount; i++)
    {
      if(solution.IsAssigned(i)) newSolution[variables[i].Name] = variables[i].Domain[solution[i]];
    }
    return newSolution;
  }

  /// <summary>Given a variable name, returns the variable's index within <see cref="variables"/>, or -1 if it could
  /// not be found.
  /// </summary>
  int GetVariableIndex(string name)
  {
    for(int i=0; i<variables.Count; i++)
    {
      if(string.Equals(name, variables[i].Name, StringComparison.Ordinal)) return i;
    }
    return -1;
  }

  /// <summary>Called whenever the CSP definition changes.</summary>
  void OnCSPChanged()
  {
    neighbors = null;
  }

  /// <summary>Determines whether the given variables and values conflict with the constraints.</summary>
  bool IFiniteDomainCSP<VarType>.Conflicts(int var1, int value1, int var2, int value2)
  {
    // try to find a constraint with the pair (var2,var1) as well as (var1,var2)
    BinaryConstraint<VarType> constraint;
    if(constraints.TryGetValue(new IntPair(var1, var2), out constraint))
    {
      return !constraint(variables[var1].Domain[value1], variables[var2].Domain[value2]);
    }
    else
    {
      return constraints.TryGetValue(new IntPair(var2, var1), out constraint) &&
             !constraint(variables[var2].Domain[value2], variables[var1].Domain[value1]);
    }
  }

  /// <summary>Gets the variables and constraints in this CSP.</summary>
  void IFiniteDomainCSP<VarType>.GetVariables(out SimpleVariable<VarType>[] variables, out INeighborList neighbors)
  {
    variables = this.variables.ToArray();
    
    if(this.neighbors == null)
    {
      int[][] neighborArray = new int[variables.Length][];

      // first count how many constraints each variable is involved in
      int[] neighborCounts = new int[variables.Length];
      foreach(IntPair pair in constraints.Keys)
      {
        neighborCounts[pair.Value1]++;
        neighborCounts[pair.Value2]++;
      }

      // then allocate the arrays for each variable, given the counts
      for(int i=0; i<neighborArray.Length; i++) neighborArray[i] = new int[neighborCounts[i]];
      
      // repurpose neighborCounts to hold how many indices are currently in each array
      Array.Clear(neighborCounts, 0, neighborCounts.Length);

      // add all of the neighbors of each variable
      foreach(IntPair pair in constraints.Keys)
      {
        neighborArray[pair.Value1][neighborCounts[pair.Value1]++] = pair.Value2;
        neighborArray[pair.Value2][neighborCounts[pair.Value2]++] = pair.Value1;
      }

      this.neighbors = new NeighborArrayList(neighborArray);
    }

    neighbors = this.neighbors;
  }

  readonly List<SimpleVariable<VarType>> variables = new List<SimpleVariable<VarType>>();
  readonly Dictionary<IntPair, BinaryConstraint<VarType>> constraints = new Dictionary<IntPair, BinaryConstraint<VarType>>();
  
  /// <summary>A cached copy of the list of neighbors of each variable.</summary>
  NeighborArrayList neighbors;
}
#endregion
#endregion

#region Search types
#region Assignment
/// <summary>This class represents the set of values assigned to the variables in a constraint satisfaction problem.</summary>
public sealed class Assignment
{
  /// <summary>Creates an empty assignment with space for the given number of variables.</summary>
  public Assignment(int varCount)
  {
    if(varCount < 0) throw new ArgumentOutOfRangeException();
    values = new int[varCount];
    for(int i=0; i<values.Length; i++) values[i] = -1; // initialize each value to -1, which means "not assigned"
  }

  /// <summary>Gets or sets the value assigned to the variable at the given index.</summary>
  /// <param name="index">The index of the variable within the assignment.</param>
  /// <returns>Returns the index of the value within the variable's domain that is assigned to the variable.</returns>
  public int this[int index]
  {
    get
    {
      if(index < 0 || index >= values.Length) throw new ArgumentOutOfRangeException();
      int value = values[index];
      if(value == -1) throw new InvalidOperationException("The variable is not currently assigned.");
      return value;
    }
    set
    {
      if(index < 0 || index >= values.Length || value < 0) throw new ArgumentOutOfRangeException();
      if(values[index] == -1) assignedCount++; // if it's not currently assigned, then we're adding a new assignment
      values[index] = value;
    }
  }

  /// <summary>Gets the number of variables currently assigned.</summary>
  public int AssignedCount
  {
    get { return assignedCount; }
  }

  /// <summary>Gets whether all of the variables are assigned.</summary>
  public bool IsComplete
  {
    get { return AssignedCount == TotalCount; }
  }

  /// <summary>Gets the total number of variables that can be assigned.</summary>
  public int TotalCount
  {
    get { return values.Length; }
  }

  /// <summary>Returns true if the variable at the given index is assigned and false if not.</summary>
  public bool IsAssigned(int index)
  {
    if(index < 0 || index >= values.Length) throw new ArgumentOutOfRangeException();
    return values[index] != -1; // -1 means "not assigned"
  }

  /// <summary>Unassigns the variable at the given index.</summary>
  public void Unassign(int index)
  {
    if(index < 0 || index >= values.Length) throw new ArgumentOutOfRangeException();

    if(values[index] != -1)
    {
      values[index] = -1;
      assignedCount--;
    }
  }

  /// <summary>The value indices for the variables.</summary>
  readonly int[] values;
  /// <summary>The number of currently-assigned variables.</summary>
  int assignedCount;
}
#endregion

#region CSPHelper
/// <summary>Provides helper functions for CSP solvers.</summary>
static class CSPHelper
{
  /// <summary>Prepares a search by retrieving the variables and neighbors from the problem, validating them, and
  /// validating the initial assignment, if any.
  /// </summary>
  public static void InitializeSearch<VarType>(IFiniteDomainCSP<VarType> problem, Assignment initialAssignment,
                                               out SimpleVariable<VarType>[] variables, out INeighborList neighbors)
  {
    problem.GetVariables(out variables, out neighbors);

    if(variables == null || neighbors == null)
    {
      variables = null;
      neighbors = null;
      throw new InvalidOperationException("The problem instance returned a null set of variables or neighbors.");
    }

    for(int i=0; i<variables.Length; i++)
    {
      if(variables[i].Domain == null)
      {
        variables = null;
        neighbors = null;
        throw new InvalidOperationException("The problem instance returned a variable with a null domain.");
      }
    }

    if(initialAssignment != null)
    {
      if(initialAssignment.TotalCount != variables.Length) // ensure it has the same number of variables
      {
        variables = null;
        neighbors = null;
        throw new ArgumentException("The initial state has the wrong number of variables.");
      }

      for(int i=0; i<initialAssignment.TotalCount; i++) // ensure that assigned value is a member of its variable's domain
      {
        if(initialAssignment.IsAssigned(i))
        {
          int value = initialAssignment[i];
          if(value < 0 || value >= variables[i].Domain.Count)
          {
            variables = null;
            neighbors = null;
            throw new ArgumentException("The initial state contains a value that does not exist in its domain.");
          }
        }
      }
    }
  }
}
#endregion

#region BacktrackingSolver
#region Configuration parameters
/// <summary>Selects the type of constraint propagation to perform.</summary>
/// <remarks>Using constraint propagation allows the solver to detect inconsistencies sooner. Maintaining the
/// constraint information takes some time and causes extra constraint checks. For most problems, the benefits of using
/// constraint propagation greatly outweigh the costs.
/// </remarks>
[Flags]
public enum ConstraintPropagation : byte
{
  /// <summary>Use no constraint propagation. Note that the <see cref="Heuristics.MostRestrictedVariable"/> heuristic
  /// requires constraint propagation, so if that heuristic is being used and you try to disable constraint
  /// propagation, forward checking constraint propagation will be performed to allow the heuristic to be used.
  /// </summary>
  None=0,
  /// <summary>When a variable is assigned, forward checking eliminates conflicting values from the domains of other
  /// variables. This is the simplest and cheapest form of constraint propagation, but also the least powerful.
  /// </summary>
  ForwardChecking=1,
  /// <summary>AC3 is an algorithm to ensure arc consistency. It is simple and more powerful than forward checking, but
  /// relatively expensive. However, the gains usually outweigh the costs. By default, selecting AC3 will run AC3 once
  /// at the start of the search. To run AC3 throughout the search and gain its full benefits, you must also use the
  /// <see cref="MAC"/> (maintaining arc consistency) option.
  /// </summary>
  AC3=2,
  /// <summary>To maintain arc consistency throughout the search, use this option. It must be combined with an arc
  /// consistency algorithm such as <see cref="AC3"/>.
  /// </summary>
  MAC=0x80
}

/// <summary>Selects the heuristics to use during the search.</summary>
/// <remarks>Heuristics enable the search to work more intelligently, selecting variables and values that are more
/// likely to be successful.
/// </remarks>
[Flags]
public enum Heuristics : byte
{
  /// <summary>Disable all heuristics. This is rarely a good idea. At the very least, the <see cref="Degree"/>
  /// heuristic, which is almost free, should be used.
  /// </summary>
  None=0,
  /// <summary>The degree heuristic causes the search to prefer to select variables that are involved in more
  /// constraints, similar to the <see cref="MostRestrictedVariable"/> heuristic. It is very powerful and comes almost
  /// for free, being precalculated before the search and incurring no new consistency checks. If combined with
  /// <see cref="MostRestrictedVariable"/>, the degree heuristic will be used as a tie-breaker between
  /// equally restricted variables.
  /// </summary>
  Degree=1,
  /// <summary>This is a relatively cheap and powerful heuristic that causes the search to select the variable with the
  /// fewest available choices. Using this heuristic will automatically enable some form of
  /// <see cref="ConstraintPropagation"/>, even if you've disabled it, as it's necessary to calculate the set of
  /// available choices for each variable.
  /// </summary>
  MostRestrictedVariable=2,
  /// <summary>This heuristic causes the search to prefer values that rule out the fewest values of the other
  /// unassigned variables. This can be somewhat expensive, as each value has to be checked against every other value
  /// of every unassigned neighbor, but on some problems it's worth it, especially if the constraint checks can be done
  /// quickly.
  /// </summary>
  LeastConstrainingValue=4,
  /// <summary>This heuristic causes the search to keep track of which variables may have caused a given conflict, and
  /// to use this information to backtrack directly to the variables that need to be revised. This heuristic is
  /// powerful, and quite cheap as well, costing only a small amount of time to maintain and incurring no new
  /// consistency checks.
  /// </summary>
  Backjumping=8,
  /// <summary>Enables all heuristics.</summary>
  All=Degree|MostRestrictedVariable|LeastConstrainingValue|Backjumping
}

/// <summary>Selects optimizations to attempt on the CSP before it is solved.</summary>
/// <remarks>Unlike the heuristics, these optimizations are performed before the search in an attempt to reduce the
/// complexity of the problem.
/// </remarks>
[Flags]
public enum Optimizations : byte
{
  /// <summary>Use no optimizations.</summary>
  None=0,
  /// <summary>Attempts to locate a small set of variables, called the cutset, such that removing those variables
  /// allows the rest of the problem to be solved in linear time. The partial solution can be combined with the
  /// remaining variables. This is quite complicated and is not implemented yet.
  /// </summary>
  CutsetConditioning=1,
  /// <summary>Attempts to decompose the problem into smaller subproblems such that each subproblem can be solved
  /// very quickly. This is quite complicated, and is not implemented yet.
  /// </summary>
  TreeDecomposition=2,
  /// <summary>Enables all optimizations.</summary>
  All=CutsetConditioning|TreeDecomposition
}
#endregion

/// <summary>This class implements a backtracking solver for finite domain constraint satisfaction problems.</summary>
/// <include file="documentation.xml" path="/AI/CSP/typeparam[@name='VarType']"/>
/// <remarks>The backtracking solver performs a systematic search of the space of possible assignments and is
/// guaranteed to find a solution if one exists, given enough time. This implementation supports various heuristics and
/// optimizations that greatly improve its efficiency over a naive backtracking search.
/// </remarks>
public sealed class BacktrackingSolver<VarType> : SearchBase<Assignment,Assignment>
{
  const int SuccessfulAssignment = -1, LimitReached = -2;

  /// <summary>Initializes the solver with the given problem instance.</summary>
  public BacktrackingSolver(IFiniteDomainCSP<VarType> problem)
  {
    if(problem == null) throw new ArgumentNullException();
    this.problem = problem;
  }

  /// <summary>Gets or sets the type of constraint propogation that will be used during the search.</summary>
  /// <remarks>The default value is <see cref="ConstraintSatisfaction.ConstraintPropagation.AC3"/> |
  /// <see cref="ConstraintSatisfaction.ConstraintPropagation.MAC"/>.
  /// </remarks>
  public ConstraintPropagation ConstraintPropagation
  {
    get { return cpMethod; }
    set
    {
      if((value & (ConstraintPropagation.MAC|ConstraintPropagation.AC3)) == ConstraintPropagation.MAC)
      {
        throw new ArgumentException("If MAC is specified, an arc consistency algorithm "+
                                    "like AC3 must also be specified.");
      }

      cpMethod = value;
    }
  }

  /// <summary>Gets or sets the heuristics that will be used during the search.</summary>
  /// <remarks>The default value is to have all heuristics enabled except
  /// <see cref="ConstraintSatisfaction.Heuristics.LeastConstrainingValue"/>.
  /// </remarks>
  public Heuristics Heuristics
  {
    get { return heuristics; }
    set { heuristics = value; }
  }

  /// <summary>Gets or sets the optimizations that will be applied to simplify the problem.</summary>
  /// <remarks>The default is to have all optimizations enabled.</remarks>
  public Optimizations Optimizations
  {
    get { return optimizations; }
    set { optimizations = value; }
  }

  /// <include file="documentation.xml" path="/AI/Search/ISearch/Search/*"/>
  public override SearchResult Search(SearchLimiter limiter, out Assignment solution)
  {
    return Search(null, limiter, out solution);
  }

  /// <summary>Performs a search starting from the given initial assignment.</summary>
  /// <param name="initialAssignment">An initial assignment to the variables of the CSP. Not all variables need to be
  /// assigned. The search will attempt to find a solution that uses the assignments given. This may cause the search
  /// to fail to find a solution to the problem even if one exists, if the initial assignments are not valid. If null,
  /// an empty assignment will be used.
  /// </param>
  /// <include file="documentation.xml" path="/AI/Search/ISearch/SearchCommon/*"/>
  public override SearchResult Search(Assignment initialAssignment, SearchLimiter limiter, out Assignment solution)
  {
    if(!BeginSearch(initialAssignment, limiter, out solution)) return SearchResult.Failed;

    SearchResult result;
    int internalResult = Search();
    if(internalResult == SuccessfulAssignment)
    {
      solution = assignment;
      result = SearchResult.Success;
    }
    else if(internalResult == LimitReached) result = SearchResult.LimitReached;
    else result = SearchResult.Failed;

    ResetSearch();
    return result;
  }

  #region CurrentDomain
  /// <summary>Represents the current domain of a finite variable, with the ability to remove and restore values.</summary>
  sealed class CurrentDomain : FiniteDomain<VarType>
  {
    /// <summary>Initializes this <see cref="FiniteDomain{T}"/> object with base domain of the variable.</summary>
    public CurrentDomain(FiniteDomain<VarType> baseDomain) : base(baseDomain.Count)
    {
      this.baseDomain = baseDomain;
      this.currentValues = new Set(baseDomain.Count);
      this.currentCount  = baseDomain.Count;

      currentValues.Fill(); // at the start, every value exists
    }

    /// <summary>Gets the value from the domain's original values at the given index.</summary>
    public override VarType this[int index]
    {
      get { return baseDomain[index]; }
    }

    /// <summary>Gets the number of currently-existing values in the domain.</summary>
    public int CurrentCount
    {
      get { return currentCount; }
    }

    /// <summary>Returns true if the domain currently contains the value at the given index.</summary>
    public bool Contains(int valueIndex)
    {
      return currentValues.Contains(valueIndex);
    }

    /// <summary>Enumerates all values that currently exist in the domain.</summary>
    public IEnumerable<int> EnumerateValues()
    {
      return indices == null ? currentValues.EnumerateItems() : indices;
    }

    /// <summary>Gets an array containing the indices of the values that currently exist in the domain. This array is
    /// internal to the class, and modifying it will modify it for everyone.
    /// </summary>
    /// <remarks>The array is recalculated if a value is removed from or added to the domain.</remarks>
    public int[] GetIndices()
    {
      if(indices == null)
      {
        indices = new int[CurrentCount];
        if(CurrentCount == Count) // if every value exists, we can quickly fill the array of indices
        {
          for(int i=0; i<indices.Length; i++) indices[i] = i;
        }
        else // otherwise, we need to enumerate the items in the set
        {
          int index = 0;
          foreach(int value in currentValues.EnumerateItems()) indices[index++] = value;
        }
      }
      return indices;
    }

    /// <summary>Removes the value at the given index from the domain.</summary>
    /// <remarks>The value is not really removed, just marked as removed, and it can be added back with
    /// <see cref="Restore"/>. In particular, removing a value does not shift the indices of other values.
    /// </remarks>
    public void Remove(int index)
    {
      if(Contains(index))
      {
        currentValues.Remove(index);
        currentCount--;
        indices = null; // the indices array is invalidated when the set changes
      }
    }

    /// <summary>Restores a value that was removed at the given index.</summary>
    public void Restore(int index)
    {
      if(!Contains(index))
      {
        currentValues.Add(index);
        currentCount++;
        indices = null; // the indices array is invalidated when the set changes
      }
    }

    /// <summary>Converts the domain to a human-readable string containing the list of values.</summary>
    public override string ToString()
    {
      // return a string with all of the current values, separated by spaces
      System.Text.StringBuilder sb = new System.Text.StringBuilder();
      for(int i=0; i<Count; i++)
      {
        if(currentValues.Contains(i))
        {
          if(sb.Length != 0) sb.Append(' ');
          sb.Append(Convert.ToString(this[i]));
        }
      }
      return sb.ToString();
    }

    readonly FiniteDomain<VarType> baseDomain;
    Set currentValues;
    int currentCount;
    /// <summary>A cached copy of the indices of the current values in this domain.</summary>
    int[] indices;
  }
  #endregion

  #region Set
  /// <summary>Represents a set that can contain a fixed number of items.</summary>
  /// <remarks>This class uses a formulation whereby each member of the set is represented by a single bit in an array.
  /// The maximum number of elements in the set is fixed in advance.
  /// </remarks>
  struct Set
  {
    /// <summary>Initializes a new <see cref="Set"/> with capacity for the given number of items.</summary>
    public Set(int maxItems)
    {
      this.array    = new uint[(maxItems+31)/32]; // divide 'maxItems' by 32, rounding up
      this.maxItems = maxItems;
    }

    /// <summary>Adds the given item to the set.</summary>
    public void Add(int item)
    {
      array[item>>5] |= (uint)(1 << (item&31));
    }

    /// <summary>Removes all items from the set.</summary>
    public void Clear()
    {
      for(int i=0; i<array.Length; i++) array[i] = 0;
    }

    /// <summary>Returns true if the set contains the given item, and false if not.</summary>
    public bool Contains(int item)
    {
      return (array[item>>5] & (uint)(1 << (item&31))) != 0;
    }

    /// <summary>Enumerates all items in the set.</summary>
    public IEnumerable<int> EnumerateItems()
    {
      for(int i=0,baseValue=0; i<array.Length; baseValue+=32,i++)
      {
        uint mask = array[i];
        for(int j=baseValue; mask != 0; mask>>=1, j++) // when the mask becomes zero, we can jump to the next element
        {
          if((mask & 1) != 0) yield return j;
        }
      }
    }

    /// <summary>Adds all items to the set.</summary>
    public void Fill()
    {
      for(int i=0; i<array.Length; i++) array[i] = 0xffffffff;

      // if the number of items is not a multiple of 32, the last element should not have all 32 bits set, so fix it
      int leftOver = maxItems&31; // the number of items in the last element, mod 32
      if(leftOver != 0) // if leftOver is non-zero, the number of items is not a multiple of 32
      {
        uint mask = 1;
        while(--leftOver != 0) mask = (mask<<1) | 1; // add only as many 1 bits as 'leftOver' allows
        array[array.Length-1] = mask;
      }
    }

    /// <summary>Removes the given item from the set.</summary>
    public void Remove(int item)
    {
      array[item>>5] &= ~(uint)(1 << (item&31));
    }

    /// <summary>Adds all items from the given set to this set. This set must be at least as large as the other set.</summary>
    public void Union(Set other)
    {
      for(int i=0; i<array.Length; i++) array[i] |= other.array[i];
    }

    readonly uint[] array;
    readonly int maxItems;
  }
  #endregion

  #region AC3
  /// <summary>Runs the AC-3 arc consistency algorithm on the entire problem.</summary>
  void AC3()
  {
    Stack<IntPair> queue = new Stack<IntPair>(256);

    // add every pair of variables connected by a constraint to the queue.
    for(int i=0; i<variables.Length; i++)
    {
      foreach(int j in neighbors[i])
      {
        queue.Push(new IntPair(i, j));
      }
    }

    AC3(null, queue, -1);
  }

  /// <summary>Runs the AC3-MAC algorithm to update the current domains after a variable assignment.</summary>
  /// <returns>Returns true if all domains still seem consistent, and false if a consistency has been detected.</returns>
  bool AC3_MAC(Assignment assignment, int variable)
  {
    Stack<IntPair> queue = new Stack<IntPair>(128);
    
    foreach(int neighbor in neighbors[variable]) // add the unassigned neighbors of the altered variable to the queue.
    {                                            // the assigned neighbors are already known to be consistent
      if(!assignment.IsAssigned(neighbor)) queue.Push(new IntPair(neighbor, variable));
    }
    return AC3(assignment, queue, variable);
  }

  bool AC3(Assignment assignment, Stack<IntPair> queue, int relatedVariable)
  {
    while(queue.Count != 0) // while there are variable pairs left to examine...
    {
      IntPair pair = queue.Pop();
      if(AC3_RemoveInconsistentValues(pair.Value1, pair.Value2, relatedVariable)) // if any values were removed from pair.Value1's domain...
      {
        // if the domain was reduced to nothing, we immediately know that the current assignment is inconsistent, so
        // return a value indicating that
        if(currentDomains[pair.Value1].CurrentCount == 0) return false;
        
        // add any unassigned variables connected to var1 by a constraint back on the queue, because the value
        // supporting them may have been the one that was removed
        foreach(int i in neighbors[pair.Value1])
        {
          if(assignment == null || !assignment.IsAssigned(i)) queue.Push(new IntPair(i, pair.Value1));
        }

        // since var2 conflicts with var1, we may need to revise var2 if var1 fails. so add var2 to var1's conflict set
        if(conflictSets != null) conflictSets[pair.Value1].Add(pair.Value2);
      }
    }

    return true;
  }

  /// <summary>Checks whether there are any values in the current domain of <paramref name="var1"/> that conflict with
  /// every value in the current domain of <paramref name="var2"/>, and if such values exist, removes them.
  /// </summary>
  /// <returns>True if any values were removed from the current domain of <paramref name="var1"/>.</returns>
  bool AC3_RemoveInconsistentValues(int var1, int var2, int relatedVariable)
  {
    bool removed = false;

    for(int i=0; i<variables[var1].Domain.Count; i++) // for all values in var1's current domain
    {
      if(currentDomains[var1].Contains(i))
      {
        for(int j=0; j<variables[var2].Domain.Count; j++) // see if the var1's value has support from var2's values
        {
          if(currentDomains[var2].Contains(j) && !problem.Conflicts(var1, i, var2, j)) goto nextValue;
        }

        // if var1's value conflicted with every one of var2's values, it is inconsistent, so remove it from var1's
        // domain, and if there is a variable that prompted this change, add it to the pruned list for that variable,
        // so that the current domain can be restored later
        currentDomains[var1].Remove(i);
        if(relatedVariable != -1) pruned[relatedVariable].Add(new IntPair(var1, i));
        removed = true;
      }
      nextValue:;
    }

    return removed;
  }
  #endregion

  /// <summary>Prepares to begin a new search.</summary>
  bool BeginSearch(Assignment initialAssignment, SearchLimiter limiter, out Assignment solution)
  {
    CSPHelper.InitializeSearch(problem, initialAssignment, out variables, out neighbors);

    this.limiter = limiter;

    solution   = null;
    assignment = initialAssignment == null ? new Assignment(variables.Length) : initialAssignment;

    // TODO: implement cutset conditioning and tree decomposition

    if(cpMethod != ConstraintPropagation.None || (heuristics & Heuristics.MostRestrictedVariable) != 0)
    {
      // if we're using constraint propagation, initialize the current domains and pruned lists
      currentDomains = new CurrentDomain[variables.Length];
      for(int i=0; i<variables.Length; i++) currentDomains[i] = new CurrentDomain(variables[i].Domain);

      pruned = new List<IntPair>[variables.Length];
      for(int i=0; i<pruned.Length; i++) pruned[i] = new List<IntPair>();

      // if an initial assignment was given, preprocess the current domains of the assigned variables to remove all
      // of the values except those assigned, effectively locking in the initial assignment
      if(initialAssignment != null)
      {
        for(int i=0; i<assignment.TotalCount; i++)
        {
          if(assignment.IsAssigned(i))
          {
            for(int j=0; j<currentDomains[i].Count; j++)
            {
              if(j != assignment[i]) currentDomains[i].Remove(j);
            }
          }
        }
      }

      // the search assumes that if constraint propagation is enabled, then the graph is consistent, so we need to run
      // a consistency algorithm on the initial graph to make sure that's true from the start.
      AC3();
    }

    if((heuristics & Heuristics.Backjumping) != 0) // if we're using backjumping, initialize the conflict sets
    {
      conflictSets = new Set[variables.Length];
      for(int i=0; i<conflictSets.Length; i++) conflictSets[i] = new Set(variables.Length);
    }

    // if an initial assignment was given, we need to ensure that it's consistent
    return initialAssignment == null || IsConsistent(assignment);
  }

  /// <summary>Returns true if assigned the given value to the given variable would conflict with the constraints of
  /// the problem.
  /// </summary>
  /// <param name="variable">The variable to which the value will be assigned.</param>
  /// <param name="value">A value from the variable's current domain.</param>
  bool ConflictingAssignment(int variable, int value)
  {
    // if constraint propogation is being used (ie, currentDomains != null), the assignment is guaranteed to be safe.
    // so we only need to do constraint checks if currentDomains == null
    if(currentDomains == null)
    {
      foreach(int neighbor in neighbors[variable])
      {
        if(assignment.IsAssigned(neighbor) && problem.Conflicts(variable, value, neighbor, assignment[neighbor]))
        {
          // since 'neighbor' is a past variable that conflicts with the current variable, we may need to revise the
          // neighbor's assignment in order to find a valid value for the current variable, so add the neighbor to
          // the current variable's conflict set.
          if(conflictSets != null) conflictSets[variable].Add(neighbor);
          return true;
        }
      }
    }

    return false;
  }

  /// <summary>Given a variable and a value from the variable's current domain, returns the total number of conflicts
  /// this value has with the values of the unassigned variables.
  /// </summary>
  int CountConflictingValues(int variable, int value)
  {
    int total = 0;
    foreach(int neighbor in neighbors[variable])
    {
      if(!assignment.IsAssigned(neighbor))
      {
        foreach(int neighborValue in EnumerateCurrentValues(neighbor))
        {
          if(problem.Conflicts(variable, value, neighbor, neighborValue)) total++;
        }
      }
    }
    return total;
  }

  /// <summary>Enumerates the set of values in the current domain of the given variable.</summary>
  IEnumerable<int> EnumerateCurrentValues(int variable)
  {
    // if currentDomains == null, the values are just 0,1,2,...domain.Count-1, ie every valid index
    return currentDomains == null ?
      EnumerateRange(variables[variable].Domain.Count) : currentDomains[variable].EnumerateValues();
  }

  /// <summary>Propagates constraints using the Forward Checking algorithm after the given variable was assigned a new
  /// value.
  /// </summary>
  /// <returns>Returns true if the current domains still seem consistent, and false if an inconsistency has been
  /// detected.
  /// </returns>
  bool ForwardCheck(int variable)
  {
    int value = assignment[variable]; // the new value assigned to the variable

    // remove values from unassigned neighbors' current domains that are inconsistent with the new assignment
    foreach(int neighborVar in neighbors[variable])
    {
      if(!assignment.IsAssigned(neighborVar))
      {
        CurrentDomain neighborDomain = currentDomains[neighborVar];
        bool removed = false;
        for(int neighborValue=0,count=variables[neighborVar].Domain.Count; neighborValue<count; neighborValue++)
        {
          if(neighborDomain.Contains(neighborValue) && problem.Conflicts(variable, value, neighborVar, neighborValue))
          {
            currentDomains[neighborVar].Remove(neighborValue);

            // also add the value to the pruned list for the variable, so the neighbor's current domain can be restored
            // later.
            pruned[variable].Add(new IntPair(neighborVar, neighborValue));

            // if an inconsistency was detected, we can return immediately
            if(currentDomains[neighborVar].CurrentCount == 0) return false;

            removed = true;
          }

          // if the current variable caused the removal of values from the neighbor's domain, the current variable may
          // be responsible for a future failure of the neighbor's assignment. so add the current variable to the
          // neighbor's conflict set
          if(removed && conflictSets != null) conflictSets[neighborVar].Add(variable);
        }
      }
    }

    return true;
  }

  /// <summary>Gets the values to consider assigning to the given variable, as an enumerator that returns the most
  /// promising values first.
  /// </summary>
  IEnumerable<int> GetOrderedValues(int variable)
  {
    // if we're using the least constrained value heuristic, choose the value that creates the fewest conflicts.
    // (but don't bother if there's only one value in the domain)
    if((heuristics & Heuristics.LeastConstrainingValue) != 0 &&
       (currentDomains == null ? variables[variable].Domain.Count : currentDomains[variable].CurrentCount) > 1)
    {
      // since we need to actually sort the values, we'll need a concrete place to store them.
      int[] valueIndices; // so get an array to holds them
      if(currentDomains == null) // if there's no constraint propagation, the list of values is simply every value
      {
        valueIndices = new int[variables[variable].Domain.Count];
        for(int i=0; i<valueIndices.Length; i++) valueIndices[i] = i;
      }
      else valueIndices = currentDomains[variable].GetIndices(); // otherwise, it's the values in the current domain

      int[] nconflicts = new int[valueIndices.Length]; // also create an array to count the conflicts that occur
      bool shouldSort  = false; // determines whether we will sort. we won't bother if all the values are the same
      for(int i=0; i<nconflicts.Length; i++)
      {
        nconflicts[i] = CountConflictingValues(variable, valueIndices[i]);
        // if a value is different from another, we'll sort
        if(!shouldSort && i != 0 && nconflicts[i] != nconflicts[i-1]) shouldSort = true;
      }
      // HACK: if currentDomains != null, we're sorting the current domain's internal indices array, but it doesn't
      // hurt anything because only one variable assignment is considered at a time, so the array won't be altered
      // until we're done with it
      if(shouldSort) Array.Sort(nconflicts, valueIndices);
      return valueIndices;
    }
    else // otherwise, we're not using any heuristic, so just return the values as they are
    {
      return EnumerateCurrentValues(variable);
    }
  }

  /// <summary>Gets the most promising unassigned variable to which the search will consider assigning a value.</summary>
  int GetUnassignedVariable()
  {
    // find the first unassigned variable
    int bestVar;
    for(bestVar=0; bestVar<variables.Length; bestVar++) if(!assignment.IsAssigned(bestVar)) break;

    // if we're using a variable selection heuristic, see if there are better unassigned variables
    if((heuristics & (Heuristics.Degree|Heuristics.MostRestrictedVariable)) != 0)
    {
      bool degree = (heuristics & Heuristics.Degree) != 0, mrv = (heuristics & Heuristics.MostRestrictedVariable) != 0;
      for(int i=bestVar+1; i<variables.Length; i++)
      {
        if(!assignment.IsAssigned(i))
        {
          if(mrv) // if we're using the MRV heuristic, we'll prefers variables with fewer legal values. if the degree
          {       // heuristic is enabled, we'll use it as a tie-breaker
            int legalValues = currentDomains[i].CurrentCount, leastValues = currentDomains[bestVar].CurrentCount;
            if(legalValues < leastValues ||
               degree && legalValues == leastValues && neighbors[i].Count > neighbors[bestVar].Count)
            {
              bestVar = i;
            }
          }
          // otherwise we'll just use the degree heuristic, which prefers variables involved in more constraints.
          // note that 'degree' must be true at this point. otherwise we wouldn't be here.
          else if(neighbors[i].Count > neighbors[bestVar].Count)
          {
            bestVar = i;
          }
        }
      }
    }

    return bestVar;
  }

  /// <summary>Given an assignment in the context of a search, checks whether the assignment is consistent.</summary>
  /// <remarks>This method is used only to check whether an initial assignment given by the user violates the
  /// assumptions made during the search or not.
  /// </remarks>
  bool IsConsistent(Assignment assignment)
  {
    if(currentDomains != null) // if a consistency check was run, then this should be easy
    {
      for(int i=0; i<assignment.TotalCount; i++) // the assigment is invalid if any domains have become empty
      {
        if(assignment.IsAssigned(i) && currentDomains[i].CurrentCount == 0) return false;
      }
    }
    else // otherwise, we need to check all constraints between assigned values
    {
      for(int i=0; i<variables.Length; i++)
      {
        if(assignment.IsAssigned(i))
        {
          foreach(int j in neighbors[i])
          {
            if(j > i && assignment.IsAssigned(j) && problem.Conflicts(i, assignment[i], j, assignment[j]))
            {
              return false;
            }
          }
        }
      }
    }

    return true;
  }

  /// <summary>Propagates the effects of having assigned a value to the given variable onto the current domains of
  /// other variables.
  /// </summary>
  /// <returns>Returns true if the current domains still seem consistent, and false if an inconsistency has been
  /// detected.
  /// </returns>
  bool PropagateConstraints(int variable)
  {
    bool seemsOkay = true; // whether no inconsistencies have been detected so far

    if(currentDomains != null) // if constraint propagation is enabled
    {
      if((cpMethod & ConstraintPropagation.MAC) != 0) // if we're using MAC
      {
        // reduce the domain of the variable to the single value we assigned. although if the variable's domain had
        // only one value to begin with, then nothing changes, so we don't need to rerun MAC
        CurrentDomain currentDomain = currentDomains[variable];
        if(currentDomains[variable].CurrentCount > 1)
        {
          int value = assignment[variable];
          foreach(int currentValue in currentDomain.GetIndices())
          {
            if(currentValue != value) // don't remove the value we assigned, of course
            {
              currentDomain.Remove(currentValue);
              // add the removed values to the pruned list, so we can restore them later
              pruned[variable].Add(new IntPair(variable, currentValue));
            }
          }

          // TODO: implement AC6-MAC (it's much more complicated than AC3-MAC...)
          seemsOkay = AC3_MAC(assignment, variable);
        }
      }
      else // otherwise, we must be using forward checking (this includes the case where cpMethod==None, but heuristics
      {    // contains MostRestrictedVariable, because that heuristic needs constraint propagation to work efficiently
        seemsOkay = ForwardCheck(variable);
      }
    }

    return seemsOkay;
  }

  void ResetSearch()
  {
    assignment     = null;
    currentDomains = null;
    pruned         = null;
    conflictSets   = null;
    limiter        = null;
  }

  int Search()
  {
    if(assignment.IsComplete) return SuccessfulAssignment;
    if(limiter != null && limiter.LimitReached) return LimitReached;

    int variable = GetUnassignedVariable(); // choose the variable to assign this time around
    foreach(int valueIndex in GetOrderedValues(variable)) // for each potential value for the variable
    {
      // if the value does not obviously conflict, try assigning it
      if(!ConflictingAssignment(variable, valueIndex))
      {
        int backjumpFrom = SuccessfulAssignment;
        assignment[variable] = valueIndex;

        // then propagate constraints, and if everything still seems okay, recurse and try to set the next variable
        if(PropagateConstraints(variable))
        {
          backjumpFrom = Search();
          // at this point, the search has unwound. either a complete solution was found, or we need to backtrack.
          if(backjumpFrom == SuccessfulAssignment) return SuccessfulAssignment; // if a solution was found, return success
        }

        // either the constraint propagation says we shouldn't assign that value, or an assignment deeper in the tree
        // failed. in any case, the value we have assigned now is no good, so unassign it.
        Unassign(variable);

        // if we're backjumping, we want to unwind back to a member of the conflict set of the variable from
        // which we're jumping back
        if(backjumpFrom != SuccessfulAssignment && conflictSets != null)
        {
          // if 'variable' is not in the conflict set, then we want to keep unwinding
          if(!conflictSets[backjumpFrom].Contains(variable))
          {
            conflictSets[variable].Clear(); // we'll clear our conflict set first
            return backjumpFrom; // keep unwinding
          }
          else // otherwise, we've unwound enough, absorb the conflict set of the other variable into our own
          {
            conflictSets[variable].Union(conflictSets[backjumpFrom]);
            conflictSets[variable].Remove(variable); // but of course we don't conflict with ourselves
          }
        }
      }
    }

    // all of the values for 'variable' failed, so return 'variable' so previous calls on the stack will know to stop
    // unwinding when they are in the conflict set for 'variable' (if backjumping is enabled)
    return variable;
  }

  /// <summary>Performs the unassignment of the given variable and restores the current domains of other variables to
  /// what they were before the variable was assigned.
  /// </summary>
  void Unassign(int variable)
  {
    // restore values that were pruned during the assignment of this variable
    if(currentDomains != null)
    {
      foreach(IntPair pair in pruned[variable])
      {
        currentDomains[pair.Value1].Restore(pair.Value2);
      }
      pruned[variable].Clear();
    }

    assignment.Unassign(variable);
  }

  readonly IFiniteDomainCSP<VarType> problem;
  SimpleVariable<VarType>[] variables;
  INeighborList neighbors;

  /// <summary>The current assignment. This is updated as the search progresses.</summary>
  Assignment assignment;
  /// <summary>An array containing the possible values that can be assigned to each variable without conflicting with
  /// any previous assignments. If null, constraint propagation is disabled and so the variables' original domains
  /// should be used to retrieve the set of possible values.
  /// </summary>
  CurrentDomain[] currentDomains;
  /// <summary>An array containing the values removed from <see cref="currentDomains"/> when each variable was
  /// assigned. This is used to undo changes to <see cref="currentDomains"/> when a variable is unassigned. If null,
  /// constraint propagation is disabled.
  /// </summary>
  List<IntPair>[] pruned;
  /// <summary>An array containing the sets of variables that may have contributed to a conflict for each variable.
  /// This is used by the backjumping heuristic to find the best point to backjump to when a dead end is reached in the
  /// search. If null, backjumping is disabled.
  /// </summary>
  Set[] conflictSets;
  
  SearchLimiter limiter;

  /// <summary>The constraint propagation method to be used.</summary>
  ConstraintPropagation cpMethod = ConstraintPropagation.AC3 | ConstraintPropagation.MAC;
  /// <summary>The set of heuristics to be used.</summary>
  Heuristics heuristics = Heuristics.All & ~Heuristics.LeastConstrainingValue;
  /// <summary>The set of optimizations to be used.</summary>
  Optimizations optimizations = Optimizations.All;

  /// <summary>Returns an enumerator that returns the integers from 0 to <c>count-1</c>.</summary>
  static IEnumerable<int> EnumerateRange(int count)
  {
    int index = 0;
    while(index < count) yield return index++;
  }
}
#endregion

#region LocalSearchSolver
/// <summary>This class implements a local solution search for finite domain constraint satisfaction problems.</summary>
/// <include file="documentation.xml" path="/AI/CSP/typeparam[@name='VarType']"/>
/// <remarks>The local solution search performs creates a complete assignment and alters the assignment randomly until
/// a solution is found. Because of the randomness, the search is not guaranteed to find a solution in any finite
/// amount of time, but it usually performs very well.
/// </remarks>
public class LocalSearchSolver<VarType> : IterativeSearchBase<Assignment,Assignment>
{
  /// <summary>Initializes the local search solver with the given problem instance, with 100 turns of no progress
  /// allowed before a restart occurs.
  /// </summary>
  public LocalSearchSolver(IFiniteDomainCSP<VarType> problem) : this(problem, 100) { }

  /// <summary>Initializes the local search solver.</summary>
  /// <param name="problem">The problem to solve.</param>
  /// <param name="maxIterationsWithoutProgress">The initial value of <see cref="MaxIterationsWithoutProgress"/>.</param>
  public LocalSearchSolver(IFiniteDomainCSP<VarType> problem, int maxIterationsWithoutProgress)
  {
    if(problem == null) throw new ArgumentNullException();
    this.problem = problem;
    MaxIterationsWithoutProgress = maxIterationsWithoutProgress;
  }

  /// <summary>Gets or sets whether the search will cache some conflicts in memory.</summary>
  /// <remarks>If set to true, the search will be able to avoid some redundant consistency checks by caching the
  /// results. This reduces consistency checks by about 15% on average while increasing memory usage. The memory used
  /// should be insignificant, but for certain worst-case problems, it could be substantial. The default is true.
  /// </remarks>
  public bool CacheConflicts
  {
    get { return cacheConflicts; }
    set
    {
      DisallowChangeDuringSearch();
      cacheConflicts = value; 
    }
  }

  /// <summary>Gets or sets the maximum number of iterations without progress allowed before the search is restarted
  /// with a new, random assignment. If equal to <see cref="SearchBase.Infinite"/>, the search will never be restarted
  /// due to lack of progress.
  /// </summary>
  public int MaxIterationsWithoutProgress
  {
    get { return maxIterationsWithoutProgress; }
    set
    {
      if(value < Infinite) throw new ArgumentOutOfRangeException();
      maxIterationsWithoutProgress = value;
    }
  }

  /// <include file="documentation.xml" path="/AI/Search/IIterativeSearch/SearchInProgress/*"/>
  public override bool SearchInProgress
  {
    get { return assignment != null; }
  }

  /// <include file="documentation.xml" path="/AI/Search/IIterativeSearch/BeginSearch/*"/>
  public override Assignment BeginSearch()
  {
    return BeginSearch(null);
  }

  /// <summary>Begins a search starting from the given initial assignment.</summary>
  /// <param name="initialAssignment">An initial assignment to the variables of the CSP. Not all variables need to be
  /// assigned. If null, an empty assignment will be used, equivalent to calling <see cref="BeginSearch()"/>.
  /// The search will use the assignment given as a starting point, but may change some of the values to create a
  /// solution.
  /// </param>
  /// <include file="documentation.xml" path="/AI/Search/IIterativeSearch/BeginSearchCommon/*"/>
  public override Assignment BeginSearch(Assignment initialAssignment)
  {
    AssertSearchStartable();
    CSPHelper.InitializeSearch(problem, initialAssignment, out variables, out neighbors);

    assignment   = initialAssignment == null ? new Assignment(variables.Length) : initialAssignment;
    random       = CreateRandom();
    valueIndices = new List<int>();
    conflicts    = cacheConflicts ? new List<List<int>>() : null;
    iterationsWithoutProgress = 0;

    // add one list to the list of conflict lists to simplify the logic in Iterate()
    if(conflicts != null) conflicts.Add(new List<int>());

    // create a complete assignment, even if it may be in conflict with the constraints
    for(int i=0; i<variables.Length; i++)
    {
      if(!assignment.IsAssigned(i)) assignment[i] = random.Next(variables[i].Domain.Count);
    }

    // calculate which variables are conflicted in our current assignment
    conflictedVarIndices = new int[variables.Length];
    CalculateConflictedVariables();

    return assignment;
  }

  /// <include file="documentation.xml" path="/AI/Search/IIterativeSearch/Iterate/*"/>
  public override SearchResult Iterate(ref Assignment solution)
  {
    AssertSearchInProgress();

    if(numConflictedVars != 0) // if we don't have a valid solution yet
    {
      // choose a random conflicted variable
      int cvIndex = random.Next(numConflictedVars), variable = conflictedVarIndices[cvIndex];

      // select the next value to assign. it should be the one with the fewest conflicts, and should not be the same
      // value as the one already assigned. we'll keep track of which neighbors the values conflict with as well
      List<int> conflictedNeighbors;
      int numberOfConflicts;
      int value = SelectValueToAssign(variable, out numberOfConflicts, out conflictedNeighbors);

      // keep track of the number of previously conflicted variables so we can tell if we're making progress
      int previousConflictedVarCount = numConflictedVars;

      // if the value doesn't conflict with any neighbors, remove 'variable' from the list of conflicted variables
      if(numberOfConflicts == 0) RemoveConflictedVariableAt(cvIndex);

      // assign the new value to the variable
      AssignValue(variable, value, conflictedNeighbors);

      if(previousConflictedVarCount <= numConflictedVars)
      {
        // the number of conflicted variables wasn't decreased this iteration, so no progress was mode. if we've hit
        // the maximum number of allowed iterations without progress, restart the search
        if(maxIterationsWithoutProgress != Infinite && ++iterationsWithoutProgress > maxIterationsWithoutProgress)
        {
          for(int i=0; i<variables.Length; i++) assignment[i] = random.Next(variables[i].Domain.Count);
          CalculateConflictedVariables();
          iterationsWithoutProgress = 0;
        }
      }
      else iterationsWithoutProgress = 0; // otherwise, we had progress
    }

    solution = assignment; // always return the most current result

    // if the number of conflicted variables drops to zero, we have a solution that is consistent, with the possible
    // exception of conflicts between variables with domains of a single value, which we didn't track during the
    // search because we can't change them anyway.
    if(numConflictedVars == 0)
    {
      return IsSolutionConsistent(assignment) ? SearchResult.Success : SearchResult.Failed;
    }
    else return SearchResult.Pending;
  }

  /// <summary>Returns a new random number generator. This method can be overridden to return a random number generator
  /// with a fixed seed, allowing searches to be replayed exactly.
  /// </summary>
  protected virtual Random CreateRandom()
  {
    return new Random();
  }

  /// <include file="documentation.xml" path="/AI/Search/IterativeSearchBase/ResetSearch/*"/>
  protected override void ResetSearch()
  {
    assignment           = null;
    random               = null;
    conflictedVarIndices = null;
    valueIndices         = null;
    conflicts            = null;
  }

  /// <summary>Assigns the given value to the given variable and updates the list of conflicted variables.</summary>
  void AssignValue(int variable, int value, List<int> conflictedNeighbors)
  {
    assignment[variable] = value;

    // the variable has changed, so we need to update 'conflictedVarIndices' to add new neighbors that are now
    // conflicted, and remove old neighbors that are no longer conflicted
    int index = 0;
    foreach(int j in neighbors[variable])
    {
      bool jConflictsWithVariable;
      if(conflictedNeighbors == null)
      {
        jConflictsWithVariable = problem.Conflicts(variable, assignment[variable], j, assignment[j]);
      }
      else
      {
        jConflictsWithVariable = index < conflictedNeighbors.Count && j == conflictedNeighbors[index];
      }

      if(jConflictsWithVariable) // the neighbor j is in conflict with 'variable'
      {
        index++;
        AddConflictedVariable(j); // so add it to the list of conflicted variables (if it's not already there)
      }
      else if(IsConflictedVariable(j)) // j doesn't conflict with the current variable. if it was known to be
      {                                // conflicted, see if it still is.
        bool stillConflicted = false;
        foreach(int k in neighbors[j])
        {
          if(k != variable && problem.Conflicts(j, assignment[j], k, assignment[k]))
          {
            stillConflicted = true;
            break;
          }
        }
        if(!stillConflicted) RemoveConflictedVariable(j);
      }
    }
  }

  /// <summary>Performs the initial calculation of which variables are conflicted after a new random assignment is
  /// created.
  /// </summary>
  void CalculateConflictedVariables()
  {
    // calculate the indices of the conflicted variables that we need to correct
    numConflictedVars = 0;

    // initialize the array of conflicted variables
    for(int i=0; i<variables.Length; i++)
    {
      if(variables[i].Domain.Count > 1) // only variables with more than one possible value should be considered for
      {                                 // revision
        foreach(int j in neighbors[i])
        {
          if(problem.Conflicts(i, assignment[i], j, assignment[j]))
          {
            conflictedVarIndices[numConflictedVars++] = i;
            break;
          }
        }
      }
    }
  }

  /// <summary>Given variable and value indices, returns the total number of other variables the value conflicts with,
  /// and store the indices of the variables in the given array.
  /// </summary>
  int CountConflictingVariables(int variable, int value, List<int> conflicts)
  {
    if(conflicts != null) conflicts.Clear();

    int total = 0;
    foreach(int neighbor in neighbors[variable])
    {
      if(problem.Conflicts(variable, value, neighbor, assignment[neighbor]))
      {
        if(conflicts != null) conflicts.Add(neighbor);
        total++;
      }
    }
    return total;
  }

  /// <summary>Given an assignment with no known conflicting variables, checks the variables that we didn't previously
  /// consider to determine whether the solution is actually consistent.
  /// </summary>
  bool IsSolutionConsistent(Assignment solution)
  {
    // we didn't track conflicts between variables with domains of a single value, because they can't be changed to
    // any other value and so are not really part of the search. we'll check them now.
    for(int i=0; i<variables.Length; i++)
    {
      if(variables[i].Domain.Count == 1)
      {
        foreach(int j in neighbors[i])
        {
          if(variables[j].Domain.Count == 1 && problem.Conflicts(i, assignment[i], j, assignment[j])) return false;
        }
      }
    }
    return true;
  }

  /// <summary>Adds the given variable to the list of conflicted variables, if has multiple potential values and
  /// hasn't already been added.
  /// </summary>
  void AddConflictedVariable(int variable)
  {
    if(variables[variable].Domain.Count > 1) // there's no point in modifying variables that have only 1 possible value
    {
      int index = Array.BinarySearch(conflictedVarIndices, 0, numConflictedVars, variable);
      if(index < 0) // if the variable is not already in the array, insert it
      {
        index = ~index;
        Array.Copy(conflictedVarIndices, index, conflictedVarIndices, index+1, numConflictedVars++ - index);
        conflictedVarIndices[index] = variable;
      }
    }
  }

  /// <summary>Determines whether the given variables in in the list of conflicted variables.</summary>
  bool IsConflictedVariable(int variable)
  {
    return Array.BinarySearch(conflictedVarIndices, 0, numConflictedVars, variable) >= 0;
  }
  
  /// <summary>Removes the given variable from the list of conflicted variables.</summary>
  void RemoveConflictedVariable(int variable)
  {
    int index = Array.BinarySearch(conflictedVarIndices, 0, numConflictedVars, variable);
    if(index >= 0) RemoveConflictedVariableAt(index);
  }

  /// <summary>Removes the variable at the given index within the list of conflicted variables.</summary>
  void RemoveConflictedVariableAt(int index)
  {
    Array.Copy(conflictedVarIndices, index+1, conflictedVarIndices, index, --numConflictedVars - index);
  }

  /// <summary>Returns the value to assign to the given variable, and gets the list of neighbors it conflicts with, if
  /// conflict caching is enabled.
  /// </summary>
  int SelectValueToAssign(int variable, out int numberOfConflicts, out List<int> conflictedNeighbors)
  {
    // we know there are at least two variables in the domain. and we don't want to assign or check the same value
    // that's already assigned

    // get the initial value of 'fewestConflicts'
    int valueIndex = assignment[variable] == 0 ? 1 : 0;
    int fewestConflicts = CountConflictingVariables(variable, valueIndex, conflicts == null ? null : conflicts[0]);
    valueIndices.Clear();
    valueIndices.Add(valueIndex);
    
    for(FiniteDomain<VarType> domain = variables[variable].Domain; valueIndex<domain.Count; valueIndex++)
    {
      if(valueIndex == assignment[variable]) continue; // there's no point in assigning the same value twice

      // add another array to hold the conflicting neighbors if we need it
      if(conflicts != null && conflicts.Count == valueIndices.Count) conflicts.Add(new List<int>());

      // find out how many neighbors this value conflicts with, and get the neighbors if we're doing that
      int numConflicts = CountConflictingVariables(variable, valueIndex,
                                                   conflicts == null ? null : conflicts[valueIndices.Count]);
      
      // we'll accumulate a list of values tied for the fewest conflicts
      if(numConflicts < fewestConflicts) // if this value has fewer conflicts than the current best...
      {
        // move the list containing the conflicting neighbors back to the beginning
        if(conflicts != null)
        {
          List<int> temp = conflicts[valueIndices.Count];
          conflicts[valueIndices.Count] = conflicts[0];
          conflicts[0] = temp;
        }

        valueIndices.Clear();
        valueIndices.Add(valueIndex);

        fewestConflicts = numConflicts;
      }
      else if(numConflicts == fewestConflicts) // otherwise, if this value is tied with the current best...
      {
        valueIndices.Add(valueIndex); // just add it to the list
      }
    }

    // now choose from the best values randomly
    valueIndex = random.Next(valueIndices.Count);

    numberOfConflicts   = fewestConflicts;
    conflictedNeighbors = conflicts == null ? null : conflicts[valueIndex]; // get the list of conflicting neighbors, too
    return valueIndices[valueIndex]; // return the actual value index (within the domain)
  }

  /// <summary>The current assignment. This is updated as the search progresses.</summary>
  Assignment assignment;
  /// <summary>The problem instance to be solved.</summary>
  IFiniteDomainCSP<VarType> problem;

  SimpleVariable<VarType>[] variables;
  INeighborList neighbors;
  
  /// <summary>The random number generator used during the search.</summary>
  Random random;
  /// <summary>A sorted array that contains the indices of variables involved in conflicts.</summary>
  int[] conflictedVarIndices;
  /// <summary>A temporary storage area for values being considered for the next variable to be assigned.</summary>
  List<int> valueIndices;
  /// <summary>Temporary storage areas for lists of variables in conflict with potential values.</summary>
  List<List<int>> conflicts;
  /// <summary>The maximum number of iterations without progress that are allowed before the search restarts.</summary>
  int maxIterationsWithoutProgress;
  /// <summary>The number of conflicted variables that need to be corrected in order to create a solution.</summary>
  int numConflictedVars;
  /// <summary>The number of iterations since any progress has been made.</summary>
  int iterationsWithoutProgress;
  /// <summary>Whether to try to reduce consistency checks at the expense of memory.</summary>
  bool cacheConflicts = true;
}
#endregion
#endregion

} // namespace AdamMil.AI.ConstraintSatisfaction