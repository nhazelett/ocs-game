#!/usr/bin/env python3
"""One screen command; stdout is exactly the screenshot and text paths."""
import json
import os
from pathlib import Path
import re
import shlex
import sys
import time
import uuid


def parse(command):
    words = shlex.split(command)
    if not words:
        raise ValueError('Expected a command.')
    verb = words.pop(0)
    occurrence = 1
    if words and re.fullmatch(r'#[1-9][0-9]*', words[-1]):
        occurrence = int(words.pop()[1:])
    if verb in ('look', 'quit') and not words:
        return dict(Verb=verb)
    if verb in ('click', 'key') and len(words) == 1:
        if verb == 'key' and words[0] != 'Escape':
            raise ValueError('Only key Escape is supported.')
        return dict(Verb=verb, Label=words[0], Occurrence=occurrence)
    if verb in ('type', 'scroll') and len(words) == 2:
        if verb == 'scroll' and words[1] not in ('up', 'down'):
            raise ValueError('Use scroll "<pane>" up or down.')
        return dict(Verb=verb, Label=words[0], Value=words[1], Occurrence=occurrence)
    raise ValueError('Use click "<label>" [#2], type "<field>" "<text>", '
                     'scroll "<pane>" up|down, key Escape, or look.')


def step(directory, command, timeout=30):
    directory = Path(directory).resolve(strict=True)
    request = parse(command)
    ready = json.loads((directory / 'ready.json').read_text())
    os.kill(ready['pid'], 0)
    lock = directory / 'step.lock'
    fd = os.open(lock, os.O_CREAT | os.O_EXCL | os.O_WRONLY, 0o600)
    try:
        request['Id'] = uuid.uuid4().hex
        temporary = directory / 'request.json.tmp'
        temporary.write_text(json.dumps(request))
        temporary.replace(directory / 'request.json')
        deadline = time.monotonic() + timeout
        while time.monotonic() < deadline:
            response_path = directory / 'response.json'
            if response_path.exists():
                response = json.loads(response_path.read_text())
                if response.get('id') == request['Id']:
                    if 'error' in response:
                        raise RuntimeError(response['error'])
                    with (directory / 'commands.jsonl').open('a') as log:
                        log.write(json.dumps(dict(command=command, **response)) + '\n')
                    return response['screenshot'], response['text']
            time.sleep(.05)
        raise TimeoutError('Driver did not answer. Inspect driver.log; do not repeat a command with an uncertain outcome.')
    finally:
        os.close(fd)
        lock.unlink()


def main():
    if len(sys.argv) != 2 or not os.environ.get('OCS_PLAY_DIR'):
        raise ValueError('Set OCS_PLAY_DIR to the external play output directory, then pass one quoted command.')
    for path in step(os.environ['OCS_PLAY_DIR'], sys.argv[1]):
        print(path)


if __name__ == '__main__':
    try:
        main()
    except (ValueError, OSError, RuntimeError, TimeoutError) as error:
        print(error, file=sys.stderr)
        sys.exit(1)
