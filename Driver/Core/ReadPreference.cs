using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MongoDB.Driver
{

    /// <summary>
    /// Indicate the preference for reading information : primary only, any secondary, set of tags.
    /// Set of tags, if any, is used first, then fallbacking to any secondary then primary.
    /// Improvement of readpreference handling would be be able to define an order of precedence.
    /// </summary>
    public class ReadPreference :  IEquatable<ReadPreference>
    {
        /// <summary>
        /// Get an instance of ReadPreference to primary.
        /// </summary>
        public static ReadPreference Primary = new ReadPreference();

        /// <summary>
        /// Gets an instance of ReadPreference to any secondary
        /// </summary>
        public static ReadPreference Secondary = new ReadPreference(true);

        private HashSet<string> tags;
        private bool secondaryOk;

        public HashSet<string> Tags
        {
            get { return tags; }
        }

        public ReadPreference(HashSet<string> tags)
            : this(true)
        {
            this.tags = tags;
        }

        public ReadPreference(bool secondary) : this()
        {
            this.secondaryOk = true;
        }

        public ReadPreference()
        {
            this.tags = new HashSet<string>();
            this.secondaryOk = false;
        }


        public override int GetHashCode()
        {
            int h = 17;
            if (Tagged)
            {
                foreach (string tag in tags)
                    h = 37 * h + tag.GetHashCode();
            }
            h = h * 37 + secondaryOk.GetHashCode();
            return h;
        }


        public override bool Equals(object obj)
        {
            ReadPreference rp = obj as ReadPreference;
            return rp != null && this.Equals(rp);
        }

        public bool Equals(ReadPreference obj)
        {
            if (this.secondaryOk != obj.secondaryOk)
                return false;
            if ((this.tags ==null && obj.tags != null) || (this.tags !=null && obj.tags == null))
                return false;
            if (this.tags ==null && obj.tags == null) 
                return true;
            if (this.tags.Count != obj.tags.Count)
                return false;

            return this.tags.Intersect(obj.tags).Count() == this.tags.Count();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(5+(tags.Count*5));
            try
            {
                if (this.Equals(Primary))
                    sb.Append("primary");
                else if (this.Equals(Secondary))
                    sb.Append("secondary");
                else if (this.Tagged)
                {
                    sb.Append("tags");
                    foreach (var tag in tags)
                        sb.Append(":" + tag);
                }
                else
                    throw new NotSupportedException();
            }
            catch (Exception e)
            {
                sb.Append("primary :" + !secondaryOk+", ");
                sb.Append("secondary :" + secondaryOk+", ");
                sb.Append("tagged : " + Tagged);
                if (Tagged)
                {
                    sb.Append("  tags : ");
                    foreach (string t in tags)
                        sb.Append(t);
                }
            }


            return sb.ToString();

        }

        public static ReadPreference Parse(string s)
        {
            if (s.StartsWith("primary"))
                return  ReadPreference.Primary;
            if (s.StartsWith("secondary"))
                return ReadPreference.Secondary;
            if (s.StartsWith("tags"))
            {
                ReadPreference rp = new ReadPreference(new HashSet<string>(s.Split(new char[1] { ':' })));
                return rp;
            }
            throw new FormatException("ReadPreference parsing failed");
        }


        public bool Tagged
        {
            get { return Tags.Count > 0; }
        }

        public bool SecondaryOk
        {
            get { return secondaryOk; }
        }

        /// <summary>
        /// Test if two read preferences are compatible (either equals, either with common tags)
        /// </summary>
        /// <param name="rp">The rp.</param>
        /// <returns></returns>
        public bool Match(ReadPreference rp)
        {
            return this.Equals(rp) || (this.Tagged && rp.Tagged && this.Tags.Intersect(rp.Tags).Count() > 0); //maybe Insect is not strong enough.. should I use Subset ?
        }
    }
}
