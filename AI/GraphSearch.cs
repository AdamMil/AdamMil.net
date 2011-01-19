using System;
using System.Collections.Generic;
using AdamMil.Collections;
using AC=AdamMil.Collections;

namespace AdamMil.AI.Search.Graph
{

// NOTE: although the graph searches can be parallelized relatively easily, they have not been because the speed of the graph
// search heavily affected by memory access times. parallelizing it results in a speedup that is quite small, due to the
// increased cache and memory pressure, and makes the code substantially more complicated, so i didn't think it was worth it.
// it may be worth revisiting when we upgrade to .NET 4, which has faster locking primitives and concurrent collections

#region Problem definitions
#region IGraphSearchable
/// <summary>Represents a problem that can be attacked with a graph search.</summary>
/// <include file="documentation.xml" path="/AI/Search/GraphSearchDescription/*"/>
/// <include file="documentation.xml" path="/AI/Search/typeparam[@name='StateType' or @name='ActionType']"/>
public interface IGraphSearchable<StateType, ActionType> : ISearchable<StateType>
{
  /// <summary>Gets the cost of performing <paramref name="action"/> in the <paramref name="from"/> state and arriving
  /// at the <paramref name="to"/> state.
  /// </summary>
  /// <param name="from">The state in which the action occurred.</param>
  /// <param name="to">The state resulting from performing the action.</param>
  /// <param name="action">The action performed.</param>
  /// <returns>Returns the cost of moving from the <paramref name="from"/> state to the <paramref name="to"/> state
  /// via <paramref name="action"/>. To ensure that all searches will work properly, the action cost should be
  /// positive. Zero or negative costs are allowed, referring to actions that can be taken for free, and actions that
  /// grant rewards, respectively, but they should be used with caution. They can break certain searches, especially if
  /// the state graph contains a cycle with a total cost that is zero or negative. This may cause an
  /// infinite loop as the search seeks the infinite reward to be found by following the cycle forever. In other
  /// cases, it may simply break the optimality guarantees of the search.
  /// </returns>
  float GetActionCost(StateType from, StateType to, ActionType action);

  /// <summary>Gets the heuristic of a state, representing an estimate of the path cost from that state to the goal.</summary>
  /// <remarks>The heuristic must be nonnegative, and if <paramref name="state"/> is a goal state, the heuristic must
  /// be zero. To guarantee optimality of the search, a heuristic must be admissible, meaning that it never
  /// overestimates the true cost to reach the goal. Furthermore, if the problem space is not a tree, the heuristic
  /// must be consistent to guarantee optimality, which means that for every state s and every successor s' of s, the
  /// heuristic for s is less than or equal to the path cost from s to s' plus the heuristic for s'. That is, the
  /// difference in the heuristics between s and s' must be less than or equal to the path cost from s to s'. Within
  /// these constraints, a heuristic that returns a higher value for a given state is better.
  /// </remarks>
  float GetHeuristic(StateType from);

  /// <summary>Gets a default initial state for the problem.</summary>
  /// <remarks>What this returns depends on the problem. Problems with multiple initial states may choose to return
  /// one at random or simply return a constant, representative initial state.
  /// </remarks>
  StateType GetInitialState();

  /// <summary>Gets the state/action pairs representing the successors of a state and the actions that must be taken
  /// to get to them.
  /// </summary>
  /// <remarks>Some searches may not use all successors returned, so it might be more efficient to generate them as
  /// they're requested by the enumerator.
  /// </remarks>
  IEnumerable<StateActionPair<StateType,ActionType>> GetSuccessors(StateType state);
}
#endregion

#region IBidirectionallySearchable
/// <summary>Represents a problem that can searched bidirectionally.</summary>
/// <include file="documentation.xml" path="/AI/Search/BidirectionalSearchDescription/*"/>
/// <include file="documentation.xml" path="/AI/Search/typeparam[@name='StateType' or @name='ActionType']"/>
public interface IBidirectionallySearchable<StateType, ActionType> : IGraphSearchable<StateType, ActionType>
{
  /// <summary>Gets the set of goal states from which the search should search backwards.</summary>
  IEnumerable<StateType> GetGoalStates();

  /// <summary>Gets the predecessors of a given state, and the actions taken to get to them.</summary>
  /// <remarks>Some searches may not use all predecessors returned, so it might be more efficient to generate them as
  /// they're requested by the enumerator.
  /// </remarks>
  IEnumerable<StateActionPair<StateType, ActionType>> GetPredecessors(StateType state);

