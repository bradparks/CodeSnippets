﻿namespace Dixin.Tests.Linq.EntityFramework
{
    using System.Linq;
    using System.Threading.Tasks;
    using System.Transactions;

    using Dixin.Linq.EntityFramework;
    using Dixin.Linq.Tests;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class PerformanceTests
    {
        [TestMethod]
        public void CompiedQueryTest()
        {
            using (LegacyAdventureWorks adventureWorks = new LegacyAdventureWorks())
            {
                string[] productNames = adventureWorks.GetProductNames(539.99M).ToArray();
                EnumerableAssert.Any(productNames);
            }
        }

        [TestMethod]
        public void ViewsTest()
        {
            Performance.MappingViews();
        }

        [TestMethod]
        public void PerformanceTest()
        {
            Performance.AddRange();
            Performance.RemoveRange();
        }

        [TestMethod]
        public void CacheTest()
        {
            Performance.CachedEntity();
            Performance.UncachedEntity();
            Performance.Find();
            Performance.TranslationCache();
            Performance.UncachedTranslation();
            Performance.CachedTranslation();
            Performance.CompiledCachedTranslation();
            Performance.Translation();
            Performance.UncachedSkipTake();
            Performance.CachedSkipTake();
        }

        [TestMethod]
        public async Task AsyncTest()
        {
            using (new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                await Performance.Async();
            }
        }

        [TestMethod]
        public async Task AsyncConcurrencyTest()
        {
            using (new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                await Performance.SaveChangesAsync();
            }
        }

        [TestMethod]
        public async Task AsyncTransactionTest()
        {
            await Performance.DbContextTransactionAsync();
            await Performance.DbTransactionAsync();
            await Performance.TransactionScopeAsync();
        }
    }
}
