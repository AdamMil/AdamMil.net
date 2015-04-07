using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Runtime.Serialization;

// TODO: remove this and replace calling code with the task parallel library when we upgrade to .NET 4

/*System.InvalidOperationException: Operation is not valid due to the current state of the object.

   at UserQuery.Task.SignalCompletion() in d:\AdamMil\temp\i21vn51c.0.cs:line 260
   at UserQuery.CompositeTask.task_Completed(Object sender, EventArgs e) in d:\AdamMil\temp\i21vn51c.0.cs:line 351
   at UserQuery.Task.SignalCompletion() in d:\AdamMil\temp\i21vn51c.0.cs:line 265
   at UserQuery.WorkItemTask.ThreadFunc() in d:\AdamMil\temp\i21vn51c.0.cs:line 396
   at System.Threading.ThreadHelper.ThreadStart_Context(Object state)
   at System.Threading.ExecutionContext.Run(ExecutionContext executionContext, ContextCallback callback, Object state, Boolean ignoreSyncCtx)
   at System.Threading.ExecutionContext.Run(ExecutionContext executionContext, ContextCallback callback, Object state)
   at System.Threading.ThreadHelper.ThreadStart()
 */

namespace AdamMil.Utilities
{

#region CompositeException
/// <summary>An exception that aggregates several other exceptions, used when an operation that can throw multiple exceptions is
/// used within a context where only a single exception can be thrown.
/// </summary>
[Serializable]
public class CompositeException : ArgumentException
{
  /// <summary>Initializes a new <see cref="CompositeException"/>.</summary>
  public CompositeException() : this(new Exception[0]) { }
  /// <summary>Initializes a new <see cref="CompositeException"/>.</summary>
  public CompositeException(IEnumerable<Exception> exceptions) : this(exceptions, "Multiple exceptions may have occurred.") { }
  /// <summary>Initializes a new <see cref="CompositeException"/>.</summary>
  public CompositeException(IEnumerable<Exception> exceptions, string message) : base(message)
  {
    if(exceptions == null) throw new ArgumentNullException();
    Exceptions = new System.Collections.ObjectModel.ReadOnlyCollection<Exception>(exceptions.ToArray());
  }

  /// <summary>Initializes a new <see cref="CompositeException"/>.</summary>
  public CompositeException(SerializationInfo info, StreamingContext context) : base(info, context) { }

  /// <summary>Gets the exceptions contained in this exception.</summary>
  public System.Collections.ObjectModel.ReadOnlyCollection<Exception> Exceptions { get; private set; }
}
#endregion

#region TaskCancellationEvent
/// <summary>Represents a task cancellation, and is used to signal multiple tasks of a task cancellation.</summary>
public sealed class TaskCancellationEvent
{
  /// <summary>Gets whether the tasks using this <see cref="TaskCancellationEvent"/> should cancel their operation.</summary>
  public bool WasCanceled { get; private set; }

  /// <summary>Cancels all tasks using this <see cref="TaskCancellationEvent"/>.</summary>
  public void Cancel()
  {
    WasCanceled = true;
  }
}
#endregion

#region TaskDelegate
/// <summary>A delegate used to specify the work to be done by a <see cref="Task"/>.</summary>
public delegate void TaskDelegate(Task taskInfo);

/// <summary>A delegate used to specify the work to be done by a <see cref="Task"/>. The task should return its result.</summary>
public delegate T TaskDelegate<T>(Task taskInfo);
#endregion

#region Task
/// <summary>Represents a unit of work that can be started, canceled, and aborted.</summary>
public abstract class Task : IDisposable
{
  /// <summary>Initializes a new <see cref="Task"/> with an optional <see cref="TaskCancellationEvent"/> allowing the task to be
  /// canceled along with other tasks that share the same cancellation event.
  /// </summary>
  protected Task(TaskCancellationEvent cancellationEvent)
  {
    this.cancellationEvent = cancellationEvent;
  }

  /// <include file="documentation.xml" path="/Utilities/Task/Dispose/node()"/>
  ~Task()
  {
    Dispose(false);
  }

  /// <summary>Raised when the task is complete. This includes cancellation and abortion.</summary>
  public event EventHandler Completed;

  /// <summary>Gets the unhandled exception that occurred when running the task, if any.</summary>
  public Exception Exception { get; protected set; }

