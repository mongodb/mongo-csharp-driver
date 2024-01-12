#!/usr/bin/env python

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

"""Compare results of two benchmark runs."""

import argparse
import json

def compare(files):
    data = {}
    jsons = [json.load(f) for f in files]
    for j in jsons:
        for result in j:
            data[result['info']['test_name']] = []

    for j in jsons:
        for name in data:
            for result in j:
                if result['info']['test_name'] == name:
                    data[name].append(result['metrics'][0]['value'])
                    break

    col_width = 2 + max(len(name) for name in data)
    file_width = 2 + max(len(f.name) for f in files)
    print(" " * file_width + "".join(name.ljust(col_width) for name in data))
    for i, f in enumerate(files):
        print(f.name.ljust(file_width) + "".join(
            ("%.2f" % (data[name][i] / float(data[name][0]))).ljust(col_width)
            for name in data))


if __name__ == "__main__":
    parser = argparse.ArgumentParser(usage='%(prog)s [-h] file file [file ...]')
    parser.add_argument('file1', nargs=1, metavar='file',
                        type=argparse.FileType())
    parser.add_argument('file2', nargs='+', metavar='file',
                        type=argparse.FileType(), help=argparse.SUPPRESS)
    args = parser.parse_args()
    args.files = args.file1 + args.file2
    compare(args.files)
