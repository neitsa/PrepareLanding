#!/usr/bin/python3
# -*- coding: UTF-8 -*-
from typing import Optional
import argparse
import sys
import requests
import urllib.parse
import os
import json
import pathlib
import zipfile
import shutil
import logging

logger = logging.getLogger(__name__)
logging.basicConfig(level=logging.DEBUG)
logger.setLevel(logging.DEBUG)
# disable logging from the package used by requests
logging.getLogger("urllib3").setLevel(logging.WARNING)

HTTP_HEADERS = {
    'User-Agent': 'neitsa',
}


class UrlDescriptor(object):
    GITHUB_LATEST_TEMPLATE = "https://api.github.com/repos/{}/releases/latest"

    def __init__(self, url: str):
        self._url = url
        self._github_profile = None
        self._github_repo = None
        self._github_latest_release_url = None
        self._filename = None

        if self._parse_github_repo_url():
            logger.info("URL : '{}' is a Github repo URL.".format(self._url))
            return

        logger.info("URL: {} is standard URL.".format(self._url))
        self.parse_url()

    def __repr__(self):
        return "[gh: {}][fname: {}] {}".format(
            self.is_github_repo_url, self._filename, self._url)

    def get_github_package_url(self) -> Optional[str]:
        profile_and_repo = "{}/{}".format(self._github_profile,
                                          self._github_repo)

        url = self.GITHUB_LATEST_TEMPLATE.format(profile_and_repo)
        response = requests.get(url, headers=HTTP_HEADERS)
        if response.status_code != 200:
            logger.error("Github URL response code was: {}"
                         .format(response.status_code))
            return None

        response_text = response.text
        response_json = json.loads(response_text)
        if "assets" not in response_json:
            logger.error("No \"assets\" in github response.")
            return None

        assets = response_json["assets"]
        if not isinstance(assets, list):
            logger.error("\"assets\" in github response is not a list.")
            return None

        if 'browser_download_url' not in assets[0]:
            logger.error("No \"browser_download_url\" in assets[0].")
            return None

        github_latest_release_url = assets[0]["browser_download_url"]
        if not github_latest_release_url:
            logger.error("browser_download_url in github response is empty.")
            return None

        logger.info("Github repo: {}\n\tLastest release URL is: {}"
                    .format(self._url, github_latest_release_url))
        self._github_latest_release_url = github_latest_release_url
        return self._github_latest_release_url

    def _parse_github_repo_url(self):
        urlsplit = urllib.parse.urlsplit(self.url)

        # net location must be github
        if not urlsplit.netloc.lower() == "github.com":
            return False

        # split the url and get its path component
        url_path = urlsplit.path

        # must have 2 OR 3 slashes
        # e.g.: http://github.com/neitsa/foo/ -> '/neitsa/foo/'
        url_slash_count = url_path.count("/")
        if url_slash_count < 2 or url_slash_count > 3:
            return False

        # 3 slashes: must start with a slash and end with one
        if url_slash_count == 3:
            if not url_path.startswith("/") and url_path.endswith("/"):
                return False

        # 2 slashes: 1st one must be at start, the 2nd one can't be at the end
        if url_slash_count == 2:
            if not url_path.startswith("/") and not url_path.endswith("/"):
                return False

        # http://github.com/neitsa/foo -> [0] = ''; [1] = 'neitsa'; [2] = foo
        self._github_profile = url_path.split("/")[1]
        self._github_repo = url_path.split("/")[2]

        return True

    def parse_url(self):
        urlsplit = urllib.parse.urlsplit(self._url)
        pure_url_path = pathlib.PurePath(urlsplit.path)
        if pure_url_path.suffix:
            self._filename = pure_url_path.name
            logger.info("URL '{}' seems to designate a file ({})."
                        .format(self._url, self._filename))

    @property
    def url(self):
        return self._url

    @property
    def is_github_repo_url(self):
        return (self._github_profile is not None and
                self._github_repo is not None)

    @property
    def github_profile(self) -> str:
        return self._github_profile

    @property
    def github_repo(self):
        return self._github_repo

    @property
    def has_filename(self):
        return self._filename is not None


