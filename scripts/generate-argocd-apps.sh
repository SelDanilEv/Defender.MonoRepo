#!/bin/bash

# Script to generate ArgoCD Application manifests for all Defender services
# This script reads the service matrix from the GitHub Actions workflow and generates
# corresponding ArgoCD Application manifests

set -e

# Configuration
ARGOCD_APPS_DIR="${ARGOCD_APPS_DIR:-helm/argocd-applications}"
GITHUB_REPO="SelDanilEv/Defender.MonoRepo"
K8S_NAMESPACE="defender"
ARGOCD_NAMESPACE="argocd"
ENVIRONMENT="dev"

# Create argocd-applications directory if it doesn't exist
mkdir -p "$ARGOCD_APPS_DIR"
mkdir -p "$ARGOCD_APPS_DIR/$ENVIRONMENT"

# Function to generate ArgoCD Application manifest
generate_argocd_app() {
    local service_name=$1
    local clean_name=$2
    local values_file=$3
    local auto_sync="${4:-true}"
    
    {
        cat << EOF
apiVersion: argoproj.io/v1alpha1
kind: Application
metadata:
  name: ${clean_name}
  namespace: ${ARGOCD_NAMESPACE}
  finalizers:
    - resources-finalizer.argocd.argoproj.io
  labels:
    app.kubernetes.io/name: ${clean_name}
    app.kubernetes.io/part-of: defender
    app.kubernetes.io/instance: ${clean_name}
spec:
  project: auto-deploy-dev
  source:
    repoURL: https://github.com/${GITHUB_REPO}.git
    targetRevision: main
    path: helm/service-template
    helm:
      valueFiles:
        - ${values_file}
  destination:
    server: https://kubernetes.default.svc
    namespace: ${K8S_NAMESPACE}
EOF

        if [[ "$clean_name" == "car-service" ]]; then
            cat << EOF
  ignoreDifferences:
    - group: apps
      kind: Deployment
      jsonPointers:
        - /spec/replicas
EOF
        fi

        cat << EOF
  syncPolicy:
EOF

        if [[ "$auto_sync" == "true" ]]; then
            cat << EOF
    automated:
      prune: true
      selfHeal: true
EOF
        fi

        cat << EOF
    syncOptions:
      - CreateNamespace=true
      - PrunePropagationPolicy=foreground
      - PruneLast=true
EOF

        if [[ "$auto_sync" == "true" ]]; then
            cat << EOF
    retry:
      limit: 5
      backoff:
        duration: 5s
        factor: 2
        maxDuration: 3m
EOF
        fi
    } > "$ARGOCD_APPS_DIR/${ENVIRONMENT}/${clean_name}-app.yaml"

    echo "Generated ArgoCD Application for ${clean_name}"
}

# Generate applications for all services based on the workflow matrix
echo "Generating ArgoCD Applications..."

# Portal service
generate_argocd_app "Defender.Portal" "portal" "values-portal.yaml"

# User Management service
generate_argocd_app "Defender.UserManagementService" "user-management" "values-user-management.yaml"

# Wallet service
generate_argocd_app "Defender.WalletService" "wallet" "values-wallet.yaml"

# Risk Games service
generate_argocd_app "Defender.RiskGamesService" "risk-games" "values-risk-games.yaml"

# Notification service
generate_argocd_app "Defender.NotificationService" "notification" "values-notification.yaml"

# Job Scheduler service
generate_argocd_app "Defender.JobSchedulerService" "job-scheduler" "values-job-scheduler.yaml"

# Identity service
generate_argocd_app "Defender.IdentityService" "identity" "values-identity.yaml"

# Budget Tracker service
generate_argocd_app "Defender.BudgetTracker" "budget-tracker" "values-budget-tracker.yaml"

# Health Care service
generate_argocd_app "Defender.HealthCareService" "health-care" "values-health-care.yaml"
generate_argocd_app "Defender.TravelCalendarService" "travel-calendar" "values-travel-calendar.yaml"
generate_argocd_app "Defender.CarService" "car-service" "values-car.yaml" "false"

# Personal Food Advisor service
generate_argocd_app "Defender.PersonalFoodAdvisor" "personal-food-advisor" "values-personal-food-advisor.yaml"

echo "All ArgoCD Applications generated successfully in $ARGOCD_APPS_DIR"