  /// <summary>Gets whether the task is completed. This includes cancellation and abortion.</summary>
  public bool IsComplete { get; private set; }

  /// <summary>Gets a reference to a handle that is signaled upon completion of the task. This includes cancellation and
  /// abortion.
  /// </summary>
  public WaitHandle WaitHandle
  {
    get
    {
      if(completionEvent == null) throw new InvalidOperationException();
      return completionEvent;
    }
  }

  /// <summary>Gets whether the task was canceled, either by a direct call to <see cref="Cancel"/> or by a call to the
  /// <see cref="TaskCancellationEvent.Cancel"/> method of the task's <see cref="TaskCancellationEvent"/>.
  /// </summary>
  public bool WasCanceled
  {
    get { return canceled || cancellationEvent != null && cancellationEvent.WasCanceled; }
  }

  /// <summary>Aborts execution of the task, if it's currently running. This will cause the completion event to become signaled.
  /// An exception will be thrown if the task has not been started.
  /// </summary>
  public void Abort()
  {
    if(completionEvent == null) throw new InvalidOperationException();
    if(!Finish(0))
    {
      AbortCore();
      SignalCompletion();
    }
  }

  /// <summary>Informs the work item that it should immediately stop working. If the task was initialized with a
  /// <see cref="TaskCancellationEvent"/>, its <see cref="TaskCancellationEvent.Cancel"/> method will not be called. This method
  /// can be called at any time, even before the task has started. The work item is not immediately aborted by calling this
  /// method, and if it fails to check the task's cancellation state, it may continue indefinitely. As a consequence, the
  /// completion event is not signaled by this method, either. As a last resort, <see cref="Abort"/> may be called to stop the
  /// work item.
  /// </summary>
  public void Cancel()
  {
    CancelCore();
    canceled = true;
  }

  /// <include file="documentation.xml" path="/Utilities/Task/Dispose/node()"/>
  public void Dispose()
  {
    Dispose(true);
    GC.SuppressFinalize(this);
  }

  /// <summary>Waits for the task to finish. An exception will be thrown if the task has not been started.</summary>
  public void Finish()
  {
    Finish(Timeout.Infinite);
  }

  /// <summary>Waits for the task to finish. Returns true if the task completed within the given time limit, and false if not.
  /// <see cref="Timeout.Infinite"/> can be passed to specify that there should be no time limit. An exception will be thrown if
  /// the task has not been started. If an exception occurred during the task execution, it will be rethrown by this method.
  /// </summary>
  public bool Finish(int timeoutMs)
  {
    if(completionEvent == null) throw new InvalidOperationException();

    if(IsComplete || completionEvent.WaitOne(timeoutMs))
    {
      if(Exception != null) throw Exception;
      return true;
    }
    else
    {
      return false;
    }
  }

  /// <summary>Starts the task and runs it to completion. An exception will be thrown if the task was already started, or if an
  /// exception occurs during the task.
  /// </summary>
  public virtual void Run()
  {
    if(started) throw new InvalidOperationException();

    completionEvent = new ManualResetEvent(false);
    started = true;
    if(WasCanceled) // if the task was canceled already, just set the completion event
    {
      SignalCompletion();
    }
    else
    {
      try
      {
        RunCore();
      }
      catch(Exception ex)
      {
        Exception = ex;
        throw;
      }
      finally
      {
        SignalCompletion();
      }
    }
  }

  /// <summary>Starts running the task. An exception will be thrown if the task was already started.</summary>
  public void Start()
  {
    if(started) throw new InvalidOperationException();

    completionEvent = new ManualResetEvent(false);
    if(WasCanceled) // if the task was canceled already, just set the completion event
    {
      started = true;
      SignalCompletion();
    }
    else
    {
      StartCore();
      started = true;
    }
  }

  /// <include file="documentation.xml" path="/Utilities/Task/AbortCore/node()"/>
  protected abstract void AbortCore();

  /// <include file="documentation.xml" path="/Utilities/Task/CancelCore/node()"/>
  protected virtual void CancelCore() { }

  /// <include file="documentation.xml" path="/Utilities/Task/RunCore/node()"/>
  protected abstract void RunCore();

  /// <include file="documentation.xml" path="/Utilities/Task/StartCore/node()"/>
  protected abstract void StartCore();