  /// <summary>Given a state, its predecessor, and the action taken to get from the state to its predecessor, returns
  /// the opposite action needed to get from the predecessor to the state.
  /// </summary>
  /// <remarks>When the forwards and backwards portions of a bidirectional search connect, they need to be combined
  /// into a single solution from the initial state to the goal state. So, the actions taken to work backwards
  /// from the goal need to be reversed to form a single forward-only solution. This method performs that reversal.
  /// </remarks>
  ActionType GetSuccessorAction(StateType state, StateType predecessor, ActionType reversedAction);
}
#endregion
#endregion

#region Search types
#region IGraphSearch
/// <summary>Represents a graph search.</summary>
/// <include file="documentation.xml" path="/AI/Search/GraphSearchDescription/*"/>
/// <include file="documentation.xml" path="/AI/Search/typeparam[@name='StateType' or @name='ActionType']"/>
public interface IGraphSearch<StateType, ActionType> : ISearch<StateType, Node<StateType, ActionType>>
{
  /// <include file="documentation.xml" path="/AI/Search/IGraphSearch/EliminateDuplicateStates/*"/>
  bool EliminateDuplicateStates { get; set; }
}
#endregion

#region IBidirectionalGraphSearch
/// <summary>Represents a graph search that can be used to search bidirectionally.</summary>
/// <include file="documentation.xml" path="/AI/Search/BidirectionalSearchDescription/*"/>
/// <include file="documentation.xml" path="/AI/Search/typeparam[@name='StateType' or @name='ActionType']"/>
public interface IBidirectionalGraphSearch<StateType, ActionType> : IGraphSearch<StateType, ActionType>
{
  /// <include file="documentation.xml" path="/AI/Search/IBidirectionalGraphSearch/BidirectionalSearch/*"/>
  SearchResult BidirectionalSearch(SearchLimiter limiter, out Node<StateType, ActionType> solution);

  /// <include file="documentation.xml" path="/AI/Search/IBidirectionalGraphSearch/BidirectionalSearch_State/*"/>
  SearchResult BidirectionalSearch(StateType initialState, SearchLimiter limiter,
                                   out Node<StateType, ActionType> solution);
}
#endregion

#region Node
/// <summary>Represents a node in the search tree as well as a node in the solution. In a solution, it represents a
/// state and the action needed to get from that state to the next, with the node for the next state found by
/// following the <see cref="Parent"/> pointer. During the search, the fields can be used in whatever way is most
/// convenient for the search algorithm.
/// </summary>
/// <include file="documentation.xml" path="/AI/Search/typeparam[@name='StateType' or @name='ActionType']"/>
public sealed class Node<StateType,ActionType>
{
  /// <summary>The state represented by this node.</summary>
  public StateType State;
  /// <summary>The node representing the next state in the solution, or null if this is the final node in the solution.</summary>
  public Node<StateType,ActionType> Parent;
  /// <summary>The action cost to get from this node to the next. This field cannot necessarily be relied upon in a
  /// solution.
  /// </summary>
  public float PathCost;
  /// <summary>The estimate of the action cost to get from this state to a goal state. This field cannot necessarily be
  /// relied upon in a solution.
  /// </summary>
  public float HeuristicCost;
  /// <summary>The depth of the node in the search tree. This field cannot necessarily be relied upon in a solution.</summary>
  public int Depth;
  /// <summary>The action taken to get from this node to the next node in the solution.</summary>
  public ActionType Action;

