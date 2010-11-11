using System;
using System.Collections.Generic;
using AC=AdamMil.Collections;

namespace AdamMil.AI.Search.Adversarial
{

#region Problem definitions
#region Move
/// <summary>Represents a move in a turn-based game, representing the successor state reached, the action taken to get
/// to there, and the probability of reaching it (if the move is based on chance).
/// </summary>
/// <include file="documentation.xml" path="/AI/Search/typeparam[@name='StateType' or @name='ActionType']"/>
public struct Move<StateType, ActionType>
{
  /// <summary>Initializes a new <see cref="Move{S,A}"/> with the given state and action.</summary>
  public Move(StateType state, ActionType action)
  {
    State       = state;
    Action      = action;
    Probability = 0;
  }

  /// <summary>Initializes a new <see cref="Move{S,A}"/> with the given state, action, and probability.</summary>
  /// <param name="state">The new state reached after the move.</param>
  /// <param name="action">The action performed to get to <paramref name="state"/>.</param>
  /// <param name="probability">The probability of the state occurring if the parent state involves randomness, or zero
  /// if the parent state does not involve randomness. See <see cref="Probability"/>.
  /// </param>
  public Move(StateType state, ActionType action, float probability)
  {
    State       = state;
    Action      = action;
    Probability = probability;
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
  /// <summary>The probability of this state occurring (as a value greater than zero and less than or equal to one) if
  /// the parent state involves randomness, such as a die roll. If the parent state does not involve randomness,
  /// this must be equal to zero.
  /// </summary>
  public float Probability;
}
#endregion

#region ITurnBasedGame
/// <include file="documentation.xml" path="/AI/Search/typeparam[@name='StateType' or @name='ActionType']"/>
public interface ITurnBasedGame<StateType, ActionType>
{
  /// <summary>Gets information about the game's problem space and configuration.</summary>
  /// <param name="numberOfPlayers">Receives the number of players in the game, a positive integer.</param>
  /// <param name="minUtility">Receives the minimum value that may be returned by <see cref="GetUtility"/>, or
  /// <see cref="float.NegativeInfinity"/> if the utility function has no lower bound.
  /// </param>
  /// <param name="maxUtility">Receives the maximum value that may be returned by <see cref="GetUtility"/>, or
  /// <see cref="float.PositiveInfinity"/> if the utility function has no upper bound.
  /// </param>
  /// <remarks>This method allows searches to configure themselves to work with the game. The information returned must
  /// not change during the course of a search.
  /// <para>
  /// This bounds of the utility function are used to allow pruning portions of the search tree. It helps in two ways.
  /// First, if any state is found with a utility equal to the maximum, the search will not pursue other avenues.
  /// Second, it allows pruning the children of chance nodes, which otherwise could not be done. The bounds should be
  /// chosen such that only winning states receive the maximum utility, and only losing states receive the minimum
  /// utility. For a zero-sum game, the minimum and maximum utility values must be equal in magnitude. You can use
  /// negative and positive infinity to indicate that the utility function is unbounded, or that the bounds of the
  /// utility function are unknown, but that will prevent some optimizations.
  /// </para>
  /// </remarks>
  void GetGameInformation(out int numberOfPlayers, out float minUtility, out float maxUtility);

  /// <summary>Gets a default initial state for the problem.</summary>
  /// <remarks>What this returns depends on the problem. Problems with multiple initial states may choose to return
  /// one at random or simply return a constant, representative initial state.
  /// </remarks>
  StateType GetInitialState();

  /// <summary>Returns which player's turn it is in the given state, from 0 to one less than the number of players, or
  /// -1 if it is chance's turn to play (eg, a die roll will occur).
  /// </summary>
  int GetPlayerToMove(StateType state);

  /// <summary>Gets the state/action pairs representing the successors of a state and the actions that must be taken
  /// to get to them.
  /// </summary>
  /// <remarks>If the given state is not a terminal state, this must return at least one successor.
  /// To make the search as efficient as possible, this method should return successors in the order of
  /// highest to lowest utility for the player whose turn it is in the given state, as much as possible. This allows
  /// more nodes to be pruned from the search.
  /// </remarks>
  IEnumerable<Move<StateType,ActionType>> GetSuccessors(StateType state);

