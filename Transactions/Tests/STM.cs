using System;
using System.Threading;
using System.Transactions;
using AdamMil.Tests;
using NUnit.Framework;

namespace AdamMil.Transactions.Tests
{

[TestFixture]
public class STMTests
{
  #region T01_TestBasicTransactions
  [Test]
  public void T01_TestBasicTransactions()
  {
    TransactionalVariable<int> a = STM.Allocate(1), b = STM.Allocate(2), c = STM.Allocate(3);
    AssertEqual(a, 1, b, 2, c, 3);

    // test a simple transaction that doesn't commit
    using(STMTransaction tx = STMTransaction.Create())
    {
      a.Read(); // open one for reading
      b.OpenForWrite(); // one for writing
      AssertEqual(a, 1, b, 2, c, 3); // and leave one unopened, and check their values

      c.Set(30);
      a.Set(10);
      AssertEqual(a, 10, b, 2, c, 30);
    }
    AssertEqual(a, 1, b, 2, c, 3); // check that the changes were reverted

    // test a transaction that does commit
    using(STMTransaction tx = STMTransaction.Create())
    {
      AssertEqual(a, 1, b, 2, c, 3); // check that the changes were reverted
      c.Set(30);
      a.Set(10);
      b.Set(20);
      tx.Commit();
      AssertEqual(a, 10, b, 20, c, 30);
    }
    AssertEqual(a, 10, b, 20, c, 30);

    // test a simple nested transaction where the inner doesn't commit
    using(STMTransaction otx = STMTransaction.Create())
    {
      a.Set(-1);
      c.Read();
      AssertEqual(a, -1, c, 30);
      using(STMTransaction tx = STMTransaction.Create())
      {
        AssertEqual(a, -1);
        a.Set(1);
        b.Set(2);
        c.Set(3);
        AssertEqual(a, 1, b, 2, c, 3);
      }
      AssertEqual(a, -1, b, 20, c, 30);
      otx.Commit();
    }
    AssertEqual(a, -1, b, 20, c, 30);

    // test a simple nested transaction where the outer doesn't commit but the inner does
    using(STMTransaction otx = STMTransaction.Create())
    {
      a.Set(1);
      c.Read();
      AssertEqual(a, 1, c, 30);
      using(STMTransaction tx = STMTransaction.Create())
      {
        AssertEqual(a, 1);
        a.Set(10);
        b.Set(-2);
        c.Set(-3);
        AssertEqual(a, 10, b, -2, c, -3);
        tx.Commit();
      }
      AssertEqual(a, 10, b, -2, c, -3);
    }
    AssertEqual(a, -1, b, 20, c, 30);

    // test a simple nested transaction where both commit
    using(STMTransaction otx = STMTransaction.Create())
    {
      a.Set(1);
      b.Read();
      b.Set(-2);
      c.Read();
      AssertEqual(a, 1, b, -2, c, 30);
      using(STMTransaction tx = STMTransaction.Create())
      {
        a.Set(-1);
        b.Set(2);
        c.Set(3);
        tx.Commit();
      }
      AssertEqual(a, -1, b, 2, c, 3);
      a.Set(1);
      otx.Commit();
    }
    AssertEqual(a, 1, b, 2, c, 3);

    // test the Release() method
    using(STMTransaction tx = STMTransaction.Create())
    {
      a.Set(10); // test that releasing a written variable discards changes
      AssertEqual(a, 10);
      b.Read(); // test that releasing a read variable prevents conflicts with other transactions
      c.Read();
      TestHelpers.RunInAnotherThread(delegate { STM.Retry(delegate { b.Set(20); c.Set(30); }); });
      AssertEqual(b, 2);
      a.Release();
      b.Release();
      c.Release();
      b.Read();
      AssertEqual(a, 1, b, 20);
      tx.Commit();
    }
    AssertEqual(a, 1, b, 20, c, 30);

    // test post-commit actions
    int ntValue = 0;
    using(STMTransaction tx = STMTransaction.Create())
    {
      a.Set(10);
      tx.Commit(delegate { ntValue = 1; });
    }
    Assert.AreEqual(1, ntValue);

    // ensure they're not executed unless the top-level transaction commits
    using(STMTransaction otx = STMTransaction.Create())
    using(STMTransaction tx = STMTransaction.Create())
    {
      a.Set(10);
      tx.Commit(delegate { ntValue = 2; });
    }
    Assert.AreEqual(1, ntValue);

    // ensure that they're executed in the right order (innermost first) and at the right time
    using(STMTransaction otx = STMTransaction.Create())
    {
      using(STMTransaction tx = STMTransaction.Create())
      {
        a.Set(10);
        tx.Commit(delegate { ntValue = 2; });
        Assert.AreEqual(1, ntValue);
      }
      Assert.AreEqual(1, ntValue);
      otx.Commit(delegate { ntValue++; });
    }
    Assert.AreEqual(3, ntValue);

    // ensure that post-commit actions given to STM.Retry() work and aren't executed multiple times if the transaction restarts
    bool first = true;
    STM.Retry(
      delegate
      {
        if(first)
        {
          first = false;
          throw new TransactionAbortedException();
        }
      },
      delegate { ntValue++; });
    Assert.AreEqual(4, ntValue);
  }
  #endregion

