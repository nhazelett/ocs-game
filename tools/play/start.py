#!/usr/bin/env python3
"""Build and run a fresh play entirely outside the source checkout."""
import argparse
import os
from pathlib import Path
import shutil
import subprocess
import sys


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--scenario', choices=['tutorial', 'spoke-b'], required=True)
    parser.add_argument('--seed', type=int, required=True)
    parser.add_argument('--out', type=Path, required=True)
    args = parser.parse_args()
    repo = Path(__file__).resolve().parents[2]
    out = args.out.resolve()
    if not args.out.is_absolute() or out == repo or repo in out.parents:
        parser.error('--out must be an absolute scratch directory outside the repository')
    if out.exists() and any(out.iterdir()):
        parser.error('--out must be empty or new')
    godot = os.environ.get('GODOT_BIN') or shutil.which('godot')
    dotnet = shutil.which('dotnet')
    if not godot or not dotnet:
        parser.error('Set GODOT_BIN to Godot 4.6.1 .NET and put .NET 8 on PATH')
    if not os.environ.get('DISPLAY'):
        parser.error('Use the smoke tests\' authenticated Xvfb display (DISPLAY and XAUTHORITY)')
    out.mkdir(parents=True, exist_ok=True)
    workspace = out / 'build'
    workspace.mkdir()
    for name in ('game', 'src', 'assets', 'scenarios'):
        shutil.copytree(repo / name, workspace / name,
                        ignore=shutil.ignore_patterns('.godot', 'bin', 'obj', '__pycache__'))
    env = os.environ.copy()
    env.update(XDG_DATA_HOME=str(out / 'data'), XDG_CONFIG_HOME=str(out / 'config'),
               XDG_CACHE_HOME=str(out / 'cache'))
    with (out / 'build.log').open('w') as log:
        subprocess.run([dotnet, 'build', str(workspace / 'game/LinesOfSupport.Game.csproj'), '-c', 'Debug'],
                       stdout=log, stderr=subprocess.STDOUT, env=env, check=True)
        subprocess.run([godot, '--headless', '--path', str(workspace / 'game'), '--editor', '--import', '--quit'],
                       stdout=log, stderr=subprocess.STDOUT, env=env, check=True)
    log = (out / 'driver.log').open('w')
    os.dup2(log.fileno(), 1)
    os.dup2(log.fileno(), 2)
    os.execve(godot, [godot, '--path', str(workspace / 'game'), '--resolution', '1280x800',
                     '--audio-driver', 'Dummy', 'res://tests/PlayDriver.tscn', '--',
                     '--scenario', args.scenario, '--seed', str(args.seed), '--out', str(out)], env)


if __name__ == '__main__':
    main()
