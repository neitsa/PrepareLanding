#!/usr/bin/python3.6
# -*- coding: UTF-8 -*-
import argparse
import sys
import os
import pathlib
import shutil

# Location of mods in RimWorld game directory.
RIMWORLD_MOD_DIR = "Mods"

# The current mod folder name in RimWorld/Mods
MOD_NAME = "PrepareLanding"

# Location of executable files in a mod folder.
MOD_ASSEMBLIES = "Assemblies"


def banner_execute() -> pathlib.Path:
    script_path = pathlib.Path(os.path.realpath(__file__))
    sep = "-" * 79
    print("{}\nExecuting: {}\n{}".format(sep, script_path.name, sep))
    return script_path


def dir_exists(path_str: pathlib, check_absolute: bool = False) -> bool:
    path = pathlib.Path(path_str)
    if not path.exists() or not path.is_dir():
        print("Provided path '{}' doesn't exists or is not a directory."
              .format(path_str), file=sys.stderr)
        return False

    if check_absolute:
        if not path.is_absolute():
            print("Provided path '{}' is not an absolute path."
                  .format(path_str), file=sys.stderr)
            return False

    return True


def main(args):
    banner_execute()

    # where target dir is, for ex.: <myrepo>\PrepareLanding\bin\Debug
    if not dir_exists(args.target_dir):
        return -1

    # The game directory, e.g: c:\RimWorld
    if not dir_exists(args.rimworld_dir):
        return -1

    # for ex.: <my_repo>\output\PrepareLanding
    # location where the whole mod is (with \About, \Assemblies, \Languages,
    #  etc.)
    if not dir_exists(args.output_dir):
        return -1

    # TODO: pass MOD_NAME in args?

    # The game directory, e.g: c:\RimWorld
    rimworld_dir = pathlib.Path(args.rimworld_dir)
    # e.g: c:\RimWorld\Mods
    rimworld_mods_dir = rimworld_dir.joinpath(RIMWORLD_MOD_DIR)
    # e.g: c:\RimWorld\Mods\PrepareLanding
    mod_dir = rimworld_mods_dir.joinpath(MOD_NAME)

    if not dir_exists(mod_dir):
        print("Provided mod path '{}' doesn't exists or is not a directory."
              .format(mod_dir), file=sys.stderr)
        print("Trying to create it", file=sys.stderr)
        try:
            os.makedirs(mod_dir)
        except OSError as e:
            if e.errno != errno.EEXIST:
                print("Couldn't create directory '{}'.".format(mod_dir), file=sys.stderr)
                return -1
        

    # check that we have an output dir
    if args.output_dir:
        # copy dll(s)
        file_types = ['*.dll']

        # also check if need to copy other file types
        if args.pdb:
            file_types.append('*.pdb')
        if args.mdb:
            file_types.append('*.mdb')

        # e.g: <my_repo>\output\PrepareLanding\Assemblies
        output_dir = pathlib.Path(args.output_dir).joinpath(MOD_ASSEMBLIES)
        # make sur there's an 'Assemblies' dir in output dir
        if not dir_exists(output_dir):
            print("Wasn't able to find the following directory: {}"
                  .format(output_dir), file=sys.stderr)
            return -1

        # copy from target_dir to output_dir
        print("Trying to copy binary files.")
        target_dir = pathlib.Path(args.target_dir)
        for file_type in file_types:
            for file_name in target_dir.glob(file_type):
                try:
                    shutil.copy2(file_name, str(output_dir))
                    if args.verbose:
                        print("Copied:\n\t- from: '{}'\n\t- to '{}'"
                              .format(file_name, output_dir))
                except Exception as err:
                    print("An error occured while trying to copy a file.\n"
                          "src: {}\ndst: {}\nThe error was: {}"
                          .format(file_name, output_dir, err),
                          file=sys.stderr)
                    return -1

    # now, delete the whole folder mod in RimWorld. Catch any errors so we
    #  get out if anything goes really wrong (e.g unable to remove some files
    #  due to a lock).
    try:
        shutil.rmtree(str(mod_dir))
    except Exception as err:
        print("An error occured while trying to remove the following "
              "directory:\n\t{}\nThe error was:\n\t{}".format(mod_dir, err),
              file=sys.stderr)
        return -1

    # copy the whole mod content to RimWorld
    print("Trying to copy the whole mod to its destination.")
    src_dir = args.output_dir if args.output_dir else args.target_dir
    try:
        shutil.copytree(src_dir, str(mod_dir))
        if args.verbose:
            print("Copied:\n\t- from: '{}'\n\t- to '{}'"
                  .format(src_dir, mod_dir))
    except Exception as err:
        print("An error occured while trying to copy a directory.\n"
              "src dir: {}\n"
              "dst dir: {}\n"
              "The error was:{}".format(src_dir, mod_dir, err), file=sys.stderr)
        return -1

    print("Successfully copied the mod to its folder: '{}'.".format(mod_dir))

    return 0

if __name__ == "__main__":
    arg_parser = argparse.ArgumentParser(
        description='Copy built mod to RimWorld location.')

    arg_parser.add_argument(
        'target_dir', type=str, action="store",
        help="Full path to the directory where the mod is built"
             " (TargetDir in VS).")

    arg_parser.add_argument(
        'rimworld_dir', action="store",
        help="Full path to RimWorld game folder.")

    # output dir and target_dir might be the same, so this one is optional
    arg_parser.add_argument(
        '--output_dir', action="store", dest="output_dir",
        help="Full path to the mod directory (containing all the required "
             "files for the mod.)")

    # only useful for debugging, don't use that for Release type
    arg_parser.add_argument(
        '-p', '--pdb', action="store_true", dest="pdb", default=True,
        help="Copy PDB files (if any).")

    # only useful for debugging, don't use that for Release type
    arg_parser.add_argument(
        '-m', '--mdb', action="store_true", dest="mdb", default=True,
        help="Copy MDB files (if any).")

    arg_parser.add_argument(
        '--verbose', action="store_true", dest="verbose", default=True,
        help="Verbose script. [default: True]")

    parsed_args = arg_parser.parse_args()
    sys.exit(main(parsed_args))