  /// <summary>Called to indicate that the task has completed.</summary>
  protected void SignalCompletion()
  {
    if(!started) throw new InvalidOperationException();
    if(!IsComplete)
    {
      ManualResetEvent completionEvent = this.completionEvent; // prevent it from being changed by another thread
      if(completionEvent != null) completionEvent.Set();
      IsComplete = true;
      if(Completed != null) Completed(this, EventArgs.Empty);
    }
  }

  /// <include file="documentation.xml" path="/Utilities/Task/Dispose/node()"/>
  protected virtual void Dispose(bool manualDispose)
  {
    if(completionEvent != null)
    {
      try { Abort(); }
      catch { }
      Utility.Dispose(ref completionEvent);
    }
  }

  TaskCancellationEvent cancellationEvent;
  ManualResetEvent completionEvent;
  bool canceled, started;
}
#endregion

#region Task<T>
/// <summary>Represents a task that computes a result.</summary>
public abstract class Task<T> : Task
{
  /// <summary>Initializes a new <see cref="Task{T}"/> with an optional <see cref="TaskCancellationEvent"/> allowing the task to
  /// be canceled along with other tasks that share the same cancellation event.
  /// </summary>
  protected Task(TaskCancellationEvent cancellationEvent) : base(cancellationEvent) { }

  /// <summary>Gets whether the task produced a result that was stored in the <see cref="Result"/> property.</summary>
  public bool HasResult
  {
    get { return _hasResult; }
  }

  /// <summary>Gets the result of the task. The value will only be valid if <see cref="HasResult"/> is true.</summary>
  public T Result
  {
    get { return _result; }
    protected set
    {
      _result = value;
      _hasResult = true;
    }
  }

  T _result;
  bool _hasResult;
}
#endregion

// TODO: aggregate exceptions that occur in a composite task
#region CompositeTask
/// <summary>Represents a task that is the combined work of several other tasks.</summary>
public class CompositeTask : Task
{
  /// <summary>Initializes a new <see cref="CompositeTask"/> with the set of other tasks to perform.</summary>
  public CompositeTask(IEnumerable<Task> tasks) : this(tasks, null) { }

  /// <summary>Initializes a new <see cref="CompositeTask"/> with the set of other tasks to perform and an optional
  /// <see cref="TaskCancellationEvent"/> allowing the task to be canceled along with other tasks that share the same
  /// <see cref="TaskCancellationEvent"/>.
  /// </summary>
  public CompositeTask(IEnumerable<Task> tasks, TaskCancellationEvent cancellationEvent) : base(cancellationEvent)
  {
    if(tasks == null) throw new ArgumentNullException();

    List<Task> taskList = new List<Task>();
    foreach(Task task in tasks)
    {
      if(task == null) throw new ArgumentException("A task was null.");
      task.Completed += task_Completed;
      taskList.Add(task);
    }

    this.tasks = taskList.ToArray();
  }

  /// <summary>Gets the set of results from executing the tasks, if any of the tasks returned results.</summary>
  public T[] GetResults<T>()
  {
    if(!IsComplete) throw new InvalidOperationException("The task is not yet complete.");

    List<T> results = new List<T>();
    foreach(Task task in tasks)
    {
      Task<T> resultTask = task as Task<T>;
      if(resultTask != null && resultTask.HasResult) results.Add(resultTask.Result);
    }
    return results.ToArray();
  }

  /// <include file="documentation.xml" path="/Utilities/Task/AbortCore/node()"/>
  protected override void AbortCore()
  {
    foreach(Task task in tasks)
    {
      try { task.Abort(); }
      catch { }
    }
  }

  /// <include file="documentation.xml" path="/Utilities/Task/CancelCore/node()"/>
  protected override void CancelCore()
  {
    foreach(Task task in tasks) task.Cancel();
    base.CancelCore();
  }

  /// <include file="documentation.xml" path="/Utilities/Task/Dispose/node()"/>
  protected override void Dispose(bool manualDispose)
  {
    base.Dispose(manualDispose);
    foreach(Task task in tasks) task.Completed -= task_Completed;
  }

