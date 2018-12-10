﻿using System;
using System.Collections.Generic;
using System.Linq;
using AdamMil.Tests;
using NUnit.Framework;
using F = AdamMil.Mathematics.Financial;

namespace AdamMil.Mathematics.Tests
{
  [TestFixture]
  public class Financial
  {
    [Test]
    public void T01_IRR()
    {
      // test periodic IRR
      TestIRR(new[] { -70000d, 12000, 15000, 18000, 21000, 26000 }, 0.086630948036531614293);
      TestIRR(new[] { -10000d, 2750, 4250, 3250, 2750 }, 0.11541278310055859438);
      TestIRR(new[] { -10000d, 2750, -4250, 3250, 2750 }, -0.20311190538966381366);
      TestIRR(new[] { -10d, 10, 20 }, 1);
      TestIRR(new[] { -10d, -1, 1 }, -0.72984378812835756568);
      TestIRR(new[] { -10d, 10 }, 0);
      TestIRR(new[] { -10d, 100 }, 9);
      TestIRR(new[] { -100d, 10, 10, 10, 10, -90, 20, 20, 20, 20, 120, 10, 10, 10, 10, 110 }, 0.1);
      TestIRR(new[] { -1000d, -4000, 5000, 2000 }, 0.2548201113387211);

      Assert.IsNaN(F.IRR(new[] { 1d }));
      Assert.IsNaN(F.IRR(new[] { -10000d, 2750, -4250, 3250, -2750 }));

      // test non-periodic IRR
      TestIRR(new[] { -10000d, 2750, 4250, 3250, 2750 }, new[] { "2008/1/1", "2008/3/1", "2008/10/30", "2009/2/15", "2009/4/1" },
              0.37366100151642265209);
      TestIRR(new[] { -10000d, 2750, -4250, 3250, 2750 }, new[] { "2008/1/1", "2008/3/1", "2008/10/30", "2009/2/15", "2009/4/1" },
              -0.53729869183768615507);
      TestIRR(new[] { -10d, 10, 20 }, new[] { "2000/1/1", "2002/1/2", "2003/1/3" }, 0.52007207699370034614);
      TestIRR(new[] { -10d, -1, 1 }, new[] { "2000/1/1", "2002/1/2", "2003/1/3" }, -0.60624440874805840070);
      TestIRR(new[] { -10d, 10 }, new[] { "2000/1/1", "2003/1/1" }, 0);
      TestIRR(new[] { -10.01, 21, -11, 1 }, new[] { 0d, 1, 2, 300 }, 0.086330243649776002292, 0.022); // test the need for bracketing

      Assert.IsNaN(F.IRR(new[] { 1d }, ToDates("2000/1/1")));
      Assert.IsNaN(F.IRR(new[] { -10d, 10 }, ToDates("2000/1/1", "2000/1/1")));
      Assert.IsNaN(F.IRR(new[] { -10d, 100 }, ToDates("2000/1/1", "2000/1/1")));
      Assert.IsNaN(F.IRR(new[] { -10000d, 2750, -4250, 3250, -2750 }, ToDates("2008/1/1", "2008/3/1", "2008/10/30", "2009/2/15", "2009/4/1")));

      // test the modified IRR
      AssertEqual(0.17908568603489275993, F.ModifiedIRR(new[] { -1000d, -4000, 5000, 2000 }, 0.1, 0.12));
      AssertEqual(0.17908568603489275993, F.ModifiedIRR(new[] { 5000d, -1000, 2000, -4000 }, 0.1, 0.12));
      Assert.IsNaN(F.ModifiedIRR(new[] { 5000d, 1000, 2000, 4000 }, 0.1, 0.12));
      Assert.IsNaN(F.ModifiedIRR(new[] { -100d, -200, -300 }, 0.1, 0.12));
    }

    [Test]
    public void T02_FV()
    {
      TestInvestment(.01, -1000, 12, 0, 12682.503013196972066);
      TestInvestment(.05/12, -500, 10*12, 0, 77641.139722833964102);
      TestInvestment(.02/12, 0, 5*12, -10000, 11050.789265308194982);
      TestInvestment(F.PeriodicRate(.05, 12), -500, 10*12, -3000, 82068.264531065282197);
      TestInvestment(.03, 1000, 5, 0, -5468.4098843, false);
      TestInvestment(.06/12, -200, 10, -500, 2581.4033740601791537, false);
      TestInvestment(.09/12, -100, 7*12, 0, 11730.013040950889105, false);
      TestInvestment(0, -100, 10, -1000, 2000);
      TestInvestment(0, 100, 10, -1000, 0, false);

      AssertEqual(-40, F.FutureValue(0.1, new[] { -100d, 10, 10, 10, 10, 70 }));
      AssertEqual(31.6712, F.FutureValue(0.1, new[] { 2d, 3, 5 }.Concat(new[] { 7d, 11 }))); // try a non IList<T>
    }