  #region T02_TestSystemTransactions
  [Test]
  public void T02_TestSystemTransactions()
  {
    TransactionalVariable<int> a = STM.Allocate(1), b = STM.Allocate(2), c = STM.Allocate(3);
    AssertEqual(a, 1, b, 2, c, 3);

    // test where neither commit
    using(TransactionScope stx = new TransactionScope())
    using(STMTransaction tx = STMTransaction.Create())
    {
      a.Set(10);
      b.Read();
      AssertEqual(a, 10, b, 2, c, 3);
    }
    AssertEqual(a, 1, b, 2, c, 3);

    // test where the STM transaction commits by the system transaction doesn't
    using(TransactionScope stx = new TransactionScope())
    using(STMTransaction tx = STMTransaction.Create())
    {
      a.Set(10);
      b.Read();
      b.Set(20);
      c.Read();
      AssertEqual(a, 10, b, 20, c, 3);
      tx.Commit();
      AssertEqual(a, 10, b, 20, c, 3);
    }
    AssertEqual(a, 1, b, 2, c, 3);

    // test where the system transaction commits by the STM transaction doesn't
    using(TransactionScope stx = new TransactionScope())
    {
      using(STMTransaction tx = STMTransaction.Create())
      {
        a.Set(10);
        b.Read();
        b.Set(20);
        c.Read();
        AssertEqual(a, 10, b, 20, c, 3);
      }
      AssertEqual(a, 1, b, 2, c, 3);
      stx.Complete();
    }
    AssertEqual(a, 1, b, 2, c, 3);

    // test where both commit
    using(TransactionScope stx = new TransactionScope())
    {
      using(STMTransaction tx = STMTransaction.Create())
      {
        a.Set(10);
        b.Read();
        b.Set(20);
        c.Read();
        AssertEqual(a, 10, b, 20, c, 3);
        tx.Commit();
      }
      AssertEqual(a, 10, b, 20, c, 3);
      stx.Complete();
    }
    AssertEqual(a, 10, b, 20, c, 3);

    // test a system transaction nested within an STM transaction
    using(STMTransaction otx = STMTransaction.Create())
    {
      using(TransactionScope stx = new TransactionScope())
      {
        using(STMTransaction tx = STMTransaction.Create())
        {
          a.Set(1);
          b.Set(2);
          tx.Commit();
          AssertEqual(a, 1, b, 2);
        }
        stx.Complete();
        AssertEqual(a, 1, b, 2);
      }
      AssertEqual(a, 1, b, 2);
    }
    AssertEqual(a, 10, b, 20);

    // test a system transaction nested within an STM transaction
    using(STMTransaction otx = STMTransaction.Create())
    {
      using(TransactionScope stx = new TransactionScope())
      {
        using(STMTransaction tx = STMTransaction.Create())
        {
          a.Set(1);
          b.Set(2);
          tx.Commit();
          AssertEqual(a, 1, b, 2);
        }
        AssertEqual(a, 1, b, 2);
      }
      AssertEqual(a, 10, b, 20);
    }
    AssertEqual(a, 10, b, 20);

    // test a system transaction nested within an STM transaction
    using(STMTransaction otx = STMTransaction.Create())
    {
      using(TransactionScope stx = new TransactionScope())
      {
        using(STMTransaction tx = STMTransaction.Create())
        {
          a.Set(1);
          b.Set(2);
          tx.Commit();
          AssertEqual(a, 1, b, 2);
        }
        stx.Complete();
        AssertEqual(a, 1, b, 2);
      }
      AssertEqual(a, 1, b, 2);
      otx.Commit();
    }
    AssertEqual(a, 1, b, 2);

    // test the ability to ignore system transactions
    using(new TransactionScope())
    {
      using(STMTransaction tx = STMTransaction.Create(STMOptions.IgnoreSystemTransaction))
      {
        a.Set(10);
        tx.Commit();
      }
    }
    AssertEqual(a, 10);

    // test that ignoring system transactions is inherited
    using(STMTransaction otx = STMTransaction.Create(STMOptions.IgnoreSystemTransaction))
    {
      using(new TransactionScope())
      {
        using(STMTransaction tx = STMTransaction.Create())
        {
          a.Set(1);
          tx.Commit();
        }
      }
      otx.Commit();
    }
    AssertEqual(a, 1);

    // test post-commit actions
    int ntValue = 0;

    // test that they're not executed if the System.Transactions transaction isn't completed
    using(new TransactionScope())
    using(STMTransaction tx = STMTransaction.Create()) tx.Commit(delegate { ntValue=1; });
    Assert.AreEqual(0, ntValue);

    // test that they are executed when it is
    using(TransactionScope stx = new TransactionScope())
    {
      using(STMTransaction tx = STMTransaction.Create()) tx.Commit(delegate { ntValue=1; });
      Assert.AreEqual(0, ntValue);
      stx.Complete();
    }
    Assert.AreEqual(1, ntValue);
  }
  #endregion

