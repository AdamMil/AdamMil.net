using System;
using NUnit.Framework;

namespace AdamMil.Mathematics.Tests
{
  [TestFixture]
  public class NumberTheoryTests
  {
    [Test]
    public void TestGCD()
    {
      TestGCD(0, 0, 0);
      TestGCD(100, 0, 100);
      TestGCD(1, 5, 7);
      TestGCD(5, 5, 35);
      TestGCD(7, 7, 35);
      TestGCD(2*5*11*17, 2*3*5*7*11*13*17, 2*5*11*17*19*23*29);
      TestGCD(2, 12411065049670607744, 9578558961197277046);
      TestGCD(1, 3436701340556985009, 10710734789589348178);
      TestGCD(254, 18322197173232047100, 1466435351843571022);
      TestGCD(605, 4964574171310862295, 13103168246785768580);
      TestGCD(292, Integer.Parse("190133407180293628637345899905613877144"), Integer.Parse("59899486053804935095873397972340589932"));
      TestGCD(1685, Integer.Parse("234651270368444371298198464450568137720"), Integer.Parse("302970554257309038402475874830016674195"));    
      TestGCD(17871, Integer.Parse("190264569597780290173397018862602413617"), Integer.Parse("1857473862523614126655284902795532813"));
    }

    [Test]
    public void TestLCM()
    {
      TestLCM(0, 0, 0);
      TestLCM(0, 0, 100);
      TestLCM(35, 5, 7);
      TestLCM(35, 5, 35);
      TestLCM(35, 7, 35);
      TestLCM(2*3*5*7*11*13*17, 2*3*5*7*13, 3*5*7*11*17);
      TestLCM(2493164873054850, 364836489, 211843150);
      TestLCM(2649723800061770806, 1613361761, 1642361846);
      TestLCM(311377899525158537, 425630917, 731567861);
      TestLCM(Integer.Parse("9408297552411116521162399345095758163"), 2493550112043966483, 3773053329455296961);
      TestLCM(Integer.Parse("10872495886286006285406985716448051465"), 7148684226133456885, 1520908679465712709);
      TestLCM(Integer.Parse("189290234361616953377836574315"), 200644048187027, 943413154150345);
      TestLCM(Integer.Parse("7508265049978596976054898356380"), 173467579470488460, 1947752590313385);
      TestLCM(Integer.Parse("21754538273023254741769210672797"), 35726964646579941, 123608913130728851);
      TestLCM(Integer.Parse("17088360486922107847450078434778521249016442685776298771611750802443720"),
              Integer.Parse("32734851458000707108537897373218265816"), Integer.Parse("1363525282804708718931205810627916540"));
      TestLCM(Integer.Parse("41062329509115957036470387105483957134772969191467129711416667931680352"),
              Integer.Parse("19154130214884489462547719808646074222"), Integer.Parse("10851837679041197649144417343387789792"));
      TestLCM(Integer.Parse("47452128821015107517212513858770125843048815153698585560319395153689064"),
              Integer.Parse("159159870318728817569977923410788146232"), Integer.Parse("1192565154168293481300477596057708"));
    }

    static void TestGCD(int expected, int a, int b)
    {
      Assert.AreEqual(expected, NumberTheory.GreatestCommonFactor(a, b));
      Assert.AreEqual(expected, NumberTheory.GreatestCommonFactor(b, a));
      Assert.AreEqual(expected, NumberTheory.GreatestCommonFactor(-a, b));
      Assert.AreEqual(expected, NumberTheory.GreatestCommonFactor(-b, a));
      Assert.AreEqual(expected, NumberTheory.GreatestCommonFactor(a, -b));
      Assert.AreEqual(expected, NumberTheory.GreatestCommonFactor(b, -a));
      Assert.AreEqual(expected, NumberTheory.GreatestCommonFactor(-a, -b));
      Assert.AreEqual(expected, NumberTheory.GreatestCommonFactor(-b, -a));
      TestGCD(expected, (long)a, (long)b);
    }

