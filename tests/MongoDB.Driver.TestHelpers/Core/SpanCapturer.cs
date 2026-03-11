/* Copyright 2025-present MongoDB Inc.
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

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MongoDB.Driver.TestHelpers.Core
{
    public class CapturedSpan
    {
        public string Name { get; set; }
        public Dictionary<string, object> Attributes { get; set; }
        public ActivityStatusCode StatusCode { get; set; }
        public string StatusDescription { get; set; }
        public List<CapturedSpan> NestedSpans { get; set; }
        public string ParentId { get; set; }
        public string SpanId { get; set; }

        public CapturedSpan()
        {
            Attributes = new Dictionary<string, object>();
            NestedSpans = new List<CapturedSpan>();
        }
    }

    public class SpanCapturer : IDisposable
    {
        private readonly object _lock = new object();
        private readonly List<Activity> _completedActivities;
        private readonly ActivityListener _listener;

        static SpanCapturer()
        {
            Activity.DefaultIdFormat =  ActivityIdFormat.W3C;
        }

        public SpanCapturer()
        {
            _completedActivities = new List<Activity>();

            _listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == MongoTelemetry.ActivitySource.Name,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStopped = OnActivityStopped
            };

            ActivitySource.AddActivityListener(_listener);
        }

        public List<CapturedSpan> Spans
        {
            get
            {
                lock (_lock)
                {
                    return BuildSpanTree(_completedActivities);
                }
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _completedActivities.Clear();
            }
        }

        public void Dispose()
        {
            _listener?.Dispose();
        }

        private void OnActivityStopped(Activity activity)
        {
            if (activity == null)
            {
                return;
            }

            lock (_lock)
            {
                _completedActivities.Add(activity);
            }
        }

        private List<CapturedSpan> BuildSpanTree(List<Activity> activities)
        {
            var spanMap = new Dictionary<string, CapturedSpan>();

            // Convert all activities to CapturedSpan
            foreach (var activity in activities)
            {
                var capturedSpan = new CapturedSpan
                {
                    Name = activity.DisplayName ?? activity.OperationName,
                    SpanId = activity.SpanId.ToString(),
                    ParentId = activity.ParentSpanId.ToString(),
                    StatusCode = activity.Status,
                    StatusDescription = activity.StatusDescription
                };

                // Capture all tags as attributes, preserving original types
                foreach (var tag in activity.TagObjects)
                {
                    capturedSpan.Attributes[tag.Key] = tag.Value;
                }

                spanMap[capturedSpan.SpanId] = capturedSpan;
            }

            // Build parent-child relationships
            var rootSpans = new List<CapturedSpan>();
            foreach (var span in spanMap.Values)
            {
                if (span.ParentId == "00000000000000000000000000000000" || !spanMap.ContainsKey(span.ParentId))
                {
                    // No parent or parent not in our captured set = root span
                    rootSpans.Add(span);
                }
                else
                {
                    // Add as child to parent
                    spanMap[span.ParentId].NestedSpans.Add(span);
                }
            }

            return rootSpans;
        }
    }
}