  /// <summary>Returns the utility of the given state for the given player.</summary>
  /// <remarks>If the state is a terminal state, then the utility must be the exact utility obtained from reaching
  /// that state. Otherwise, the utility can be estimated. If the game is a game of chance (ie, it contains moves
  /// with <see cref="Move{S,A}.Probability"/> greater than zero), the utility value must be a positive linear
  /// transformation of the probability of winning from the given state (or more generally, of the actual utility of
  /// the state). For games that are not games of chance, it is sufficient for the utility values to simply be higher
  /// for better states. However, see <see cref="GetGameInformation"/> for caveats about zero-sum games.
  /// </remarks>
  float GetUtility(StateType state, int player);

  /// <summary>Determines whether the given state is a terminal state (ie, has no successors).</summary>
  /// <remarks>If this returns false, then <see cref="GetSuccessors"/> applied to the state must yield at least one
  /// successor.
  /// </remarks>
  bool IsTerminalState(StateType state);
}
#endregion
#endregion

// TODO: implement transposition tables!

#region Search types
#region GameSearchBase
/// <summary>A base class for adversarial (or game) search algorithms, which attempt to decide which action should be
/// performed by an agent in a situation containing other agents, where agents take turns performing actions, and where
/// the actions of one agent have a significant effect on the others.
/// </summary>
/// <include file="documentation.xml" path="/AI/Search/typeparam[@name='StateType' or @name='ActionType']"/>
public abstract class GameSearchBase<StateType, ActionType> :
  SearchBase<StateType, StateActionPair<StateType,ActionType>>
{
  /// <summary>Initializes the search with the given game instance.</summary>
  protected GameSearchBase(ITurnBasedGame<StateType, ActionType> game)
  {
    if(game == null) throw new ArgumentNullException();
    this.game = game;
  }

  /// <summary>Gets or sets the maximum depth of the search, or <see cref="SearchBase.Infinite"/> to set no maximum.</summary>
  /// <remarks>Searches may not strictly respect the depth limit. For instance, they may temporarily exceed the depth
  /// if it's necessary to resolve a dangerous uncertainty (as in a quiescence search), or if it's expected to result
  /// in a substantially improved search result for minimal extra effort (as in a singular extension). The default
  /// value is <see cref="SearchBase.Infinite"/>.
  /// </remarks>
  public int DepthLimit
  {
    get { return depthLimit; }
    set
    {
      if(value != Infinite && value < 1)
      {
        throw new ArgumentOutOfRangeException("DepthLimit", "The depth limit must be positive or Infinite.");
      }
      depthLimit = value;
    }
  }

  /// <include file="documentation.xml" path="/AI/Search/ISearch/Search/*"/>
  public sealed override SearchResult Search(SearchLimiter limiter,
                                             out StateActionPair<StateType, ActionType> solution)
  {
    return Search(game.GetInitialState(), limiter, out solution);
  }

  /// <include file="documentation.xml" path="/AI/Search/ISearch/Search_State/*"/>
  public sealed override SearchResult Search(StateType initialState, SearchLimiter limiter,
                                             out StateActionPair<StateType, ActionType> solution)
  {
    PrepareToStartSearch(initialState);
    if(limiter != null) limiter.Start();
    // from PerformSearch(), Failed means that the limiter caused the search to abort, while LimitReached means that the
    // search completed, but was truncated by the depth limit. we'll convert Failed to LimitReached here.
    SearchResult result = PerformSearch(initialState, limiter, out solution);
    return result == SearchResult.Failed ? SearchResult.LimitReached : result;
  }

  /// <include file="documentation.xml" path="/AI/Search/GameSearchBase/IterativeDeepeningSearch/*"/>
  /// <include file="documentation.xml" path="/AI/Search/SearchBase/Search_Timeout/param[@name='msTimeLimit']"/>
  public SearchResult IterativeDeepeningSearch(int msTimeLimit,
                                               out StateActionPair<StateType, ActionType> solution)
  {
    return IterativeDeepeningSearch(game.GetInitialState(), msTimeLimit, out solution);
  }

  /// <include file="documentation.xml" path="/AI/Search/GameSearchBase/IterativeDeepeningSearch/*"/>
  /// <include file="documentation.xml" path="/AI/Search/SearchBase/Search_Timeout/param[@name='msTimeLimit']"/>
  /// <include file="documentation.xml" path="/AI/Search/ISearch/Search_State/param[@name = 'initialState']"/>
  public SearchResult IterativeDeepeningSearch(StateType initialState, int msTimeLimit,
                                               out StateActionPair<StateType, ActionType> solution)
  {
    return IterativeDeepeningSearch(initialState, msTimeLimit == Infinite ? null : new TimeLimiter(msTimeLimit),
                                    out solution);
  }

  /// <include file="documentation.xml" path="/AI/Search/GameSearchBase/IterativeDeepeningSearch/*"/>
  /// <include file="documentation.xml" path="/AI/Search/ISearch/SearchCommon/param[@name='limiter']"/>
  public SearchResult IterativeDeepeningSearch(SearchLimiter limiter,
                                               out StateActionPair<StateType, ActionType> solution)
  {
    return IterativeDeepeningSearch(game.GetInitialState(), limiter, out solution);
  }

  /// <include file="documentation.xml" path="/AI/Search/GameSearchBase/IterativeDeepeningSearch/*"/>
  /// <include file="documentation.xml" path="/AI/Search/ISearch/SearchCommon/param[@name='limiter']"/>
  /// <include file="documentation.xml" path="/AI/Search/ISearch/Search_State/param[@name='initialState']"/>
  public SearchResult IterativeDeepeningSearch(StateType initialState, SearchLimiter limiter,
                                               out StateActionPair<StateType, ActionType> solution)
  {
    SearchResult result;
    int userDepthLimit = DepthLimit; // save the original depth limit so we can restore it later

    if(limiter == null) // if we have an no limit, we might as well do a regular search
    {
      DepthLimit = Infinite; // with unlimited depth because we have unlimited time
      result = Search(initialState, limiter, out solution);
    }
    else
    {
      PrepareToStartSearch(initialState); // otherwise, verify that the search is valid

      if(limiter != null) limiter.Start();
      BeginIterativeDeepeningSearch(initialState);

      result = SearchResult.Failed; // assume that we couldn't complete a single iteration
      solution = new StateActionPair<StateType, ActionType>();

      // gradually increase the depth limit, starting from 1
      for(DepthLimit=1; ; DepthLimit = DepthLimit==int.MaxValue ? Infinite : DepthLimit+1)
      {
        // start a new search with the given depth limit, and run it until it completes or the time expires
        StateActionPair<StateType, ActionType> currentSolution;
        SearchResult currentResult = PerformSearch(initialState, limiter, out currentSolution);

        // Failed, in this case, means that the search couldn't complete because of the limiter, while LimitReached
        // means that the search completed but was limited by the depth limit
        if(currentResult == SearchResult.Failed) break;

        // the search completed, so store the result and solution
        result   = currentResult;
        solution = currentSolution;

        // if the search was not limited by depth, increasing the depth won't help, so we're done
        if(currentResult != SearchResult.LimitReached) break;
      }

      EndIterativeDeepeningSearch();
    }

    DepthLimit = userDepthLimit; // restore the previous depth limit
    return result;
  }

  /// <summary>Gets the game instance associated with the search.</summary>
  protected ITurnBasedGame<StateType, ActionType> Game
  {
    get { return game; }
  }

  /// <summary>Gets the maximum value that will be returned from <see cref="ITurnBasedGame{S,A}.GetUtility"/>, or
  /// <see cref="float.PositiveInfinity"/> if no maximum is known.
  /// </summary>
  protected float MaxUtility
  {
    get { return maxUtility; }
  }

  /// <summary>Gets the minimum value that will be returned from <see cref="ITurnBasedGame{S,A}.GetUtility"/>, or
  /// <see cref="float.NegativeInfinity"/> if no minimum is known.
  /// </summary>
  protected float MinUtility
  {
    get { return minUtility; }
  }

  /// <summary>The number of players in the game.</summary>
  protected int NumberOfPlayers
  {
    get { return numberOfPlayers; }
  }

  /// <summary>Given a non-terminal state and the depth of the state within the search tree, determines whether the
  /// search should cut off and simply estimate the utility of the state, or whether it should continue on to the
  /// successors of the state in hopes of obtaining a more exact utility value.
  /// </summary>
  /// <param name="state">A non-terminal state in the search.</param>
  /// <param name="depth">The depth of the search within the search tree.</param>
  /// <returns>True if the search should be cut off, or false if the search should recurse further.</returns>
  protected virtual bool CutOffSearch(StateType state, int depth)
  {
    return DepthLimit != Infinite && depth >= DepthLimit;
  }

  /// <include file="documentation.xml" path="/AI/Search/GameSearchBase/PerformSearch/*"/>
  protected abstract SearchResult PerformSearch(StateType initialState, SearchLimiter limiter,
                                                out StateActionPair<StateType, ActionType> solution);

  /// <include file="documentation.xml" path="/AI/Search/GameSearchBase/BeginIterativeDeepeningSearch/*"/>
  protected virtual void BeginIterativeDeepeningSearch(StateType initialState) { }

  /// <include file="documentation.xml" path="/AI/Search/GameSearchBase/EndIterativeDeepeningSearch/*"/>
  protected virtual void EndIterativeDeepeningSearch() { }

  /// <include file="documentation.xml" path="/AI/Search/GameSearchBase/PrepareToStartSearch/*"/>
  protected virtual void PrepareToStartSearch(StateType initialState)
  {
    game.GetGameInformation(out numberOfPlayers, out minUtility, out maxUtility);

    if(NumberOfPlayers < 1) throw new InvalidOperationException("There must be at least one player.");
    if(maxUtility < minUtility)
    {
      throw new InvalidOperationException("The minimum utility must be less than or equal to the maximum utility.");
    }

    if(game.IsTerminalState(initialState))
    {
      throw new ArgumentException("The initial state is a terminal state, so no move is possible.");
    }

    if(game.GetPlayerToMove(initialState) == -1)
    {
      throw new ArgumentException("The initial state is a chance state, so no move can be selected.");
    }
  }

  readonly ITurnBasedGame<StateType, ActionType> game;
  float minUtility, maxUtility;
  int numberOfPlayers;
  int depthLimit = Infinite;
}
#endregion

#region AlphaBetaSearch
/// <summary>Implements a search that selects the best move for a player in a 2-player zero-sum game.</summary>
/// <remarks>Alpha-beta search is designed for zero-sum games with 2 players. The two players don't necessarily have to
/// alternate, however. For non-zero-sum games or games with more than 2 players, use <see cref="MaxSearch{S,A}"/>. On
/// all games where the two searches are applicable, the results are identical (assuming each searches to the same
/// depth in the search tree), but alpha-beta is specifically optimized for 2-player zero-sum games, and is
/// substantially faster.
/// </remarks>
public class AlphaBetaSearch<StateType, ActionType> : GameSearchBase<StateType,ActionType>
{
  /// <summary>Initializes the <see cref="AlphaBetaSearch{S,A}"/> with the given game instance.</summary>
  public AlphaBetaSearch(ITurnBasedGame<StateType, ActionType> game) : base(game) { }