  /// <include file="documentation.xml" path="/Utilities/Task/RunCore/node()"/>
  protected override void RunCore()
  {
    if(tasks.Length == 1)
    {
      tasks[0].Run();
    }
    else
    {
      StartCore();
      foreach(Task task in tasks)
      {
        try { task.Finish(); }
        catch { }
      }

      List<Exception> exceptions = new List<Exception>();
      foreach(Task task in tasks)
      {
        if(task.Exception != null) exceptions.Add(task.Exception);
      }
      if(exceptions.Count != 0) throw exceptions.Count == 1 ? exceptions[0] : new CompositeException(exceptions);
    }
  }

  /// <include file="documentation.xml" path="/Utilities/Task/StartCore/node()"/>
  protected override void StartCore()
  {
    foreach(Task task in tasks) task.Start();
  }

  void task_Completed(object sender, EventArgs e)
  {
    if(Interlocked.Increment(ref completed) == tasks.Length) SignalCompletion();
  }

  internal Task[] tasks;
  int completed;
}
#endregion

#region WorkItemTask
/// <summary>Represents a task that represents a work item to complete.</summary>
public sealed class WorkItemTask : Task
{
  /// <summary>Initializes a new <see cref="WorkItemTask"/> with the work to be done by that task.</summary>
  public WorkItemTask(TaskDelegate work) : this(work, null) { }

  /// <summary>Initializes a new <see cref="WorkItemTask"/> with the work to be done by that task, and an optional
  /// <see cref="TaskCancellationEvent"/> allowing the task to be canceled along with other tasks that share the same
  /// cancellation event.
  /// </summary>
  public WorkItemTask(TaskDelegate work, TaskCancellationEvent cancellationEvent) : base(cancellationEvent)
  {
    if(work == null) throw new ArgumentNullException();
    this.work = work;
  }

  /// <include file="documentation.xml" path="/Utilities/Task/AbortCore/node()"/>
  protected override void AbortCore()
  {
    thread.Abort();
  }

  /// <include file="documentation.xml" path="/Utilities/Task/RunCore/node()"/>
  protected override void RunCore()
  {
    work(this);
  }

  /// <include file="documentation.xml" path="/Utilities/Task/StartCore/node()"/>
  protected override void StartCore()
  {
    thread = new Thread(ThreadFunc);
    thread.Start();
  }

  void ThreadFunc()
  {
    try { RunCore(); }
    catch(Exception ex) { Exception = ex; } // store unhandled exceptions, including ThreadAbortExceptions
    finally { SignalCompletion(); }
  }

  TaskDelegate work;
  Thread thread;
}
#endregion

#region WorkItemTask<T>
/// <summary>Represents a task that represents a work item to complete.</summary>
public sealed class WorkItemTask<T> : Task<T>
{
  /// <summary>Initializes a new <see cref="WorkItemTask{T}"/> with the work to be done by that task.</summary>
  public WorkItemTask(TaskDelegate<T> work) : this(work, null) { }

  /// <summary>Initializes a new <see cref="WorkItemTask{T}"/> with the work to be done by that task, and an optional
  /// <see cref="TaskCancellationEvent"/> allowing the task to be canceled along with other tasks that share the same
  /// cancellation event.
  /// </summary>
  public WorkItemTask(TaskDelegate<T> work, TaskCancellationEvent cancellationEvent) : base(cancellationEvent)
  {
    if(work == null) throw new ArgumentNullException();
    this.work = work;
  }

  /// <include file="documentation.xml" path="/Utilities/Task/AbortCore/node()"/>
  protected override void AbortCore()
  {
    thread.Abort();
  }

  /// <include file="documentation.xml" path="/Utilities/Task/RunCore/node()"/>
  protected override void RunCore()
  {
    Result = work(this);
  }

  /// <include file="documentation.xml" path="/Utilities/Task/StartCore/node()"/>
  protected override void StartCore()
  {
    thread = new Thread(ThreadFunc);
    thread.Start();
  }

  void ThreadFunc()
  {
    try { RunCore(); }
    catch(Exception ex) { Exception = ex; } // store unhandled exceptions, including ThreadAbortExceptions
    finally { SignalCompletion(); }
  }

  TaskDelegate<T> work;
  Thread thread;
}
#endregion

#region Tasks
/// <summary>Provides methods to manage <see cref="Task">tasks</see> and execute tasks in parallel.</summary>
/// <include file="documentation.xml" path="/Utilities/Tasks/ParallelismRemarks/node()"/>
public static class Tasks
{
  #region LoopThreadInfo
  /// <summary>Represents information about a thread processing items within a loop.</summary>
  public sealed class LoopThreadInfo
  {
    internal LoopThreadInfo(int threadNumber)
    {
      ThreadNumber = threadNumber;
    }

