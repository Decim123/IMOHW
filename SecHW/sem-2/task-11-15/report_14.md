# Report 14

## Repository

- Repository URL: `<вставьте ссылку на репозиторий>`
- Branch merged into `main`
- Final commit message: `Task 14 ready.`

## SAST

### Bandit command

```bash
bandit -r . -f json -o bandit_report.json
```

### What was fixed

- Removed hardcoded passwords from Python code and moved them to environment variables
- Marked safe `subprocess` usage in `test_security.py` with `# nosec`
- Added automatic Bandit checks locally and in GitHub Actions

### Screenshots

- Bandit before fixes
- Bandit after fixes

![bandit local before after](./screenshots/bandit-before-after.png)

## Pre-commit

- Added `.pre-commit-config.yaml` with Bandit hook
- Installed hook locally via `pre-commit install`

## GitHub Actions

Workflow file:

- `.github/workflows/security.yml`

Screenshot:

![github actions success](./screenshots/github-actions-success.png)

## DAST Bonus

Optional:

- `zap_report.txt` if ZAP baseline scan was executed

## Notes

- `bandit_report.json` is generated locally and in CI
- The CI workflow uploads the Bandit report as an artifact
