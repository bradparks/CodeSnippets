﻿namespace Dixin.Tests.Linq.EntityFramework
{
    using System;
    using System.Linq.Expressions;

    using Dixin.Linq.EntityFramework;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class BinaryArithmeticTranslatorTests
    {
        [TestMethod]
        public void TranslateToSql()
        {
            ExpressionTree.Translate();

            InfixVisitor infixVisitor = new InfixVisitor();
            Expression<Func<double, double, double>> expression1 = (a, b) => a * a + b * b;
            Assert.AreEqual(infixVisitor.VisitBody(expression1), "((@a * @a) + (@b * @b))");

            Expression<Func<double, double, double, double, double, double>> expression2 =
                (a, b, c, d, e) => a + b - c * d / 2 + e * 3;
            Assert.AreEqual(infixVisitor.VisitBody(expression2), "(((@a + @b) - ((@c * @d) / 2)) + (@e * 3))");
        }

        [TestMethod]
        public void ExecuteSql()
        {
            ExpressionTree.ExecuteSql();

            Expression<Func<double, double, double>> expression1 = (a, b) => a * a + b * b;
            Func<double, double, double> local1 = expression1.Compile();
            Func<double, double, double> remote1 = BinaryArithmeticTranslator.Sql(expression1);
            Assert.AreEqual(local1(1, 2), remote1(1, 2));

            Expression<Func<double, double, double, double, double, double>> expression2 =
                (a, b, c, d, e) => a + b - c * d / 2 + e * 3;
            Func<double, double, double, double, double, double> local2 = expression2.Compile();
            Func<double, double, double, double, double, double> remote2 = BinaryArithmeticTranslator.Sql(expression2);
            Assert.AreEqual(local2(1, 2, 3, 4, 5), remote2(1, 2, 3, 4, 5));
        }
    }
}
