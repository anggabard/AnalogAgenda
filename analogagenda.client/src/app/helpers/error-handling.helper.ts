/**
 * Helper functions for error handling
 */
export class ErrorHandlingHelper {
  
  /**
   * Handles and logs errors consistently
   */
  static handleError(error: any, context: string): string {
    // Check for standard HTTP error response patterns
    if (error?.error?.message) {
      console.error(`Error in ${context}:`, error);
      return error.error.message;
    }
    
    if (error?.message) {
      console.error(`Error in ${context}:`, error);
      return error.message;
    }

    // Handle HTTP status codes
    if (error?.status) {
      console.error(`Error in ${context}:`, error);
      switch (error.status) {
        case 400:
          return 'Invalid request. Please check your input.';
        case 401:
          return 'You are not authorized to perform this action.';
        case 403:
          return 'Access denied. You do not have permission for this action.';
        case 404:
          return 'The requested resource was not found.';
        case 422:
          return 'The data provided is not valid.';
        case 500:
          return 'An internal server error occurred. Please try again later.';
        default:
          return `An error occurred (Status: ${error.status}).`;
      }
    }

    console.error(`Error in ${context}:`, error);
    return 'An unexpected error occurred. Please try again.';
  }
}
