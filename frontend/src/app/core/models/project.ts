export interface GitHubKeys {
  accessToken: string;
  privateKey?: string | null;
}

/** A project of the portfolio (the GitHub keys are never returned by the API). */
export interface Project {
  publicId: string;
  name: string;
  description?: string | null;
  newRelicId: string;
  gitHubRepository: string;
  priorityBranch: string;
  hasGitHubKeys?: boolean;
  lastErrorSyncAt?: string | null;
  lastUpdated: string;
  createdAt: string;
}

export interface ProjectCreateRequest {
  name: string;
  description?: string | null;
  newRelicId: string;
  gitHubRepository: string;
  gitHubKeys: GitHubKeys;
  priorityBranch: string;
}

/** On update the keys are optional: when omitted the current ones are kept. */
export interface ProjectUpdateRequest extends Omit<ProjectCreateRequest, 'gitHubKeys'> {
  gitHubKeys?: GitHubKeys | null;
}

/** Summary returned by POST /Projects/{id}/Errors/Sync. */
export interface SyncProjectErrorsResponse {
  projectId: string;
  projectName: string;
  since: string;
  until: string;
  eventsProcessed: number;
  newErrors: number;
  updatedErrors: number;
}
