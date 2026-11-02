import { HttpErrorResponse } from '@angular/common/http';

/**
 * Extracts human readable messages from the API error payloads:
 *  - FluentValidation (ValidationProblemDetails): { title, errors: { Field: ["msg"] } }
 *  - Business exceptions (ExceptionResponse):     { title, notificationType, errors: ["msg"] }
 */
export function extractApiErrors(error: unknown): string[] {
  if (error instanceof HttpErrorResponse) {
    const body = error.error;

    if (body && typeof body === 'object') {
      const errors = (body as { errors?: unknown }).errors;

      if (Array.isArray(errors)) {
        return errors.map(String);
      }

      if (errors && typeof errors === 'object') {
        return Object.values(errors as Record<string, unknown>).flatMap((value) =>
          Array.isArray(value) ? value.map(String) : [String(value)],
        );
      }

      const title = (body as { title?: unknown }).title;
      if (typeof title === 'string' && title) {
        return [title];
      }
    }

    if (error.status === 0) {
      return ['The API could not be reached. Check that it is running.'];
    }

    if (error.status === 401) {
      return ['Your session has expired. Please sign in again.'];
    }

    if (error.status === 403) {
      return ['You are not allowed to perform this action.'];
    }

    if (error.status === 404) {
      return ['The requested resource was not found.'];
    }

    return [`Unexpected error (${error.status}).`];
  }

  if (error instanceof Error) {
    return [error.message];
  }

  return ['Unexpected error.'];
}

export function extractApiErrorMessage(error: unknown): string {
  return extractApiErrors(error).join(' ');
}