  /// <summary>Converts the node into a human-readable string.</summary>
  public override string ToString()
  {
    return "Node<State="+Convert.ToString(State)+
           ", Action="+Convert.ToString(Action)+", Depth="+Depth+", Cost="+PathCost+", Heur="+HeuristicCost+">";
  }
}
#endregion

#region GraphSearchBase
/// <summary>A base class for the graph search algorithms implemented in this library.</summary>
/// <include file="documentation.xml" path="/AI/Search/typeparam[@name='StateType' or @name='ActionType']"/>
public abstract class GraphSearchBase<StateType, ActionType>
  : SearchBase<StateType,Node<StateType,ActionType>>, IBidirectionalGraphSearch<StateType,ActionType>
{
  /// <summary>Initializes the graph search with a problem instance.</summary>
  protected GraphSearchBase(IGraphSearchable<StateType,ActionType> problem)
  {
    if(problem == null) throw new ArgumentNullException();
    Problem = problem;
  }

  /// <include file="documentation.xml" path="/AI/Search/IGraphSearch/EliminateDuplicateStates/*"/>
  public bool EliminateDuplicateStates { get; set; }

  /// <include file="documentation.xml" path="/AI/Search/GraphSearchBase/BidirectionalSearch/*"/>
  public SearchResult BidirectionalSearch(out Node<StateType, ActionType> solution)
  {
    return BidirectionalSearch(Problem.GetInitialState(), null, out solution);
  }

  /// <include file="documentation.xml" path="/AI/Search/GraphSearchBase/BidirectionalSearch/*"/>
  /// <include file="documentation.xml" path="/AI/Search/SearchBase/Search_Timeout/param[@name='msTimeLimit']"/>
  public SearchResult BidirectionalSearch(int msTimeLimit, out Node<StateType, ActionType> solution)
  {
    return BidirectionalSearch(Problem.GetInitialState(), msTimeLimit, out solution);
  }

  /// <include file="documentation.xml" path="/AI/Search/GraphSearchBase/BidirectionalSearch_State/*"/>
  public SearchResult BidirectionalSearch(StateType initialState, out Node<StateType, ActionType> solution)
  {
    return BidirectionalSearch(initialState, null, out solution);
  }

  /// <include file="documentation.xml" path="/AI/Search/GraphSearchBase/BidirectionalSearch_State/*"/>
  /// <include file="documentation.xml" path="/AI/Search/SearchBase/Search_Timeout/param[@name='msTimeLimit']"/>
  public SearchResult BidirectionalSearch(StateType initialState, int msTimeLimit,
                                          out Node<StateType, ActionType> solution)
  {
    return BidirectionalSearch(initialState, msTimeLimit == Infinite ? null : new TimeLimiter(msTimeLimit),
                               out solution);
  }

  /// <include file="documentation.xml" path="/AI/Search/IBidirectionalGraphSearch/BidirectionalSearch/*"/>
  public SearchResult BidirectionalSearch(SearchLimiter limiter, out Node<StateType, ActionType> solution)
  {
    return BidirectionalSearch(Problem.GetInitialState(), limiter, out solution);
  }

  /// <include file="documentation.xml" path="/AI/Search/IBidirectionalGraphSearch/BidirectionalSearch_State/*"/>
  public abstract SearchResult BidirectionalSearch(StateType initialState, SearchLimiter limiter,
                                                   out Node<StateType,ActionType> solution);

  /// <include file="documentation.xml" path="/AI/Search/ISearch/Search/*"/>
  public sealed override SearchResult Search(SearchLimiter limiter, out Node<StateType, ActionType> solution)
  {
    return Search(Problem.GetInitialState(), limiter, out solution);
  }

  /// <summary>The problem to be solved by this search instance.</summary>
  protected IGraphSearchable<StateType, ActionType> Problem { get; private set; }
}
#endregion

#region SingleQueueSearchBase
/// <summary>A base class for all graph searches that can be implemented with a single algorithm operating on various
/// types of queues.
/// </summary>
/// <remarks>The basic algorithm implemented by this base class starts by pushing the initial node onto a queue, and
/// then for each iteration, popping the first node and pushing its successors, and continuing until a goal state is
/// found or until the queue is empty. Different types of searches can then be implemented by simply using different
/// types of queues. For instance, with a LIFO queue, it becomes a depth-first search, and with a FIFO queue, it
/// becomes a breadth-first search.
/// </remarks>
/// <include file="documentation.xml" path="/AI/Search/typeparam[@name='StateType' or @name='ActionType']"/>
public abstract class SingleQueueSearchBase<StateType, ActionType> : GraphSearchBase<StateType, ActionType>
{
  /// <summary>Initializes the single-queue search base with the given problem.</summary>
  /// <param name="problem">The problem instance to be solved.</param>
  /// <param name="usesHeuristic">Determines whether heuristic information is required by the search. If not, it won't
  /// be gathered. Passing true also has the effect of disabling bidirectional searches, because bidirectional
  /// searching with heuristics is not yet implemented.
  /// </param>
  protected SingleQueueSearchBase(IGraphSearchable<StateType, ActionType> problem, bool usesHeuristic) : base(problem)
  {
    this.BidiProblem  = problem as IBidirectionallySearchable<StateType,ActionType>;
    this.useHeuristic = usesHeuristic;
  }

  /// <include file="documentation.xml" path="/AI/Search/IBidirectionalGraphSearch/BidirectionalSearch_State/*"/>
  public override SearchResult BidirectionalSearch(StateType initialState, SearchLimiter limiter,
                                                   out Node<StateType, ActionType> solution)
  {
    AssertBidirectionallySearchable();
    if(limiter != null) limiter.Start();
    return FinishBidirectionalSearch(initialState, limiter, out solution);
  }

  /// <include file="documentation.xml" path="/AI/Search/ISearch/Search_State/*"/>
  public override SearchResult Search(StateType initialState, SearchLimiter limiter,
                                      out Node<StateType,ActionType> solution)
  {
    if(limiter != null) limiter.Start();
    return FinishSearch(initialState, limiter, out solution);
  }