class UrlDownloader(object):

    def __init__(self, url_descriptor: Optional[UrlDescriptor] = None):
        self._current_url_desc = url_descriptor
        self._download_path = None

    @property
    def current_url_descriptor(self):
        return self._current_url_desc

    @current_url_descriptor.setter
    def current_url_descriptor(self, value: UrlDescriptor):
        self._current_url_desc = value

    @property
    def download_path(self):
        return self._download_path

    def download_from_url(self, url: str, download_path: pathlib.Path):
        self._current_url_desc = UrlDescriptor(url)
        return self.download_from_url_descriptor(download_path=download_path)

    def download_from_url_descriptor(
            self, url_descriptor: Optional[UrlDescriptor] = None,
            download_path: Optional[pathlib.Path] = None) -> bool:
        if not url_descriptor and self._current_url_desc:
            url_descriptor = self._current_url_desc
        else:
            logger.error("No descriptor available for "
                         "download_from_url_descriptor")
            return False

        if url_descriptor.is_github_repo_url:
            logger.info("Trying to get package url from github url")
            latest_release_url = url_descriptor.get_github_package_url()
            if not latest_release_url:
                logger.debug("No package URL could be inferred from '{}'"
                             .format(url_descriptor))
                return False

            download_path = self.download_latest_github_release(
                latest_release_url, download_path)
            return download_path is not None
        else:
            return self.download_file(url_descriptor.url, download_path)

    def download_latest_github_release(
            self, url: Optional[str] = None,
            download_path: Optional[pathlib.Path] = None) \
            -> Optional[pathlib.Path]:

        if not url:
            if self.current_url_descriptor:
                url = self.current_url_descriptor.get_github_package_url()
                if not url:
                    raise ValueError("No viable URL to download.")
            else:
                raise ValueError("No URL descriptor.")

        if not self.download_file(url, download_path):
            return None

        self._download_path = download_path
        return self._download_path

    @staticmethod
    def download_file(url: str, download_path: pathlib.Path) -> bool:
        logger.info("Downloading:\n\tURL: {}\n\tDestination path: {}"
                    .format(url, download_path))

        response = requests.get(url, stream=True)
        if response.status_code != 200:
            logger.error("Failed to download file at: {}\n\tThe status "
                         "code was: {}".format(url, response.status_code))
            return False

        # make sure download path is correct, we should end up with a file name
        filename = urllib.parse.urlsplit(url).path.split("/")[-1]
        if not download_path:
            download_path = pathlib.Path(filename)
        else:
            if download_path.is_dir():
                if not download_path.exists():
                    try:
                        os.makedirs(str(download_path))
                    except Exception as err:
                        logger.error("Couldn't make download directory at: "
                                     "'{}'. Error was: {}".
                                     format(download_path, err))
                        return False

                download_path = download_path.joinpath(filename)
            else:
                download_path = download_path

        # if file exists, remove it
        if download_path.exists():
            try:
                logger.info("Download path already exist. Trying to remove it.")
                os.remove(str(download_path))
            except Exception as err:
                logger.error(
                    "Couldn't remove the following existing file: "
                    "{}\n\tThe error was: {}".format(download_path, err))
                return False

        with open(download_path, 'wb') as f:
            for chunk in response.iter_content(chunk_size=2048):
                if chunk:  # filter out keep-alive new chunks
                    f.write(chunk)

        logger.info("Successfully downloaded file!")

        return True

    def extract(self, package_path: Optional[pathlib.Path] = None,
                extract_path: Optional[pathlib.Path] = None,
                password: str = None) -> bool:

        if not package_path and self._download_path:
            package_path = self._download_path
        else:
            return False

        if not zipfile.is_zipfile(package_path):
            return False

        try:
            with zipfile.ZipFile(str(package_path), 'r') as archive:
                if password:
                    archive.setpassword(password)
                archive.extractall(extract_path)
        except Exception as err:
            logger.error("Error while extracting zip file: {}. "
                         "The error was: {}"
                         .format(package_path, err), file=sys.stderr)
            return False

        logger.info("Successfully extracted '{}' to '{}'."
                    .format(package_path, extract_path))

        return True

    @staticmethod
    def is_zip_file(file_path: pathlib.Path) -> bool:
        if file_path.exists() and file_path.is_file():
            return zipfile.is_zipfile(str(file_path))

        return False

    @staticmethod
    def is_zipped_dir(file_path: pathlib.Path,
                      password: Optional[str] = None) -> bool:
        if not file_path.exists() or not file_path.is_file():
            return False

        with zipfile.ZipFile(str(file_path), 'r') as archive:
            if password:
                archive.setpassword(password)
            infolist = archive.infolist()
            if len(infolist) <= 0:
                return False

            return infolist[0].is_dir()

    @staticmethod
    def zipped_dir_name(file_path: pathlib.Path,
                        pwd: Optional[str] = None) -> Optional[pathlib.Path]:
        if not UrlDownloader.is_zipped_dir(file_path, pwd):
            return None

        with zipfile.ZipFile(str(file_path), 'r') as archive:
            if pwd:
                archive.setpassword(pwd)
            return archive.infolist()[0].filename


