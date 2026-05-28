from __future__ import annotations

import json
import subprocess  # nosec B404
import sys
import tempfile
from pathlib import Path


PROJECT_ROOT = Path(__file__).resolve().parent.parent
BANDIT_CONFIG_PATH = PROJECT_ROOT / "bandit.yaml"


def main() -> int:
    with tempfile.NamedTemporaryFile(suffix='.json', delete=False) as tmp:
        report_path = Path(tmp.name)

    try:
        command = [
            sys.executable,
            '-m',
            'bandit',
            '-r',
            str(PROJECT_ROOT),
            '-c',
            str(BANDIT_CONFIG_PATH),
            '-f',
            'json',
            '-o',
            str(report_path),
        ]
        completed = subprocess.run(command, cwd=PROJECT_ROOT, check=False)  # nosec B603
        if completed.returncode not in (0, 1):
            return completed.returncode

        report = json.loads(report_path.read_text(encoding='utf-8'))
        results = report.get('results', [])
        if results:
            print(f'Bandit found {len(results)} issue(s).')
            return 1

        print('Bandit found no issues.')
        return 0
    finally:
        report_path.unlink(missing_ok=True)


if __name__ == '__main__':
    raise SystemExit(main())