    [Test]
    public void T03_PV()
    {
      AssertEqual(-60716.104029902083489, F.PresentValue(.05/12, 0, 10*12, 100000));
      TestInvestment(.08/12, 500, 12*20, -59777.145851188021817);
      TestInvestment(.045/12, -1250, 30*12, 246701.44876107713821);
      TestInvestment(F.PeriodicRate(.05, 12), -250, 10*12, -37699.927708720942270, 100000);
      TestInvestment(0.05/12, -1250, 30*12, 232852.02130759440896);
      TestInvestment(F.PeriodicRate(.05, 365.25), 1000, 20*12, -236177.89990613201544);
      TestInvestment(.09/12, 100, 7*12, -6262.0119295608783911, 0, false);
      TestInvestment(.05/12, -100, 10*12, -51248.685101108485610, 100000, false);
      TestInvestment(0, 100, 10, 0, -1000);
    }

    [Test]
    public void T04_Rates()
    {
      AssertEqual(0.12682503013196972066, F.CompoundRate(0.01, 12));
      AssertEqual(0.44061124131308475614, F.CompoundRate(0.001, 365.25));
      AssertEqual(0.0040741237836483016054, F.PeriodicRate(0.05, 12));
      AssertEqual(0.00013358911160635399973, F.PeriodicRate(0.05, 365.25));

      AssertEqual(0.0077014724882020438160, F.Rate(-200, 4*12, 8000));
      AssertEqual(0.013910762587407681666, F.Rate(-200, 4*12, 8000, -2000));
      AssertEqual(0.0080529819239060342467, F.Rate(-200, 4*12, 8000, false));
      AssertEqual(0.014414334213886103585, F.Rate(-200, 4*12, 8000, -2000, false));
      AssertEqual(0.0049355932778172330408, F.Rate(50, 21, -1000));
    }

    [Test]
    public void T05_Periods()
    {
      AssertEqual(60.082122853761722554, F.Periods(0.01, -100, -1000, 10000, true));
      AssertEqual(59.673865674294625588, F.Periods(0.01, -100, -1000, 10000, false));
      AssertEqual(-9.5785940398131666704, F.Periods(0.01, -100, -1000, 0));
      AssertEqual(28.911809737480831494, F.Periods(0.01, 100, -1000, -2000));
      AssertEqual(8.6690930741204999513, F.Periods(0.01, 100, 1000, -2000, false));
      AssertEqual(10.802372638626807957, F.Periods(.05/12, -1000, 100000) / 12);
      AssertEqual(3.8598661626226451824, F.Periods(F.PeriodicRate(.025, 12), 0, -2000, 2200) / 12);
      Assert.AreEqual(0, F.Periods(0.01, 100, -1000, 1000));

      TestInvestment(0, -100, 10, 1000);
      TestInvestment(0, -100, 30, 1000, 2000);
      Assert.AreEqual(0, F.Periods(0, -100, 1000, -1000));

      Assert.AreEqual(0, F.Periods(0, 0, 1000, 1000));
      Assert.IsNaN(F.Periods(0, 0, 1000, 2000));
      Assert.IsNaN(F.Periods(0, 0, 1000, -2000));
    }

    [Test]
    public void T06_Payments()
    {
      TestInvestment(.08/12, -244.129223415024806, 4*12, 10000);
      TestInvestment(.05, -2539.015879981144921, 18, 0, 75000, false);
      TestInvestment(0, 10, 100, -1000);
    }

    static void AssertEqual(double expected, double actual)
    {
      if(expected == 0 || actual == 0) Assert.AreEqual(expected, actual, 1e-8); // use absolute error if either is zero
      else Assert.LessOrEqual(actual / expected - 1, 1e-13, "Expected " + expected + " but got " + actual); // otherwise use relative error
    }

    static void TestInvestment(double rate, double payment, int paymentCount, double presentValue = 0, double futureValue = 0, bool payAtEnd = true)
    {
      AssertEqual(futureValue, F.FutureValue(rate, payment, paymentCount, presentValue, payAtEnd));
      AssertEqual(payment, F.Payment(rate, paymentCount, presentValue, futureValue, payAtEnd));
      AssertEqual(paymentCount, F.Periods(rate, payment, presentValue, futureValue, payAtEnd));
      AssertEqual(presentValue, F.PresentValue(rate, payment, paymentCount, futureValue, payAtEnd));
    }

    static void TestIRR(double[] values, double expectedRate)
    {
      double rate = F.IRR(values);
      AssertEqual(expectedRate, rate);
      AssertEqual(0, F.PresentValue(rate, values)); // IRR(x) is the value of r where NPV(r,x) == 0, so test that
    }

    static void TestIRR(double[] values, string[] dateStrs, double expectedRate)
    {
      DateTime[] dates = ToDates(dateStrs);
      double rate = F.IRR(values, dates);
      AssertEqual(expectedRate, rate);
      AssertEqual(0, F.PresentValue(rate, values, dates)); // IRR(x) is the value of r where NPV(r,x) == 0, so test that
    }

    static void TestIRR(double[] values, double[] times, double expectedRate, double guess = 0.1)
    {
      double rate = F.IRR(values, times, guess);
      AssertEqual(expectedRate, rate);
      AssertEqual(0, F.PresentValue(rate, values, times)); // IRR(x) is the value of r where NPV(r,x) == 0, so test that
    }

    static DateTime[] ToDates(params string[] dateStrs)
    {
      DateTime[] dates = new DateTime[dateStrs.Length];
      for(int i = 0; i < dates.Length; i++) dates[i] = DateTime.Parse(dateStrs[i]);
      return dates;
    }
  }
}