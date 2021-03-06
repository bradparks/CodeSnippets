﻿namespace Dixin.Tests.Linq.LinqToXml
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Xml;
    using System.Xml.Linq;

    using Dixin.Linq.LinqToXml;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ModelingTests
    {
        [TestMethod]
        public void CreateAndSerializeTest()
        {
            Modeling.CreateAndSerialize();
            Modeling.Construction();
        }

        [TestMethod]
        public void ApiTest()
        {
            Modeling.Name();
            Modeling.Namespace();
            Modeling.Element();
            Modeling.Attribute();
            Modeling.Node();
        }

        [TestMethod]
        public void ReadWriteTest()
        {
            try
            {
                Modeling.Read();
                Assert.Fail();
            }
            catch (XmlException exception)
            {
                Trace.WriteLine(exception);
            }
            IEnumerable<XElement> items = Modeling.RssItems("https://weblogs.asp.net/dixin/rss");
            Assert.IsTrue(items.Any());
            Modeling.XNodeToString();
#if NETFX
            Modeling.Write();
#endif
        }

        [TestMethod]
        public void StreamingTest()
        {
            Modeling.StreamingElement();
        }
    }
}
