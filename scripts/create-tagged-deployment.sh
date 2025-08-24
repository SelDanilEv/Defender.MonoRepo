#!/bin/bash

# Script to create ArgoCD applications with specific image tags
# This enables deployment strategies like blue-green, canary, or tagged releases

set -e

# Configuration
ARGOCD_APPS_DIR="helm/argocd-applications"
GITHUB_REPO="SelDanilEv/Defender.MonoRepo"
K8S_NAMESPACE="defender"
ARGOCD_NAMESPACE="argocd"

# Function to create tagged deployment application
create_tagged_deployment() {
    local service_name=$1
    local clean_name=$2
    local values_file=$3
    local image_tag=$4
    local deployment_type=${5:-"tagged"} # tagged, canary, blue-green
    
    local app_name="${clean_name}-${deployment_type}"
    local app_file="${ARGOCD_APPS_DIR}/${app_name}-app.yaml"
    
    # Create namespace-specific deployment
    local namespace="${K8S_NAMESPACE}-${deployment_type}"
    
    cat > "$app_file" << EOF
apiVersion: argoproj.io/v1alpha1
kind: Application
metadata:
  name: ${app_name}
  namespace: ${ARGOCD_NAMESPACE}
  finalizers:
    - resources-finalizer.argocd.argoproj.io
  labels:
    app.kubernetes.io/name: ${clean_name}
    app.kubernetes.io/part-of: defender
    app.kubernetes.io/instance: ${app_name}
    deployment.type: ${deployment_type}
    image.tag: ${image_tag}
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
          value: ${image_tag}
        - name: namespace
          value: ${namespace}
  destination:
    server: https://kubernetes.default.svc
    namespace: ${namespace}
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

    echo "Generated ${deployment_type} deployment for ${clean_name} with tag ${image_tag}"
}

# Function to create production deployment
create_production_deployment() {
    local service_name=$1
    local clean_name=$2
    local values_file=$3
    local image_tag=$4
    
    local app_name="${clean_name}-prod"
    local app_file="${ARGOCD_APPS_DIR}/${app_name}-app.yaml"
    
    cat > "$app_file" << EOF
apiVersion: argoproj.io/v1alpha1
kind: Application
metadata:
  name: ${app_name}
  namespace: ${ARGOCD_NAMESPACE}
  finalizers:
    - resources-finalizer.argocd.argoproj.io
  labels:
    app.kubernetes.io/name: ${clean_name}
    app.kubernetes.io/part-of: defender
    app.kubernetes.io/instance: ${app_name}
    deployment.type: production
    image.tag: ${image_tag}
spec:
  project: production
  source:
    repoURL: https://github.com/${GITHUB_REPO}.git
    targetRevision: HEAD
    path: helm/service-template
    helm:
      valueFiles:
        - ${values_file}
      parameters:
        - name: image.tag
          value: ${image_tag}
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

    echo "Generated production deployment for ${clean_name} with tag ${image_tag}"
}

# Function to create staging deployment
create_staging_deployment() {
    local service_name=$1
    local clean_name=$2
    local values_file=$3
    local image_tag=$4
    
    local app_name="${clean_name}-staging"
    local app_file="${ARGOCD_APPS_DIR}/${app_name}-app.yaml"
    
    cat > "$app_file" << EOF
apiVersion: argoproj.io/v1alpha1
kind: Application
metadata:
  name: ${app_name}
  namespace: ${ARGOCD_NAMESPACE}
  finalizers:
    - resources-finalizer.argocd.argoproj.io
  labels:
    app.kubernetes.io/name: ${clean_name}
    app.kubernetes.io/part-of: defender
    app.kubernetes.io/instance: ${app_name}
    deployment.type: staging
    image.tag: ${image_tag}
spec:
  project: staging
  source:
    repoURL: https://github.com/${GITHUB_REPO}.git
    targetRevision: HEAD
    path: helm/service-template
    helm:
      valueFiles:
        - ${values_file}
      parameters:
        - name: image.tag
          value: ${image_tag}
  destination:
    server: https://kubernetes.default.svc
    namespace: ${K8S_NAMESPACE}-staging
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

    echo "Generated staging deployment for ${clean_name} with tag ${image_tag}"
}

# Main execution
main() {
    local image_tag=${1:-"latest"}
    local deployment_strategy=${2:-"all"} # all, production, staging, tagged
    
    echo "Creating ArgoCD deployments with tag: ${image_tag}"
    echo "Deployment strategy: ${deployment_strategy}"
    echo ""
    
    # Create argocd-applications directory if it doesn't exist
    mkdir -p "$ARGOCD_APPS_DIR"
    
    # Service definitions
    local services=(
        "Defender.Portal:defender-portal:values-portal.yaml"
        "Defender.UserManagementService:defender-user-management:values-user-management.yaml"
        "Defender.WalletService:defender-wallet:values-wallet.yaml"
        "Defender.RiskGamesService:defender-risk-games:values-risk-games.yaml"
        "Defender.NotificationService:defender-notification:values-notification.yaml"
        "Defender.JobSchedulerService:defender-job-scheduler:values-job-scheduler.yaml"
        "Defender.IdentityService:defender-identity:values-identity.yaml"
    )
    
    for service in "${services[@]}"; do
        IFS=':' read -r service_name clean_name values_file <<< "$service"
        
        case "$deployment_strategy" in
            "production")
                create_production_deployment "$service_name" "$clean_name" "$values_file" "$image_tag"
                ;;
            "staging")
                create_staging_deployment "$service_name" "$clean_name" "$values_file" "$image_tag"
                ;;
            "tagged")
                create_tagged_deployment "$service_name" "$clean_name" "$values_file" "$image_tag" "tagged"
                ;;
            "all")
                create_production_deployment "$service_name" "$clean_name" "$values_file" "$image_tag"
                create_staging_deployment "$service_name" "$clean_name" "$values_file" "$image_tag"
                create_tagged_deployment "$service_name" "$clean_name" "$values_file" "$image_tag" "tagged"
                ;;
            *)
                echo "Unknown deployment strategy: $deployment_strategy"
                exit 1
                ;;
        esac
    done
    
    echo ""
    echo "Deployment applications created successfully!"
    echo ""
    echo "To deploy:"
    echo "1. Update the GitHub repository URL in the generated files"
    echo "2. Apply the applications: kubectl apply -f $ARGOCD_APPS_DIR/"
    echo ""
    echo "Available deployment types:"
    echo "- Production: Main production environment"
    echo "- Staging: Pre-production testing environment"
    echo "- Tagged: Specific version deployments for testing"
}

# Script usage
if [[ "$1" == "--help" || "$1" == "-h" ]]; then
    echo "Usage: $0 [IMAGE_TAG] [DEPLOYMENT_STRATEGY]"
    echo ""
    echo "Arguments:"
    echo "  IMAGE_TAG              Docker image tag to deploy (default: latest)"
    echo "  DEPLOYMENT_STRATEGY    Deployment strategy (default: all)"
    echo "                         Options: all, production, staging, tagged"
    echo ""
    echo "Examples:"
    echo "  $0                    # Deploy latest tag to all environments"
    echo "  $0 v1.2.3            # Deploy v1.2.3 tag to all environments"
    echo "  $0 v1.2.3 production # Deploy v1.2.3 tag to production only"
    echo "  $0 v1.2.3 staging    # Deploy v1.2.3 tag to staging only"
    exit 0
fi

# Execute main function
main "$@"
