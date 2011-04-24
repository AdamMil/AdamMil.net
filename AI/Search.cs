/*
AdamMil.AI is a library providing a set of flexible artificial intelligence
classes for the .NET Framework.

http://www.adammil.net/
Copyright (C) 2008-2011 Adam Milazzo

This program is free software; you can redistribute it and/or
modify it under the terms of the GNU General Public License
as published by the Free Software Foundation; either version 2
of the License, or (at your option) any later version.
This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.
You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using AdamMil.Utilities;
using AdamMil.Collections;

namespace AdamMil.AI.Search
{

#region SearchResult
/// <summary>The result of a search operation.</summary>
public enum SearchResult
{
  /// <summary>The search is not yet complete. This value is used by iterative searches to indicate that
  /// <see cref="IIterativeSearch{S,T,C}.Iterate"/> should be called more times.
  /// </summary>
  Pending,
  /// <summary>The entire space was searched and no solution was found.</summary>
  Failed,
  /// <summary>The search completed, but some parts of the space were not searched due to time, memory, or other
  /// constraints. Whether a solution was returned depends on the type of search.
  /// </summary>
  LimitReached,
  /// <summary>The search completed and a complete solution was found.</summary>
  Success
}
#endregion

#region ISearchable
/// <summary>The base interface for problems that can be solved by search.</summary>
public interface ISearchable<StateType>
{
  /// <summary>Evaluates the state and returns true if the state is good enough to be considered a solution to the problem.</summary>
  /// <remarks>Search algorithms will explore the state space until they find a state for which this method returns true.</remarks>
  bool IsGoal(StateType state);
}
#endregion

#region ISearch
/// <summary>The base interface for search algorithms.</summary>
/// <include file="documentation.xml" path="/AI/Search/typeparam[@name='StateType' or @name='SolutionType']"/>
public interface ISearch<StateType, SolutionType>
{
  /// <include file="documentation.xml" path="/AI/Search/ISearch/Search/*"/>
  SearchResult Search(SearchLimiter limiter, out SolutionType solution);

  /// <include file="documentation.xml" path="/AI/Search/ISearch/Search_State/*"/>
  SearchResult Search(StateType initialState, SearchLimiter limiter, out SolutionType solution);
}
#endregion

#region IIterativeSearch
/// <summary>The base interface for search algorithms that can be performed piecewise.</summary>
/// <include file="documentation.xml" path="/AI/Search/typeparam[@name='StateType' or @name='SolutionType' or @name='ContextType']"/>
public interface IIterativeSearch<StateType, SolutionType, ContextType> where ContextType : IterativeSearchContext<SolutionType>
{
  /// <include file="documentation.xml" path="/AI/Search/IIterativeSearch/BeginSearch/*"/>
  ContextType BeginSearch();
  /// <include file="documentation.xml" path="/AI/Search/IIterativeSearch/BeginSearch_State/*"/>
  ContextType BeginSearch(StateType initialState);
  /// <include file="documentation.xml" path="/AI/Search/IIterativeSearch/Iterate/*"/>
  SearchResult Iterate(ContextType context);
}
#endregion

#region IterativeSearchContext
/// <summary>Represents the current best solution and the associated context information for an iterative search.</summary>
/// <include file="documentation.xml" path="/AI/Search/typeparam[@name='SolutionType']"/>
public abstract class IterativeSearchContext<SolutionType>
{
  /// <summary>Gets the current best solution.</summary>
  public abstract SolutionType Solution { get; }
}
#endregion

#region StateActionPair
/// <summary>Represents a state and the action that was taken to get to it.</summary>
/// <include file="documentation.xml" path="/AI/Search/typeparam[@name='StateType' or @name='ActionType']"/>
public struct StateActionPair<StateType, ActionType>
{
  /// <summary>Initializes a new <see cref="StateActionPair{S,A}"/> with the given state and action.</summary>
  public StateActionPair(StateType state, ActionType action)
  {
    State  = state;
    Action = action;
  }

  /// <summary>Converts the state/action pair into a human-readable string.</summary>
  public override string ToString()
  {
    return Convert.ToString(State) + "->" + Convert.ToString(Action);
  }

  /// <summary>The state in this state/action pair.</summary>
  public StateType State;
  /// <summary>The action taken from the previous state to get to <see cref="State"/>.</summary>
  public ActionType Action;
}
#endregion

#region SearchBase
/// <summary>A base class for <see cref="SearchBase{S,A}"/>, containing non-generic members.</summary>
public abstract class SearchBase
{
  /// <summary>A value to be used for various methods that take timeouts, maximums, etc., indicating that there should
  /// be no limit.
  /// </summary>
  public const int Infinite = -1;

  /// <summary>Gets or sets the maximum degree of parallelism the search will employ. A value of one specifies that the search
  /// will not be parallelized at all. Greater values specify that the search may be parallelized. A value of zero specifies that
  /// the search will try to use the maximum degree of parallelism (i.e. it will attempt to use as many concurrent
  /// hardware threads as are available). The default value is one.
  /// </summary>
  /// <remarks>See the documentation for the <see cref="Tasks"/> class for information about optimizing for parallel code.</remarks>
  /// <seealso cref="Tasks"/>
  public int MaxParallelism
  {
    get { return _maxParallelism; }
    set
    {
      if(value < 0) throw new ArgumentOutOfRangeException();
      _maxParallelism = value;
    }
  }

  /// <summary>Gets the number of hardware threads that should be used to parallelize a search, given the number of available
  /// threads and the current value of <see cref="MaxParallelism"/>.
  /// </summary>
  protected int GetEffectiveParallelism()
  {
    int availableThreads = SystemInformation.GetAvailableCpuThreads();
    return MaxParallelism == 0 ? availableThreads : Math.Min(MaxParallelism, availableThreads);
  }

  int _maxParallelism = 1;
}

/// <summary>The base class for all types of searches defined in this library.</summary>
/// <include file="documentation.xml" path="/AI/Search/typeparam[@name='StateType' or @name='SolutionType']"/>
public abstract class SearchBase<StateType, SolutionType> : SearchBase, ISearch<StateType, SolutionType>
{
  /// <include file="documentation.xml" path="/AI/Search/ISearch/Search/summary"/>
  /// <include file="documentation.xml" path="/AI/Search/ISearch/SearchCommon/*[@name != 'limiter']"/>
  public SearchResult Search(out SolutionType solution)
  {
    return Search(null, out solution);
  }

  /// <include file="documentation.xml" path="/AI/Search/ISearch/Search_State/summary"/>
  /// <include file="documentation.xml" path="/AI/Search/ISearch/Search_State/param[@name='initialState']"/>
  /// <include file="documentation.xml" path="/AI/Search/ISearch/SearchCommon/*[@name != 'limiter']"/>
  public SearchResult Search(StateType initialState, out SolutionType solution)
  {
    return Search(initialState, null, out solution);
  }

  /// <include file="documentation.xml" path="/AI/Search/ISearch/Search/summary"/>
  /// <include file="documentation.xml" path="/AI/Search/SearchBase/Search_Timeout/*"/>
  public SearchResult Search(int msTimeLimit, out SolutionType solution)
  {
    return Search(msTimeLimit == Infinite ? null : new TimeLimiter(msTimeLimit), out solution);
  }

  /// <include file="documentation.xml" path="/AI/Search/ISearch/Search_State/summary"/>
  /// <include file="documentation.xml" path="/AI/Search/ISearch/Search_State/param[@name='initialState']"/>
  /// <include file="documentation.xml" path="/AI/Search/SearchBase/Search_Timeout/*"/>
  public SearchResult Search(StateType initialState, int msTimeLimit, out SolutionType solution)
  {
    return Search(initialState, msTimeLimit == Infinite ? null : new TimeLimiter(msTimeLimit), out solution);
  }

  /// <include file="documentation.xml" path="/AI/Search/ISearch/Search/*"/>
  public abstract SearchResult Search(SearchLimiter limiter, out SolutionType solution);

  /// <include file="documentation.xml" path="/AI/Search/ISearch/Search_State/*"/>
  public abstract SearchResult Search(StateType initialState, SearchLimiter limiter, out SolutionType solution);
}
#endregion

#region IterativeSearchBase
/// <summary>The base class for seaches that can be performed piecewise.</summary>
/// <include file="documentation.xml" path="/AI/Search/typeparam[@name='StateType' or @name='SolutionType' or @name='ContextType']"/>
public abstract class IterativeSearchBase<StateType, SolutionType, ContextType>
  : SearchBase<StateType,SolutionType>, IIterativeSearch<StateType, SolutionType, ContextType>
  where ContextType : IterativeSearchContext<SolutionType>
{
  /// <include file="documentation.xml" path="/AI/Search/IIterativeSearch/BeginSearch/*"/>
  public abstract ContextType BeginSearch();

  /// <include file="documentation.xml" path="/AI/Search/IIterativeSearch/BeginSearch_State/*"/>
  public abstract ContextType BeginSearch(StateType initialState);

  /// <include file="documentation.xml" path="/AI/Search/IIterativeSearch/Iterate/*"/>
  public abstract SearchResult Iterate(ContextType context);

  /// <include file="documentation.xml" path="/AI/Search/ISearch/Search/*"/>
  public override SearchResult Search(SearchLimiter limiter, out SolutionType solution)
  {
    return FinishLimitedSearch(limiter, () => BeginSearch(), out solution);
  }

  /// <include file="documentation.xml" path="/AI/Search/ISearch/Search_State/*"/>
  public override SearchResult Search(StateType initialState, SearchLimiter limiter, out SolutionType solution)
  {
    return FinishLimitedSearch(limiter, () => BeginSearch(initialState), out solution);
  }

  /// <include file="documentation.xml" path="/AI/Search/IIterativeSearch/UseAutomaticParallelism/*"/>
  protected virtual bool UseAutomaticParallelism
  {
    get { return true; }
  }

  /// <include file="documentation.xml" path="/AI/Search/IIterativeSearch/SelectBestSolution/*"/>
  protected abstract SolutionType SelectBestSolution(ContextType[] contexts);

  /// <summary>Finishes a potentially-limited search. To be called by a <see cref="Search"/> method after
  /// <see cref="BeginSearch"/>. This method calls <see cref="Iterate"/> repeatedly until the search completes or the
  /// limit expires (if a limit was given), and then returns the result.
  /// </summary>
  SearchResult FinishLimitedSearch(SearchLimiter limiter, Func<ContextType> contextMaker, out SolutionType solution)
  {
    TaskCancellationEvent foundEvent = new TaskCancellationEvent();
    int parallelism = UseAutomaticParallelism ? GetEffectiveParallelism() : 1;
    KeyValuePair<ContextType,SearchResult>[] results = Tasks.Parallelize(task =>
    {
      ContextType context = contextMaker();
      SearchResult result = SearchResult.Pending;
      while(result == SearchResult.Pending && !task.WasCanceled && (limiter == null || limiter.LimitReached))
      {
        result = Iterate(context);
      }
      if(result == SearchResult.Success) foundEvent.Cancel(); // stop the other threads if we've found a solution
      return new KeyValuePair<ContextType,SearchResult>(
        context, result == SearchResult.Pending ? SearchResult.LimitReached : result);
    }, parallelism, foundEvent);

    // find the best result type
    SearchResult bestResult = SearchResult.Failed;
    foreach(var result in results)
    {
      if(result.Value != SearchResult.Failed)
      {
        bestResult = result.Value;
        if(bestResult == SearchResult.Success) break;
      }
    }

    // find the best solution among those having the best result type
    solution = SelectBestSolution((from r in results where r.Value == bestResult select r.Key).ToArray());
    return bestResult;
  }
}
#endregion

#region SearchLimiter
/// <summary>A base for classes that can be used to abort a search if a certain condition is met.</summary>
/// <remarks>Derived classes must ensure that the <see cref="LimitReached"/> property is safe for use by multiple threads
/// simultaneously.
/// </remarks>
public abstract class SearchLimiter
{
  /// <include file="documentation.xml" path="/AI/Search/SearchLimiter/LimitReached/*"/>
  public abstract bool LimitReached { get; }

  /// <include file="documentation.xml" path="/AI/Search/SearchLimiter/Start/*"/>
  public abstract void Start();
}
#endregion

#region TimeLimiter
/// <summary>Aborts a search if it exceeds a given time limit.</summary>
public sealed class TimeLimiter : SearchLimiter
{
  /// <summary>Initializes the <see cref="TimeLimiter"/> with the given time limit.</summary>
  /// <param name="msTimeLimit">The initial value of <see cref="TimeLimit"/>.</param>
  public TimeLimiter(int msTimeLimit)
  {
    TimeLimit = msTimeLimit;
  }

  /// <summary>Gets or sets the number of milliseconds that the search is allowed to run before it must abort. Note
  /// that the search may run slightly longer, as the limit is only checked at certain points during the search.
  /// </summary>
  public int TimeLimit
  {
    get { return msTimeLimit; }
    set
    {
      if(value < 0) throw new ArgumentOutOfRangeException("The time limit cannot be negative.");
      msTimeLimit = value;
    }
  }

  /// <include file="documentation.xml" path="/AI/Search/SearchLimiter/LimitReached/*"/>
  public override bool LimitReached
  {
    get
    {
      if(timer == null) throw new InvalidOperationException("The limiter has not been started yet.");
      return timer.ElapsedMilliseconds >= msTimeLimit;
    }
  }

  /// <include file="documentation.xml" path="/AI/Search/SearchLimiter/Start/*"/>
  public override void Start()
  {
    timer = new System.Diagnostics.Stopwatch();
    timer.Start();
  }

  System.Diagnostics.Stopwatch timer;
  int msTimeLimit;
}
#endregion

} // namespace AdamMil.AI.Search