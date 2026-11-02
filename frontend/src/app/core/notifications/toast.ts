import { Injectable, signal } from '@angular/core';

export type ToastKind = 'success' | 'error' | 'info';

export interface ToastMessage {
  id: number;
  kind: ToastKind;
  text: string;
}

/** Lightweight notifications shown by the <app-toasts> component. */
@Injectable({ providedIn: 'root' })
export class Toast {
  private static nextId = 1;

  readonly messages = signal<ToastMessage[]>([]);

  success(text: string): void {
    this.push('success', text);
  }

  error(text: string): void {
    this.push('error', text, 8000);
  }

  info(text: string): void {
    this.push('info', text);
  }

  dismiss(id: number): void {
    this.messages.update((messages) => messages.filter((message) => message.id !== id));
  }

  private push(kind: ToastKind, text: string, duration = 4000): void {
    const message: ToastMessage = { id: Toast.nextId++, kind, text };

    this.messages.update((messages) => [...messages, message]);

    setTimeout(() => this.dismiss(message.id), duration);
  }
}
