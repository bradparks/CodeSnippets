﻿namespace Dixin.Tests.Linq.LinqToSql
{
    using System.Linq;

    using Dixin.Linq.LinqToSql;
    using Dixin.Linq.Tests;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class PerformanceTests
    {
        [TestMethod]
        public void CompiedQueryTest()
        {
            using (AdventureWorks adventureWorks = new AdventureWorks())
            {
                string[] productNames = adventureWorks.GetProductNames(539.99M).ToArray();
                EnumerableAssert.Any(productNames);
            }
        }

        [TestMethod]
        public void PerformanceTest()
        {
            Performance.QueryPlanCache();
        }
    }
}
