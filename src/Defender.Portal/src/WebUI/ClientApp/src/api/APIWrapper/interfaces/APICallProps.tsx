import type IUtils from "src/appUtils/interface";

export interface APICallFailure {
  status: number;
  title?: string;
  detail?: string;
  type?: string;
  instance?: string;
  traceId?: string;
  errors?: Record<string, string[]>;
}

interface APICallProps {
  url: string;
  options: RequestInit;
  utils?: IUtils | null;
  onSuccess?: (response: Response) => Promise<void> | void;
  onFailure?: (failure: APICallFailure) => Promise<void> | void;
  onFinal?: () => Promise<void> | void;
  showSuccess?: boolean;
  successMessage?: string;
  showError?: boolean;
  doLock?: boolean;
  timeoutMs?: number;
  onSessionExpired?: () => Promise<void> | void;
}

export default APICallProps;