  /// <include file="documentation.xml" path="/AI/Search/GameSearchBase/PerformSearch/*"/>
  protected override SearchResult PerformSearch(StateType initialState, SearchLimiter limiter,
                                                out StateActionPair<StateType,ActionType> solution)
  {
    solution = new StateActionPair<StateType, ActionType>();

    // If we're performing an iterative deepening search, rootUtilities and rootSuccessors will be non-null. In that
    // case, we should use them to store utilities and retrieve successor nodes. The purpose is to allow some state,
    // in particular move ordering at the root, to be retained between iterations of the iterative deepening search.
    // The rootUtilities array is used to sort the rootSuccessors array so that the best moves from the previous
    // iteration can be tried first.
    //
    // TODO: should we extend this idea to store the best moves from each ply in the search?
    // TODO: we should probably also implement the killer heuristic and/or history heuristics (this requires some
    //       more complex communication between the game and the search)
    // TODO: we should implement the scout part of negascout
    // this url is quite good: http://www.fierz.ch/strategy.htm
    if(rootUtilities != null)
    {
      for(int i=0; i<rootUtilities.Length; i++) rootUtilities[i] = float.PositiveInfinity;
    }

    this.limiter  = limiter;
    depthLimitHit = wasTerminal = searchAborted = false;

    float bestUtility = float.NegativeInfinity, best0 = float.NegativeInfinity, best1 = float.NegativeInfinity;
    int player = Game.GetPlayerToMove(initialState);
    int index = 0; // the index of the successor within rootSuccessor that we're currently examining

    foreach(Move<StateType,ActionType> move in
            rootSuccessors != null ? rootSuccessors : Game.GetSuccessors(initialState))
    {
      // invoke the the alpha-beta search to estimate the utility value of the move's ending state
      float utility = GetUtilityEstimate(move.State, best0, best1, 1, player);

      // we'll use Failed to indicate that the limiter caused the search to abort
      if(searchAborted) return SearchResult.Failed;

      // the alpha-beta search returns the utility of the move from the perspective of the player whose turn it is in
      // the state resulting from the move. so find out which player that is.
      int otherPlayer = Game.GetPlayerToMove(move.State);

      // if the player moving at the successor is not the same as the player moving at the root, then we need to
      // reverse the utility. however, if the move led to a chance node, represented as a "player" of -1, the utility
      // is already from the correct viewpoint, so we don't need to reverse it.
      if(player != otherPlayer && otherPlayer != -1) utility = -utility;

      // if we're doing an iterative deepening search, store the utility of this move
      if(rootUtilities != null) rootUtilities[index++] = utility;

      // if the move is better than whatever we've got so far, store the move as the new best move
      if(utility > bestUtility)
      {
        bestUtility = utility;
        // we'll store the best move in the "solution" parameter
        solution = new StateActionPair<StateType, ActionType>(move.State, move.Action);
        // also, if the move was so good that it got the maximum utility, there's no point in searching further, so
        // we'll end the search immediately
        if(utility == MaxUtility)
        {
          // if a terminal state was reached in the line of search that led to this maximal utility, then we don't
          // need another round of iterative deepening search because there's no doubt about this particular move,
          // and since this move is optimal, that's all that matters
          if(wasTerminal) depthLimitHit = false;
          break;
        }

        // update the alpha-beta values for the player at the root
        if(player == 0)
        {
          if(utility > best0) best0 = utility;
        }
        else
        {
          if(utility > best1) best1 = utility;
        }
      }
    }

    // if we're doing iterative deepening, sort the moves by utility
    if(rootSuccessors != null) SortRootSuccessors();

    this.limiter = null; // release the limiter

    return depthLimitHit ? SearchResult.LimitReached : SearchResult.Success;
  }