    /// <summary>Gets the number of the thread, from zero to the number of parallel loop threads minus one.</summary>
    public int ThreadNumber { get; private set; }
  }
  #endregion

  /// <summary>Creates and returns a task that represents the running of an instance of a work item on each hardware thread.</summary>
  /// <include file="documentation.xml" path="/Utilities/Tasks/ParallelismRemarks/node()"/>
  public static Task CreateParallel(TaskDelegate work)
  {
    return CreateParallel(work, SystemInformation.GetAvailableCpuThreads(), null);
  }

  /// <summary>Creates and returns a task that represents the running of an instance of a work item on each hardware thread. The
  /// method accepts an optional cancellation event that can be associated with all instances of the work item, as well as the
  /// aggregate task that encapsulates them.
  /// </summary>
  /// <include file="documentation.xml" path="/Utilities/Tasks/ParallelismRemarks/node()"/>
  public static Task CreateParallel(TaskDelegate work, TaskCancellationEvent cancellationEvent)
  {
    return CreateParallel(work, SystemInformation.GetAvailableCpuThreads(), cancellationEvent);
  }

  /// <summary>Creates and returns a task that represents the running of a number of instances of a work item simultaneously.</summary>
  /// <include file="documentation.xml" path="/Utilities/Tasks/ParallelismRemarks/node()"/>
  public static Task CreateParallel(TaskDelegate work, int instanceCount)
  {
    return CreateParallel(work, instanceCount, null);
  }

  /// <summary>Creates and returns a task that represents the running of a number of instances of a work item simultaneously.
  /// The method accepts an optional cancellation event that can be associated with all instances of the work item, as well as
  /// the aggregate task that encapsulates them.
  /// </summary>
  /// <include file="documentation.xml" path="/Utilities/Tasks/ParallelismRemarks/node()"/>
  public static Task CreateParallel(TaskDelegate work, int instanceCount, TaskCancellationEvent cancellationEvent)
  {
    if(instanceCount <= 0) throw new ArgumentOutOfRangeException();

    if(instanceCount == 1)
    {
      return new WorkItemTask(work, cancellationEvent);
    }
    else
    {
      Task[] tasks = new Task[instanceCount];
      for(int i=0; i<tasks.Length; i++) tasks[i] = new WorkItemTask(work, cancellationEvent);
      return new CompositeTask(tasks, cancellationEvent);
    }
  }

  /// <summary>Creates and returns a task that represents the running of an instance of a work item on each hardware thread.</summary>
  /// <include file="documentation.xml" path="/Utilities/Tasks/ParallelismRemarks/node()"/>
  public static CompositeTask CreateParallel<T>(TaskDelegate<T> work)
  {
    return CreateParallel(work, SystemInformation.GetAvailableCpuThreads(), null);
  }

  /// <summary>Creates and returns a task that represents the running of an instance of a work item on each hardware thread. The
  /// method accepts an optional cancellation event that can be associated with all instances of the work item, as well as the
  /// aggregate task that encapsulates them.
  /// </summary>
  /// <include file="documentation.xml" path="/Utilities/Tasks/ParallelismRemarks/node()"/>
  public static CompositeTask CreateParallel<T>(TaskDelegate<T> work, TaskCancellationEvent cancellationEvent)
  {
    return CreateParallel(work, SystemInformation.GetAvailableCpuThreads(), cancellationEvent);
  }

  /// <summary>Creates and returns a task that represents the running of a number of instances of a work item simultaneously.</summary>
  /// <include file="documentation.xml" path="/Utilities/Tasks/ParallelismRemarks/node()"/>
  public static CompositeTask CreateParallel<T>(TaskDelegate<T> work, int instanceCount)
  {
    return CreateParallel(work, instanceCount, null);
  }

