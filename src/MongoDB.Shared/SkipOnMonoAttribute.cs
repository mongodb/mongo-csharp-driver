using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace MongoDB
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class SkipOnMonoAttribute : Attribute, ITestAction
    {
        private readonly string _reason;

        public SkipOnMonoAttribute()
            : this(null)
        { }

        public SkipOnMonoAttribute(string reason)
        {
            _reason = reason;
        }

        public void AfterTest(TestDetails testDetails)
        {
        }

        public void BeforeTest(TestDetails testDetails)
        {
            var type = Type.GetType("Mono.Runtime");
            if (type != null)
            {
                var message = "Test is not valid on Mono.";
                if(_reason != null)
                {
                    message += " " + _reason;
                }
                Assert.Ignore(message);
            }
        }

        public ActionTargets Targets
        {
            get { return ActionTargets.Default; }
        }
    }
}
