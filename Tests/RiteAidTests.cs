using NUnit.Framework;
using RiteAidChecker;
using System;

namespace RiteAidTests
{
    [TestFixture]
    public class Tests
    {
       [Test]
       public void RiteAidCreate()
        {
            var data = new RiteAidData()
            {
                BirthDate = "01/01/2000",
                City = "***REMOVED***",
                State = "Pennsylvania",
                Zip = "***REMOVED***",
                Condition = ConditionType.WeakendImmuneSystem,
                Occupation = OccupationType.NoneOfTheAbove
            };

            var cache = new BrowserCache(1, data, Checker.Initializer, Checker.Resetter);
            var browser = cache.Pop();
            var available = Checker.Check("19406", "11158", browser);
        }

    }
}
