"""Run the installed iterator with bounded retries for Windows checkpoint locks."""
import os
from pathlib import Path
import runpy
import sys
import time


original_replace = Path.replace


def retry_replace(path, target):
    for attempt in range(6):
        try:
            return original_replace(path, target)
        except PermissionError as error:
            if os.name != 'nt' or getattr(error, 'winerror', None) not in (5, 32) or attempt == 5:
                raise
            time.sleep(.1 * (attempt + 1))


Path.replace = retry_replace
script = Path.home() / 'Tools/local-sounds/scripts/iterate.py'
sys.argv[0] = str(script)
runpy.run_path(str(script), run_name='__main__')
