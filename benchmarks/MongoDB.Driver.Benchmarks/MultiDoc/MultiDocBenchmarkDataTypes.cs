/* Copyright 2010-present MongoDB Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Benchmarks.MultiDoc;

public sealed class SmallDocPoco
{
    [BsonElement("3MXe8Wi7")]
    public string _3MXe8Wi7 { get; set; }
    [BsonElement("5C8GSDC5")]
    public int _5C8GSDC5 { get; set; }

    public string oggaoR4O { get; set; }
    public string Vxri7mmI { get; set; }
    public string IQ8K4ZMG { get; set; }
    public string WzI8s1W0 { get; set; }
    public string E5Aj2zB3 { get; set; }
    public string PHPSSV51 { get; set; }
    public int CxSCo4jD { get; set; }
    public int fC63DsLR { get; set; }
    public int l6e0U4bR { get; set; }
    public int TLRpkltp { get; set; }
    public int Ph9CZN5L { get; set; }
}

public sealed class Tweet
{
    [BsonId]
    public ObjectId Id { get; set; }

    public long id { get; set; }

    public string text { get; set; }
    public long? in_reply_to_status_id { get; set; }
    public int? retweet_count { get; set; }
    public object contributors { get; set; }
    public string created_at { get; set; }
    public object geo { get; set; }
    public string source { get; set; }
    public object coordinates { get; set; }
    public string in_reply_to_screen_name { get; set; }
    public bool truncated { get; set; }
    public TweetEntities entities { get; set; }
    public bool retweeted { get; set; }
    public object place { get; set; }
    public User user { get; set; }
    public bool favorited { get; set; }
    public long? in_reply_to_user_id { get; set; }
}

public sealed class TweetEntities
{
    public UserMention[] user_mentions { get; set; }
    public string[] urls { get; set; }
    public string[] hashtags { get; set; }
}

[BsonNoId]
public sealed class UserMention
{
    public long id { get; set; }

    public int[] indices { get; set; }
    public string screen_name { get; set; }
    public string name { get; set; }
}

[BsonNoId]
public sealed class User
{
    public long id { get; set; }

    public int friends_count { get; set; }
    public string profile_sidebar_fill_color { get; set; }
    public string location { get; set; }
    public bool verified { get; set; }
    public object follow_request_sent { get; set; }
    public int favourites_count { get; set; }
    public string profile_sidebar_border_color { get; set; }
    public string profile_image_url { get; set; }
    public bool geo_enabled { get; set; }
    public string created_at { get; set; }
    public string description { get; set; }
    public string time_zone { get; set; }
    public string url { get; set; }
    public string screen_name { get; set; }
    public object notifications { get; set; }
    public string profile_background_color { get; set; }
    public int listed_count { get; set; }
    public string lang { get; set; }
    public string profile_background_image_url { get; set; }
    public int statuses_count { get; set; }
    public object following { get; set; }
    public string profile_text_color { get; set; }
    public bool @protected { get; set; }
    public bool show_all_inline_media { get; set; }
    public bool profile_background_tile { get; set; }
    public string name { get; set; }
    public bool contributors_enabled { get; set; }
    public string profile_link_color { get; set; }
    public int followers_count { get; set; }
    public bool profile_use_background_image { get; set; }
    public int utc_offset { get; set; }
}