  /// <summary>Gets or sets the depth limit of the search. This can be set by a derived class to cause the search to
  /// not expand any nodes at the given depth, preventing the search from going any deeper into the problem space. A
  /// value of <see cref="SearchBase.Infinite"/> means that there is no limit to the depth of the search.
  /// The default value is <see cref="SearchBase.Infinite"/>.
  /// </summary>
  protected int DepthLimit
  {
    get { return depthLimit; }
    set
    {
      if(value != Infinite && value < 0) throw new ArgumentOutOfRangeException();
      depthLimit = value;
    }
  }

  /// <summary>Creates the queue to be used by the search.</summary>
  /// <remarks>The properties of the queue determine the properties of the search. For instance, a LIFO queue results
  /// in a depth-first traversal, whereas a FIFO queue results in a breadth-first traversal.
  /// </remarks>
  protected abstract IQueue<Node<StateType, ActionType>> CreateQueue();

  /// <summary>This method does all of the work of running a bidirectional search except for ensuring that searching
  /// bidirectionally is safe and starting the limiter, if any.
  /// </summary>
  protected SearchResult FinishBidirectionalSearch(StateType initialState, SearchLimiter limiter,
                                                   out Node<StateType, ActionType> solution)
  {
    solution = new Node<StateType, ActionType>();

    // the queues into which nodes will be placed
    IQueue<Node<StateType, ActionType>> leftFringe, rightFringe;
    // dictionaries keeping track of states that have been visited before, and the best paths to them.
    Dictionary<StateType, Node<StateType, ActionType>> leftStates, rightStates;

    // initialize the search
    leftFringe  = CreateQueue();
    rightFringe = CreateQueue();
    leftStates  = new Dictionary<StateType, Node<StateType, ActionType>>();
    rightStates = EliminateDuplicateStates ? new Dictionary<StateType, Node<StateType, ActionType>>() : null;

    leftFringe.Enqueue(MakeNode(initialState));
    foreach(StateType goalState in BidiProblem.GetGoalStates()) rightFringe.Enqueue(MakeNode(goalState));

    bool limitHit = false; // keeps track of whether the search has hit some limit preventing it from exploring further

    while(leftFringe.Count != 0 && rightFringe.Count != 0) // while there are still nodes in both open lists
    {
      if(limiter != null && limiter.LimitReached)
      {
        limitHit = true;
        break;
      }

      // get a node from the forward search
      Node<StateType, ActionType> leftNode = leftFringe.Dequeue();
      if(Problem.IsGoal(leftNode.State)) // if it's a goal, we've found a forward-only solution, so return it
      {
        solution = leftNode;
        return SearchResult.Success;
      }

      // this proceeds much like standard search...
      if(EliminateDuplicateStates)
      {
        limitHit = TryEnqueueNodes(leftFringe, leftNode, leftStates, false) || limitHit;
      }
      else if(leftNode.Depth >= DepthLimit)
      {
        limitHit = true;
      }
      else
      {
        foreach(Node<StateType, ActionType> child in GetNodes(leftNode, null, false))
        {
          leftFringe.Enqueue(child);

          Node<StateType, ActionType> existingChild;
          if(!leftStates.TryGetValue(child.State, out existingChild) || IsNodeBetter(child, existingChild))
          {
            leftStates[child.State] = child;
          }
        }
      }

      // now take a node from the reverse search
      Node<StateType, ActionType> rightNode = rightFringe.Dequeue();
      // if it has been visited by the forward search, we've found a link between the two
      if(leftStates.TryGetValue(rightNode.State, out leftNode))
      {
        solution = BuildBidirectionalSolution(leftNode, rightNode); // convert it into a forward-only solution
        return SearchResult.Success; // and return it
      }

      limitHit = TryEnqueueNodes(rightFringe, rightNode, rightStates, true) || limitHit;
    }

    // if it gets here, we couldn't find a solution
    return limitHit ? SearchResult.LimitReached : SearchResult.Failed;
  }

