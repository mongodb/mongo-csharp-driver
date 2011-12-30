using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using MongoDB.Driver;

namespace MongoDB.DriverUnitTests.Core
{
    [TestFixture]
    public class ReadPreferenceTests
    {

        [Test]
        public void TestEquals()
        {
            ReadPreference primary = ReadPreference.Primary;
            ReadPreference secondary = ReadPreference.Secondary;
            ReadPreference voidTag = new ReadPreference(new HashSet<string>());
            ReadPreference someTags = new ReadPreference(new HashSet<string>());
            someTags.Tags.Add("new york");
            someTags.Tags.Add("rack 1");

            ReadPreference someTagsBis = new ReadPreference(new HashSet<string>());
            someTagsBis.Tags.Add("rack 1");
            someTagsBis.Tags.Add("new york");
            

            ReadPreference someTagsAgain = new ReadPreference(new HashSet<string>());
            someTagsAgain.Tags.Add("palo alto");
            someTagsAgain.Tags.Add("rack 1");

            //Equals to self
            Assert.AreEqual(true, primary.Equals(primary));
            Assert.AreEqual(true, secondary.Equals(secondary));
            Assert.AreEqual(true, voidTag.Equals(voidTag));
            Assert.AreEqual(true, someTags.Equals(someTags));
            Assert.AreEqual(true, someTagsAgain.Equals(someTagsAgain));

            //Basic stuff are not equal
            Assert.AreEqual(false, primary.Equals(secondary));
            Assert.AreEqual(false, secondary.Equals(primary));
            Assert.AreEqual(false, primary.Equals(voidTag));
            Assert.AreEqual(false, voidTag.Equals(primary));

            Assert.AreEqual(false, primary.Equals(secondary));
            Assert.AreEqual(false, secondary.Equals(primary));
            Assert.AreEqual(false, primary.Equals(someTags));
            Assert.AreEqual(false, someTags.Equals(primary));
            Assert.AreEqual(false, primary.Equals(someTagsAgain));
            Assert.AreEqual(false, someTagsAgain.Equals(primary));

            Assert.AreEqual(false, someTags.Equals(secondary));
            Assert.AreEqual(false, secondary.Equals(someTags));
            

            Assert.AreEqual(true, someTags.Equals(someTagsBis));
            Assert.AreEqual(true, someTagsBis.Equals(someTags));


            //two object equals constructed separately are equals
            Assert.AreEqual(true, someTags.Equals(someTagsBis));
            Assert.AreEqual(true, someTagsBis.Equals(someTags));

            //default tagged is not tagged but defaulted to secondary ok
            Assert.AreEqual(true, voidTag.Equals(secondary));
            Assert.AreEqual(true, secondary.Equals(voidTag));
            Assert.AreEqual(false, someTagsAgain.Equals(secondary));
            Assert.AreEqual(false, secondary.Equals(someTagsAgain));
            

        }
    }
}
