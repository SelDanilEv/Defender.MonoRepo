const APIEndpoints = {
  admin: {
    //User management
    loginAsUser: "user/login",
    searchFullUserInfo: "user/search/full-user-info",
    userList: "user/list",
    updateUserInfo: "user/update",
    updateAccountInfo: "user/account/update",
    updateAccountPassword: "user/account/password/update",

    //Banking
    startRecharge: "banking/recharge",
  },
  authorization: {
    login: "login",
    create: "create",
    google: "google",
    logout: "logout",
  },
  account: {
    updateInfo: "update",
    updateSensitiveInfo: "update/sensitive",
    resetPassword: "reset-password",
  },
  verification: {
    check: "check",
    verifyEmail: "verify/email",
    sendUpdateAccountCode: "send/update-account",
    sendResetPasswordCode: "send/reset-password",
    verifyAccessCode: "verify/code",
    resendEmail: "resend/email",
  },
  home: {
    healthcheck: "health",
    authcheck: "authorization/check",
    configuration: "configuration",
  },
  banking: {
    walletInfo: "wallet/info",
    walletPublicInfo: "wallet/info/public",
    walletCreateAccount: "wallet/account/create",
    startTransfer: "transaction/start/transfer",
    transactionHistory: "transaction/history",
  },
  lottery: {
    getMyTickets: "tickets",
    getAvailableTickets: "tickets/available",
    getActiveDraws: "draw/active",
    purchaseTickets: "tickets/purchase",
  },
  budgetTracker: {
    getPositions: "positions",
    position: "position",

    getReviews: "budget-reviews",
    getReviewsByDateRange: "budget-reviews/by-date-range",
    getReviewTemplate: "budget-review/template",
    review: "budget-review",

    mainDiagramSetup: "diagram-setup/main",

    getGroups: "groups",
    group: "group",

    regularExpenses: {
      getExpenses: "regular-expenses/expenses",
      expense: "regular-expenses/expense",
      getReviews: "regular-expenses/reviews",
      getReviewsByDateRange: "regular-expenses/reviews/by-date-range",
      getReviewTemplate: "regular-expenses/review/template",
      review: "regular-expenses/review",
      diagramSetup: "regular-expenses/diagram-setup",
    },
  },
  foodAdvisor: {
    getPreferences: "preferences",
    updatePreferences: "preferences",
    createSession: "session",
    getSessions: "sessions",
    getSession: "session",
    deleteSession: "session",
    uploadImages: "session",
    confirmMenu: "session",
    requestParsing: "session",
    requestRecommendations: "session",
    getRecommendations: "session",
    getRatings: "ratings",
    submitRating: "rating",
  },
  healthCare: {
    events: "events",
    medicationOptions: "events/medication-options",
    chartShares: "chart-shares",
    publicChartShares: "public/chart-shares",
  },
};

const myGarageApiUrls = {
  getVehicles: "/api/my-garage/vehicles",
  createVehicle: "/api/my-garage/vehicles",
  getVehicle: "/api/my-garage/vehicles/:vehicleId",
  updateVehicle: "/api/my-garage/vehicles/:vehicleId",
  archiveVehicle: "/api/my-garage/vehicles/:vehicleId/archive",
  unarchiveVehicle: "/api/my-garage/vehicles/:vehicleId/unarchive",
  getMaintenanceItems: "/api/my-garage/vehicles/:vehicleId/maintenance",
  createMaintenanceItem: "/api/my-garage/vehicles/:vehicleId/maintenance",
  updateMaintenanceItem: "/api/my-garage/vehicles/:vehicleId/maintenance/:maintenanceId",
  deleteMaintenanceItem: "/api/my-garage/vehicles/:vehicleId/maintenance/:maintenanceId",
  getHistory: "/api/my-garage/vehicles/:vehicleId/history",
  createHistory: "/api/my-garage/vehicles/:vehicleId/history",
  updateHistory: "/api/my-garage/vehicles/:vehicleId/history/:historyId",
  deleteHistory: "/api/my-garage/vehicles/:vehicleId/history/:historyId",
  getInsurancePolicies: "/api/my-garage/vehicles/:vehicleId/insurance",
  createInsurancePolicy: "/api/my-garage/vehicles/:vehicleId/insurance",
  updateInsurancePolicy: "/api/my-garage/vehicles/:vehicleId/insurance/:insuranceId",
};

const APIUrls = () => {
  let urls = APIEndpoints;

  const mapControllerEndpoints = (controllerName: string, controller: any) => {
    for (let endpointName in controller) {
      const endpoint = controller[endpointName];

      if (typeof endpoint === "object" && endpoint !== null) {
        mapControllerEndpoints(controllerName, endpoint);
      } else {
        controller[endpointName] = `/api/${controllerName}/${endpoint}`;
      }
    }
  };

  for (let controllerName in APIEndpoints) {
    mapControllerEndpoints(controllerName, urls[controllerName]);
  }

  return urls;
};

const apiUrls = {
  ...(APIUrls() || APIEndpoints),
  myGarage: myGarageApiUrls,
};

export default apiUrls;