  /// <include file="documentation.xml" path="/AI/Search/GameSearchBase/BeginIterativeDeepeningSearch/*"/>
  protected override void BeginIterativeDeepeningSearch(StateType initialState)
  {
    base.BeginIterativeDeepeningSearch(initialState);
    rootSuccessors = new List<Move<StateType, ActionType>>(Game.GetSuccessors(initialState)).ToArray();
    rootUtilities  = new float[rootSuccessors.Length];
  }

  /// <include file="documentation.xml" path="/AI/Search/GameSearchBase/EndIterativeDeepeningSearch/*"/>
  protected override void EndIterativeDeepeningSearch()
  {
    rootSuccessors = null;
    rootUtilities  = null;
    base.EndIterativeDeepeningSearch();
  }

  /// <include file="documentation.xml" path="/AI/Search/GameSearchBase/PrepareToStartSearch/*"/>
  protected override void PrepareToStartSearch(StateType initialState)
  {
    base.PrepareToStartSearch(initialState);
    if(NumberOfPlayers != 2) throw new InvalidOperationException("This must be a 2-player zero-sum game.");
  }

  float GetUtilityEstimate(StateType state, float best0, float best1, int depth, int lastPlayer)
  {
    if(limiter != null && limiter.LimitReached)
    {
      searchAborted = true;
      return float.NaN;
    }

    int player = Game.GetPlayerToMove(state);

    // If this search hit a terminal state, or we'll cut it off, just return the game's estimate of the current
    // state's utility, from the perspective of the player to move in that state.
    wasTerminal = Game.IsTerminalState(state);
    if(wasTerminal || (CutOffSearch(state, depth) && (depthLimitHit=true))) // the assignment to depthLimitHit is intended
    {
      return Game.GetUtility(state, Game.GetPlayerToMove(state));
    }

    // It's not a terminal node, so we'll continue downward in the game tree and evaluate the successors of the current
    // state. How this will be done depends on the type of state we're looking at. If it's a chance state (a state
    // where "chance" is the player (represented by a player of -1), and the successors have a random chance of
    // occurring, then we'll take a weighted average of the successor utilities. Otherwise, an agent is playing and
    // can decide which move to make, so we'll take the maximum.

    if(player == -1) // If it's a chance state, we'll take the average of the successor utilities, weighted by the
    {                // probability of each successor occurring
      // TODO: implement alpha-beta pruning of successors of chance states
      float totalUtility = 0;

      foreach(Move<StateType,ActionType> move in Game.GetSuccessors(state))
      {
        float utility = GetUtilityEstimate(move.State, best0, best1, depth+1, lastPlayer);
        if(searchAborted) return utility;

        // at this point, we've estimated the utility of a successor of a chance state, from the perspective of the
        // player whose turn it is in the successor state. to simplify the logic elsewhere, we'll ensure that the
        // utility returned from a chance state is from the perspective of the player up the stack who most recently
        // had the option to move. This way, the utility of a chance state itself will never need to be negated because
        // it's already from the correct perspective.

        int otherPlayer = Game.GetPlayerToMove(move.State); // the player to move at the successor
        // if the players are different, and the successor isn't another chance state, reverse the utility to ensure
        // the assumption described above holds
        if(lastPlayer != otherPlayer && otherPlayer != -1) utility = -utility;

        totalUtility += utility * move.Probability; // weight the utility by the probability of the successor occurring
      }

      return totalUtility;
    }
    else // it's not a chance state
    {
      // the utility of a non-chance state for the player to move at that state is the utility of the best successor
      float bestUtility = float.NegativeInfinity;

      foreach(Move<StateType,ActionType> move in Game.GetSuccessors(state))
      {
        float utility = GetUtilityEstimate(move.State, best0, best1, depth+1, player);
        // find out which player was to move at the current state and the successor state, and if they are different,
        // reverse the utility to convert it to the right perspective (the perspective of the current player to move)
        int otherPlayer = Game.GetPlayerToMove(move.State);
        if(player != otherPlayer && otherPlayer != -1) utility = -utility;

        // if the utility for this move is the best yet, store it
        if(utility > bestUtility)
        {
          bestUtility = utility;
          // and, if the utility is the maximum possible, then we don't need to look at further moves, so just return.
          if(bestUtility == MaxUtility) return bestUtility;
        }

        // here is where we perform the alpha-beta pruning. the values best0 and best1 represent the highest utilities
        // available to player 0 and 1 respectively anywhere that we've searched so far. we'll stop examining
        // successors if we can show that we would never get here in practice, which occurs if the utility of this
        // state is worse than what the OTHER player has available elsewhere, such that the other player would not
        // choose a branch of the search that leads here
        if(player == 0)
        {
          // if the current player is 0 and the successor is player 1, and the utility from the perspective of player 1
          // is worse than what he has available elsewhere, then he wouldn't play such that we would ever get here,
          // and we can end this branch of the search.
          if(otherPlayer == 1 && -utility < best1) return bestUtility;
          // otherwise, if it's better than what player 0 has available elsewhere, mark this as the new best
          if(utility > best0) best0 = utility;
        }
        else
        {
          // similar to the above, end this branch of the search if it's worse for the other player
          if(otherPlayer == 0 && -utility < best0) return bestUtility;
          if(utility > best1) best1 = utility; // and update the current best
        }
      }

      return bestUtility;
    }
  }

