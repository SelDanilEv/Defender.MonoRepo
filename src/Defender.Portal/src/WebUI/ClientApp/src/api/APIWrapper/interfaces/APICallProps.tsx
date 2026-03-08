import type IUtils from "src/appUtils/interface";

export interface APICallFailure {
  status: number;
  detail?: string;
}

interface APICallProps {
  url: string;
  options: RequestInit;
  utils?: IUtils | null;
  onSuccess?: (response: Response) => Promise<void> | void;
  onFailure?: (response: Response | APICallFailure) => Promise<void> | void;
  onFinal?: () => Promise<void> | void;
  showSuccess?: boolean;
  successMessage?: string;
  showError?: boolean;
  doLock?: boolean;
}

export default APICallProps;
