using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MongoDB.Driver.Core
{
    /// <summary>
    /// Put instances by buckets created by exponentially ping time growth. Using the JAVA-428 ticket as a guide.
    /// </summary>
    public class PingBuckets
    {

        private List<MongoServerInstance>[] buckets;


        /// <summary>
        /// Initializes a new instance of the <see cref="PingBuckets"/> class.
        /// </summary>
        public PingBuckets()
        {
            buckets = new List<MongoServerInstance>[0] {};
        }

        /// <summary>
        /// Adds the specified instance to a bucket choosen by ping time
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <param name="pingTime">The ping time.</param>
        public void Add(MongoServerInstance instance, TimeSpan pingTime)
        {
            double ms = Math.Max(1, pingTime.TotalMilliseconds);
            int bucket = Convert.ToInt32(Math.Floor(Math.Log10(ms))); //might be written better
            Extends(bucket);

            // remove the instance previous ping
            Remove(instance);

            buckets[bucket].Add(instance);
        }

        private void Extends(int finalLength)
        {
            if (finalLength <= buckets.Length)
                return;

            List<MongoServerInstance>[] newBuckets = new List<MongoServerInstance>[finalLength];
            buckets.CopyTo(newBuckets, 0);
            for (var i = buckets.Length; i < finalLength; i++)
                newBuckets[i] = new List<MongoServerInstance>();
            buckets = newBuckets;
        }

        /// <summary>
        /// Gets a given bucket. Throw exception if the bucket does not exist.
        /// </summary>
        /// <param name="bucket">The bucket.</param>
        /// <returns></returns>
        public List<MongoServerInstance> GetBucket(int bucket)
        {
            // you should be aware that there is no check for length. Take your precautions !
            return buckets[bucket];
        }

        public int BucketsCount
        {
            get { return buckets.Length; }
        }



        internal void Remove(MongoServerInstance mongoServerInstance)
        {
            foreach (var bucket in buckets)
            {
                if (bucket.Remove(mongoServerInstance))
                    return;

            }
        }
    }
}