  /// <summary>Sorts the root successors at the end of an iteration of iterative deepening search, so that the best
  /// moves identified in this iteration will be tried first during the next iteration.
  /// </summary>
  void SortRootSuccessors()
  {
    // we'll use insertion sort because it's stable and very efficient for nearly-sorted lists, and we expect the list
    // to be almost sorted most of the time. we'll sort in descending order.
    //
    // we want a stable sort because it often makes the results of the search "look" better. the search may realize
    // that several plies away, the opponent has a forced win. then, the utility of every move is the same minimal
    // value, in which case the first move in the list of successors will be picked. the first move is a very arbitrary
    // choice and in a tic-tac-toe like game may result in playing in the top-left corner. however, before the search
    // discovered the opponent's forced win, there may have been some good-looking moves. the stable sort will preserve
    // those previously-good-looking moves at the beginning of the list, so that in the case of a forced win, say, the
    // move that previously looked best will be chosen.
    for(int i=1; i<rootSuccessors.Length; i++)
    {
      Move<StateType,ActionType> move = rootSuccessors[i];
      float utility = rootUtilities[i];

      int j;
      for(j=i-1; j >= 0 && rootUtilities[j] < utility; j--)
      {
        rootSuccessors[j+1] = rootSuccessors[j];
        rootUtilities[j+1]  = rootUtilities[j];
      }
      rootSuccessors[j+1] = move;
      rootUtilities[j+1]  = utility;
    }
  }