  #region T03_TestErrors
  [Test]
  public void T03_TestErrors()
  {
    // test that variables can't be created with uncloneable types
    TestHelpers.TestException<NotSupportedException>(delegate { STM.Allocate<object>(); });
    TestHelpers.TestException<NotSupportedException>(delegate { STM.Allocate<UncopyableStruct>(); });

    // test that transactions can't be committed twice
    using(STMTransaction tx = STMTransaction.Create())
    {
      tx.Commit();
      TestHelpers.TestException<InvalidOperationException>(delegate { tx.Commit(); });
    }

    // test that variables can't be opened for write outside transactions
    TransactionalVariable<int> a = STM.Allocate<int>();
    TestHelpers.TestException<InvalidOperationException>(delegate { a.OpenForWrite(); });
    TestHelpers.TestException<InvalidOperationException>(delegate { a.Set(1); });

    // test that bad implementations of ICloneable are detected
    TransactionalVariable<BadCloneable> b = STM.Allocate(new BadCloneable());
    using(STMTransaction tx = STMTransaction.Create())
    {
      TestHelpers.TestException<InvalidOperationException>(delegate { b.OpenForWrite(); });
    }

    // test that a transaction can't be committed before nested transactions have been dealt with
    using(STMTransaction otx = STMTransaction.Create())
    {
      STMTransaction tx = STMTransaction.Create();
      TestHelpers.TestException<InvalidOperationException>(delegate { otx.Commit(); });
    }

    // test that STM.Retry() fails when a transaction times out
    TestHelpers.TestException<TransactionAbortedException>(delegate
    {
      STM.Retry(25, delegate
      {
        a.Read();
        TestHelpers.RunInAnotherThread(delegate { STM.Retry(delegate { a.Set(a.OpenForWrite()+1); }); });
      });
    });

    // test that STM.Retry() doesn't loop infinitely if a transaction was consistent when an exception was thrown
    TransactionalVariable<int> c = STM.Allocate(0);
    TestHelpers.TestException<ArgumentOutOfRangeException>(delegate
    {
      STM.Retry(delegate
      {
        a.Read();
        c.Set(42);
        throw new ArgumentOutOfRangeException();
      });
    });
    AssertEqual(c, 0);

    // but test that STM.Retry() does retry if the transaction was inconsistent
    STM.Retry(delegate { a.Set(0); });
    bool first = true;
    STM.Retry(delegate
    {
      int aValue = a.Read();
      c.Set(42);
      if(first)
      {
        first = false;
        TestHelpers.RunInAnotherThread(delegate { STM.Retry(delegate { a.Set(a.OpenForWrite()+1); }); });
      }
      if(aValue == 0) throw new ArgumentOutOfRangeException();
    });
    AssertEqual(c, 42);
  }
  #endregion

