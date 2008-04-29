using System;

namespace AdamMil.AI.Search
{

#region SearchResult
/// <summary>The result of a search operation.</summary>
public enum SearchResult
{
  /// <summary>The search is not yet complete. This value is used by iterative searches to indicate that
  /// <see cref="IIterativeSearch{S,T}.Iterate"/> should be called more times.
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
  /// <summary>Evaluates the state and returns true if the state is good enough to be considered a solution to the
  /// problem.
  /// </summary>
  /// <remarks>Search algorithms will explore the state space until they find a state for which this method returns
  /// true.
  /// </remarks>
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
/// <include file="documentation.xml" path="/AI/Search/typeparam[@name='StateType' or @name='SolutionType']"/>
public interface IIterativeSearch<StateType, SolutionType>
{
  /// <include file="documentation.xml" path="/AI/Search/IIterativeSearch/SearchInProgress/*"/>
  bool SearchInProgress { get; }
  /// <include file="documentation.xml" path="/AI/Search/IIterativeSearch/BeginSearch/*"/>
  SolutionType BeginSearch();
  /// <include file="documentation.xml" path="/AI/Search/IIterativeSearch/BeginSearch_State/*"/>
  SolutionType BeginSearch(StateType initialState);
  /// <include file="documentation.xml" path="/AI/Search/IIterativeSearch/EndSearch/*"/>
  void EndSearch();
  /// <include file="documentation.xml" path="/AI/Search/IIterativeSearch/Iterate/*"/>
  SearchResult Iterate(ref SolutionType solution);
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
/// <summary>A base class for <see cref="SearchBase{S,A}"/>, to reduce typing by moving non-generic members here.</summary>
public abstract class SearchBase
{
  /// <summary>A value to be used for various methods that take timeouts, maximums, etc., indicating that there should
  /// be no limit.
  /// </summary>
  public const int Infinite = -1;
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
/// <include file="documentation.xml" path="/AI/Search/typeparam[@name='StateType' or @name='SolutionType']"/>
public abstract class IterativeSearchBase<StateType, SolutionType>
  : SearchBase<StateType,SolutionType>, IIterativeSearch<StateType, SolutionType>
{
  /// <include file="documentation.xml" path="/AI/Search/IIterativeSearch/SearchInProgress/*"/>
  public abstract bool SearchInProgress { get; }

  /// <include file="documentation.xml" path="/AI/Search/IIterativeSearch/EndSearch/*"/>
  public virtual void EndSearch()
  {
    AssertSearchInProgress();
    ResetSearch();
  }

  /// <include file="documentation.xml" path="/AI/Search/IIterativeSearch/BeginSearch/*"/>
  public abstract SolutionType BeginSearch();

  /// <include file="documentation.xml" path="/AI/Search/IIterativeSearch/BeginSearch_State/*"/>
  public abstract SolutionType BeginSearch(StateType initialState);

  /// <include file="documentation.xml" path="/AI/Search/IIterativeSearch/Iterate/*"/>
  public abstract SearchResult Iterate(ref SolutionType solution);

  /// <include file="documentation.xml" path="/AI/Search/ISearch/Search/*"/>
  public override SearchResult Search(SearchLimiter limiter, out SolutionType solution)
  {
    StartLimitedSearch(limiter);
    solution = BeginSearch();
    return FinishLimitedSearch(ref solution, limiter);
  }

  /// <include file="documentation.xml" path="/AI/Search/ISearch/Search_State/*"/>
  public override SearchResult Search(StateType initialState, SearchLimiter limiter, out SolutionType solution)
  {
    StartLimitedSearch(limiter);
    solution = BeginSearch(initialState);
    return FinishLimitedSearch(ref solution, limiter);
  }

  /// <summary>Throws an exception if a search is not in progress.</summary>
  protected void AssertSearchInProgress()
  {
    if(!SearchInProgress) throw new InvalidOperationException("A search is not currently in progress.");
  }

  /// <summary>Throws an exception if a search cannot be started. (For instance, because one is currently in progress.)</summary>
  protected void AssertSearchStartable()
  {
    if(SearchInProgress)
    {
      throw new InvalidOperationException("A search is already in progress. Either abort it by calling EndSearch() "+
                                          "or finish it by calling Iterate().");
    }
  }

  /// <summary>Throws an exception if a search is in progress. This is intended to be called by property setters that
  /// don't allow a change during a search.
  /// </summary>
  protected void DisallowChangeDuringSearch()
  {
    if(SearchInProgress)
    {
      throw new InvalidOperationException("This property cannot be changed while a search is in progress.");
    }
  }

  /// <include file="documentation.xml" path="/AI/Search/IterativeSearchBase/ResetSearch/*"/>
  protected abstract void ResetSearch();

  /// <summary>Prepares to begin a new potentially-limited search. To be called by a <see cref="Search"/> method before
  /// <see cref="BeginSearch"/>.
  /// </summary>
  protected void StartLimitedSearch(SearchLimiter limiter)
  {
    AssertSearchStartable();
    if(limiter != null) limiter.Start();
  }

  /// <summary>Finishes a potentially-limited search. To be called by a <see cref="Search"/> method after
  /// <see cref="BeginSearch"/>. This method calls <see cref="Iterate"/> repeatedly until the search completes or the
  /// limit expires (if a limit was given), and then calls <see cref="EndSearch"/> and returns the result.
  /// </summary>
  protected SearchResult FinishLimitedSearch(ref SolutionType solution, SearchLimiter limiter)
  {
    SearchResult result = SearchResult.Pending;
    while(result == SearchResult.Pending && (limiter == null || limiter.LimitReached))
    {
      result = Iterate(ref solution);
    }
    EndSearch();
    return result == SearchResult.Pending ? SearchResult.LimitReached : result;
  }
}
#endregion

#region SearchLimiter
/// <summary>A base for classes that can be used to abort a search if a certain condition is met.</summary>
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