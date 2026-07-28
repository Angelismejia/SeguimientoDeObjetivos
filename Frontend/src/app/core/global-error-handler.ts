import { ErrorHandler, Injectable } from '@angular/core';
import { showErrorBanner } from './error-banner';

@Injectable()
export class GlobalErrorHandler implements ErrorHandler {
  handleError(error: unknown): void {
    console.error(error);
    const message = error instanceof Error ? (error.stack ?? error.message) : String(error);
    showErrorBanner(message);
  }
}