  /// <summary>Creates and returns a task that represents the running of a number of instances of a work item simultaneously.
  /// The method accepts an optional cancellation event that can be associated with all instances of the work item, as well as
  /// the aggregate task that encapsulates them.
  /// </summary>
  /// <include file="documentation.xml" path="/Utilities/Tasks/ParallelismRemarks/node()"/>
  public static CompositeTask CreateParallel<T>(TaskDelegate<T> work, int instanceCount, TaskCancellationEvent cancellationEvent)
  {
    if(instanceCount <= 0) throw new ArgumentOutOfRangeException();

    Task[] tasks = new Task[instanceCount];
    for(int i=0; i<tasks.Length; i++) tasks[i] = new WorkItemTask<T>(work, cancellationEvent);
    return new CompositeTask(tasks, cancellationEvent);
  }

  /// <summary>Executes a loop using the maximum number of hardware threads available.</summary>
  /// <include file="documentation.xml" path="/Utilities/Tasks/ParallelFor/node()"/>
  public static void ParallelFor(int start, int endExclusive, Action<int> body)
  {
    ParallelFor(start, endExclusive, body, SystemInformation.GetAvailableCpuThreads());
  }

  /// <summary>Executes a loop using up to the given number of threads.</summary>
  /// <param name="parallelism">The maximum number of threads to use to execute the loop.</param>
  /// <include file="documentation.xml" path="/Utilities/Tasks/ParallelFor/node()"/>
  public static void ParallelFor(int start, int endExclusive, Action<int> body, int parallelism)
  {
    if(body == null) throw new ArgumentNullException();
    ParallelFor<object>(start, endExclusive, info => null, (index,context,info) => body(index));
  }

  /// <summary>Executes a loop using the maximum number of hardware threads available.</summary>
  /// <include file="documentation.xml" path="/Utilities/Tasks/ParallelFor/*[not(@name='body')]"/>
  /// <include file="documentation.xml" path="/Utilities/Tasks/ParallelForWithInit/node()"/>
  public static void ParallelFor<T>(int start, int endExclusive, Func<LoopThreadInfo, T> threadInitializer,
                                    Action<int, T, LoopThreadInfo> body)
  {
    ParallelFor(start, endExclusive, threadInitializer, body, SystemInformation.GetAvailableCpuThreads());
  }

  /// <summary>Executes a loop using up to the given number of threads.</summary>
  /// <param name="maxParallelism">The maximum number of threads to use to execute the loop.</param>
  /// <include file="documentation.xml" path="/Utilities/Tasks/ParallelFor/*[not(@name='body')]"/>
  /// <include file="documentation.xml" path="/Utilities/Tasks/ParallelForWithInit/node()"/>
  public static void ParallelFor<T>(int start, int endExclusive, Func<LoopThreadInfo, T> threadInitializer,
                                    Action<int, T, LoopThreadInfo> body, int maxParallelism)
  {
    if(start > endExclusive || maxParallelism <= 0) throw new ArgumentOutOfRangeException();
    if(threadInitializer == null || body == null) throw new ArgumentNullException();

    int iterations = endExclusive - start;
    if(iterations == 0) return;

    if(maxParallelism > iterations) maxParallelism = iterations;
    if(maxParallelism == 1)
    {
      LoopThreadInfo info = new LoopThreadInfo(0);
      T value = threadInitializer(info);
      for(; start < endExclusive; start++) body(start, value, info);
    }
    else
    {
      WorkItemTask[] tasks = new WorkItemTask[maxParallelism];
      for(int i=0; i<tasks.Length; i++)
      {
        int threadNumber = i;
        tasks[i] = new WorkItemTask(task =>
        {
          LoopThreadInfo info = new LoopThreadInfo(threadNumber);
          T value = threadInitializer(info);

          // get an available index
          int currentIndex;
          while(true)
          {
            do
            {
              currentIndex = start;
              if(currentIndex == endExclusive) goto done;
            } while(Interlocked.CompareExchange(ref start, currentIndex+1, currentIndex) != currentIndex);

            body(currentIndex, value, info);
          }

          done:;
        });
      }

      new CompositeTask(tasks).Run();
    }
  }

  /// <summary>Executes a loop in chunks using the maximum number of hardware threads available.</summary>
  /// <include file="documentation.xml" path="/Utilities/Tasks/ParallelForChunked/node()"/>
  public static void ParallelFor(int start, int endExclusive, Action<int, int, LoopThreadInfo> body)
  {
    ParallelFor(start, endExclusive, body, SystemInformation.GetAvailableCpuThreads());
  }