  /// <summary>During an iterative deepening search, the successors of the root node.</summary>
  Move<StateType, ActionType>[] rootSuccessors;
  /// <summary>During an iterative deepening search, the utilities of the successors in <see cref="rootSuccessors"/>.</summary>
  float[] rootUtilities;
  /// <summary>The limiter that applies to the current search, if any.</summary>
  SearchLimiter limiter;
  /// <summary>Whether a state could not be expanded because of the depth limit at any time during the search.</summary>
  bool depthLimitHit;
  /// <summary>Whether the most recently evaluated state was a terminal state.</summary>
  bool wasTerminal;
  /// <summary>Whether the limiter caused the search to abort.</summary>
  bool searchAborted;
}
#endregion

#region MaxSearch
/// <summary>Implements a search that selects the best move for a player in an N-player turn-based game.</summary>
/// <remarks>MaxSearch is designed for general turn-based games. The players don't have to move in any fixed order.
/// For two-player zero-sum games, it's more efficient to use <see cref="AlphaBetaSearch{S,A}"/>.
/// </remarks>
public class MaxSearch<StateType, ActionType> : GameSearchBase<StateType,ActionType>
{
  /// <summary>Initializes the <see cref="MaxSearch{S,A}"/> with the given game instance.</summary>
  public MaxSearch(ITurnBasedGame<StateType, ActionType> game) : base(game) { }