def copy_to_dir(source_dir: pathlib.Path, dest_dir: pathlib.Path) -> bool:
    if not source_dir.exists() or not source_dir.is_dir():
        return False

    if not dest_dir.exists() or not dest_dir.is_dir():
        return False

    for dll_file in source_dir.glob("**/*.dll"):
        logger.info("Copying '{}' to '{}'".format(dll_file, dest_dir))
        shutil.copy2(str(dll_file), str(dest_dir))


def banner_execute() -> pathlib.Path:
    script_path = pathlib.Path(os.path.realpath(__file__))
    sep = "-" * 79
    print("{}\nExecuting: {}\n{}".format(sep, script_path.name, sep))
    return script_path


def main(args):
    banner_execute()

    urls = list()
    for url in args.url_list:
        urls.append(UrlDescriptor(url))

    downloader = UrlDownloader()

    for url_descriptor in urls:
        downloader.current_url_descriptor = url_descriptor
        if not downloader.download_from_url_descriptor(
                download_path=args.download_path):
            return -1

        # check if extraction is asked for
        if not args.extract:
            continue

        # extract package
        if not downloader.extract(
                extract_path=args.extract_path, password=args.zip_password):
            return -1

        # copy extracted files to destination
        if args.copy_destination and downloader.download_path:
            dir_name = UrlDownloader.zipped_dir_name(
                downloader.download_path, args.zip_password)
            if not dir_name:
                # use the extract path
                copy_source = args.extract_path
            else:
                # append the directory at the 'top' of the zip
                copy_source = args.extract_path.joinpath(dir_name)
            # copy
            copy_to_dir(copy_source, args.copy_destination)

    return 0

if __name__ == "__main__":
    arg_parser = argparse.ArgumentParser(
        description="Download required dependencies from Github or directly "
                    "from a web link.")

    arg_parser.add_argument(
        '-u', action='append', dest='url_list', default=[],
        help="Add one or more URLs to the url list [can also be URLs to "
             "repositories].")

    arg_parser.add_argument(
        '-d', '--download_path', action="store", type=pathlib.Path,
        help="Directory path where to download dependency archives. "
             "[default: working directory]")

    arg_parser.add_argument(
        '-c', '--copy_destination', action="store", type=pathlib.Path,
        help="Directory where to copy all DLLs from downloaded package.")

    arg_parser.add_argument(
        '-x', '--extract', action="store_true", default=False,
        help="Extract zip files [default: False]")

    arg_parser.add_argument(
        '-e', '--extract_path', action="store", type=pathlib.Path,
        help="Directory path where to extract dependencies. "
             "[default: working directory]")

    arg_parser.add_argument(
        '-z', '--zip_password', action="store", type=str,
        help="Zip password for zip added with the '-u' option. "
             "[note: same password is used for all zip files]")

    parsed_args = arg_parser.parse_args()
    if not parsed_args.url_list:
        print("'-u option is mandatory", file=sys.stderr)
        sys.exit(-1)

    sys.exit(main(parsed_args))