  #region T04_TestCloning
  [Test]
  public void T04_TestCloning()
  {
    TransactionalVariable<Cloneable> cloneable = STM.Allocate(new Cloneable(42));
    TransactionalVariable<CopyableStruct> copyable = STM.Allocate(new CopyableStruct(42));
    TransactionalVariable<Immutable> immutable = STM.Allocate(new Immutable(42));
    TransactionalVariable<ImmutableStruct> immutableStruct = STM.Allocate(new ImmutableStruct("Neat"));

    using(STMTransaction tx = STMTransaction.Create())
    {
      Cloneable cloneable1 = cloneable.Read(), cloneable2 = cloneable.OpenForWrite();
      Assert.AreNotSame(cloneable1, cloneable2); // test that it clones on write but not read
      Assert.AreEqual(42, cloneable1.Value); // test that the clone has the right value
      Assert.AreEqual(42, cloneable2.Value);
      cloneable2.Value = 24; // mutate the clone to test that editing via mutation works

      object c1 = ((TransactionalVariable)copyable).Read(), c2 = copyable.OpenForWrite();
      Assert.AreNotSame(c1, c2); // make sure the struct was actually copied
      CopyableStruct copyable1 = (CopyableStruct)c1, copyable2 = (CopyableStruct)c2;
      Assert.AreEqual(42, copyable1.Immutable.Value); // test that the copy contains the same values
      Assert.AreEqual(42, copyable2.Immutable.Value);
      Assert.AreEqual(42, copyable1.Int);
      Assert.AreEqual(42, copyable2.Int);
      copyable.Set(new CopyableStruct(24)); // set a new value for the copyable struct

      Immutable immutable1 = immutable.Read(), immutable2 = immutable.OpenForWrite();
      Assert.AreSame(immutable1, immutable2); // test that the immutable object is not cloned
      Assert.AreEqual(42, immutable1.Value);
      immutable.Set(new Immutable(24)); // set a new value for the immutable object

      object i1 = ((TransactionalVariable)immutableStruct).Read(), i2 = ((TransactionalVariable)immutableStruct).OpenForWrite();
      Assert.AreSame(i1, i2); // test that the immutable struct was not cloned
      ImmutableStruct is1 = (ImmutableStruct)i1, is2 = (ImmutableStruct)i2;
      Assert.AreSame(is1.Value, is2.Value);
      Assert.AreEqual("Neat", (string)is1.Value);
      immutableStruct.Set(new ImmutableStruct("Feet"));

      tx.Commit();
    }
    Assert.AreEqual(24, cloneable.ReadWithoutOpening().Value);
    Assert.AreEqual(24, copyable.ReadWithoutOpening().Int);
    Assert.AreEqual(24, immutable.ReadWithoutOpening().Value);
    Assert.AreEqual("Feet", (string)immutableStruct.ReadWithoutOpening().Value);

    using(STMTransaction tx = STMTransaction.Create())
    {
      cloneable.OpenForWrite().Value = 0; // make sure mutations get rolled back
      copyable.Set(new CopyableStruct(0));
    }
    Assert.AreEqual(24, cloneable.ReadWithoutOpening().Value);
    Assert.AreEqual(24, copyable.ReadWithoutOpening().Int);
  }
  #endregion