  /// <summary>This method does all of the work of running a forward-only search except for starting the limiter, if
  /// any.
  /// </summary>
  protected SearchResult FinishSearch(StateType initialState, SearchLimiter limiter,
                                      out Node<StateType, ActionType> solution)
  {
    solution = new Node<StateType, ActionType>();

    IQueue<Node<StateType, ActionType>> fringe = CreateQueue();
    Dictionary<StateType, Node<StateType, ActionType>> statesSeen =
      EliminateDuplicateStates ? new Dictionary<StateType, Node<StateType, ActionType>>() : null;
    bool limitHit = false;

    fringe.Enqueue(MakeNode(initialState));

    while(fringe.Count != 0)
    {
      if(limiter != null && limiter.LimitReached)
      {
        limitHit = true;
        break;
      }

      Node<StateType, ActionType> node = fringe.Dequeue();
      if(Problem.IsGoal(node.State))
      {
        solution = node;
        return SearchResult.Success;
      }

      limitHit = TryEnqueueNodes(fringe, node, statesSeen, false) || limitHit;
    }

    return limitHit ? SearchResult.LimitReached : SearchResult.Failed;
  }

  /// <summary>Throws an exception if the problem cannot be searched bidirectionally, either because the problem
  /// doesn't support it, or because the search doesn't support it.
  /// </summary>
  void AssertBidirectionallySearchable()
  {
    if(BidiProblem == null) throw new InvalidOperationException("This problem is not bidirectionally searchable.");

    if(useHeuristic)
    {
      throw new NotImplementedException("Bidirectional searching is not implemented for heuristic-based searches.");
    }
  }

  /// <summary>Given the forward and backward chains formed by bidirectional searching, joins them together and returns
  /// a single forward-only solution.
  /// </summary>
  Node<StateType, ActionType> BuildBidirectionalSolution(Node<StateType, ActionType> start,
                                                         Node<StateType, ActionType> end)
  {
    // 'start' contains the solution leading back to the start node, and 'end' contains the solution leading back to
    // the end node. both have the same state. we need to reverse the solution of the end node, and adjust the
    // actions, depths, and costs
    while(end.Parent != null)
    {
      Node<StateType, ActionType> nextEnd = end.Parent;

      end.Action        = BidiProblem.GetSuccessorAction(end.Parent.State, end.State, end.Action);
      end.State         = nextEnd.State;
      end.PathCost      = start.PathCost + (end.Parent == null ? 0 : end.PathCost-end.Parent.PathCost);
      end.Depth         = start.Depth + 1;
      end.Parent        = start;
      end.HeuristicCost = nextEnd.HeuristicCost;

      start = end;
      end   = nextEnd;
    }

    return start;
  }

  /// <summary>Gets and returns the successors or predecessors of the given node, omitting paths that correspond to
  /// previously visited states and are not better than the existing path.
  /// </summary>
  /// <param name="parent">The state whose successors or predecessors will be returned.</param>
  /// <param name="statesSeen">A dictionary mapping states to the best known path to that state, or null if
  /// <see cref="GraphSearchBase{S,A}.EliminateDuplicateStates"/> is false.
  /// </param>
  /// <param name="reversed">If true, the predecessors of the node will be retrieved. If false, the successors.</param>
  /// <returns>Returns the new nodes to add to the queue.</returns>
  IEnumerable<Node<StateType,ActionType>> GetNodes(Node<StateType,ActionType> parent,
                                                   Dictionary<StateType,Node<StateType,ActionType>> statesSeen,
                                                   bool reversed)
  {
    IEnumerable<StateActionPair<StateType,ActionType>> statePairs =
      reversed ? BidiProblem.GetPredecessors(parent.State) : Problem.GetSuccessors(parent.State);
    return statesSeen == null ? GetNodes(parent, statePairs)
                              : GetNonDuplicatedNodes(parent, statePairs, statesSeen);
  }

  /// <summary>Given a node and an enumerator for its successor or predecessor states, returns an enumerator for the
  /// corresponding nodes. In other words, it converts states to nodes representing those states.
  /// </summary>
  /// <param name="parent">A node representing a state.</param>
  /// <param name="statePairs">An enumerator for the successors or predecessors of <paramref name="parent"/>.</param>
  IEnumerable<Node<StateType,ActionType>> GetNodes(Node<StateType,ActionType> parent,
                                                   IEnumerable<StateActionPair<StateType,ActionType>> statePairs)
  {
    foreach(StateActionPair<StateType,ActionType> pair in statePairs)
    {
      yield return MakeNode(parent, pair);
    }
  }

