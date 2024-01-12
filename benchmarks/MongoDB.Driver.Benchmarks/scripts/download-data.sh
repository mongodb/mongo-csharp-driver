#!/bin/sh

# Copyright 2010 MongoDB, Inc.
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
#
# http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

fetch_data_file() {
	echo "Fetching $1.tgz from github..."

	curl --retry 5 "https://raw.githubusercontent.com/mongodb/specifications/master/source/benchmarking/data/$1.tgz" --max-time 120 --remote-name --silent
	mkdir -p data
	tar xf "$1.tgz"
	mv "$1" data
	rm "$1.tgz"
}

fetch_data_file extended_bson
fetch_data_file parallel
fetch_data_file single_and_multi_document
