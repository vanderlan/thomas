import { PageQuery } from './paged-list';

/** Mirrors the ErrorLogStatus enum of the API (serialized as numbers). */
export enum ErrorLogStatus {
  New = 1,
  Recurring = 2,
  Resolved = 3,
  Reopened = 4,
}

export const ERROR_LOG_STATUSES: { value: ErrorLogStatus; label: string }[] = [
  { value: ErrorLogStatus.New, label: 'New' },
  { value: ErrorLogStatus.Recurring, label: 'Recurring' },
  { value: ErrorLogStatus.Resolved, label: 'Resolved' },
  { value: ErrorLogStatus.Reopened, label: 'Reopened' },
];

export function errorLogStatusLabel(status: ErrorLogStatus): string {
  return ERROR_LOG_STATUSES.find((s) => s.value === status)?.label ?? `Unknown (${status})`;
}

/** Item of the knowledge base listing. */
export interface ErrorLog {
  publicId: string;
  projectId: string;
  fingerprint: string;
  errorClass: string;
  message?: string | null;
  transactionName?: string | null;
  status: ErrorLogStatus;
  occurrences: number;
  firstSeenAt: string;
  lastSeenAt: string;
  resolvedAt?: string | null;
  lastUpdated: string;
  createdAt: string;
}

/** Full entry, including the stack trace. */
export interface ErrorLogDetail extends ErrorLog {
  stackTrace?: string | null;
  newRelicErrorGroupId?: string | null;
}

export interface ErrorLogFilter extends PageQuery {
  projectId?: string | null;
  status?: ErrorLogStatus | null;
}
