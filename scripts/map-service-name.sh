#!/bin/bash

SERVICE_NAME="${1:-}"

if [ -z "$SERVICE_NAME" ]; then
    echo "Error: service name parameter is required"
    exit 1
fi

if [ "$SERVICE_NAME" == "ALL" ]; then
    echo "ALL"
    exit 0
fi

case "$SERVICE_NAME" in
    "Defender.Portal")
        echo "portal"
        ;;
    "Defender.UserManagementService")
        echo "user-management"
        ;;
    "Defender.WalletService")
        echo "wallet"
        ;;
    "Defender.RiskGamesService")
        echo "risk-games"
        ;;
    "Defender.NotificationService")
        echo "notification"
        ;;
    "Defender.JobSchedulerService")
        echo "job-scheduler"
        ;;
    "Defender.IdentityService")
        echo "identity"
        ;;
    "Defender.BudgetTracker")
        echo "budget-tracker"
        ;;
    *)
        echo "$SERVICE_NAME"
        ;;
esac
