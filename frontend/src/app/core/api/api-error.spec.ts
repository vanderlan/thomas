import { HttpErrorResponse } from '@angular/common/http';
import { extractApiErrorMessage, extractApiErrors } from './api-error';

describe('extractApiErrors', () => {
  it('reads the FluentValidation problem details', () => {
    const error = new HttpErrorResponse({
      status: 400,
      error: {
        title: 'One or more validation errors occurred.',
        errors: {
          Name: ['Is necessary to inform the Name'],
          GitHubRepository: ['Invalid', 'Empty'],
        },
      },
    });

    expect(extractApiErrors(error)).toEqual([
      'Is necessary to inform the Name',
      'Invalid',
      'Empty',
    ]);
  });

  it('reads the business exception response', () => {
    const error = new HttpErrorResponse({
      status: 409,
      error: {
        title: 'Validation Error',
        notificationType: 'ERROR',
        errors: ['There is already a project with this name'],
      },
    });

    expect(extractApiErrors(error)).toEqual(['There is already a project with this name']);
  });

  it('falls back to the title when there are no errors', () => {
    const error = new HttpErrorResponse({ status: 500, error: { title: 'Unexpected error' } });

    expect(extractApiErrors(error)).toEqual(['Unexpected error']);
  });

  it('describes the common status codes', () => {
    expect(extractApiErrors(new HttpErrorResponse({ status: 0 }))[0]).toContain(
      'could not be reached',
    );
    expect(extractApiErrors(new HttpErrorResponse({ status: 401 }))[0]).toContain(
      'session has expired',
    );
    expect(extractApiErrors(new HttpErrorResponse({ status: 403 }))[0]).toContain('not allowed');
    expect(extractApiErrors(new HttpErrorResponse({ status: 404 }))[0]).toContain('not found');
    expect(extractApiErrors(new HttpErrorResponse({ status: 503 }))[0]).toContain('503');
  });

  it('handles plain errors and unknown values', () => {
    expect(extractApiErrors(new Error('boom'))).toEqual(['boom']);
    expect(extractApiErrors('nope')).toEqual(['Unexpected error.']);
  });

  it('joins the messages', () => {
    const error = new HttpErrorResponse({ status: 400, error: { errors: ['a', 'b'] } });

    expect(extractApiErrorMessage(error)).toBe('a b');
  });
});
