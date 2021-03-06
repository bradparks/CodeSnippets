﻿namespace Dixin.Linq.EntityFramework
{
    using System.Diagnostics;
    using System.Linq;

    public static class QueryExpressions
    {
        private static readonly AdventureWorks AdventureWorks = new AdventureWorks();

        #region Join

        internal static void InnerJoinWithJoin()
        {
            IQueryable<ProductSubcategory> outer = AdventureWorks.ProductSubcategories;
            IQueryable<ProductCategory> inner = AdventureWorks.ProductCategories;
            var subcategories =
                from subcategory in outer
                join category in inner
                on subcategory.ProductCategoryID equals category.ProductCategoryID
                select new { Subcategory = subcategory.Name, Category = category.Name }; // Define query.
            subcategories.ForEach(subcategory => Trace.WriteLine(
                $"{subcategory.Category}: {subcategory.Subcategory}")); // Execute query.
        }

        internal static void InnerJoinWithSelectMany()
        {
            IQueryable<ProductSubcategory> outer = AdventureWorks.ProductSubcategories;
            IQueryable<ProductCategory> inner = AdventureWorks.ProductCategories;
            var subcategories =
                from subcategory in outer
                from category in inner
                where subcategory.ProductCategoryID == category.ProductCategoryID
                select new { Subcategory = subcategory.Name, Category = category.Name }; // Define query.
            subcategories.ForEach(subcategory => Trace.WriteLine(
                $"{subcategory.Category}: {subcategory.Subcategory}")); // Execute query.
        }

        internal static void InnerJoinWithGroupJoin()
        {
            IQueryable<ProductSubcategory> outer = AdventureWorks.ProductSubcategories;
            IQueryable<ProductCategory> inner = AdventureWorks.ProductCategories;
            var subcategories =
                from subcategory in outer
                join category in inner
                on subcategory.ProductCategoryID equals category.ProductCategoryID into categories
                from category in categories // LEFT OUTER JOIN if DefaultIfEmpty is called.
                select new { Subcategory = subcategory.Name, Category = category.Name }; // Define query.
            subcategories.ForEach(subcategory => Trace.WriteLine(
                $"{subcategory.Category}: {subcategory.Subcategory}")); // Execute query.
        }

        internal static void InnerJoinWithSelect()
        {
            IQueryable<ProductSubcategory> outer = AdventureWorks.ProductSubcategories;
            IQueryable<ProductCategory> inner = AdventureWorks.ProductCategories;
            var categories =
                from subcategory in outer
                select new
                {
                    Subcategory = subcategory,
                    Categories = from category in inner
                                 where category.ProductCategoryID == subcategory.ProductCategoryID
                                 select category
                } into subcategory
                from category in subcategory.Categories // LEFT OUTER JOIN if DefaultIfEmpty is called.
                select new { Subcategory = subcategory.Subcategory.Name, Category = category.Name }; // Define query.
            categories.ForEach(category => Trace.WriteLine(
                $"{category.Category}: {category.Subcategory}")); // Execute query.
        }

        internal static void InnerJoinWithAssociation()
        {
            IQueryable<ProductSubcategory> outer = AdventureWorks.ProductSubcategories;
            var subcategories =
                from subcategory in outer
                select new { Subcategory = subcategory.Name, Category = subcategory.ProductCategory.Name }; // Define query.
            subcategories.ForEach(subcategory => Trace.WriteLine(
                $"{subcategory.Category}: {subcategory.Subcategory}")); // Execute query.
        }

        internal static void MultipleInnerJoinsWithAssociations()
        {
            IQueryable<Product> source = AdventureWorks.Products;
            var products =
                from product in source
                from productProductPhoto in product.ProductProductPhotos
                select new
                {
                    Product = product.Name,
                    Photo = productProductPhoto.ProductPhoto.LargePhotoFileName
                }; // Define query.
            products.ForEach(product => Trace.WriteLine($"{product.Product}: {product.Photo}")); // Execute query.
        }

        internal static void InnerJoinWithMultipleKeys()
        {
            IQueryable<ProductSubcategory> outer = AdventureWorks.ProductSubcategories;
            IQueryable<ProductCategory> inner = AdventureWorks.ProductCategories;
            var subcategories =
                from subcategory in outer
                join category in inner
                on new { Id = subcategory.ProductCategoryID, FirstLetter = subcategory.Name.Substring(0, 1) }
                    equals new { Id = category.ProductCategoryID, FirstLetter = category.Name.Substring(0, 1) }
                select new { Subcategory = subcategory.Name, Category = category.Name }; // Define query.
            subcategories.ForEach(subcategory => Trace.WriteLine($"{subcategory.Category}: {subcategory.Subcategory}")); // Execute query.
        }

        internal static void LeftOuterJoinWithGroupJoin()
        {
            IQueryable<ProductCategory> outer = AdventureWorks.ProductCategories;
            IQueryable<ProductSubcategory> inner = AdventureWorks.ProductSubcategories;
            var categories =
                from category in outer
                join subcategory in inner
                on category.ProductCategoryID equals subcategory.ProductCategoryID into subcategories
                select new
                {
                    Category = category.Name,
                    Subcategories = subcategories.Select(subcategory => subcategory.Name)
                }; // Define query.
            categories.ForEach(category => Trace.WriteLine(
                $"{category.Category}: {string.Join(", ", category.Subcategories)}")); // Execute query.
        }

        internal static void LeftOuterJoinWithSelect()
        {
            IQueryable<ProductCategory> outer = AdventureWorks.ProductCategories;
            IQueryable<ProductSubcategory> inner = AdventureWorks.ProductSubcategories;
            var categories =
                from category in outer
                select new
                {
                    Category = category,
                    Subcategories = from subcategory in inner
                                    where subcategory.ProductCategoryID == category.ProductCategoryID
                                    select subcategory
                }; // Define query.
            categories.ForEach(category => Trace.WriteLine(
                $"{category.Category}: {string.Join(", ", category.Subcategories)}")); // Execute query.
        }

        internal static void LeftOuterJoinWithGroupJoinAndSelectMany()
        {
            IQueryable<ProductCategory> outer = AdventureWorks.ProductCategories;
            IQueryable<ProductSubcategory> inner = AdventureWorks.ProductSubcategories;
            var categories =
                from category in outer
                join subcategory in inner
                on category.ProductCategoryID equals subcategory.ProductCategoryID into subcategories
                from subcategory in subcategories.DefaultIfEmpty() // INNER JOIN if DefaultIfEmpty is missing.
                select new { Category = category.Name, Subcategory = subcategory.Name }; // Define query.
            categories.ForEach(category => Trace.WriteLine(
                $"{category.Category}: {category.Subcategory}")); // Execute query.
        }

        internal static void LeftOuterJoinWithSelectAndSelectMany()
        {
            IQueryable<ProductCategory> outer = AdventureWorks.ProductCategories;
            IQueryable<ProductSubcategory> inner = AdventureWorks.ProductSubcategories;
            var categories =
                from category in outer
                select new
                {
                    Category = category,
                    Subcategories = from subcategory in inner
                                    where subcategory.ProductCategoryID == category.ProductCategoryID
                                    select subcategory
                } into category
                from subcategory in category.Subcategories.DefaultIfEmpty() // INNER JOIN if DefaultIfEmpty is missing.
                select new { Category = category.Category.Name, Subcategory = subcategory.Name }; // Define query.
            categories.ForEach(category => Trace.WriteLine(
                $"{category.Category}: {category.Subcategory}")); // Execute query.
        }

        internal static void LeftOuterJoinWithAssociation()
        {
            IQueryable<ProductCategory> source = AdventureWorks.ProductCategories;
            var categories = source.SelectMany(
                category => category.ProductSubcategories.DefaultIfEmpty(), // INNER JOIN if DefaultIfEmpty is missing.
                (category, subcategory) =>
                    new { Category = category.Name, Subcategory = subcategory.Name }); // Define query.
            categories.ForEach(subcategory => Trace.WriteLine(
                $"{subcategory.Category}: {subcategory.Subcategory}")); // Execute query.
        }

        internal static void CrossJoinWithSelectMany()
        {
            IQueryable<Product> outer = AdventureWorks.Products.Where(product => product.ListPrice > 2000);
            IQueryable<Product> inner = AdventureWorks.Products.Where(product => product.ListPrice < 100);
            var bundles =
                from outerProduct in outer
                from innerProduct in inner
                    // where true == true
                select new { Expensive = outerProduct.Name, Cheap = innerProduct.Name }; // Define query.
            bundles.ForEach(bundle => Trace.WriteLine($"{bundle.Expensive}: {bundle.Cheap}")); // Execute query.
        }

        internal static void CrossJoinWithJoin()
        {
            IQueryable<Product> outer = AdventureWorks.Products.Where(product => product.ListPrice > 2000);
            IQueryable<Product> inner = AdventureWorks.Products.Where(product => product.ListPrice < 100);
            var bundles =
                from outerProduct in outer
                join innerProduct in inner
                on true equals true
                select new { Expensive = outerProduct.Name, Cheap = innerProduct.Name }; // Define query.
            bundles.ForEach(bundle => Trace.WriteLine($"{bundle.Expensive}: {bundle.Cheap}")); // Execute query.
        }

        internal static void SelfJoin()
        {
            IQueryable<Product> outer = AdventureWorks.Products;
            IQueryable<Product> inner = AdventureWorks.Products;
            var products =
                from outerProduct in outer
                join innerProduct in inner
                on outerProduct.ListPrice equals innerProduct.ListPrice into samePriceProducts
                select new
                {
                    Name = outerProduct.Name,
                    ListPrice = outerProduct.ListPrice,
                    SamePriceProducts = from samePriceProduct in samePriceProducts
                                        where samePriceProduct.ProductID != outerProduct.ProductID
                                        select samePriceProduct.Name
                }; // Define query.
            products.ForEach(product => Trace.WriteLine(
                $"{product.Name} ({product.ListPrice}): {string.Join(", ", product.SamePriceProducts)}")); // Execute query.
        }

        #endregion
    }
}