  #region T05_TestContention
  [Test]
  public void T05_TestContention()
  {
    // test the EnsureConsistency option
    TransactionalVariable<int> a = STM.Allocate<int>(), b = STM.Allocate<int>();
    using(STMTransaction tx = STMTransaction.Create(STMOptions.EnsureConsistency))
    {
      Assert.IsTrue(tx.EnsureConsistency);
      a.Read();
      Assert.IsTrue(a.IsConsistent());
      Assert.IsTrue(tx.IsConsistent());
      a.CheckConsistency();
      TestHelpers.RunInAnotherThread(delegate { STM.Retry(delegate { a.Set(1); }); });
      Assert.IsFalse(a.IsConsistent());
      Assert.IsFalse(tx.IsConsistent());
      TestHelpers.TestException<TransactionAbortedException>(delegate { a.CheckConsistency(); });
      TestHelpers.TestException<TransactionAbortedException>(delegate { tx.CheckConsistency(); });
      TestHelpers.TestException<TransactionAbortedException>(delegate { b.Read(); });
    }

    const int Iterations = 500;
    TransactionalVariable<int>[] vars = new TransactionalVariable<int>[10]; // the array length must be even
    for(int i=0; i<vars.Length; i++) vars[i] = STM.Allocate<int>();

    Random rand = new Random();
    Exception exception = null;
    ThreadStart code = delegate
    {
      try
      {
        int index;
        lock(rand) index = rand.Next(vars.Length); // start at a random location in each thread
        for(int time=0; time<Iterations; time++)
        {
          // create a loop that increments every variable in the array once in a series of small transactions that write
          // two elements and read the intervening ones
          for(int i=0; i<vars.Length/2; i++)
          {
            int origIndex = index; // store the index so we can restore it if a transaction aborts
            STM.Retry(delegate
            {
              index = origIndex;
              vars[index].Set(vars[index].OpenForWrite()+1);
              if(++index == vars.Length) index = 0;
              for(int j=0; j<vars.Length/2-1; j++)
              {
                vars[index].Read();
                if(++index == vars.Length) index = 0;
              }
              vars[index].Set(vars[index].OpenForWrite()+1);
              if(++index == vars.Length) index = 0;
            });
            origIndex = index;
          }
        }
      }
      catch(Exception ex) { exception = ex; }
    };

    Thread[] threads = new Thread[16];
    for(int i=0; i<threads.Length; i++) threads[i] = new Thread(code);
    for(int i=0; i<threads.Length; i++) threads[i].Start();
    for(int i=0; i<threads.Length; i++)
    {
      if(!threads[i].Join(10000)) threads[i].Abort(); // give the test 10 seconds to run
    }

    if(exception != null) throw exception;

    // make sure all variables were incremented the right number of times
    for(int i=0; i<vars.Length; i++) Assert.AreEqual(Iterations*threads.Length, vars[i].ReadWithoutOpening());
  }
  #endregion

  #region BadCloneable
  sealed class BadCloneable : ICloneable
  {
    public object Clone()
    {
      return "not a BadCloneable";
    }
  }
  #endregion

  #region Cloneable
  sealed class Cloneable : ICloneable
  {
    public Cloneable(int initialValue)
    {
      Value = initialValue;
    }

    public int Value { get; set; }

    public object Clone()
    {
      return new Cloneable(Value);
    }
  }
  #endregion

  #region CopyableStruct
  struct CopyableStruct
  {
    public CopyableStruct(int intValue)
    {
      Immutable = new Immutable(intValue);
      Int = intValue;
    }

    public Immutable Immutable;
    public int Int;
  }
  #endregion

  #region Immutable
  [STMImmutable]
  sealed class Immutable
  {
    public Immutable(int value)
    {
      Value = value;
    }

    public int Value { get; private set; }
  }
  #endregion

  #region ImmutableStruct
  [STMImmutable]
  struct ImmutableStruct
  {
    public ImmutableStruct(object value)
    {
      _value = value;
    }

    public object Value
    {
      get { return _value; }
    }

    object _value;
  }
  #endregion

  #region UncopyableStruct
  struct UncopyableStruct
  {
    public UncopyableStruct(object value) { Object = value; }
    public object Object;
  }
  #endregion

  static void AssertEqual(params object[] args)
  {
    if(args.Length % 2 != 0) throw new ArgumentException();
    for(int i=0; i<args.Length; i += 2)
    {
      Assert.AreEqual(args[i+1], ((TransactionalVariable)args[i]).ReadWithoutOpening());
    }
  }
}

} // namespace AdamMil.Transactions.Tests