  /// <summary>Given a node and an enumerator for its successor or predecessor states, returns an enumerator for the
  /// corresponding nodes, excluding known states with better paths to them. In other words, it converts states to
  /// nodes representing those states, excluding nodes for known states to which we already have a better path.
  /// </summary>
  /// <param name="parent">A node representing a state.</param>
  /// <param name="statePairs">An enumerator for the successors or predecessors of <paramref name="parent"/>.</param>
  /// <param name="statesSeen">A dictionary mapping states to the best known path to that state.</param>
  IEnumerable<Node<StateType, ActionType>> GetNonDuplicatedNodes(Node<StateType, ActionType> parent,
                                                        IEnumerable<StateActionPair<StateType,ActionType>> statePairs,
                                                        Dictionary<StateType,Node<StateType,ActionType>> statesSeen)
  {
    foreach(StateActionPair<StateType,ActionType> pair in statePairs)
    {
      Node<StateType,ActionType> child = MakeNode(parent, pair), existingNode;
      if(!statesSeen.TryGetValue(pair.State, out existingNode) || IsNodeBetter(child, existingNode))
      {
        statesSeen[pair.State] = child;
        yield return child;
      }
    }
  }

  /// <summary>Given a parent node and a <see cref="StateActionPair{S,A}"/> of one of its sucessors or predecessors,
  /// returns a new <see cref="Node{S,A}"/> object representing the state.
  /// </summary>
  Node<StateType, ActionType> MakeNode(Node<StateType, ActionType> parent, StateActionPair<StateType, ActionType> pair)
  {
    Node<StateType,ActionType> node = new Node<StateType,ActionType>();
    node.Action   = pair.Action;
    node.PathCost = parent.PathCost + Problem.GetActionCost(parent.State, pair.State, pair.Action);
    node.Depth    = parent.Depth + 1;
    node.Parent   = parent;
    node.State    = pair.State;
    if(useHeuristic) node.HeuristicCost = Problem.GetHeuristic(pair.State);
    return node;
  }

  /// <summary>Adds the successors or predecessors of <paramref name="parent"/> to the <paramref name="fringe"/>,
  /// assuming it doesn't exceed the depth limit and the paths to the new states are better than any existing paths.
  /// </summary>
  /// <param name="fringe">The queue to which the nodes will be added.</param>
  /// <param name="parent">The parent of the given states.</param>
  /// <param name="statesSeen">A dictionary mapping states to the best known path to that state, or null if
  /// <see cref="GraphSearchBase{S,A}.EliminateDuplicateStates"/> is false.
  /// </param>
  /// <param name="reversed">If true, the predecessors of the node will be retrieved. If false, the successors.</param>
  /// <returns>Returns true if the depth limit was hit.</returns>
  bool TryEnqueueNodes(IQueue<Node<StateType, ActionType>> fringe, Node<StateType, ActionType> parent,
                       Dictionary<StateType, Node<StateType, ActionType>> statesSeen, bool reversed)
  {
    if(depthLimit == Infinite || parent.Depth < depthLimit)
    {
      foreach(Node<StateType, ActionType> child in GetNodes(parent, statesSeen, reversed))
      {
        fringe.Enqueue(child);
      }
      return false;
    }
    else
    {
      return true;
    }
  }

  /// <summary>The interface to the bidirectional problem. This is either equal to
  /// <see cref="GraphSearchBase{S,A}.Problem"/>, or null if the problem doesn't support bidirectional searching.
  /// </summary>
  readonly IBidirectionallySearchable<StateType, ActionType> BidiProblem;
  /// <summary>The depth limit.</summary>
  int depthLimit = Infinite;
  /// <summary>Whether this search uses heuristic information.</summary>
  readonly bool useHeuristic;

  /// <summary>Given two nodes representing the same state, returns true if the first node represents a better path to
  /// the state.
  /// </summary>
  static bool IsNodeBetter(Node<StateType, ActionType> node, Node<StateType, ActionType> comparedTo)
  {
    return node.PathCost < comparedTo.PathCost ||
           node.Depth < comparedTo.Depth && node.PathCost == comparedTo.PathCost;
  }

  /// <summary>Given an initial state, returns a node representing it.</summary>
  static Node<StateType, ActionType> MakeNode(StateType initialState)
  {
    Node<StateType, ActionType> node = new Node<StateType, ActionType>();
    node.State = initialState;
    return node;
  }
}
#endregion

#region AStarSearch
/// <summary>A search method that expands nodes with the lowest sum of path cost and state heuristic first.</summary>
/// <remarks>A* search is complete if the branching factor is finite, optimal when searching a tree if the heuristic is
/// admissible, and optimal when searching a graph if the heuristic is both admissible and consistent.
/// </remarks>
/// <include file="documentation.xml" path="/AI/Search/typeparam[@name='StateType' or @name='ActionType']"/>
public sealed class AStarSearch<StateType, ActionType> : SingleQueueSearchBase<StateType, ActionType>
{
  /// <summary>Initializes a new <see cref="AStarSearch{S,A}"/>.</summary>
  public AStarSearch(IGraphSearchable<StateType, ActionType> problem) : base(problem, true) { }

