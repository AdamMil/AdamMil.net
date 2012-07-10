using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace AdamMil.Utilities
{

/// <summary>Provides information about the current system.</summary>
public static class SystemInformation
{
  /// <summary>Gets the number of CPU cores available. Note that each core may support multiple threads, and may share
  /// computation units with other cores (making those cores unable to be fully utilized independently), so for parallelizing
  /// tasks, it is usually better to use either <see cref="IndependentProcessorCount"/> or <see cref="CpuThreadCount"/>
  /// (depending on the operation the tasks perform) to calculate the number of threads to use. You can also retrieve the number
  /// of currently available cores (considering the current processor affinity mask) using <see cref="GetAvailableCpuCores"/>.
  /// </summary>
  public static int CpuCoreCount
  {
    get
    {
      if(_cpuCores == 0) InitializeProcessorInformation();
      return _cpuCores;
    }
  }

  /// <summary>Gets the number of CPU (hardware) threads available. Tasks should not be parallelized beyond this limit, as doing
  /// so degrades performance, and should only be parallelized up to this limit if the tasks perform operations that technologies
  /// like HyperThreading can effectively parallelize. Consider parallelizing up to <see cref="IndependentProcessorCount"/>
  /// instead. You can also retrieve the number of currently available threads (considering the current processor affinity mask)
  /// using <see cref="GetAvailableCpuThreads"/>.
  /// </summary>
  public static int CpuThreadCount
  {
    get
    {
      if(_cpuThreads == 0) InitializeProcessorInformation();
      return _cpuThreads;
    }
  }

  /// <summary>Gets the number of independent processing units available. Each CPU core supports one or more hardware threads,
  /// and the threads may or may not share computation units within the core. This property is calculated by summing the numbers
  /// of threads for each core, except that cores whose threads share computation units are considered to support only a single
  /// independent thread. Tasks should only be parallelized beyond this limit if the tasks perform operations that technologies
  /// like HyperThreading are capable of parallelizing effectively. (Typically, CPU-bound tasks don't effectively utilize
  /// HyperThreading.) Otherwise, performance will be degraded. You can also retrieve the number of currently available
  /// independent processors (considering the current processor affinity mask) using
  /// <see cref="GetAvailableIndependentProcessors"/>.
  /// </summary>
  public static int IndependentProcessorCount
  {
    get
    {
      if(_independentProcessors == 0) InitializeProcessorInformation();
      return _independentProcessors;
    }
  }

  /// <summary>Gets the number of CPU cores available, considering the current processor affinity mask.</summary>
  public static int GetAvailableCpuCores()
  {
    #if WINDOWS
    int totalCores = CpuCoreCount; // this also ensures that cpuCoreMasks is initialized
    return GetAvailableProcessors(cpuCoreMasks, totalCores);
    #else
    #warning Checking thread affinity on this OS is not implemented (or the appropriate preprocessor flag was not set).
    return CpuCoreCount;
    #endif
  }

  /// <summary>Gets the number of CPU (hardware) threads available, considering the current processor affinity mask.</summary>
  public static int GetAvailableCpuThreads()
  {
    #if WINDOWS
    UIntPtr processMask, systemMask;
    try
    {
      if(GetProcessAffinityMask(currentProcess, out processMask, out systemMask)) return BitCount((ulong)processMask);
    }
    catch { }
    return CpuThreadCount;
    #else
    #warning Checking thread affinity on this OS is not implemented (or the appropriate preprocessor flag was not set).
    return CpuThreadCount;
    #endif
  }

  /// <summary>Gets the number of independent processing units available, considering the current processor affinity mask.</summary>
  public static int GetAvailableIndependentProcessors()
  {
    #if WINDOWS
    int totalIndependentProcessors = IndependentProcessorCount; // also ensures that independentProcessorMasks is initialized
    return GetAvailableProcessors(independentProcessorMasks, totalIndependentProcessors);
    #else
    #warning Checking thread affinity on this OS is not implemented (or the appropriate preprocessor flag was not set).
    return IndependentProcessorCount;
    #endif
  }

  #if WINDOWS
  enum CacheType
  {
    Unified, Instruction, Data, Trace
  }

  enum CoreFlags : byte
  {
    ShareFunctionalUnits=1
  }

  enum ProcessorSharing
  {
    Core, NumaNode, Cache, Package
  }

  struct Cache
  {
    public byte Level, Associativity;
    public ushort LineSize;
    public uint Size;
    public CacheType Type;
  }

  struct ProcessorInformation
  {
    public UIntPtr ProcessorMask;
    public ProcessorSharing Sharing;
    public ProcessorRelation Relation;
  }

  [StructLayout(LayoutKind.Explicit, Size=16)]
  struct ProcessorRelation
  {
    [FieldOffset(0)] public Cache Cache;
    [FieldOffset(0)] public CoreFlags CoreFlags;
    [FieldOffset(0)] public uint NumaNode;
  }

  [DllImport("kernel32.dll")]
  static extern IntPtr GetCurrentProcess();

  [DllImport("kernel32.dll")]
  static unsafe extern bool GetLogicalProcessorInformation(ProcessorInformation* info, ref int byteLength);

  [DllImport("kernel32.dll")]
  static extern bool GetProcessAffinityMask(IntPtr processHandle, out UIntPtr processorMask, out UIntPtr systemMask);

  static ulong AddMasks(List<UIntPtr> masks, ulong mask)
  {
    do // add each bit to the list of masks
    {
      ulong sub = mask-1; // if mask == 101000 then sub == 100111
      masks.Add(new UIntPtr(mask ^ sub & mask)); // mask ^ sub == 001111, that & mask == 001000
      mask &= sub; // finally, mask & sub == 100000 (so we're done with that bit)
    } while(mask != 0);
    return mask;
  }

  static int BitCount(ulong n)
  {
    int count = 0;
    while(n != 0)
    {
      n &= n-1; // zero out the least significant one bit
      count++;
    }
    return count;
  }

  static unsafe int GetAvailableProcessors(UIntPtr[] masks, int totalCount)
  {
    if(masks != null)
    {
      UIntPtr processMask, systemMask;
      try
      {
        if(GetProcessAffinityMask(currentProcess, out processMask, out systemMask))
        {
          int count = 0;
          foreach(UIntPtr mask in masks)
          {
            if(sizeof(UIntPtr) == 4)
            {
              if(((uint)mask & (uint)processMask) != 0) count++;
            }
            else
            {
              if(((ulong)mask & (ulong)processMask) != 0) count++;
            }
          }
          return count;
        }
      }
      catch { }
    }

    return totalCount;
  }

  static UIntPtr[] cpuCoreMasks, independentProcessorMasks;
  static readonly IntPtr currentProcess = GetCurrentProcess(); // it returns a constant pseudo-handle, so we only need to call it once
  #endif

  static unsafe void InitializeProcessorInformation()
  {
    int cpuCores = 0, cpuThreads = 0, independentProcessors = 0;

    #if WINDOWS
    // TODO: this method only supports a maximum of 64 processors (32 on 32-bit systems), i.e. one processor group. we can use
    // GetLogicalProcessorInformationEx() to get information about all of the processor groups, but it might not even be helpful.
    // I'm not sure whether a single application can be scheduled across multiple processor groups anyway. also, that function is
    // only supported on Windows 7/2008 or later
    try
    {
      int length = 0;
      GetLogicalProcessorInformation(null, ref length);
      if(length != 0)
      {
        length /= sizeof(ProcessorInformation); // determine the number of structures from the byte length
        ProcessorInformation[] infos = new ProcessorInformation[length];
        fixed(ProcessorInformation* pInfos = infos)
        {
          // TODO: test this on a single core machine to make sure all the relevant information is returned
          int byteLength = length*sizeof(ProcessorInformation);
          if(GetLogicalProcessorInformation(pInfos, ref byteLength))
          {
            List<UIntPtr> coreMasks = new List<UIntPtr>(), independentProcessorMasks = new List<UIntPtr>();
            // the information is returned as a list of information about features of groups of processors. the set of processors
            // that the information relates to are represented in the ProcessorMask field, and the information describes the
            // features shared by all of those processors. processors will usually be listed multiple times, in different sets,
            // as different sets of processors share different features, usually overlapping with other sets
            for(int i=0; i<infos.Length; i++)
            {
              ProcessorInformation info = infos[i];
              if(info.Sharing == ProcessorSharing.Core) // if the information describes a set of hardware threads sharing a core
              {
                int processorCount = BitCount((ulong)info.ProcessorMask);
                cpuThreads += processorCount;

                cpuCores++;
                coreMasks.Add(info.ProcessorMask);

                // treat the threads as independent processors if they don't share functional (computing) units
                if((info.Relation.CoreFlags & CoreFlags.ShareFunctionalUnits) == 0)
                {
                  independentProcessors += processorCount;
                  AddMasks(independentProcessorMasks, (ulong)info.ProcessorMask);
                }
                else // otherwise, lump them together as a single independent processor
                {
                  independentProcessors++;
                  independentProcessorMasks.Add(info.ProcessorMask);
                }
              }
            }

            if(coreMasks.Count != 0) cpuCoreMasks = coreMasks.ToArray();
            if(independentProcessorMasks.Count != 0)
            {
              SystemInformation.independentProcessorMasks = independentProcessorMasks.ToArray();
            }
          }
        }
      }
    }
    catch { } // the GetLogicalProcessorInformation call is not available on all systems
    #else
    #warning Retrieving specific CPU information has not been implemented for this OS or the approprate preprocessor flag was not set. Falling back to Environment.ProcessorCount.
    #endif

    // fall back on using Environment.ProcessorCount if the information was not available
    if(cpuThreads == 0) cpuThreads = Math.Max(1, Environment.ProcessorCount);
    if(independentProcessors == 0) independentProcessors = cpuThreads;
    if(cpuCores == 0) cpuCores = cpuThreads;

    _cpuCores              = cpuCores;
    _cpuThreads            = cpuThreads;
    _independentProcessors = independentProcessors;
  }

  static int _cpuCores, _cpuThreads, _independentProcessors;
}

} // namespace AdamMil.Utilities
