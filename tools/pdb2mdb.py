#!/usr/bin/python3.6
# -*- coding: UTF-8 -*-
# author: neitsa
import argparse
import os
import pathlib
import sys
import subprocess


def file_exists(path_str: pathlib, check_absolute: bool = False) -> bool:
    path = pathlib.Path(path_str)
    if not path.exists() or not path.is_file():
        print("Provided path '{}' doesn't exists or is not a file."
              .format(path_str), file=sys.stderr)
        return False

    if check_absolute:
        if not path.is_absolute():
            print("Provided path '{}' is not an absolute path."
                  .format(path_str), file=sys.stderr)
            return False

    return True


def main(args):
    if not file_exists(args.dll_path, True):
        return -1

    pdb2mdb_path = "pdb2mdb"
    if args.bin_path:
        if not file_exists(args.bin_path, True):
            return -1
        pdb2mdb_path = args.bin_path
    else:
        # try to build a full absolute path for pdb2mdb binary

        # get location of current script
        script_path = os.path.realpath(__file__)
        pdb2mdb_path = str(pathlib.Path(script_path).parent.joinpath(
            pdb2mdb_path))

    print("pdb2mdb binary path: '{}'".format(pdb2mdb_path))
    print("Generating mdb file")

    # run pdb2mdb
    process = subprocess.run([pdb2mdb_path, args.dll_path],
                             stdout=subprocess.PIPE)
    if process.stdout != b'':
        print(process.stdout)

    if process.returncode != 0:
        print("An error occured from pdb2mdb. Return code: {}".format(
            process.returncode), file=sys.stderr)
    else:
        print("pdb2mdb success. Return code: {}".format(process.returncode))

    # return pdb2mdb return code to caller.
    return process.returncode

if __name__ == "__main__":
    arg_parser = argparse.ArgumentParser(description='Pdb2mdb starter script.')
    arg_parser.add_argument(
        'dll_path', type=str, action="store",
        help='Full path to the DLL for which to generate the mdb file')
    arg_parser.add_argument(
        '-p', action="store", dest="bin_path",
        help="Full path to pdb2mdb (default: use current dir)")

    parsed_args = arg_parser.parse_args()
    sys.exit(main(parsed_args))