  /// <summary>Creates a priority queue that dequeues the node with the lowest sum of path cost and heuristic first.</summary>
  protected override IQueue<Node<StateType, ActionType>> CreateQueue()
  {
    return new PriorityQueue<Node<StateType, ActionType>>(new NodeCostComparer());
  }

  sealed class NodeCostComparer : IComparer<Node<StateType,ActionType>>
  {
    public int Compare(Node<StateType,ActionType> a, Node<StateType,ActionType> b)
    {
      return (b.PathCost+b.HeuristicCost).CompareTo(a.PathCost+a.HeuristicCost);
    }
  }
}
#endregion

#region BreadthFirstSearch
/// <summary>A search that expands nodes in a breadth-first fashion, trying all nodes at a given depth before moving
/// deeper into the tree.
/// </summary>
/// <remarks>Breadth-first search is complete if the branching factor is finite, optimal if step costs are uniform,
/// and has O(b^(d+1)) space and time complexity.
/// </remarks>
/// <include file="documentation.xml" path="/AI/Search/typeparam[@name='StateType' or @name='ActionType']"/>
public sealed class BreadthFirstSearch<StateType, ActionType> : SingleQueueSearchBase<StateType, ActionType>
{
  /// <summary>Initializes a new <see cref="BreadthFirstSearch{S,A}"/>.</summary>
  public BreadthFirstSearch(IGraphSearchable<StateType, ActionType> problem) : base(problem, false) { }

  /// <summary>Creates and returns a new FIFO queue.</summary>
  protected override IQueue<Node<StateType, ActionType>> CreateQueue()
  {
    return new CircularList<Node<StateType, ActionType>>();
  }
}
#endregion

#region DepthBasedSearch
/// <summary>A base class for depth-first searches.</summary>
/// <include file="documentation.xml" path="/AI/Search/typeparam[@name='StateType' or @name='ActionType']"/>
public abstract class DepthBasedSearch<StateType, ActionType> : SingleQueueSearchBase<StateType, ActionType>
{
  /// <summary>Initializes a new <see cref="DepthBasedSearch{S,A}"/>.</summary>
  protected DepthBasedSearch(IGraphSearchable<StateType, ActionType> problem) : base(problem, false) { }

  /// <summary>Creates and returns a new stack.</summary>
  protected override IQueue<Node<StateType, ActionType>> CreateQueue()
  {
    return new AC.Stack<Node<StateType, ActionType>>();
  }
}
#endregion

#region DepthLimitedSearch
/// <summary>A search that is equivalent to <see cref="DepthFirstSearch{S,A}"/>, except that it is guaranteed to terminate.</summary>
/// <include file="documentation.xml" path="/AI/Search/typeparam[@name='StateType' or @name='ActionType']"/>
public class DepthLimitedSearch<StateType, ActionType> : DepthBasedSearch<StateType, ActionType>
{
  /// <summary>Initializes a new <see cref="DepthLimitedSearch{S,A}"/>.</summary>
  public DepthLimitedSearch(IGraphSearchable<StateType, ActionType> problem, int depthLimit)
    : base(problem)
  {
    if(depthLimit < 0) throw new ArgumentOutOfRangeException("Depth limit must not be negative.");
    DepthLimit = depthLimit;
  }
}
#endregion

#region DepthFirstSearch
/// <summary>A search that expands the deepest node in the tree first.</summary>
/// <remarks>Depth-first search is neither complete nor optimal and may never terminate even if the search space
/// contains a solution, but it has O(b^m) time complexity and O(bm) space complexity.
/// </remarks>
/// <include file="documentation.xml" path="/AI/Search/typeparam[@name='StateType' or @name='ActionType']"/>
public sealed class DepthFirstSearch<StateType, ActionType> : DepthLimitedSearch<StateType, ActionType>
{
  /// <summary>Initializes a new <see cref="DepthFirstSearch{S,A}"/>.</summary>
  public DepthFirstSearch(IGraphSearchable<StateType, ActionType> problem) : base(problem, int.MaxValue) { }
}
#endregion

#region GreedyBestFirstSearch
/// <summary>A search method that expands nodes with the lowest heuristic first.</summary>
/// <remarks>Greedy best first search is neither optimal nor complete, and is dominated by <see cref="AStarSearch{S,A}"/>.</remarks>
/// <include file="documentation.xml" path="/AI/Search/typeparam[@name='StateType' or @name='ActionType']"/>
public sealed class GreedyBestFirstSearch<StateType, ActionType> : SingleQueueSearchBase<StateType, ActionType>
{
  /// <summary>Initializes a new <see cref="GreedyBestFirstSearch{S,A}"/>.</summary>
  public GreedyBestFirstSearch(IGraphSearchable<StateType, ActionType> problem) : base(problem, true) { }

