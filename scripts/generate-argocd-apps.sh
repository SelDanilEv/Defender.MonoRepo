#!/bin/bash

# Script to generate ArgoCD Application manifests for all Defender services
# This script reads the service matrix from the GitHub Actions workflow and generates
# corresponding ArgoCD Application manifests

set -e

# Configuration
ARGOCD_APPS_DIR="helm/argocd-applications"
GITHUB_REPO="SelDanilEv/Defender.MonoRepo"
K8S_NAMESPACE="defender"
ARGOCD_NAMESPACE="argocd"

# Create argocd-applications directory if it doesn't exist
mkdir -p "$ARGOCD_APPS_DIR"

# Function to generate ArgoCD Application manifest
generate_argocd_app() {
    local service_name=$1
    local clean_name=$2
    local values_file=$3
    
    cat > "$ARGOCD_APPS_DIR/${clean_name}-app.yaml" << EOF
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
  project: default
  source:
    repoURL: https://github.com/${GITHUB_REPO}.git
    targetRevision: HEAD
    path: helm/service-template
    helm:
      valueFiles:
        - ${values_file}
      parameters:
        - name: image.tag
          value: latest
  destination:
    server: https://kubernetes.default.svc
    namespace: ${K8S_NAMESPACE}
  syncPolicy:
    automated:
      prune: true
      selfHeal: true
    syncOptions:
      - CreateNamespace=true
      - PrunePropagationPolicy=foreground
      - PruneLast=true
    retry:
      limit: 5
      backoff:
        duration: 5s
        factor: 2
        maxDuration: 3m
EOF

    echo "Generated ArgoCD Application for ${clean_name}"
}

# Generate applications for all services based on the workflow matrix
echo "Generating ArgoCD Applications..."

# Portal service
generate_argocd_app "Defender.Portal" "defender-portal" "values-portal.yaml"

# User Management service
generate_argocd_app "Defender.UserManagementService" "defender-user-management" "values-user-management.yaml"

# Wallet service
generate_argocd_app "Defender.WalletService" "defender-wallet" "values-wallet.yaml"

# Risk Games service
generate_argocd_app "Defender.RiskGamesService" "defender-risk-games" "values-risk-games.yaml"

# Notification service
generate_argocd_app "Defender.NotificationService" "defender-notification" "values-notification.yaml"

# Job Scheduler service
generate_argocd_app "Defender.JobSchedulerService" "defender-job-scheduler" "values-job-scheduler.yaml"

# Identity service
generate_argocd_app "Defender.IdentityService" "defender-identity" "values-identity.yaml"

echo "All ArgoCD Applications generated successfully in $ARGOCD_APPS_DIR"
echo ""
echo "To deploy these applications:"
echo "1. Update the GitHub repository URL in the generated files"
echo "2. Apply the applications to your ArgoCD cluster:"
echo "   kubectl apply -f $ARGOCD_APPS_DIR/"
echo ""
echo "Note: Make sure your ArgoCD cluster has access to the GitHub repository"
