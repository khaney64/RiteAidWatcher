using NUnit.Framework;
using RiteAidChecker;
using System;
using System.Collections.Generic;
using System.Text;

namespace RiteAidTests
{
    [TestFixture]
    public class BrowserCacheTest
    {
        [Test]
        public void Test()
        {
            var data = new RiteAidData()
            {
                BirthDate = "01/01/2000",
                City = "***REMOVED***",
                StateName = "Pennsylvania",
                Zip = "***REMOVED***",
                Condition = ConditionType.WeakendImmuneSystem,
                Occupation = OccupationType.NoneOfTheAbove
            };

            var cache = new BrowserCache(5, data, Checker.Initializer, Checker.Resetter);

            var browser1 = cache.Pop();
            var browser2 = cache.Pop();

            cache.Push(browser1);

            browser1 = cache.Pop();




        }
    }
}
