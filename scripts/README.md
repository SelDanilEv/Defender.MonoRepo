# Repository automation

Use these wrappers first. They keep successful output compact and expose only the failing log tail.

## Portal verification

```powershell
# Full frontend gate
powershell -NoProfile -File scripts/verify-portal.ps1

# Full gate plus browser journeys
powershell -NoProfile -File scripts/verify-portal.ps1 -IncludeE2E

# Fast iteration for one test file
powershell -NoProfile -File scripts/verify-portal.ps1 -TestPath src/components/LogoSign/index.test.tsx
```

Default verification runs TypeScript, zero-warning ESLint, Vitest, Vite build, and npm audit. `-TestPath` runs only the named test. On success, output is two lines; on failure, the script prints the failing step and last 80 log lines.

## Portal deployment

Deployment is preview-only unless `-Execute` is present:

```powershell
powershell -NoProfile -File scripts/deploy-portal.ps1
powershell -NoProfile -File scripts/deploy-portal.ps1 -Execute
powershell -NoProfile -File scripts/deploy-portal.ps1 -Execute -SkipLiveCheck
```

Execution requires a clean `main`, authenticated `gh`, Python with `paramiko`, and ignored HomeServer credentials at `E:\MyApps\Defender.HomeServer\creds\argo-cd.config`. It pushes `main`, dispatches a Portal-only image build, reads its published release tag, promotes that tag, waits for ArgoCD, then verifies the deployed image and public Portal/health routes. Secrets are never printed.

## Token-efficient agent workflow

- Read root instructions plus only the nearest service instructions.
- Run targeted tests while iterating; run the compact full wrapper before completion.
- Do not stream successful build, test, workflow-watch, or Kubernetes logs.
- On failure, report command, exit code, decisive error, and bounded log tail.
- Report final evidence as counts/statuses, not raw logs.
