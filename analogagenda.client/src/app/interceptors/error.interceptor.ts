import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      let errorMessage = '';

      if (error.error instanceof ErrorEvent) {
        // Client-side error
        errorMessage = `Client Error: ${error.error.message}`;
        console.error('Client-side error occurred:', error.error.message);
      } else {
        // Server-side error
        errorMessage = getServerErrorMessage(error);
        console.error(`Server Error - Status: ${error.status}, Message: ${errorMessage}`);
        
        // Handle specific HTTP status codes
        handleHttpErrorStatus(error.status);
      }

      // Show user-friendly error notification
      showErrorNotification(errorMessage, error.status);

      // Return the original HttpErrorResponse to preserve all error information for guards
      return throwError(() => error);
    })
  );
};

function getServerErrorMessage(error: HttpErrorResponse): string {
  // Try to extract error message from server response
  if (error.error?.message) {
    return error.error.message;
  }

  // Fallback error messages based on status code
  switch (error.status) {
    case 400:
      return 'Bad request. Please check your input and try again.';
    case 401:
      return 'You are not authorized to perform this action.';
    case 403:
      return 'Access forbidden. You don\'t have permission for this action.';
    case 404:
      return 'The requested resource was not found.';
    case 500:
      return 'An internal server error occurred. Please try again later.';
    case 503:
      return 'Service temporarily unavailable. The server is processing your request. Please wait...';
    case 507:
      return 'Server is processing photos. Please wait a moment and the upload will continue...';
    default:
      return error.message || 'An unexpected error occurred';
  }
}

function handleHttpErrorStatus(status: number): void {
  switch (status) {
    case 401:
      // Don't auto-redirect on 401 during uploads - let the upload service retry
      // Guards will handle redirect for navigation, but we don't want to interrupt uploads
      console.warn('Authentication required - upload service will handle retry');
      break;
    case 403:
      // Could redirect to access denied page
      console.warn('Access denied');
      break;
    case 500:
      // Could redirect to error page for server errors
      console.error('Server error occurred');
      break;
    case 503:
    case 507:
      // These are temporary server overload errors - don't treat as authentication failure
      // The upload service will handle retries
      console.warn(`Server temporarily unavailable (${status}) - upload will retry`);
      break;
  }
}

function showErrorNotification(message: string, status: number): void {
  // For now, we'll use console.error, but this should be replaced with a toast service
  // or notification component in a real application
  console.error(`Error ${status}: ${message}`);
  
  // You could integrate with a toast library like ngx-toastr here
  // this.toastr.error(message, `Error ${status}`);
}