  /// <summary>Creates and returns a priority queue that dequeues the node with the lowest heuristic first.</summary>
  protected override IQueue<Node<StateType, ActionType>> CreateQueue()
  {
    return new PriorityQueue<Node<StateType, ActionType>>(new NodeCostComparer());
  }

  sealed class NodeCostComparer : IComparer<Node<StateType,ActionType>>
  {
    public int Compare(Node<StateType,ActionType> a, Node<StateType,ActionType> b)
    {
      return b.HeuristicCost.CompareTo(a.HeuristicCost);
    }
  }
}
#endregion

#region IterativeDeepeningSearch
/// <summary>A search that expands the deepest node in the tree first, up to a depth limit that is gradually increased.</summary>
/// <remarks>Unlike depth-first search, iterative deepening search is complete and optimal, and is guaranteed to
/// terminate if the search tree contains a solution. It has O(b^d) time complexity and O(bd) space complexity.
/// </remarks>
public sealed class IterativeDeepeningSearch<StateType, ActionType> : DepthBasedSearch<StateType, ActionType>
{
  /// <summary>Initializes a new <see cref="IterativeDeepeningSearch{S,A}"/>.</summary>
  public IterativeDeepeningSearch(IGraphSearchable<StateType, ActionType> problem) : base(problem) { }

  /// <include file="documentation.xml" path="/AI/Search/IBidirectionalGraphSearch/BidirectionalSearch_State/*"/>
  public override SearchResult BidirectionalSearch(StateType initialState, SearchLimiter limiter,
                                                   out Node<StateType, ActionType> solution)
  {
    return Search(initialState, limiter, out solution, true);
  }

  /// <include file="documentation.xml" path="/AI/Search/ISearch/Search_State/*"/>
  public override SearchResult Search(StateType initialState, SearchLimiter limiter,
                                      out Node<StateType, ActionType> solution)
  {
    return Search(initialState, limiter, out solution, false);
  }

  /// <summary>Performs the iterative deepening search.</summary>
  SearchResult Search(StateType initialState, SearchLimiter limiter,
                      out Node<StateType, ActionType> solution, bool bidirectional)
  {
    if(limiter != null) limiter.Start();

    solution = new Node<StateType,ActionType>();

    for(DepthLimit=1; limiter == null || !limiter.LimitReached; DepthLimit++)
    {
      SearchResult result = bidirectional ?
        base.FinishBidirectionalSearch(initialState, limiter, out solution) :
        base.FinishSearch(initialState, limiter, out solution);

      // if the search completed without reaching any limit, then we're done
      if(result != SearchResult.LimitReached) return result;

      // if the depth limit is about to wrap around, there's no point in continuing
      if(DepthLimit == int.MaxValue) return SearchResult.LimitReached;
    }

    return SearchResult.LimitReached;
  }
}
#endregion

// TODO: A* with partial expansion (http://www.ai.soc.i.kyoto-u.ac.jp/services/publications/00/00conf04.pdf)
// TODO: SMA* and SMAG* (http://www2.parc.com/isl/members/rzhou/papers/flairs02.ps.gz)

// TODO: check documentation (first "complete" to "optimal"?)
#region UniformCostSearch
/// <summary>A search that expands the node with the lowest accumulated path cost first.</summary>
/// <remarks>Uniform cost search is complete with positive step costs, is complete if the branching factor is finite
/// and step costs are positive, and has O(b^(C/e)) space and time complexity, where C is the cost of the optimal
/// solution and e is the minimum step cost.
/// </remarks>
public sealed class UniformCostSearch<StateType,ActionType> : SingleQueueSearchBase<StateType,ActionType>
{
  /// <summary>Initializes a new <see cref="UniformCostSearch{S,A}"/>.</summary>
  public UniformCostSearch(IGraphSearchable<StateType, ActionType> problem) : base(problem, false) { }

  /// <summary>Creates and returns a new priority queue that dequeues the node with the lowest path cost first.</summary>
  protected override IQueue<Node<StateType, ActionType>> CreateQueue()
  {
    return new PriorityQueue<Node<StateType, ActionType>>(new NodeCostComparer());
  }

  sealed class NodeCostComparer : IComparer<Node<StateType,ActionType>>
  {
    public int Compare(Node<StateType,ActionType> a, Node<StateType,ActionType> b)
    {
      return b.PathCost.CompareTo(a.PathCost); // we want the node with the lowest path cost
    }
  }
}
#endregion
#endregion

} // namespace AdamMil.AI.Search
