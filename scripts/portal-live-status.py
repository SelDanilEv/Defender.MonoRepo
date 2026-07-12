"""Compact, secret-safe Portal deployment verification."""

from __future__ import annotations

import argparse
import json
import time
import urllib.request
from pathlib import Path

import paramiko


def read_credentials(home_server_root: Path) -> dict[str, str]:
    values: dict[str, str] = {}
    path = home_server_root / "creds" / "argo-cd.config"
    for line in path.read_text(encoding="utf-8").splitlines():
        if "=" in line:
            key, value = line.split("=", 1)
            values[key.strip().lower()] = value.strip()
    if not values.get("username") or not values.get("password") or values["password"] == "<CHANGE_ME>":
        raise RuntimeError(f"Valid credentials required in {path}")
    return values


def http_status(url: str) -> int:
    request = urllib.request.Request(url, method="GET", headers={"User-Agent": "Defender-Portal-Deploy-Check"})
    with urllib.request.urlopen(request, timeout=20) as response:
        return response.status


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--home-server-root", type=Path, required=True)
    parser.add_argument("--expected-tag", required=True)
    parser.add_argument("--timeout-seconds", type=int, default=900)
    args = parser.parse_args()

    credentials = read_credentials(args.home_server_root)
    client = paramiko.SSHClient()
    client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
    client.connect(
        "192.168.1.35",
        username=credentials["username"],
        password=credentials["password"],
        timeout=12,
        banner_timeout=12,
        auth_timeout=12,
    )

    deadline = time.monotonic() + args.timeout_seconds
    refreshed = False
    try:
        while time.monotonic() < deadline:
            command = r"""printf '%s|' "$(kubectl -n argocd get application portal -o jsonpath='{.status.sync.status}')"; printf '%s|' "$(kubectl -n argocd get application portal -o jsonpath='{.status.health.status}')"; printf '%s|' "$(kubectl -n argocd get application portal -o jsonpath='{.status.sync.revision}')"; kubectl -n defender get deployment portal -o jsonpath='{.status.readyReplicas}|{.spec.replicas}|{.status.availableReplicas}|{.spec.template.spec.containers[0].image}'"""
            _, stdout, stderr = client.exec_command(command, timeout=30)
            output = stdout.read().decode(errors="replace").strip()
            error = stderr.read().decode(errors="replace").strip()
            if stdout.channel.recv_exit_status() != 0:
                raise RuntimeError(error or "kubectl status query failed")
            sync, health, revision, ready, desired, available, image = output.split("|", 6)
            if sync == "Synced" and health == "Healthy" and ready == desired == available and image.endswith(f":{args.expected_tag}"):
                portal = http_status("https://portal.coded-by-danil.dev/")
                health_code = http_status("https://portal.coded-by-danil.dev/health")
                print(json.dumps({
                    "sync": sync,
                    "health": health,
                    "revision": revision,
                    "image": image,
                    "portal_http": portal,
                    "health_http": health_code,
                }))
                return 0
            if not refreshed:
                client.exec_command(
                    "kubectl -n argocd annotate application portal argocd.argoproj.io/refresh=hard --overwrite",
                    timeout=30,
                )
                refreshed = True
            time.sleep(10)
    finally:
        client.close()

    raise TimeoutError(f"Portal did not reach expected tag {args.expected_tag} before timeout")


if __name__ == "__main__":
    raise SystemExit(main())
