/*
AdamMil.AI is a library providing a set of flexible artificial intelligence
classes for the .NET Framework.

http://www.adammil.net/
Copyright (C) 2008-2013 Adam Milazzo

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
using System.Threading;
using AdamMil.Mathematics.Combinatorics;
using AdamMil.Mathematics.Random;
using AdamMil.Utilities;

namespace AdamMil.AI.Search.Local
{

#region Problem definitions
#region ILocallySearchable
/// <summary>An interface for problems that can be attacked with local search algorithms.</summary>
public interface ILocallySearchable<StateType> : ISearchable<StateType>
{
  /// <summary>Evaluates a state and returns a value indicating the desirability of that state, with higher values
  /// indicating more desirable states. The local search will attempt to maximize this value.
  /// </summary>
  /// <returns>Returns the desirability, or fitness, of the given state. Any values can be returned, but higher values
  /// are considered to be better. Some types of searches make use of the apparent gradient of the fitness function
  /// and the ratio or difference between one state's fitness and another's, so it's a good idea to devise a sensible
  /// scale the fitness function.
  /// </returns>
  float EvaluateState(StateType state);

  /// <summary>Generates and returns a random state for the problem, using the given random number generator.
  /// The state need not be fully random, but it should be random enough to cover the valuable sections of the search
  /// space.
  /// </summary>
  /// <remarks>This method should return states that are close to the goal if possible, but random enough such that
  /// the set of possible returned states covers all of the valuable parts of the search space.
  /// </remarks>
  StateValuePair<StateType> GetRandomState(RandomNumberGenerator random);

  /// <summary>Gets a random successor of the given state, using the given random number generator.</summary>
  StateValuePair<StateType> GetRandomSuccessor(RandomNumberGenerator random, StateValuePair<StateType> state);
}
#endregion

#region IClimbingSearchable
/// <summary>Represents a problem that can be attacked with a hill-climbing or similar local search algorithm.</summary>
public interface IClimbingSearchable<StateType> : ILocallySearchable<StateType>
{
  /// <summary>Gets the successors of a given state along with their fitness values.</summary>
  /// <remarks>The successors of a state should not change between calls to this method. That is, given a state, the same
  /// successors should always be returned.
  /// </remarks>
  IEnumerable<StateValuePair<StateType>> GetSuccessors(StateValuePair<StateType> state);
}
#endregion

#region IGeneticallySearchable
/// <summary>Represents a problem that can be attacked with a genetic algorithm.</summary>
public interface IGeneticallySearchable<StateType> : ILocallySearchable<StateType>
{
  /// <summary>Performs the crossover operation upon two (possibly equal) states, and returns the result.</summary>
  /// <param name="random">The random number generator to be used in the process.</param>
  /// <param name="a">The first parent state. This is possibly equal to <paramref name="b"/>.</param>
  /// <param name="b">The second parent state. This is possibly equal to <paramref name="a"/>.</param>
  /// <returns>Returns the child state, along with its fitness.</returns>
  /// <remarks>The crossover operation should create a new state from the two states given by combining portions of
  /// both parent states to create the new state.
  /// </remarks>
  StateValuePair<StateType> Crossover(RandomNumberGenerator random, StateValuePair<StateType> a, StateValuePair<StateType> b);

  /// <summary>Randomly mutates a state and a new state containing the result.</summary>
  /// <param name="random">The random number generator to be used in the process.</param>
  /// <param name="state">The state to be mutated.</param>
  /// <returns>Returns a new state that is similar to the given state, but with a small, random difference.</returns>
  StateValuePair<StateType> Mutate(RandomNumberGenerator random, StateValuePair<StateType> state);
}
#endregion
#endregion

#region Search types
#region StateValuePair
/// <summary>Represents a state and its fitness value, as returned by
/// <see cref="ILocallySearchable{T}.EvaluateState"/>.
/// </summary>
public struct StateValuePair<StateType>
{
  /// <summary>Initializes a new <see cref="StateValuePair{T}"/> with the given state and value.</summary>
  public StateValuePair(StateType state, float value)
  {
    State = state;
    Value = value;
  }

  /// <summary>The state represented by this structure.</summary>
  public StateType State;
  /// <summary>The fitness value of <see cref="State"/>.</summary>
  public float Value;
}
#endregion

#region ILocalSearch
/// <summary>Represents a local search algorithm.</summary>
/// <remarks>
/// <para>
/// A local search algorithm attempts to optimize a state by comparing it to its neighbors. Unlike graph search
/// algorithms, which attempt to find a path from a known start state to a (usually known) goal state, local search
/// algorithms attempt to find a state in a state space by maximizing an objective function that calculates the
/// desirability, or fitness, of each state. The start state in a local search is often randomly chosen.
/// </para>
/// <para>
/// Because local search algorithms are only concerned with the final result, they do not store the path taken to get
/// from the start state to the goal state. Because of this, local search algorithms typically use only a constant
/// amount of memory and behave semi-randomly. So depending on the type of local search employed, it may be necessary
/// to run the search several times, possibly propogating the best solution found from each run to the next. The
/// <see cref="IIterativeSearch{T,S,C}.Iterate"/> method of a local search algorithm will update the solution to the best
/// known state on each call, and the solution will be valid even if it is not a goal state, or optimal.
/// </para>
/// <para>
/// Therefore, local search algorithms are best suited to tasks where the end result is unknown, and the path taken to
/// get there is irrelevant. An example of this is the classic N-queens problem, in which one attempts to place N
/// queens on an NxN chess board such that no queen attacks any other. Whether a given board state is a solution is
/// easy to determine, but the exact goal states are unknown and the path taken through the search space to find one
/// doesn't matter.
/// </para>
/// </remarks>
public interface ILocalSearch<StateType, ContextType> : IIterativeSearch<StateType,StateValuePair<StateType>, ContextType>
  where ContextType : IterativeSearchContext<StateValuePair<StateType>>
{
}
#endregion

#region LocalSearchBase
/// <summary>A base class for most of the local searches implemented in this library.</summary>
/// <typeparam name="ProblemType">The type of problem interface expected by the derived class.</typeparam>
/// <include file="documentation.xml" path="/AI/Search/typeparam[@name='StateType' or @name='ContextType']"/>
public abstract class LocalSearchBase<StateType, ProblemType, ContextType>
  : IterativeSearchBase<StateType,StateValuePair<StateType>,ContextType>, ILocalSearch<StateType, ContextType>
    where ProblemType : ILocallySearchable<StateType>
    where ContextType : LocalSearchBase<StateType, ProblemType, ContextType>.LocalSearchContextBase
{
  /// <summary>Initializes a new <see cref="LocalSearchBase{S,P,C}"/> with the given problem instance.</summary>
  protected LocalSearchBase(ProblemType problem)
  {
    if(problem == null) throw new ArgumentNullException("problem");
    this.problem = problem;
  }

  #region LocalSearchContextBase
  /// <summary>Provides a base class for local search context classes.</summary>
  public abstract class LocalSearchContextBase : IterativeSearchContext<StateValuePair<StateType>>
  {
    /// <summary>Initializes a new <see cref="LocalSearchContextBase"/> with the <see cref="RandomNumberGenerator"/> to be used
    /// during the search.
    /// </summary>
    protected LocalSearchContextBase(RandomNumberGenerator random)
    {
      Random = random;
    }

    /// <summary>Gets the <see cref="RandomNumberGenerator"/> that the context was initialized with.</summary>
    public RandomNumberGenerator Random { get; private set; }
  }
  #endregion

  /// <summary>Gets the problem instance used to construct this search.</summary>
  protected ProblemType Problem
  {
    get { return problem; }
  }

  /// <summary>Returns a new random number generator. This method can be overridden to return a random number generator
  /// with different characteristics or a fixed seed.
  /// </summary>
  protected virtual RandomNumberGenerator CreateRandom()
  {
    return RandomNumberGenerator.CreateDefault();
  }

  /// <include file="documentation.xml" path="/AI/Search/IIterativeSearch/SelectBestSolution/node()"/>
  protected override StateValuePair<StateType> SelectBestSolution(ContextType[] contexts)
  {
    if(contexts == null) throw new ArgumentNullException();
    if(contexts.Length == 0) throw new ArgumentException();

    ContextType bestContext = contexts[0];
    float bestValue = bestContext.Solution.Value;
    for(int i=1; i<contexts.Length; i++)
    {
      float value = contexts[i].Solution.Value;
      if(value > bestValue)
      {
        bestValue   = value;
        bestContext = contexts[i];
      }
    }
    return bestContext.Solution;
  }

  readonly ProblemType problem;
}
#endregion

#region ClimbingSearchBase
/// <summary>A base class for local search algorithms based on or similar to hill-climbing, such as hill climbing
/// itself, local beam search, simulated annealing, etc.
/// </summary>
/// <include file="documentation.xml" path="/AI/Search/typeparam[@name='StateType' or @name='ContextType']"/>
public abstract class ClimbingSearchBase<StateType,ContextType>
  : LocalSearchBase<StateType, IClimbingSearchable<StateType>, ContextType>
    where ContextType : LocalSearchBase<StateType, IClimbingSearchable<StateType>, ContextType>.LocalSearchContextBase
{
  /// <summary>Initializes the <see cref="ClimbingSearchBase{S,C}"/> with a problem instance.</summary>
  protected ClimbingSearchBase(IClimbingSearchable<StateType> problem) : base(problem) { }
}
#endregion

#region GeneticAlgorithmSearch
/// <summary>Implements a genetic search algorithm.</summary>
/// <remarks>
/// <para>
/// A genetic search is a relative of stochastic beam search in which the members of the population can be updated
/// based on crossover between two other states, and point mutations. In this implementation, members can be updated
/// based on crossover and mutation, as well as simple replication and regeneration from scratch. Unlike stochastic
/// beam search, a genetic algorithm can be applied directly to continous state spaces because it does not rely on the
/// ability of the problem to generate the successors of a state.
/// </para>
/// <para>
/// (Stochastic beam search is variant of hill-climbing that maintains a population of states and selects new states
/// by finding the set of all successors of states in the population, and selecting a new population from the
/// successors by choosing ones with a probability proportional to their fitness.)
/// </para>
/// </remarks>
/// <include file="documentation.xml" path="/AI/Search/typeparam[@name='StateType']"/>
public class GeneticAlgorithmSearch<StateType>
  : LocalSearchBase<StateType, IGeneticallySearchable<StateType>, GeneticAlgorithmSearch<StateType>.Context>
{
  /// <summary>Initializes the <see cref="GeneticAlgorithmSearch{S}"/> with the given population size, a 90% chance
  /// of crossover, a 0.5% chance of regeneration, and a 2% chance of mutation.
  /// </summary>
  /// <include file="documentation.xml" path="/AI/Search/GeneticAlgorithm/Constructor/node()"/>
  public GeneticAlgorithmSearch(IGeneticallySearchable<StateType> problem, int populationSize)
    : this(problem, populationSize, 0.90, 0.005, 0.02) { }

  /// <summary>Initializes the <see cref="GeneticAlgorithmSearch{S}"/> with the given population size, crossover
  /// chance, regeneration chance, and mutation chance.
  /// </summary>
  /// <include file="documentation.xml" path="/AI/Search/GeneticAlgorithm/Constructor/node()"/>
  /// <param name="crossoverChance">The initial value for <see cref="CrossoverChance"/>.</param>
  /// <param name="regenerationChance">The initial value for <see cref="RegenerationChance"/>.</param>
  /// <param name="mutationChance">The initial value for <see cref="MutationChance"/>.</param>
  public GeneticAlgorithmSearch(IGeneticallySearchable<StateType> problem, int populationSize, double crossoverChance,
                                double regenerationChance, double mutationChance) : base(problem)
  {
    PopulationSize     = populationSize;
    CrossoverChance    = crossoverChance;
    RegenerationChance = regenerationChance;
    MutationChance     = mutationChance;
  }

  #region Context
  /// <summary>Contains the context for a genetic algorithm search.</summary>
  public sealed class Context : LocalSearchContextBase
  {
    internal Context(RandomNumberGenerator random, int populationSize) : base(random)
    {
      population = new StateValuePair<StateType>[populationSize];
    }

    /// <summary>Gets the solution from the current best member of the population.</summary>
    public override StateValuePair<StateType> Solution
    {
      get { return population[bestMember]; }
    }

    internal StateValuePair<StateType>[] population;
    internal int bestMember;
  }
  #endregion

  /// <summary>Gets or sets the chance of a member of a new generation being generated via crossover.</summary>
  /// <remarks>This property represents the chance of a new member of a generation being calculated by combining two
  /// members of the previous generation, as a probability from 0 to 1. This chance should be high, between 0.75 and
  /// 1.0.
  /// </remarks>
  public double CrossoverChance
  {
    get { return _crossoverChance; }
    set
    {
      if(value < 0 || value > 1) throw new ArgumentOutOfRangeException("CrossoverChance");
      _crossoverChance = value;
      AdjustRegenChance(); // changing the crossover chance affects the regeneration chance
    }
  }

  /// <summary>Gets or sets the chance of a member of a new generation undergoing a random mutation.</summary>
  /// <remarks>The chance of a new member of the population undergoing a slight mutation, as a
  /// probability from 0 to 1. The mutation introduces a small amount of new genetic material but also destroys a small
  /// amount of existing genetic material, so it should happen infrequently. A good value for this is between 0.0025
  /// and 0.025.
  /// </remarks>
  public double MutationChance
  {
    get { return _mutationChance; }
    set
    {
      if(value < 0 || value > 1) throw new ArgumentOutOfRangeException("MutationChance");
      _mutationChance = value;
    }
  }

  /// <summary>Gets or sets the number of members in the population. Changing this property will not affect searches in progress.</summary>
  public int PopulationSize
  {
    get { return _populationSize; }
    set
    {
      if(value < 2) throw new ArgumentOutOfRangeException("PopulationSize", "Population size must be at least 2.");
      _populationSize = value;
    }
  }

  /// <summary>Gets or sets the chance of a member of a new generation being generated randomly from scratch.</summary>
  /// <remarks>This property represents the chance of a member being regenerated from scratch, as a probability from 0
  /// to 1. The regeneration introduces a substantial amount of new genetic material into the population but also
  /// destroys a substantial amount of existing genetic material, and so it should happen only rarely, if at all.
  /// Regeneration only happens to individuals that do not experience crossover, but regardless you should set this
  /// parameter to the proportion of the total population that should be regenerated. (The value will be automatically
  /// adjusted based on the crossover chance to achieve this.) Of course, if the crossover chance is set to 1.0, no
  /// individuals will be regenerated regardless of the value of this parameter.
  /// </remarks>
  public double RegenerationChance
  {
    get { return _regenerationChance; }
    set
    {
      if(value < 0 || value > 1) throw new ArgumentOutOfRangeException("RegenerationChance");
      _regenerationChance = value;
      AdjustRegenChance();
    }
  }

  /// <summary>Begins a new search with a completely random initial population.</summary>
  public override Context BeginSearch()
  {
    Context context = new Context(CreateRandom(), PopulationSize);
    MakeInitialPopulation(context);
    return context;
  }

  /// <summary>Begins a new search with an initial population related to the given initial state.</summary>
  /// <param name="initialState">A state upon which the initial population will be based.</param>
  /// <remarks>The initial population will contain the initial state, as well as its successors (if the problem
  /// implements <see cref="IClimbingSearchable{S}"/>) and a number of its random mutations. The rest of the population
  /// will be filled out randomly.
  /// </remarks>
  public override Context BeginSearch(StateType initialState)
  {
    Context context = new Context(CreateRandom(), PopulationSize);
    MakeInitialPopulation(context, new StateValuePair<StateType>(initialState, Problem.EvaluateState(initialState)));
    return context;
  }

  /// <summary>Iterates the genetic algorithm search. Each iteration corresponds to one generation of the population.</summary>
  public override SearchResult Iterate(Context context)
  {
    if(context == null) throw new ArgumentNullException();

    // otherwise, generate a new population and calculate the new best solution
    StateValuePair<StateType>[] newPopulation = new StateValuePair<StateType>[context.population.Length];

    // ensure that the current best member is propogated to the next generation unchanged
    newPopulation[0]   = context.population[context.bestMember];
    context.bestMember = 0;

    float[] populationWeights = CalculatePopulationWeights(context);
    int parallelism = GetEffectiveParallelism();
    bool parallelized = parallelism > 1;
    Tasks.ParallelFor(1, context.population.Length, info => info.ThreadNumber == 0 ? context.Random : CreateRandom(),
                      (index,random,info) =>
    {
      bool tryMutation = true; // whether the new member has a chance of undergoing mutation
      if(random.NextDouble() < CrossoverChance) // if crossover is to be used, do the reproduction
      {
        int a = SelectRandomMember(populationWeights, random), b = SelectRandomMember(populationWeights, random);
        newPopulation[index] = Problem.Crossover(random, context.population[a], context.population[b]);
      }
      else if(random.NextDouble() < adjustedRegenChance) // if not, see if the member is to be regenerated randomly
      {
        newPopulation[index] = Problem.GetRandomState(random);
        tryMutation = false; // we shouldn't mutate members that are already completely random
      }
      else // otherwise, just copy a member from the previous generation
      {
        newPopulation[index] = context.population[SelectRandomMember(populationWeights, random)];
      }

      // potentially apply a random mutation
      if(tryMutation && random.NextDouble() < MutationChance)
      {
        newPopulation[index] = Problem.Mutate(random, newPopulation[index]);
      }

      // update 'bestMember' as necessary
      if(parallelized) Monitor.Enter(context);
      try
      {
        if(newPopulation[index].Value > newPopulation[context.bestMember].Value) context.bestMember = index;
      }
      finally
      {
        if(parallelized) Monitor.Exit(context);
      }
    }, parallelism);

    context.population = newPopulation; // switch to the new population
    // if the current best member is good enough, return success
    return Problem.IsGoal(context.Solution.State) ? SearchResult.Success : SearchResult.Pending;
  }

  /// <summary>Returns a member of a search's current population.</summary>
  public StateValuePair<StateType> GetMember(Context context, int index)
  {
    if(context == null) throw new ArgumentNullException();
    if((uint)index >= (uint)context.population.Length) throw new ArgumentOutOfRangeException();
    return context.population[index];
  }

  /// <summary>Gets whether the parallelism will be automatically employed by the <see cref="Search" /> methods to run multiple
  /// searches in parallel. This method has been overridden to return false, because parallelism is employed to update a single
  /// population rather than run multiple searches with multiple populations in parallel.
  /// </summary>
  protected sealed override bool UseAutomaticParallelism
  {
    get { return false; }
  }

  /// <include file="documentation.xml" path="/AI/Search/IIterativeSearch/SelectBestSolution/node()"/>
  protected override StateValuePair<StateType> SelectBestSolution(Context[] contexts)
  {
    throw new NotImplementedException();
  }

  /// <summary>Adds an initial population member at the given index, updating the best known member as necessary.</summary>
  void AddInitialMember(Context context, int index, StateValuePair<StateType> member)
  {
    if(index == 0 || member.Value > context.population[context.bestMember].Value) context.bestMember = index;
    context.population[index] = member;
  }

  /// <summary>Calculates an adjusted regeneration chance based on the crossover chance.</summary>
  /// <remarks>Since regeneration can only happen to members of a new generation that don't undergo crossover, the
  /// chance of a member being regenerated is dependent upon the crossover chance. So if we want 1% of the population
  /// to be regenerated each round, but the crossover chance is 90%, then we need the adjusted regeneration chance to
  /// be 10%, so that the appropriate number of members of the total population will end up being regenerated.
  /// </remarks>
  void AdjustRegenChance()
  {
    double nonCrossoverChance = 1-CrossoverChance;
    adjustedRegenChance = nonCrossoverChance == 0 ? 0 : _regenerationChance/nonCrossoverChance;
  }

  /// <summary>Calculates weights from 0 to 1 for each member of the population based on how fit they are compared to
  /// the average. The weights can be taken as a probability that the member should be selected. The sum of all the
  /// weights should equal 1.0.
  /// </summary>
  float[] CalculatePopulationWeights(Context context)
  {
    // the weight for each member will be equal to the percentage that they contribute to the total. that is, the
    // weight for a member with fitness value X will be equal to X/total.

    // calculate the total fitness of the entire population
    double total = 0;
    StateValuePair<StateType>[] population = context.population;
    for(int i=0; i<population.Length; i++) total += population[i].Value;

    double inverseTotal = 1/total; // invert the total so we can do multiplication rather than division (faster)
    float[] weights = new float[population.Length];
    for(int i=0; i<weights.Length; i++) weights[i] = (float)(population[i].Value * inverseTotal);

    return weights;
  }

  /// <summary>Creates an initial population containing random members.</summary>
  void MakeInitialPopulation(Context context)
  {
    for(int i=0; i<context.population.Length; i++) AddInitialMember(context, i, Problem.GetRandomState(context.Random));
  }

  /// <summary>Creates an initial population containing the given state, related states, and possibly random states.</summary>
  void MakeInitialPopulation(Context context, StateValuePair<StateType> initialState)
  {
    // add the initial state to the population (to the 0th index)
    AddInitialMember(context, 0, initialState);

    int index = 1;

    // add successors to the population if the problem supports that
    IClimbingSearchable<StateType> climbable = Problem as IClimbingSearchable<StateType>;
    if(climbable != null)
    {
      List<StateValuePair<StateType>> successors =
        new List<StateValuePair<StateType>>(climbable.GetSuccessors(initialState));

      // if there are more successors than the population size, permute them to get a random sample
      if(successors.Count > context.population.Length-1) Permutations.RandomlyPermute(successors, context.Random);

      for(int end=Math.Min(successors.Count+1, context.population.Length); index<end; index++)
      {
        AddInitialMember(context, index, successors[index-1]);
      }
    }

    // fill half of the remaining space with mutations of the initial state
    for(int end=index + (context.population.Length-index)/2; index<end; index++)
    {
      AddInitialMember(context, index, Problem.Mutate(context.Random, context.population[0]));
    }

    // fill the rest with random states
    while(index++ < context.population.Length) AddInitialMember(context, index, Problem.GetRandomState(context.Random));
  }

  int _populationSize;
  double _crossoverChance, _regenerationChance, _mutationChance, adjustedRegenChance;

  /// <summary>Given a list of population member weights, returns a random member of the population with a likelihood
  /// proportional to its weight.
  /// </summary>
  static int SelectRandomMember(float[] populationWeights, RandomNumberGenerator random)
  {
    // select a random index, and a random weight (which determines how far we'll travel from the index)
    float totalWeight = (float)random.NextDouble();
    int index = random.Next(populationWeights.Length);

    while(totalWeight > 0)
    {
      totalWeight -= populationWeights[index];
      if(++index == populationWeights.Length) index = 0;
    }

    return index;
  }
}
#endregion

#region HillClimbingSearch
/// <summary>Implements a random restart hill-climbing search algorithm.</summary>
/// <remarks>A hill-climbing search works by simply examining all successors of the current state and moving to the
/// successor with the highest fitness value, if it is better than the current state. If there are multiple successors
/// tied for the highest fitness value, it chooses one at random. Of course, such a search will get stuck on local
/// maxima, and cannot navigate ridges in the search space. To improve the algorithm, sideways moves are allowed,
/// where the search can move to successors of equal value in hopes that the flat region it is currently on turns out
/// to be a ridge rather than a plateau. In addition, when the search has reached a local maximum, if the state is not
/// good enough, it can be restarted from a random position in the search space. With these improvements, hill-climbing
/// becomes capable of finding relatively good solutions to many problems in a reasonable amount of time.
/// </remarks>
/// <include file="documentation.xml" path="/AI/Search/typeparam[@name='StateType']"/>
public class HillClimbingSearch<StateType>
  : ClimbingSearchBase<StateType, HillClimbingSearch<StateType>.Context>
{
  /// <summary>Initializes a new <see cref="HillClimbingSearch{S}"/> with up to 50 sideways moves, and no limit to the
  /// number of random restarts allowed.
  /// </summary>
  /// <include file="documentation.xml" path="/AI/Search/HillClimbingSearch/*[@name != 'maxSidewaysMoves']"/>
  public HillClimbingSearch(IClimbingSearchable<StateType> problem) : this(problem, 50, Infinite) { }

  /// <summary>Initializes a new <see cref="HillClimbingSearch{S}"/> with the given maximum number of sideways moves,
  /// and no limit to the number of random restarts allowed.
  /// </summary>
  /// <include file="documentation.xml" path="/AI/Search/HillClimbingSearch/node()"/>
  public HillClimbingSearch(IClimbingSearchable<StateType> problem, int maxSidewaysMoves)
    : this(problem, maxSidewaysMoves, Infinite) { }

  /// <summary>Initializes a new <see cref="HillClimbingSearch{S}"/>.</summary>
  /// <include file="documentation.xml" path="/AI/Search/HillClimbingSearch/node()"/>
  /// <param name="maxRestarts">The initial value of <see cref="MaxRestarts"/>.</param>
  public HillClimbingSearch(IClimbingSearchable<StateType> problem, int maxSidewaysMoves, int maxRestarts) : base(problem)
  {
    MaxSidewaysMoves = maxSidewaysMoves;
    MaxRestarts      = maxRestarts;
  }

  #region Context
  /// <summary>Contains the context for a hill climbing search.</summary>
  public sealed class Context : LocalSearchContextBase
  {
    internal Context(RandomNumberGenerator random, StateValuePair<StateType> initialSolution,
                     int maxRestarts, int maxSidewaysMoves) : base(random)
    {
      solution         = initialSolution;
      MaxRestarts      = maxRestarts;
      MaxSidewaysMoves = maxSidewaysMoves;
    }

    /// <summary>Gets the current best solution.</summary>
    public override StateValuePair<StateType> Solution
    {
      get { return solution; }
    }

    /// <summary>Temporary storage for the successors being considered during the search.</summary>
    internal List<StateValuePair<StateType>> bestSuccessors = new List<StateValuePair<StateType>>();
    internal readonly int MaxRestarts, MaxSidewaysMoves;
    /// <summary>The number of restarts performed so far during this search.</summary>
    internal int numRestarts;
    /// <summary>The number of sideways moves performed since the last restart.</summary>
    internal int numSidewaysMoves;
    internal StateValuePair<StateType> solution;
  }
  #endregion

  /// <summary>Gets or sets the maximum number of sideways moves allowed. A sideways move is a move from a state to a
  /// successor with an identical fitness value. Such a move is only considered when no successor has a higher fitness
  /// value, and allowing some number of sideways moves can help the search escape from a ridge in the search space. A
  /// good value is typically 50, or 100, or so. Changing this property has no effect on searches that have already been started.
  /// </summary>
  public int MaxSidewaysMoves
  {
    get { return _maxSidewaysMoves; }
    set
    {
      if(value < 0) throw new ArgumentOutOfRangeException();
      _maxSidewaysMoves = value;
    }
  }

  /// <summary>Gets or sets the maximum number of random restarts that are allowed during the search. If nonzero, the search will
  /// restart from a random state when no further progress is being made up to the maximum number of allowed restarts, before
  /// giving up. If <see cref="SearchBase.Infinite"/> is passed, the search will restart as many times as is necessary to find a
  /// goal state. Changing this property has no effect on searches that have already been started.
  /// </summary>
  public int MaxRestarts
  {
    get { return _maxRestarts; }
    set
    {
      if(_maxRestarts < Infinite) throw new ArgumentOutOfRangeException();
      _maxRestarts = value;
    }
  }

  /// <include file="documentation.xml" path="/AI/Search/IIterativeSearch/BeginSearch/node()"/>
  public override Context BeginSearch()
  {
    RandomNumberGenerator random = CreateRandom();
    return new Context(random, Problem.GetRandomState(random), MaxRestarts, MaxSidewaysMoves);
  }

  /// <include file="documentation.xml" path="/AI/Search/IIterativeSearch/BeginSearch_State/node()"/>
  public override Context BeginSearch(StateType initialState)
  {
    StateValuePair<StateType> statePair = new StateValuePair<StateType>(initialState, Problem.EvaluateState(initialState));
    return new Context(CreateRandom(), statePair, MaxRestarts, MaxSidewaysMoves);
  }

  /// <include file="documentation.xml" path="/AI/Search/IIterativeSearch/Iterate/node()"/>
  public override SearchResult Iterate(Context context)
  {
    if(context == null) throw new ArgumentNullException();

    // if what we have is good enough, return success
    if(Problem.IsGoal(context.Solution.State)) return SearchResult.Success;

    // otherwise, we want to choose randomly from the best successors
    List<StateValuePair<StateType>> bestSuccessors = context.bestSuccessors;
    foreach(StateValuePair<StateType> successor in Problem.GetSuccessors(context.solution))
    {
      if(bestSuccessors.Count == 0 || successor.Value > bestSuccessors[0].Value) // if we've found a better successor
      {
        bestSuccessors.Clear(); // clear the current list of best successors
        bestSuccessors.Add(successor); // add the new one
      }
      else if(bestSuccessors[0].Value == successor.Value)
      {
        bestSuccessors.Add(successor); // otherwise, we've found another successor tied with the best, so add it
      }
    }

    // choose the successor randomly from the list
    StateValuePair<StateType>? bestSuccessor =
      bestSuccessors.Count == 0 ? (StateValuePair<StateType>?)null : bestSuccessors.SelectRandom(context.Random);
    bestSuccessors.Clear();

    // if there is no successor, or the best successor is worse than the current state, or it's a sideways move and
    // we've reached our limit, then we can't make any more progress, so try to restart the search.
    if(!bestSuccessor.HasValue || bestSuccessor.Value.Value < context.solution.Value ||
       bestSuccessor.Value.Value == context.solution.Value && context.numSidewaysMoves++ == context.MaxSidewaysMoves)
    {
      // if we can't restart, we're finished.
      if(_maxRestarts != Infinite && context.numRestarts == context.MaxRestarts) return SearchResult.LimitReached;

      // we can restart, so do so.
      context.numSidewaysMoves = 0;
      context.numRestarts++; // increment this here instead of in the 'if' statement above to prevent the it from overflowing
      context.solution = Problem.GetRandomState(context.Random);  // if Iterate() is called repeatedly after the search has ended
    }
    else
    {
      // here, the best successor is better than or equal to what we've got, so move to it
      context.solution = bestSuccessor.Value;
    }

    return SearchResult.Pending;
  }

  int _maxSidewaysMoves, _maxRestarts;
}
#endregion

#region SimulatedAnnealingSearch
#region SimulatedAnnealingSearch
/// <summary>Provides helper functions for using <see cref="SimulatedAnnealingSearch{S}"/>.</summary>
public static class SimulatedAnnealingSearch
{
  /// <summary>Given a zero-based interation number, returns the annealing temperature for that iteration.</summary>
  public delegate float Schedule(int iteration);
  /// <summary>Given a state fitness value and the best known value, returns a new fitness value for the state.</summary>
  /// <returns>Tunneling functions attempt to warp the search space dynamically based on the best known value to help
  /// the search escape from local maxima that are less desirable than the best known value.
  /// </returns>
  /// <remarks><include file="documentation.xml" path="/AI/Search/SimulatedAnnealing/Tunneling/node()"/></remarks>
  public delegate float Tunneler(float value, float bestKnownValue);

  /// <summary>Given an annealing schedule <c>f(x)</c>, returns a new annealing schedule that alternates between
  /// <c>f(x)</c> and <c>f(x) * <paramref name="factor"/></c>. The switch occurs every <paramref name="periodLength"/>
  /// iterations.
  /// </summary>
  /// <param name="schedule">A schedule that produces the base temperature, which will be alternated with a reduced
  /// temperature.
  /// </param>
  /// <param name="factor">A factor that will multiplied with the base temperature to produce the reduced
  /// temperature. For instance, a value of 0.5 will reduce the base temperature to half.
  /// </param>
  /// <param name="periodLength">How long each period lasts. After this number of iterations, the temperature will
  /// toggle from low to high or high to low.
  /// </param>
  public static Schedule MakeAlternatingSchedule(Schedule schedule, float factor, int periodLength)
  {
    if(schedule == null) throw new ArgumentNullException();
    if(periodLength < 1) throw new ArgumentOutOfRangeException();
    return delegate(int i)
    {
      float baseTemperature = schedule(i);
      return (i/periodLength & 1) == 0 ? baseTemperature : baseTemperature*factor;
    };
  }

  /// <summary>Returns an annealing schedule that lowers the temperature exponentially from
  /// <paramref name="startTemperature"/> to <paramref name="endTemperature"/> over the given number of iterations,
  /// and then reduces it to zero thereafter.
  /// </summary>
  public static Schedule MakeExponentialSchedule(double startTemperature, double endTemperature, int iterations)
  {
    if(startTemperature <= 0 || endTemperature <= 0 || iterations < 1) throw new ArgumentOutOfRangeException();

    // we'll use the equation start * e^(iterations*x) == end to calculate a dropoff factor x such that the temperature
    // that drops exponentially from start to end over the given number of iterations. we can solve the equation as
    // x = ln(end/start) / iterations
    double dropoffFactor = Math.Log(endTemperature / startTemperature) / iterations;
    return delegate(int i) { return i >= iterations ? 0 : (float)(startTemperature * Math.Exp(dropoffFactor * i)); };
  }

  /// <summary>Returns an annealing schedule that lowers the temperature in steps from
  /// <paramref name="startTemperature"/> to <paramref name="endTemperature"/> with the temperature being multiplied
  /// by <paramref name="stepFactor"/> at each step transition. Each step lasts <paramref name="iterationsPerStep"/>
  /// iterations.
  /// </summary>
  public static Schedule MakeSteppedSchedule(double startTemperature, double endTemperature, double stepFactor,
                                             int iterationsPerStep)
  {
    if(startTemperature <= 0 || endTemperature <= 0 || stepFactor < 0 || stepFactor >= 1 || iterationsPerStep < 1)
    {
      throw new ArgumentOutOfRangeException();
    }

    double iterationMul = 1.0 / iterationsPerStep;
    return delegate(int i)
    {
      double temperature = Math.Pow(stepFactor, i*iterationMul) * startTemperature;
      return temperature < endTemperature ? 0 : (float)temperature;
    };
  }

  /// <summary>Returns a STUN tunneler with a single tuning parameter for both amplification and compression.</summary>
  /// <remarks>
  /// <include file="documentation.xml" path="/AI/Search/SimulatedAnnealing/Tunneling/node()"/>
  /// <include file="documentation.xml" path="/AI/Search/SimulatedAnnealing/STUN/node()"/>
  /// </remarks>
  public static Tunneler MakeStunTunneler(double gamma)
  {
    if(gamma <= 0) throw new ArgumentOutOfRangeException();
    return delegate(float value, float bestKnown) { return (float)Math.Exp(gamma * (value-bestKnown)) - 1; };
  }

  /// <summary>Returns a STUN tunneler with separate tuning parameters for amplification and compression.</summary>
  /// <remarks>
  /// <include file="documentation.xml" path="/AI/Search/SimulatedAnnealing/Tunneling/node()"/>
  /// <include file="documentation.xml" path="/AI/Search/SimulatedAnnealing/STUN/node()"/>
  /// </remarks>
  public static Tunneler MakeStunTunneler(double amplification, double compression)
  {
    if(amplification <= 0 || compression <= 0) throw new ArgumentOutOfRangeException();
    return delegate(float value, float bestKnown)
    {
      float difference = value - bestKnown;
      return (float)Math.Exp((difference < 0 ? compression : amplification) * difference) - 1;
    };
  }
}
#endregion

#region SimulatedAnnealingSearch<StateType>
/// <summary>Implements a simulated annealing search.</summary>
/// <remarks>
/// <para>
/// Simulated annealing is a search method that works by analogy with the annealing process in metallurgy, where metal
/// is heated to a high temperature to break bonds between atoms (which are at local minima of energy), and slowly
/// cooled in hopes that the atoms will settle into local minima lower than before.
/// </para>
/// <para>
/// Like other local searches, simulated annealing attempts to find an optimal state within a state space by maximizing
/// (or minimizing) an objective function that represents the fitness of the state. The annealing process is controlled
/// by its temperature, which is slowly decreased by the annealing schedule. At higher temperatures, the chance of
/// allowing downhill moves (moves to a state of lower fitness) is relatively high, allowing the search to wander
/// across larger parts of the state space. As the temperature is decreased, the search becomes less likely to make
/// downhill moves. Other annealing schedules alternate between low and high temperatures, effectively alternating
/// between hill-climbing and wandering stages. The result of the search is the local maxima above the state with the
/// highest fitness found so far.
/// </para>
/// <para>
/// On each iteration, the simulated annealing search chooses a single random successor of the current state, using
/// <see cref="ILocallySearchable{S}.GetRandomSuccessor"/>. If the successor has a higher fitness, it becomes the new
/// current state. Otherwise, it is accepted with a probability proportional to the temperature. Because simulated
/// annealing does not require all successors of a state to be generated, it can be applied directly to continuous
/// search spaces, as long as quenching is disabled, as quenching does require all successors to be generated.
/// (Quenching is a feature that performs a hill-climbing search on on the final solution to maximize it before it's
/// returned, if a goal state could not be reached by the simulated annealing. It only works if the problem implements
/// <cref see="IClimbingSearchable{S}"/>.)
/// </para>
/// <para>
/// Because of the large amount of randomness involved in simulated annealing search, it may take much longer to find
/// exact solutions to problems than other kinds of search. It is best suited to problems where an approximate solution
/// is acceptable.
/// </para>
/// </remarks>
/// <include file="documentation.xml" path="/AI/Search/typeparam[@name='StateType']"/>
public class SimulatedAnnealingSearch<StateType>
  : LocalSearchBase<StateType,ILocallySearchable<StateType>,SimulatedAnnealingSearch<StateType>.Context>
{
  /// <summary>Initializes a new simulated annealing search with the given starting temperature and number of
  /// iterations, with tunneling disabled and quenching enabled.
  /// </summary>
  /// <include file="documentation.xml" path="/AI/Search/SimulatedAnnealing/ConstructorI/node()"/>
  public SimulatedAnnealingSearch(ILocallySearchable<StateType> problem, double startTemperature, int iterations)
    : this(problem,
           SimulatedAnnealingSearch.MakeExponentialSchedule(startTemperature, startTemperature*0.02, iterations)) { }

  /// <include file="documentation.xml" path="/AI/Search/SimulatedAnnealing/ConstructorI/node()"/>
  /// <include file="documentation.xml" path="/AI/Search/SimulatedAnnealing/Constructor/param[@name='quench']"/>
  public SimulatedAnnealingSearch(ILocallySearchable<StateType> problem, double startTemperature, int iterations,
                                  bool quench)
    : this(problem,
           SimulatedAnnealingSearch.MakeExponentialSchedule(startTemperature, startTemperature*0.02, iterations),
           null, quench) { }

  /// <summary>Initializes a new simulated annealing search with the given schedule, with tunneling disabled and
  /// quenching enabled.
  /// </summary>
  /// <include file="documentation.xml" path="/AI/Search/SimulatedAnnealing/ConstructorD/*[@name != 'tunneler']"/>
  public SimulatedAnnealingSearch(ILocallySearchable<StateType> problem, SimulatedAnnealingSearch.Schedule schedule)
    : this(problem, schedule, null, true) { }

  /// <summary>Initializes a new simulated annealing search with the given schedule and tunneler, with quenching
  /// enabled.
  /// </summary>
  /// <include file="documentation.xml" path="/AI/Search/SimulatedAnnealing/ConstructorD/node()"/>
  public SimulatedAnnealingSearch(ILocallySearchable<StateType> problem, SimulatedAnnealingSearch.Schedule schedule,
                                  SimulatedAnnealingSearch.Tunneler tunneler)
    : this(problem, schedule, tunneler, true) { }

  /// <summary>Initializes a simulated annealing search.</summary>
  /// <include file="documentation.xml" path="/AI/Search/SimulatedAnnealing/ConstructorD/node()"/>
  /// <include file="documentation.xml" path="/AI/Search/SimulatedAnnealing/Constructor/param[@name='quench']"/>
  public SimulatedAnnealingSearch(ILocallySearchable<StateType> problem, SimulatedAnnealingSearch.Schedule schedule,
                                  SimulatedAnnealingSearch.Tunneler tunneler, bool quench) : base(problem)
  {
    if(schedule == null) throw new ArgumentNullException();
    Schedule = schedule;
    Tunneler = tunneler;
    Quench   = quench;
  }

  #region Context
  /// <summary>Contains the context for a simulated annealing search.</summary>
  public sealed class Context : LocalSearchContextBase
  {
    internal Context(RandomNumberGenerator random, SimulatedAnnealingSearch.Schedule schedule,
                     SimulatedAnnealingSearch.Tunneler tunneler, StateValuePair<StateType> initialSolution, bool quench)
      : base(random)
    {
      Schedule           = schedule;
      Tunneler           = tunneler;
      Quench             = quench;
      solution           = bestSolution = initialSolution;
      bestPreStunValue   = bestSolution.Value;
      bestSolution.Value = TunnelValue(bestSolution.Value);
    }

    /// <summary>Gets the current best solution.</summary>
    public override StateValuePair<StateType> Solution
    {
      get { return solution; }
    }

    /// <summary>Retrieves the fitness value adjusted by the tunneling technique.</summary>
    internal float TunnelValue(float value)
    {
      return Tunneler == null ? value : Tunneler(value, bestPreStunValue);
    }

    internal readonly SimulatedAnnealingSearch.Schedule Schedule;
    internal readonly SimulatedAnnealingSearch.Tunneler Tunneler;
    internal HillClimbingSearch<StateType> quencher;
    internal HillClimbingSearch<StateType>.Context quenchContext;
    internal float bestPreStunValue;
    internal int iteration;
    internal StateValuePair<StateType> solution, bestSolution;
    internal readonly bool Quench;
  }
  #endregion

  /// <summary>Gets or sets whether a hill-climbing algorithm should be run on the final solution to maximize it before
  /// it's returned, if a goal state could not be reached by the simulated annealing. In order for quenching to work,
  /// the problem must implement <cref see="IClimbingSearchable{S}"/>. Changing this property will not affect searches that have
  /// already been started.
  /// </summary>
  public bool Quench { get; set; }

  /// <summary>Gets or sets the <see cref="SimulatedAnnealingSearch.Schedule">scheduling method</see> that determines the
  /// temperature of the annealing process, as a function of the iteration number. Changing this property will not affect
  /// searches that have already been started.
  /// </summary>
  public SimulatedAnnealingSearch.Schedule Schedule
  {
    get { return _schedule; }
    set
    {
      if(value == null) throw new ArgumentNullException();
      _schedule = value;
    }
  }

  /// <summary>Gets or sets the <see cref="SimulatedAnnealingSearch.Tunneler">tunnel method</see> that warps the
  /// fitness space given knowledge about the highest maximum found so far. If this is null, tunneling will be disabled.
  /// Changing this property will not affect searches that have already been started.
  /// </summary>
  public SimulatedAnnealingSearch.Tunneler Tunneler { get; set; }

  /// <include file="documentation.xml" path="/AI/Search/IIterativeSearch/BeginSearch/node()"/>
  public override SimulatedAnnealingSearch<StateType>.Context BeginSearch()
  {
    RandomNumberGenerator random = CreateRandom();
    return new Context(random, Schedule, Tunneler, Problem.GetRandomState(random), Quench);
  }

  /// <include file="documentation.xml" path="/AI/Search/IIterativeSearch/BeginSearch_State/node()"/>
  public override Context BeginSearch(StateType initialState)
  {
    StateValuePair<StateType> initialSolution = new StateValuePair<StateType>(initialState, Problem.EvaluateState(initialState));
    return new Context(CreateRandom(), Schedule, Tunneler, initialSolution, Quench);
  }

  /// <include file="documentation.xml" path="/AI/Search/IIterativeSearch/Iterate/node()"/>
  public override SearchResult Iterate(Context context)
  {
    if(context == null) throw new ArgumentNullException();

    // if we're quenching the final solution, do that
    if(context.quencher != null)
    {
      SearchResult result = context.quencher.Iterate(context.quenchContext);
      context.solution = context.quenchContext.solution;
      return result;
    }

    // if the current state is good enough, return success
    if(Problem.IsGoal(context.Solution.State))
    {
      if(context.Tunneler != null) context.solution.Value = context.bestPreStunValue; // undo the tunneling operation
      return SearchResult.Success;
    }

    float temperature = context.Schedule(context.iteration++);

    if(temperature <= 0) // if the temperature drops to zero, we're done.
    {
      // return the best solution we've found so far
      context.solution = context.bestSolution;
      if(context.Tunneler != null) context.solution.Value = context.bestPreStunValue; // undo the tunneling operation

      // the solution is not "good enough" (otherwise it would have been returned already), so attempt to quench it if
      // that's enabled
      if(context.Quench)
      {
        IClimbingSearchable<StateType> climbable = Problem as IClimbingSearchable<StateType>;
        if(climbable != null) // if the problem supports climbing, run a hill climber on it
        {
          context.quencher = new HillClimbingSearch<StateType>(climbable, 0, 0);
          context.quenchContext = context.quencher.BeginSearch(context.Solution.State);
          SearchResult result = context.quencher.Iterate(context.quenchContext);
          context.solution = context.quenchContext.solution;
          return result;
        }
      }

      // the solution can't be quenched, so just return what we've got
      return SearchResult.LimitReached;
    }

    StateValuePair<StateType> next =
      Problem.GetRandomSuccessor(context.Random, context.Tunneler == null ?
        context.solution : new StateValuePair<StateType>(context.solution.State, context.bestPreStunValue));

    float nextValue = context.TunnelValue(next.Value), valueDelta = nextValue - context.solution.Value;

    // always accept uphill moves and accept downhill moves with a probability that depends on the temperature
    if(valueDelta > 0 || context.Random.NextDouble() < Math.Exp(valueDelta / temperature))
    {
      context.solution = next;
      if(nextValue > context.bestSolution.Value)
      {
        context.bestSolution = next;
        // since the best solution is changing, the tunneling-adjusted value will change, so recalculate it
        if(context.Tunneler != null)
        {
          context.bestPreStunValue   = next.Value;
          context.bestSolution.Value = context.TunnelValue(next.Value);
        }
      }
    }

    return SearchResult.Pending;
  }

  SimulatedAnnealingSearch.Schedule _schedule;
}
#endregion
#endregion
#endregion

} // namespace AdamMil.AI.Search.Local
