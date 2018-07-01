#!/usr/bin/python3
# -*- coding: UTF-8 -*-
import sys
import logging
import os.path
import argparse
import subprocess
from typing import Optional
from pathlib import Path

logger = logging.getLogger(__name__)
logging.basicConfig(level=logging.DEBUG)
logger.setLevel(logging.DEBUG)

EXTRACT_METHODS = ['e', 'x']


def get_latest_file_in_dir(dir_path: Path) -> Optional[Path]:
    latest_file_time = 0
    latest_file = None
    for file in dir_path.glob("*"):
        if file.is_dir():
            continue

        file_time = os.path.getctime(file)
        if file_time > latest_file_time:
            latest_file_time = file_time
            latest_file = file

    return latest_file


def banner_execute() -> Path:
    script_path = Path(os.path.realpath(__file__))
    sep = "-" * 79
    print("{}\nExecuting: {}\n{}".format(sep, script_path.name, sep))
    return script_path


def main(args):
    banner_execute()

    # check if input path exists
    if not args.input_file.exists():
        logger.error("Input file: '{}' doesn't exist.".format(args.input_file))
        return -1

    # check input path type (either dir or file)
    if not args.input_file.is_dir():
        input_file = args.input_file
    else:
        # it's a directory
        # default to the latest (by creation time) file in the directory
        input_file = get_latest_file_in_dir(args.input_file)

    # check output path
    if not args.output_path:
        # no output path given:
        # if input path is a dir, take it. If it's a file, take its directory
        output_path = input_file if input_file.is_dir() else input_file.parent
    else:
        # check if output path exists and it's a directory
        if not args.output_path.exists() or not args.output_path.is_dir():
            logger.error(
                "Output Path: '{}' either doesn't exist or is not a directory."
                .format(args.output_path))
            return - 1
        else:
            output_path = args.output_path

    if args.program_path:
        if not args.program_path.exists() or not args.program_path.is_file():
            logger.error(
                "Program Path: '{}' either doesn't exist or is not a file."
                .format(args.input_file))
            return -1
        program_path = args.program_path
    else:
        # it's in the path
        program_path = "7z"

    # -o option must be attached to the directory, otherwise it doesn't work...
    o_switch = "-o{}".format(str(output_path))

    # extraction method
    if args.extract_method:
        if args.extract_method not in EXTRACT_METHODS:
            logger.error("Unknown extract method: '{}'".format(
                args.extract_method))
            return -1
        else:
            extract_method = args.extract_method
    else:
        extract_method = "x"

    subprocess_args = [str(program_path), extract_method, str(input_file),
                       o_switch]

    # check for password, if any
    if args.password:
        subprocess_args.append("-p{}".format(args.password))

    # check for extensions
    if args.extension_list:
        for extension in args.extension_list:
            subprocess_args.append(extension)
        subprocess_args.append("-r")

    try:
        # extract using 7zip (must be in PATH env. variable)
        process = subprocess.run(subprocess_args, stdout=subprocess.PIPE,
                                 timeout=10)
    except Exception as err:
        logger.error("An error occured while trying to run the program."
                     "\n\tThe error was: {}".format(err))
        return -1

    if process.stdout != b'':
        print(process.stdout.decode("utf-8"))

    if process.returncode != 0:
        logging.error("An error occured from 7z. Return code: {}".format(
            process.returncode))
    else:
        print("7z success. Return code: {}".format(process.returncode))

    return process.returncode

if __name__ == "__main__":
    arg_parser = argparse.ArgumentParser(
        description="Extract archive using 7zip.")

    arg_parser.add_argument(
        'input_file', action="store", type=Path,
        help="The archive file to extract. [Note: if this argument defines "
             "a directory, the latest file (by creation time) in this "
             "directory is used instead].")

    arg_parser.add_argument(
        '-o', '--output_path', action="store", type=Path,
        help="Directory path where to extract files. "
             "[default: same directory as input file]")

    arg_parser.add_argument(
        '-p', '--password', action="store", type=str,
        help="Archive Password (if any).")

    arg_parser.add_argument(
        '-e', action='append', dest='extension_list', default=[],
        help="One or more file extension that must be exclusively extracted. "
             "[default: all files].")

    arg_parser.add_argument(
        '-z', '--program_path', action="store", type=Path,
        help="Full path to 7Zip program if needed. [default: use the PATH env. "
             "var.]")

    arg_parser.add_argument(
        '-x', '--extract_method', action="store",
        help="7Zip extraction method, must be 'e' or 'x' [default: x]")

    parsed_args = arg_parser.parse_args()

    sys.exit(main(parsed_args))