  /// <include file="documentation.xml" path="/AI/Search/GameSearchBase/PerformSearch/*"/>
  protected override SearchResult PerformSearch(StateType initialState, SearchLimiter limiter,
                                                out StateActionPair<StateType, ActionType> solution)
  {
    solution = new StateActionPair<StateType, ActionType>();

    float bestUtility = float.NegativeInfinity;
    int player = Game.GetPlayerToMove(initialState);

    this.limiter  = limiter;
    depthLimitHit = searchAborted = false;

    foreach(Move<StateType, ActionType> move in Game.GetSuccessors(initialState)) // for each move available
    {
      // get the estimated utilities of the move for all players
      float[] utilities = GetExpectedUtilities(move.State, 1);
      if(searchAborted) return SearchResult.Failed; // if the limiter caused an abort, just return immediately

      // if the estimated utility of this move for the player moving at the root is the best yet, save it
      if(utilities[player] > bestUtility)
      {
        bestUtility = utilities[player];
        solution    = new StateActionPair<StateType, ActionType>(move.State, move.Action);

        if(bestUtility == MaxUtility) // if the given move is optimal, we don't need to search further
        {
          // if a terminal state was reached in the line of search that led to this maximal utility, then we don't
          // need another round of iterative deepening search because there's no doubt about this particular move,
          // and since this move is optimal, that's all that matters
          if(wasTerminal) depthLimitHit = false;
          break;
        }
      }
    }

    this.limiter = null; // release the limiter

    return depthLimitHit ? SearchResult.LimitReached : SearchResult.Success;
  }

