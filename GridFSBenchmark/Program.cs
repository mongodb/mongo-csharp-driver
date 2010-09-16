/* Copyright 2010 10gen Inc.
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
using System.Linq;
using System.Text;
using System.Threading;

using MongoDB.BsonLibrary;
using MongoDB.CSharpDriver;

namespace MongoDBGridFSTest {
    public static class Program {
        private static MongoGridFS gridFS;

        public static void Main(string[] args) {
            var connectionString = "mongodb://127.0.0.1";
            var server = MongoServer.Create(connectionString);
            server.Connect(TimeSpan.FromMinutes(5)); // give time to debug
            var database = server.GetDatabase("gridfstest");
            gridFS = database.GridFS;
            gridFS.Settings.Root = "uploads";
            gridFS.SafeMode = SafeMode.False;

            Thread.Sleep(TimeSpan.FromSeconds(1)); // give a chance to start other instances
            Console.WriteLine("Starting");

            int iterations = 400;
            DateTime start = DateTime.UtcNow;
            for (int i = 0; i < iterations; i++) {
                // gridFS.Delete("06 Headstrong.mp3");
                var fileInfo = gridFS.Upload("06 Headstrong.mp3");
                gridFS.DeleteById(fileInfo.Id);
            }
            DateTime end = DateTime.UtcNow;
            TimeSpan duration = end - start;
            double fps = iterations / duration.TotalSeconds;
            Console.WriteLine("Uploaded {0} files per second", fps);

            //iterations = 100;
            //start = DateTime.UtcNow;
            //for (int i = 0; i < iterations; i++) {
            //    gridFS.Download("06 Headstrong.mp3");
            //}
            //end = DateTime.UtcNow;
            //duration = end - start;
            //fps = iterations / duration.TotalSeconds;
            //Console.WriteLine("Downloaded {0} files per second", fps);

            ListFiles();
            Console.ReadLine();
        }

        private static void ListFiles() {
            foreach (var file in gridFS.Find()) {
                Console.WriteLine(file.Name);
            }
        }
    }
}