  /// <summary>Executes a loop in chunks, using up to the given number of threads.</summary>
  /// <param name="maxParallelism">The maximum number of threads to use to execute the loop.</param>
  /// <include file="documentation.xml" path="/Utilities/Tasks/ParallelForChunked/node()"/>
  public static void ParallelFor(int start, int endExclusive, Action<int, int, LoopThreadInfo> body, int maxParallelism)
  {
    if(start > endExclusive || maxParallelism <= 0) throw new ArgumentOutOfRangeException();
    if(body == null) throw new ArgumentNullException();

    uint iterations = (uint)(endExclusive - start), parallelism = Math.Min((uint)maxParallelism, iterations);
    if(iterations == 0) return;

    if(parallelism == 1)
    {
      body(start, endExclusive, new LoopThreadInfo(0));
    }
    else
    {
      WorkItemTask[] tasks = new WorkItemTask[parallelism];
      for(uint basicChunkSize=iterations/parallelism, errorInc=iterations%parallelism, error=0, i=0; i<(uint)tasks.Length; i++)
      {
        int threadNumber = (int)i, chunkStart = start; // make a copy of these so they can be properly captured by the closure
        uint chunkSize = basicChunkSize;

        // say we're dividing 100 iterations over 6 threads. then each chunk should be 16.666... iterations in length. since we
        // can't have a fractional iteration, we'll truncate the chunk size (to 16) and keep track of an error value. the error
        // will be incremented by the fractional part of the ideal chunk size (0.666...). if the error is greater than or equal
        // to 0.5, then we'll add one to the chunk size and subtract 1 from the error. since we don't want to use floating point
        // math, due to its inaccuracy, we'll do the same thing with integers. the error increment will be the integer remainder
        // from the calculation of the chunk size (so 100/6 leaves a remainder of 4). and instead of comparing against 0.5, we'll
        // compare double the error against the divisor (parallelism). this is the same as comparing the error against half the
        // divisor, except that it avoids truncation error. unfortunately, i can't really prove that the algorithm works for all
        // combinations of iterations and thread counts, but it seems to, and i'm showing my trust in it by not adding a special
        // case for the last chunk that simply takes up the remaining iterations.
        error += errorInc;
        if(error*2 >= parallelism)
        {
          chunkSize++;
          error -= parallelism;
        }

        tasks[i] = new WorkItemTask(task =>
        {
          LoopThreadInfo info = new LoopThreadInfo(threadNumber);
          body(chunkStart, chunkStart+(int)chunkSize, info);
        });

        start += (int)chunkSize;
      }

      new CompositeTask(tasks).Run();
    }
  }

  /// <summary>Runs an instance of a work item on each hardware thread, and returns when all instances have completed.</summary>
  /// <include file="documentation.xml" path="/Utilities/Tasks/ParallelismRemarks/node()"/>
  public static void Parallelize(TaskDelegate work)
  {
    Parallelize(work, SystemInformation.GetAvailableCpuThreads(), null);
  }

  /// <summary>Runs an instance of a work item on each hardware thread, and returns when all instances have completed or been
  /// canceled using the given cancellation event.
  /// </summary>
  /// <include file="documentation.xml" path="/Utilities/Tasks/ParallelismRemarks/node()"/>
  public static void Parallelize(TaskDelegate work, TaskCancellationEvent cancellationEvent)
  {
    Parallelize(work, SystemInformation.GetAvailableCpuThreads(), cancellationEvent);
  }

  /// <summary>Runs a number of instances of a work item simultaneously, and returns when all instances have completed.</summary>
  /// <include file="documentation.xml" path="/Utilities/Tasks/ParallelismRemarks/node()"/>
  public static void Parallelize(TaskDelegate work, int instanceCount)
  {
    Parallelize(work, instanceCount, null);
  }

  /// <summary>Runs a number of instances of a work item simultaneously, and returns when all instances have completed or been
  /// canceled using the given cancellation event.
  /// </summary>
  /// <include file="documentation.xml" path="/Utilities/Tasks/ParallelismRemarks/node()"/>
  public static void Parallelize(TaskDelegate work, int instanceCount, TaskCancellationEvent cancellationEvent)
  {
    using(Task task = CreateParallel(work, instanceCount, cancellationEvent)) task.Run();
  }

