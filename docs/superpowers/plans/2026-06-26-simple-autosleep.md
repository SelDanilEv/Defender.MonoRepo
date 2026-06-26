# Simple Autosleep Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add the simplest working automatic wake-on-request and sleep-on-idle behavior for Defender Kubernetes services.

**Architecture:** Use KEDA HTTP Add-on for HTTP request wake-up and KEDA cron triggers for simple periodic wake windows for background services. Keep one shared Helm chart and per-service values; avoid service code changes.

**Tech Stack:** K3s, Argo CD, Helm, KEDA, KEDA HTTP Add-on, Traefik ingress.

---

### Task 1: Add autoscaling templates to the shared chart

**Files:**
- Modify: `helm/service-template/templates/deployment.yaml`
- Modify: `helm/service-template/templates/ingress.yaml`
- Create: `helm/service-template/templates/keda-http-interceptorroute.yaml`
- Create: `helm/service-template/templates/keda-scaledobject.yaml`
- Modify: `helm/service-template/values.yaml`

- [x] Omit `spec.replicas` from Deployment whenever HTTP or cron autoscaling is enabled.
- [x] Route ingress to `keda-add-ons-http-interceptor-proxy` when HTTP autoscaling is enabled.
- [x] Render `InterceptorRoute` for HTTP-enabled services.
- [x] Render one `ScaledObject` per autoscaled service with HTTP and/or cron triggers.

### Task 2: Enable simple autosleep values

**Files:**
- Modify: all `helm/service-template/values-*.yaml` service files.

- [x] Enable HTTP autoscaling for all public/internal HTTP services.
- [x] Add cron wake windows for `job-scheduler`, `wallet`, `risk-games`, and `personal-food-advisor`.
- [x] Use `minReplicaCount: 0`, `maxReplicaCount: 1`, cooldown around 20 minutes for HTTP services.
- [x] Use recurring 10-minute cron windows every 30 minutes for background work.

### Task 3: Stop Argo CD from fighting KEDA replicas

**Files:**
- Modify: all `helm/argocd-applications/dev/*-app.yaml`.

- [x] Add `ignoreDifferences` for `apps/Deployment` `/spec/replicas`.

### Task 4: Validate and deploy

- [x] Run `helm template` for representative services.
- [x] Install KEDA core and HTTP Add-on in the live K3s cluster if missing.
- [x] Apply/sync the updated Argo applications.
- [x] Verify KEDA CRDs, ScaledObjects, InterceptorRoutes, and pod scale down/up behavior.