  /// <summary>Estimates the utility of the given state for all players.</summary>
  float[] GetExpectedUtilities(StateType state, int depth)
  {
    if(limiter != null && limiter.LimitReached) // if the limit was reached, just return immediately
    {
      searchAborted = true;
      return null;
    }

    float[] utilities; // an array that will contain the estimated utilities of the state, for all players

    wasTerminal = Game.IsTerminalState(state);
    if(wasTerminal || (depthLimitHit=CutOffSearch(state, depth))) // the assignment to depthLimitHit is intentional
    {
      utilities = new float[NumberOfPlayers];
      for(int i=0; i<utilities.Length; i++) utilities[i] = Game.GetUtility(state, i);
    }
    else // this is not a terminal node, so the utility of this state is based on the utilities of the successor states
    {
      int player = Game.GetPlayerToMove(state); // figure out which player is to move at this state

      if(player == -1) // if it's a chance state, where no player moves but instead a random selection of successor
      {                // states occurs, the utility of the state is a weighted average of the successors' utilities
        utilities = new float[NumberOfPlayers];
        foreach(Move<StateType, ActionType> move in Game.GetSuccessors(state)) // for each successor
        {
          // get the estimated utilities of the successor state
          float[] moveUtilities = GetExpectedUtilities(move.State, depth+1);
          if(searchAborted) return null; // if the search was aborted, just return

          // add the utility to the total, weighted by the probability of the successor occurring
          for(int i=0; i<utilities.Length; i++) utilities[i] += moveUtilities[i] * move.Probability;
        }
      }
      else // an agent moves from this state, so find the best successor from that agent's perspective
      {
        utilities = null;

        foreach(Move<StateType, ActionType> move in Game.GetSuccessors(state)) // for each successor
        {
          // get the estimated utilities of the successor state
          float[] moveUtilities = GetExpectedUtilities(move.State, depth+1);
          if(searchAborted) return null; // if the search was aborted, just return

          // find the best successor for the player who moves at the current state
          if(utilities == null || moveUtilities[player] > utilities[player])
          {
            utilities = moveUtilities;
            if(utilities[player] == MaxUtility) break; // if the utility is maximal, we don't need to continue searching
          }
        }
      }
    }

    return utilities;
  }

  /// <summary>The limiter that applies to the current search, if any.</summary>
  SearchLimiter limiter;
  /// <summary>Whether the most recently evaluated leaf state was a terminal state (as opposed to being cut off).</summary>
  bool wasTerminal;
  /// <summary>Whether the depth limit was hit during this search.</summary>
  bool depthLimitHit;
  /// <summary>Whether the limiter caused the search to abort.</summary>
  bool searchAborted;
}
#endregion

// TODO: proof searches -- proof number (PN) search, proof set (PSS) search, bounded proof set search, PN*, PDS, etc.
#endregion

} // namespace AdamMil.AI.Search.Adversarial