  /// <summary>Runs an instance of a work item on each hardware thread, and returns the results from all instances they've
  /// completed.
  /// </summary>
  /// <include file="documentation.xml" path="/Utilities/Tasks/ParallelismRemarks/node()"/>
  public static T[] Parallelize<T>(TaskDelegate<T> work)
  {
    return Parallelize(work, SystemInformation.GetAvailableCpuThreads(), null);
  }

  /// <summary>Runs an instance of a work item on each hardware thread, and returns the results from all instances when they've
  /// completed or been canceled using the given cancellation event.
  /// </summary>
  /// <include file="documentation.xml" path="/Utilities/Tasks/ParallelismRemarks/node()"/>
  public static T[] Parallelize<T>(TaskDelegate<T> work, TaskCancellationEvent cancellationEvent)
  {
    return Parallelize(work, SystemInformation.GetAvailableCpuThreads(), cancellationEvent);
  }

  /// <summary>Runs a number of instances of a work item simultaneously, and returns the results from all instances when they've
  /// completed.
  /// </summary>
  /// <include file="documentation.xml" path="/Utilities/Tasks/ParallelismRemarks/node()"/>
  public static T[] Parallelize<T>(TaskDelegate<T> work, int instanceCount)
  {
    return Parallelize(work, instanceCount, null);
  }

  /// <summary>Runs a number of instances of a work item simultaneously, and returns the results from all instances when they've
  /// completed or been canceled using the given cancellation event.
  /// </summary>
  /// <include file="documentation.xml" path="/Utilities/Tasks/ParallelismRemarks/node()"/>
  public static T[] Parallelize<T>(TaskDelegate<T> work, int instanceCount, TaskCancellationEvent cancellationEvent)
  {
    using(CompositeTask task = CreateParallel(work, instanceCount, cancellationEvent))
    {
      task.Run();
      return task.GetResults<T>();
    }
  }

  /// <summary>Waits for all of the given tasks to complete.</summary>
  public static void WaitForAll(IEnumerable<Task> tasks)
  {
    WaitForAll(tasks, Timeout.Infinite);
  }

  /// <summary>Waits for all of the tasks in the given collection to complete, or the timeout to expire. Returns true if the
  /// tasks completed (or no tasks were given), or false if the time limit expired.
  /// </summary>
  public static bool WaitForAll(IEnumerable<Task> tasks, int timeoutMs)
  {
    WaitHandle[] handles = GetWaitHandles(tasks, null);
    return handles.Length == 0 ? true : WaitHandle.WaitAll(handles, timeoutMs);
  }

  /// <summary>Waits for any task of the given tasks to complete. Returns a task that was complete, or null if no tasks were
  /// given. If a task is returned, it is possible that other tasks are also complete, so you may wish to check all of them.
  /// </summary>
  public static Task WaitForAny(IEnumerable<Task> tasks)
  {
    return WaitForAny(tasks, Timeout.Infinite);
  }

  /// <summary>Waits for any of the given tasks to complete, or the time limit to expire. Returns true if a task completed, or
  /// false if no task completed within the given time limit (or no tasks were given). If a task is returned, it is possible that
  /// other tasks are also complete, so you may wish to check all of them.
  /// </summary>
  public static Task WaitForAny(IEnumerable<Task> tasks, int timeoutMs)
  {
    // TODO: the underlying WaitHandle API is incapable of handling more than a certain number of tasks, although the limit is
    // different from one system to another. perhaps we should handle this somehow
    List<Task> taskList = new List<Task>();
    WaitHandle[] handles = GetWaitHandles(tasks, taskList);
    int index = handles.Length == 0 ? WaitHandle.WaitTimeout : WaitHandle.WaitAny(handles, timeoutMs);
    return index == WaitHandle.WaitTimeout ? null : taskList[index];
  }

  static WaitHandle[] GetWaitHandles(IEnumerable<Task> tasks, List<Task> taskList)
  {
    if(tasks == null) throw new ArgumentNullException();
    List<WaitHandle> handles = new List<WaitHandle>();
    foreach(Task task in tasks)
    {
      if(task == null) throw new ArgumentException("A task was null.");
      if(taskList != null) taskList.Add(task);
      handles.Add(task.WaitHandle);
    }
    return handles.ToArray();
  }
}
#endregion

} // namespace AdamMil.Utilities