    static void TestGCD(long expected, long a, long b)
    {
      Assert.AreEqual(expected, NumberTheory.GreatestCommonFactor(a, b));
      Assert.AreEqual(expected, NumberTheory.GreatestCommonFactor(b, a));
      Assert.AreEqual(expected, NumberTheory.GreatestCommonFactor(-a, b));
      Assert.AreEqual(expected, NumberTheory.GreatestCommonFactor(-b, a));
      Assert.AreEqual(expected, NumberTheory.GreatestCommonFactor(a, -b));
      Assert.AreEqual(expected, NumberTheory.GreatestCommonFactor(b, -a));
      Assert.AreEqual(expected, NumberTheory.GreatestCommonFactor(-a, -b));
      Assert.AreEqual(expected, NumberTheory.GreatestCommonFactor(-b, -a));
      TestGCD(expected, (Integer)a, (Integer)b);
    }

    static void TestGCD(Integer expected, Integer a, Integer b)
    {
      Assert.AreEqual(expected, NumberTheory.GreatestCommonFactor(a, b));
      Assert.AreEqual(expected, NumberTheory.GreatestCommonFactor(b, a));
      Assert.AreEqual(expected, NumberTheory.GreatestCommonFactor(-a, b));
      Assert.AreEqual(expected, NumberTheory.GreatestCommonFactor(-b, a));
      Assert.AreEqual(expected, NumberTheory.GreatestCommonFactor(a, -b));
      Assert.AreEqual(expected, NumberTheory.GreatestCommonFactor(b, -a));
      Assert.AreEqual(expected, NumberTheory.GreatestCommonFactor(-a, -b));
      Assert.AreEqual(expected, NumberTheory.GreatestCommonFactor(-b, -a));
    }

    static void TestLCM(long expected, int a, int b)
    {
      Assert.AreEqual(expected, NumberTheory.LeastCommonMultiple(a, b));
      Assert.AreEqual(expected, NumberTheory.LeastCommonMultiple(b, a));
      Assert.AreEqual(expected, NumberTheory.LeastCommonMultiple(-a, b));
      Assert.AreEqual(expected, NumberTheory.LeastCommonMultiple(-b, a));
      Assert.AreEqual(expected, NumberTheory.LeastCommonMultiple(a, -b));
      Assert.AreEqual(expected, NumberTheory.LeastCommonMultiple(b, -a));
      Assert.AreEqual(expected, NumberTheory.LeastCommonMultiple(-a, -b));
      Assert.AreEqual(expected, NumberTheory.LeastCommonMultiple(-b, -a));
      TestLCM(expected, (long)a, (long)b);
    }

    static void TestLCM(Integer expected, long a, long b)
    {
      Assert.AreEqual(expected, NumberTheory.LeastCommonMultiple(a, b));
      Assert.AreEqual(expected, NumberTheory.LeastCommonMultiple(b, a));
      Assert.AreEqual(expected, NumberTheory.LeastCommonMultiple(-a, b));
      Assert.AreEqual(expected, NumberTheory.LeastCommonMultiple(-b, a));
      Assert.AreEqual(expected, NumberTheory.LeastCommonMultiple(a, -b));
      Assert.AreEqual(expected, NumberTheory.LeastCommonMultiple(b, -a));
      Assert.AreEqual(expected, NumberTheory.LeastCommonMultiple(-a, -b));
      Assert.AreEqual(expected, NumberTheory.LeastCommonMultiple(-b, -a));
      TestLCM(expected, (Integer)a, (Integer)b);
    }

    static void TestLCM(Integer expected, Integer a, Integer b)
    {
      Assert.AreEqual(expected, NumberTheory.LeastCommonMultiple(a, b));
      Assert.AreEqual(expected, NumberTheory.LeastCommonMultiple(b, a));
      Assert.AreEqual(expected, NumberTheory.LeastCommonMultiple(-a, b));
      Assert.AreEqual(expected, NumberTheory.LeastCommonMultiple(-b, a));
      Assert.AreEqual(expected, NumberTheory.LeastCommonMultiple(a, -b));
      Assert.AreEqual(expected, NumberTheory.LeastCommonMultiple(b, -a));
      Assert.AreEqual(expected, NumberTheory.LeastCommonMultiple(-a, -b));
      Assert.AreEqual(expected, NumberTheory.LeastCommonMultiple(-b, -a));
    }
  }
}
