#!/bin/sh

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
