/*
AdamMil.Mathematics is a library that provides some useful mathematics classes
for the .NET framework.

http://www.adammil.net/
Copyright (C) 2007-2019 Adam Milazzo

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
using System.Text;
using AdamMil.Mathematics.RootFinding;
using AdamMil.Utilities;

// TODO: add bond valuation functions

namespace AdamMil.Mathematics
{
  /// <summary>Provides functions related to financial calculations.</summary>
  public static class Financial
  {
    /// <summary>Computes a compound interest rate given a nominal rate and a number of periods.</summary>
    /// <param name="nominalRate">A nominal, or simple, interest rate</param>
    /// <param name="periods">The number of periods over which the nominal rate will be compounded</param>
    /// <returns>Returns the compound rate. For example, 1% interest per month is CompoundRate(0.01, 12), which equals an annual compound
    /// rate (APY) of about 12.68%.
    /// </returns>
    public static double CompoundRate(double nominalRate, int periods)
    {
      return CompoundRate(nominalRate, (double)periods);
    }

    /// <summary>Computes a periodic interest rate given a compounded rate and a number of periods.</summary>
    /// <param name="nominalRate">A nominal, or simple, interest rate</param>
    /// <param name="periods">The number of periods over which the nominal rate will be compounded</param>
    /// <returns>Returns the compound rate. For example, 0.1% interest per day is CompoundRate(0.001, 365.25), which equals an annual
    /// compound rate (APY) of about 44.06%.
    /// </returns>
    public static double CompoundRate(double nominalRate, double periods)
    {
      if(nominalRate < 0) throw new ArgumentOutOfRangeException("nominalRate");
      if(periods <= 0) throw new ArgumentOutOfRangeException("periods");
      return Math.Pow(1 + nominalRate, periods) - 1;
    }

    /// <summary>Computes the future value of a series of cash flows, assuming they earn interest at a given rate.</summary>
    /// <param name="rate">The periodic rate at which the cash flow values earn interest</param>
    /// <param name="values">The cash flow values, where negative values are amounts paid and positive values are amounts received.
    /// The first value relates to the present time and receives interest for the full number of periods, the second relates to the
    /// subsequent period, and receives one less period of interest, etc.
    /// </param>
    public static double FutureValue(double rate, IEnumerable<double> values)
    {
      if(rate < -1) throw new ArgumentOutOfRangeException("rate");
      if(values == null) throw new ArgumentNullException();
      // FV(rate, values) = sum(v_i * (1+rate)^(n-i-1))
      rate += 1;
      double fv = 0;
      IList<double> valueList = values as IList<double>;
      if(valueList == null)
      {
        double power = Math.Pow(rate, values.Count() - 1);
        rate = 1 / rate; // invert the rate so we can reduce the power by multiplying
        foreach(double value in values)
        {
          fv += value * power;
          power *= rate;
        }
      }
      else if(valueList.Count != 0) // if we have a list, then we can work backwards and exponentiate incrementally
      {
        int i = valueList.Count - 1;
        fv = valueList[i--];
        for(double power = rate; i >= 0; power *= rate, i--) fv += valueList[i] * power;
      }

      return fv;
    }

    /// <include file="documentation.xml" path="/Math/Financial/FutureValue/node()[@node != 'presentValue' and @node != 'payAtEnd']"/>
    public static double FutureValue(double rate, double payment, int paymentCount)
    {
      return FutureValue(rate, payment, paymentCount, 0, true);
    }

    /// <include file="documentation.xml" path="/Math/Financial/FutureValue/node()[@node != 'presentValue']"/>
    public static double FutureValue(double rate, double payment, int paymentCount, bool payAtEnd)
    {
      return FutureValue(rate, payment, paymentCount, 0, payAtEnd);
    }

    /// <include file="documentation.xml" path="/Math/Financial/FutureValue/node()[@node != 'payAtEnd']"/>
    public static double FutureValue(double rate, double payment, int paymentCount, double presentValue)
    {
      return FutureValue(rate, payment, paymentCount, presentValue, true);
    }

    /// <include file="documentation.xml" path="/Math/Financial/FutureValue/node()"/>
    public static double FutureValue(double rate, double payment, int paymentCount, double presentValue, bool payAtEnd)
    {
      if(paymentCount < 0) throw new ArgumentOutOfRangeException("paymentCount");
      double sum;
      if(rate != 0)
      {
        if(rate < -1) throw new ArgumentOutOfRangeException("rate");
        // for an annuity-immediate (payAtEnd == true), the future value of the payments is p*(r+1)^(n-1) + p*(r+1)^(n-2) + ... + p*(r+1)^0
        // i.e. the first payment gets n-1 periods of interest (minus one because the first payment is at the end of the first period and
        // gets no interest from it), the second payment gets n-2 periods of interest, and the last payment gets 0 periods of interest.
        // now, sum(b^i) for i from 0 to n-1 equals (b^n - 1) / (b - 1). b in this case is r+1 and after substitution we have
        // ((r+1)^n - 1) / r. after multiplying by p and negating, the full answer is -p * ((r+1)^n - 1) = p * (1 - (r+1)^n). we negate
        // since if you pay now, you receive in the future, and vice versa.
        double power = Math.Pow(rate + 1, paymentCount);
        sum = (1 - power) / rate * payment;
        // if payAtEnd is false, it's an annuity-due, meaning the payments are made at the beginnings of the periods instead of the ends,
        // so the future value of the payments is p*(r+1)^n + p*(r+1)^(n-1) + ... + p*(r+1)^1. this is equal to the annuity-immediate sum
        // times r+1, which effectively increments all the exponents
        if(!payAtEnd) sum *= rate + 1;
        // finally, we have to add the future value of our starting value, negated, which is it multiplied by all n periods of interest
        if(presentValue != 0) sum -= presentValue * power;
      }
      else // if the rate is zero, then the future value is simply the negation of the present value plus the payments.
      {    // (it's negated since if you pay X now, you receive X in the future, and vice versa)
        sum = -paymentCount * payment - presentValue;
      }
      return sum;
    }

    /// <summary>Computes the periodic internal rate of return for an series of cash flows.</summary>
    /// <param name="values">The cash flow values, where negative values are amounts paid and positive values are amounts received. The
    /// list must contain at least one positive and one negative value.
    /// </param>
    /// <param name="guess">A guess for the result, which must be greater than or equal to -1. The default is 0.1 (10%).</param>
    /// <returns>Returns the rate that would make the <see cref="PresentValue(double,IEnumerable{double})"/> of the series equal to zero.
    /// </returns>
    public static double IRR(IEnumerable<double> values, double guess = 0.1)
    {
      if(values == null) throw new ArgumentNullException();
      return RateNewton(guess, (rate, needDerivative) =>
      {
        // this function uses Newton's method to solve for the rate where NPV(rate, values, times) == 0. NPV(rate, values, times) is
        // equivalent to sum(values[i] / (rate+1)^times[i]) over the lists. for brevity, we'll abbreviate this as sum(v_i / r^t_i).
        // Newton's method requires the derivative of this, which is sum(-t_i * v_i / r^(t_i+1)). for efficiency, we can compute
        // v / r^t as v * r^-t (avoiding division) and v / r^(t+1) as v / r^t * r (reusing the power)
        double fx = 0, dfx = 0, factor = rate + 1;
        int i = 1;
        foreach(double value in values)
        {
          if(--i == 0) { fx = value; continue; }
          double vpow = value * Math.Pow(factor, i);
          fx  += vpow;
          if(needDerivative) dfx += vpow * i / factor;
        }
        if(i >= 0) fx = double.NaN; // we need at least two values to perform the computation
        return new Result(fx, dfx);
      });
    }

    /// <include file="documentation.xml" path="/Math/Financial/XIRRA/node()[@name != 'guess']"/>
    public static double IRR(IEnumerable<double> values, IEnumerable<DateTime> dates)
    {
      return IRR(values, dates, RateGuess);
    }

    /// <include file="documentation.xml" path="/Math/Financial/XIRRA/node()"/>
    public static double IRR(IEnumerable<double> values, IEnumerable<DateTime> dates, double guess)
    {
      if(dates == null) throw new ArgumentNullException();
      return IRR(values, dates.Select(t => t.Ticks * (1 / TicksPerYear)), guess);
    }

    /// <include file="documentation.xml" path="/Math/Financial/XIRR/node()[@name != 'guess']"/>
    public static double IRR(IEnumerable<double> values, IEnumerable<double> times)
    {
      return IRR(values, times, RateGuess);
    }

    /// <include file="documentation.xml" path="/Math/Financial/XIRR/node()"/>
    public static double IRR(IEnumerable<double> values, IEnumerable<double> times, double guess)
    {
      return RateNewton(guess, (rate, needDerivative) =>
      {
        // this function uses Newton's method to solve for the rate where NPV(rate, values, times) == 0. NPV(rate, values, times) is
        // equivalent to sum(values[i] / (rate+1)^(times[i]-times[0]).TotalYears) over the lists. for brevity, we'll abbreviate
        // this as sum(v_i / r^t_i). Newton's method requires the derivative of this, which is sum(-t_i * v_i / r^(t_i+1)). for
        // efficiency, we can compute v / r^t as v * r^-t (avoiding division) and v / r^(t+1) as v / r^t * r (reusing the power)
        double fx = 0, firstTime = 0, dfx = 0, factor = rate + 1;
        int count = Apply(values, times,
          (a, b) => { fx = a; firstTime = b; },
          (a, b) => { double t = firstTime - b, vpow = a * Math.Pow(factor, t); fx += vpow; if(needDerivative) dfx += vpow * t / factor; });
        if(count < 2) fx = double.NaN; // we need at least two values to perform the computation
        return new Result(fx, dfx);
      });
    }

    /// <summary>Computes the modified internal rate of return for a series of periodic cash flows.</summary>
    /// <param name="values">The cash flow values, where negative values are amounts paid and positive values are amounts received. The
    /// sequence must contain at least one positive and one negative value.
    /// </param>
    /// <param name="financeRate">The interest rate on the money you pay (i.e. on the negative cash flow values)</param>
    /// <param name="reinvestmentRate">The interest rate on the money you receive (i.e. on the positive cash flow values)</param>
    /// <returns></returns>
    public static double ModifiedIRR(IEnumerable<double> values, double financeRate, double reinvestmentRate)
    {
      if(values == null) throw new ArgumentNullException();
      double pv = PresentValue(financeRate, values.Where(v => v < 0));
      double fv = FutureValue(reinvestmentRate, values.Where(v => v > 0));
      if(pv == 0 || fv == 0) return double.NaN;
      return Math.Pow(fv / -pv, 1.0 / (values.Count() - 1)) - 1;
    }

    /// <include file="documentation.xml" path="/Math/Financial/Payment/node()[@name != 'futureValue' and @name != 'payAtEnd']"/>
    public static double Payment(double rate, int paymentCount, double presentValue)
    {
      return Payment(rate, paymentCount, presentValue, 0, true);
    }

    /// <include file="documentation.xml" path="/Math/Financial/Payment/node()[@name != 'futureValue']"/>
    public static double Payment(double rate, int paymentCount, double presentValue, bool payAtEnd)
    {
      return Payment(rate, paymentCount, presentValue, 0, payAtEnd);
    }

    /// <include file="documentation.xml" path="/Math/Financial/Payment/node()[@name != 'payAtEnd']"/>
    public static double Payment(double rate, int paymentCount, double presentValue, double futureValue)
    {
      return Payment(rate, paymentCount, presentValue, futureValue, true);
    }

    /// <include file="documentation.xml" path="/Math/Financial/Payment/node()"/>
    public static double Payment(double rate, int paymentCount, double presentValue, double futureValue, bool payAtEnd)
    {
      if(paymentCount < 0) throw new ArgumentOutOfRangeException("paymentCount");
      if(paymentCount == 0)
      {
        if(presentValue == futureValue) return 0;
        else return double.NaN;
      }
      else if(rate != 0)
      {
        if(rate < -1) throw new ArgumentOutOfRangeException("rate");
        double power = Math.Pow(rate + 1, paymentCount), denom = 1 - power;
        if(!payAtEnd) denom *= rate + 1;
        return ((presentValue*power + futureValue) * rate) / denom;
      }
      else
      {
        return -(presentValue + futureValue) / paymentCount;
      }
    }

    /// <summary>Computes a periodic interest rate given a compounded rate and a number of periods.</summary>
    /// <param name="compoundRate">A compound interest rate</param>
    /// <param name="periods">The number of periods over which the periodic rate is compounded to equal the <paramref name="compoundRate"/></param>
    /// <returns>Returns the periodic rate. For example, 5% APY compounded monthly is PeriodicRate(0.05, 12), which equals a monthly rate
    /// of about 0.407%.
    /// </returns>
    public static double PeriodicRate(double compoundRate, int periods)
    {
      return PeriodicRate(compoundRate, (double)periods);
    }

    /// <summary>Computes a periodic interest rate given a compounded rate and a number of periods.</summary>
    /// <param name="compoundRate">A compound interest rate</param>
    /// <param name="periods">The number of periods over which the periodic rate is compounded to equal the <paramref name="compoundRate"/></param>
    /// <returns>Returns the periodic rate. For example, 5% APY compounded daily is PeriodicRate(0.05, 365.25), which equals a daily rate
    /// of about 0.01336%.
    /// </returns>
    public static double PeriodicRate(double compoundRate, double periods)
    {
      if(compoundRate < 0) throw new ArgumentOutOfRangeException("compoundRate");
      if(periods <= 0) throw new ArgumentOutOfRangeException("periods");
      return Math.Pow(1 + compoundRate, 1.0 / periods) - 1;
    }

    /// <include file="documentation.xml" path="/Math/Financial/Periods/node()[@name != 'futureValue' and @name != 'payAtEnd']"/>
    public static double Periods(double rate, double payment, double presentValue)
    {
      return Periods(rate, payment, presentValue, 0, true);
    }

    /// <include file="documentation.xml" path="/Math/Financial/Periods/node()[@name != 'payAtEnd']"/>
    public static double Periods(double rate, double payment, double presentValue, double futureValue)
    {
      return Periods(rate, payment, presentValue, futureValue, true);
    }

    /// <include file="documentation.xml" path="/Math/Financial/Periods/node()[@name != 'futureValue']"/>
    public static double Periods(double rate, double payment, double presentValue, bool payAtEnd)
    {
      return Periods(rate, payment, presentValue, 0, payAtEnd);
    }

    /// <include file="documentation.xml" path="/Math/Financial/Periods/node()"/>
    public static double Periods(double rate, double payment, double presentValue, double futureValue, bool payAtEnd)
    {
      if(rate != 0)
      {
        if(rate < -1) throw new ArgumentOutOfRangeException("rate");
        double num = payment, den = payment;
        if(futureValue != 0) num -= futureValue * rate;
        if(presentValue != 0) den += presentValue * rate;
        if(!payAtEnd)
        {
          double pr = payment * rate;
          num += pr;
          den += pr;
        }
        return Math.Log(num / den, rate + 1);
      }
      else if (payment != 0)
      {
        return -(presentValue + futureValue) / payment;
      }
      else if (futureValue == presentValue)
      {
        return 0;
      }
      else
      {
        return double.NaN;
      }
    }

    /// <summary>Computes the net present value of periodic cash flows.</summary>
    /// <param name="rate">The periodic rate at which the cash flow values are discounted</param>
    /// <param name="values">The cash flow values, where negative values are amounts paid and positive values are amounts received.
    /// The first value is undiscounted and relates to the present time, the second relates to the subsequent period, the third
    /// relates to the period after that, etc.
    /// </param>
    /// <returns>Returns the sum of the cash flows discounted back to their present values.</returns>
    public static double PresentValue(double rate, IEnumerable<double> values)
    {
      if(rate < -1) throw new ArgumentOutOfRangeException("rate");
      if(values == null) throw new ArgumentNullException();
      // NPV(rate, values) = sum(v_i / (1+rate)^i), but we use 1/(1+rate) to avoid division, and we exponentiate incrementally
      rate = 1 / (rate + 1);
      double power = rate, npv = 0;
      int count = 0;
      foreach(double value in values)
      {
        if(count++ == 0) { npv = value; continue; }
        npv += value * power;
        power *= rate;
      }
      return npv;
    }

    /// <summary>Computes the net present value of non-periodic cash flows.</summary>
    /// <param name="rate">The annual rate at with the cash flow values are discounted</param>
    /// <param name="values">The cash flow values, where negative values are amounts paid and positive values are amounts received. The
    /// list must contain at least one positive and one negative value.
    /// </param>
    /// <param name="dates">The list of dates on which the cash flows occurred</param>
    /// <returns>Returns the sum of the cash flows discounted back to their present values.</returns>
    /// <remarks>This method assumes there are 365.25 days per year.</remarks>
    public static double PresentValue(double rate, IEnumerable<double> values, IEnumerable<DateTime> dates)
    {
      if(dates == null) throw new ArgumentNullException();
      return PresentValue(rate, values, dates.Select(d => d.Ticks * (1 / TicksPerYear)));
    }

    /// <summary>Computes the net present value of non-periodic cash flows.</summary>
    /// <param name="rate">The rate at with the cash flow values are discounted per unit of time</param>
    /// <param name="values">The cash flow values, where negative values are amounts paid and positive values are amounts received</param>
    /// <param name="times">The sequence of times at which the cash flows occurred, expressed in arbitrary units</param>
    /// <returns>Returns the sum of the cash flows discounted back to their present values.</returns>
    public static double PresentValue(double rate, IEnumerable<double> values, IEnumerable<double> times)
    {
      if(rate < -1) throw new ArgumentOutOfRangeException("rate");
      double npv = 0, firstTime = 0;
      Apply(values, times, (a, b) => { npv = a; firstTime = b; rate = rate + 1; }, (a, b) => npv += a * Math.Pow(rate, firstTime - b));
      return npv;
    }

    /// <include file="documentation.xml" path="/Math/Financial/PresentValue/node()[@name != 'futureValue' and @name != 'payAtEnd']"/>
    public static double PresentValue(double rate, double payment, int paymentCount)
    {
      return PresentValue(rate, payment, paymentCount, 0, true);
    }

    /// <include file="documentation.xml" path="/Math/Financial/PresentValue/node()[@name != 'futureValue']"/>
    public static double PresentValue(double rate, double payment, int paymentCount, bool payAtEnd)
    {
      return PresentValue(rate, payment, paymentCount, 0, payAtEnd);
    }

    /// <include file="documentation.xml" path="/Math/Financial/PresentValue/node()[@name != 'payAtEnd']"/>
    public static double PresentValue(double rate, double payment, int paymentCount, double futureValue)
    {
      return PresentValue(rate, payment, paymentCount, futureValue, true);
    }

    /// <include file="documentation.xml" path="/Math/Financial/PresentValue/node()"/>
    public static double PresentValue(double rate, double payment, int paymentCount, double futureValue, bool payAtEnd)
    {
      if(paymentCount < 0) throw new ArgumentOutOfRangeException("paymentCount");
      double sum;
      if(rate != 0)
      {
        if(rate < -1) throw new ArgumentOutOfRangeException("rate");
        // for an annuity-immediate (payAtEnd == true), the present value of the payments is p/(r+1)^1 + p/(r+1)^2 + ... + p/(r+1)^n,
        // i.e. each payment is discounted by the discount rate and we start with an exponent of 1 because the payments are made at
        // the ends of the periods, so the first payment must already be discounted by one period. the equation simplifies to
        // p*(r+1)^-1 + p*(r+1)^-2 + ... = p*((r+1)^-1 + (r+1)-2 + ...). now, sum(b^-i) for i from 1 to n equals (1 - b^-n) / (b - 1) and
        // since b in our case equals r+1, after substituting we have (1 - (r+1)^-n) / r. after negating and multiplying by p, the full
        // answer is p * ((r+1)^-n - 1) / r. we negate because to receive in the future you must pay now and vice versa
        double power = Math.Pow(rate + 1, -paymentCount);
        sum = (power - 1) / rate * payment;
        // if payAtEnd is false, it's an annuity-due, meaning the payments are made at the beginnings of the periods instead of the ends,
        // so the present value of the payments is p/(r+1)^0 + p/(r+1)^1 + ... + p/(r+1)^(n-1). this is equal to the annuity-immediate sum
        // multiplied by r+1, which effectively decrements all the exponents in the denominators
        if(!payAtEnd) sum *= rate + 1;
        // finally, we have to add the future value of our starting value, negated, which is it divided by all n periods of interest
        if(futureValue != 0) sum -= futureValue * power;
      }
      else // if the interest rate is zero, the present value is simply the negation of the future value, which is futureValue
      {    // plus the sum of the payments. we negate because to receive X in the future you must pay X now, and vice versa
        sum = -paymentCount * payment - futureValue; // -(payments+futureValue)
      }
      return sum;
    }

    /// <include file="documentation.xml" path="/Math/Financial/Rate/node()[@name != 'futureValue' and @name != 'payAtEnd' and @name != 'guess']"/>
    public static double Rate(double payment, int paymentCount, double presentValue)
    {
      return Rate(payment, paymentCount, presentValue, 0, true, RateGuess);
    }

    /// <include file="documentation.xml" path="/Math/Financial/Rate/node()[@name != 'payAtEnd' and @name != 'guess']"/>
    public static double Rate(double payment, int paymentCount, double presentValue, double futureValue)
    {
      return Rate(payment, paymentCount, presentValue, futureValue, true, RateGuess);
    }

    /// <include file="documentation.xml" path="/Math/Financial/Rate/node()[@name != 'futureValue' and @name != 'guess']"/>
    public static double Rate(double payment, int paymentCount, double presentValue, bool payAtEnd)
    {
      return Rate(payment, paymentCount, presentValue, 0, payAtEnd, RateGuess);
    }

    /// <include file="documentation.xml" path="/Math/Financial/Rate/node()[@name != 'guess']"/>
    public static double Rate(double payment, int paymentCount, double presentValue, double futureValue, bool payAtEnd)
    {
      return Rate(payment, paymentCount, presentValue, futureValue, payAtEnd, RateGuess);
    }

    /// <include file="documentation.xml" path="/Math/Financial/Rate/node()"/>
    public static double Rate(double payment, int paymentCount, double presentValue, double futureValue, bool payAtEnd, double guess)
    {
      return RateNewton(guess, (rate, needDerivative) =>
      {
        double fx, dfx = 0;
        if(rate != 0)
        {
          if(rate < -1) throw new ArgumentOutOfRangeException("rate");
          // the value of this method is PresentValue(r, p, n, futureValue, payAtEnd) - presentValue. first, compute PresentValue
          // while saving some intermediate values that will be of use in computing the derivative. (see PresentValue for details.)
          double rp1 = rate + 1, lpower = Math.Pow(rp1, -paymentCount-1), power = lpower * rp1;
          fx = (power - 1) / rate * payment;
          if(!payAtEnd) fx *= rp1;
          if(futureValue != 0) fx -= futureValue * power;
          fx -= presentValue; // now that we have the present value given the rate, compare it to the known present value
          if(needDerivative)
          {
            double nlpower = lpower * paymentCount;
            dfx = (1 - power - nlpower * rate) / (rate * rate) * payment;
            if(!payAtEnd) dfx = dfx*rp1 + fx;
            if(futureValue != 0) dfx += nlpower * futureValue;
          }
        }
        else // if the interest rate is zero, the present value is simply the negation of the future value, which is futureValue
        {    // plus the sum of the payments. we negate because to receive X in the future you must pay X now, and vice versa
          fx = -paymentCount * payment - futureValue - presentValue;
          dfx = 0;
        }
        return new Result(fx, dfx);
      });
    }

    const double RateGuess = 0.1, TicksPerYear = TimeSpan.TicksPerDay * 365.25;

    struct Result
    {
      public Result(double fx, double dfx) { FX = fx; DFX = dfx; }
      public double FX, DFX;
    }

    static int Apply<T>(IEnumerable<T> items, Action<T> first, Action<T> subsequent)
    {
      if(items == null) throw new ArgumentNullException();
      int count = 0;
      using(IEnumerator<T> e = items.GetEnumerator())
      {
        if(e.MoveNext())
        {
          count = 1;
          first(e.Current);
          for(; e.MoveNext(); count++) subsequent(e.Current);
        }
      }
      return count;
    }

    static int Apply<T,U>(IEnumerable<T> a, IEnumerable<U> b, Action<T,U> first, Action<T,U> subsequent)
    {
      if(a == null || b == null) throw new ArgumentNullException();
      int count = 0;
      IEnumerator<T> ea = a.GetEnumerator();
      IEnumerator<U> eb = null;
      try
      {
        eb = b.GetEnumerator();
        bool na = ea.MoveNext(), nb = eb.MoveNext();
        if(na != nb) goto countMismatch;
        if(na)
        {
          first(ea.Current, eb.Current);
          for(count=1; ; count++)
          {
            na = ea.MoveNext();
            nb = eb.MoveNext();
            if(na != nb) goto countMismatch;
            if(!na) break;
            subsequent(ea.Current, eb.Current);
          }
        }
      }
      finally
      {
        ea.Dispose();
        if(eb != null) eb.Dispose();
      }
      return count;

      countMismatch: throw new ArgumentException("Count mismatch");
    }

    static double RateNewton(double guess, Func<double, bool, Result> evaluate)
    {
      if(guess < -1) throw new ArgumentOutOfRangeException("guess");

      for(int tries = 0; tries < 2; tries++)
      {
        // we'll first try without bracketing the root because bracketing tends to fail. the numbers involved are such that they tend to
        // produce a nearly flat curve with an extremely thin spike around the correct value, which is hard for the root bracketer to find
        double rate = guess;
        for(int iters = 0; iters < 50; iters++)
        {
          Result r = evaluate(rate, true);
          double newRate = rate - r.FX / r.DFX;
          if(RelativeError(newRate, rate) < 1e-12) return Math.Max(-1, newRate); // if converged, clip to the legal range and return
          else if(double.IsNaN(newRate)) break; // otherwise, if the calculation has failed, abort
          // there are often multiple solutions to the problem, but we want the solution where rate >= -1. sometimes there are multiple
          // such solutions, but choosing among them is nontrivial, so we just rely on the 'guess' being close enough
          if(newRate > -1) rate = newRate; // however, we can't iterate when rate is exactly -1 so don't accept that here
          // if it's out of bounds, we can't just clip rate to -1+epsilon since there's an infinity at -1 which we need to stay
          // away from. we'll handle that by halving our distance from the old rate to -1 in order to approach it gradually.
          // setting it to -0.9 generally works too, but in some non-comprehensive tests it converged more slowly
          else rate = rate*0.5 - 0.5; // rate = average(rate, -1) = (rate + -1) / 2 = (rate - 1) / 2 = rate/2 - 1/2
        }

        // if the initial attempt failed, see if we can bracket the root in case the failure was due to a local minimum. normal root
        // bracketing methods don't work so well because positive rates are unbounded while negative rates are limited to the interval
        // [-1,0). so, positive rates can be scaled normally (75% -> 150%), but negative rates cannot (-150% is not a valid rate). to
        // handle this, we'll expand negative rates asymptotically towards -1
        RootBracket bracket;
        if(guess > 0) bracket = new RootBracket(guess * 0.5, guess * 1.5);
        else if(guess < 0) bracket = new RootBracket(guess * 1.5, guess * 0.5); // allow rates to be < -1 in the bracket
        else bracket = new RootBracket(-0.1, 0.1);

        // now expand the bracket until we find a zero crossing. create a transformation that converts all values into legal rates
        Func<double, double> transform = x => x >= 0 ? x : 1/(1-x) - 1; // make negative x values asymptotically approach -1
        if(!FindRoot.BracketOutward(x => evaluate(transform(x), false).FX, ref bracket)) break; // no root, no solution
        guess = transform(bracket.Middle); // if we found a bracket containing a root, set the guess to the center of it
      }

      return double.NaN;
    }

    static double RelativeError(double a, double b)
    {
      if(a == 0) return b; // relative error is undefined when either value is zero, so use absolute error in that case
      else if(b == 0) return a;
      else return Math.Abs(1 - a/b);
    }
  }
}
