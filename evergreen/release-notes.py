import argparse
import yaml
import requests
import re
import os

parser = argparse.ArgumentParser()
parser.add_argument('version')
parser.add_argument('repo')
parser.add_argument('version_tag')
parser.add_argument('previous_tag')
parser.add_argument('template_file')

options = parser.parse_args()

options.docs_version = options.version_tag[:options.version_tag.rfind('.')]
options.github_api_base_url = 'https://api.github.com/repos/'
options.github_api_key = os.environ.get("GITHUB_APIKEY")
options.github_headers = {
    "Authorization": "Bearer {api_key}".format(api_key=options.github_api_key),
    "X-GitHub-Api-Version": "2022-11-28",
    "Accept": "application/vnd.github+json"
}

print("Preparing release notes for: tag {version_tag}, previous tag {previous_tag}".format(version_tag = options.version_tag, previous_tag = options.previous_tag))

def load_config(opts):
    print("Loading template...")
    with open(opts.template_file, 'r') as stream:
        try:
            opts.template = yaml.safe_load(stream)
            for section in opts.template["sections"]:
                if type(section) is dict:
                    section["items"] = []
        except yaml.YAMLError as e:
            print('Cannot load template file:', e)


def mapPullRequest(pullRequest, opts):
    title = pullRequest["title"]
    for regex in opts.template["autoformat"]:
        title = re.sub(regex["match"], regex["replace"], title)

    return {
        "title": title,
        "labels": list(map(lambda l: l["name"], pullRequest["labels"]))
    }


def is_in_section(pullrequest, section):
    if section is None:
        return False
    if type(section) is str:
        return False
    if "exclude-labels" in section:
        for lbl in section["exclude-labels"]:
            if lbl in pullrequest["labels"]:
                return False

    if "labels" in section:
        if section["labels"] == "*":
            return True
        for lbl in section["labels"]:
            if lbl in pullrequest["labels"]:
                return True
        return False

    return True


def load_pull_requests(opts):
    print("Loading changeset...")
    page = 0
    total_pages = 1
    page_size = 100
    commits_url = "{github_api_base_url}{repo}/compare/{previous_tag}...{version_tag}".format(
        github_api_base_url=opts.github_api_base_url,
        repo=opts.repo,
        previous_tag=opts.previous_tag,
        version_tag=opts.version_tag)
    ignore_section = opts.template["ignore"]

    while total_pages > page:
        response = requests.get(commits_url, params={'per_page': page_size, 'page': page}, headers=opts.github_headers)
        response.raise_for_status()
        commits = response.json()
        total_pages = commits["total_commits"] / page_size

        for commit in commits["commits"]:
            pullrequests_url = "{github_api_base_url}{repo}/commits/{commit_sha}/pulls".format(
                github_api_base_url=opts.github_api_base_url,
                repo=opts.repo,
                commit_sha=commit["sha"])
            pullrequests = requests.get(pullrequests_url, headers=opts.github_headers).json()
            for pullrequest in pullrequests:
                mapped = mapPullRequest(pullrequest, opts)
                if is_in_section(mapped, ignore_section):
                    break

                for section in opts.template["sections"]:
                    if is_in_section(mapped, section):
                        if mapped in section["items"]:
                            break  # PR was already added to the section
                        section["items"].append(mapped)
                        break  # adding PR to the section, skip evaluating next sections
                else:
                    opts.template["unclassified"].append(mapped)
        page = page + 1


def get_field(source, path):
    elements = path.split('.')
    for elem in elements:
        source = getattr(source, elem)
    return source


def apply_template(template, parameters):
    return re.sub(r'\$\{([\w.]+)}', lambda m: get_field(parameters, m.group(1)), template)


def process_section(section):
    if type(section) is str:
        return apply_template(section, options)
    if len(section["items"]) == 0:
        return ""

    content = ""
    title = section.get("title", "")
    if title != "":
        content = apply_template(title, options) + '\n'

    for pullrequest in section["items"]:
        content = content + '\n - ' + pullrequest["title"]

    return content


def publish_release_notes(opts, title, content):
    print("Publishing release notes...")
    url = '{github_api_base_url}{repo}/releases/tags/{tag}'.format(github_api_base_url=opts.github_api_base_url, repo=opts.repo, tag=opts.version_tag)
    response = requests.get(url, headers=opts.github_headers)
    response.raise_for_status()
    if response.status_code != 404:
        raise SystemExit("Release with the tag already exists")

    post_data = {
        "tag_name": opts.version_tag,
        "name": title,
        "body": content,
        "draft": True,
        "generate_release_notes": False,
        "make_latest": "false"
    }
    response = requests.post(url, json=post_data, headers=opts.github_headers)
    response.raise_for_status()


load_config(options)
options.template["unclassified"] = []
load_pull_requests(options)

print("Processing title...")
release_title = apply_template(options.template["title"], options)
print("Title: {title}".format(title=release_title))

print("Processing content...")
release_content = ""
for section in options.template["sections"]:
    section_content = process_section(section)
    if section_content != "":
        release_content += "\n\n" + section_content

if len(options.template["unclassified"]) > 0:
    release_content += "\n\n================================"
    release_content += "\n\n!!!UNCLASSIFIED PULL REQUESTS!!!"
    for pr in options.template["unclassified"]:
        release_content += "\n" + pr["title"]
    release_content += "\n\n================================"

print("----------")
print(release_content)
print("----------")

publish_release_notes(options, release_title, release_content)

print("Done.")
