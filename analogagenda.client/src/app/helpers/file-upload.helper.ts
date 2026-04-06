/**
 * Helper functions for file upload patterns
 */
export class FileUploadHelper {
  
  /**
   * Creates a file input element and triggers file selection
   */
  static selectFiles(multiple: boolean = false, accept: string = 'image/*'): Promise<FileList | null> {
    return new Promise((resolve) => {
      const fileInput = document.createElement('input');
      fileInput.type = 'file';
      fileInput.multiple = multiple;
      fileInput.accept = accept;
      
      fileInput.onchange = (event: any) => {
        const files = event.target.files;
        resolve(files && files.length > 0 ? files : null);
      };
      
      fileInput.oncancel = () => resolve(null);
      fileInput.click();
    });
  }

  /**
   * Converts a single file to base64 data URL
   */
  static fileToBase64(file: File): Promise<string> {
    return new Promise((resolve, reject) => {
      const reader = new FileReader();
      reader.readAsDataURL(file);
      reader.onload = () => resolve(reader.result as string);
      reader.onerror = (error) => reject(error);
    });
  }

  /**
   * Converts multiple files to base64 data URLs
   */
  static filesToBase64(files: FileList | File[]): Promise<string[]> {
    const fileArray = Array.from(files);
    const promises = fileArray.map(file => this.fileToBase64(file));
    return Promise.all(promises);
  }

  /**
   * Converts multiple files to photo upload DTOs
   */
  static async filesToPhotoUploadDtos<T extends { imageBase64: string }>(
    files: FileList | File[],
    createDto: (imageBase64: string) => T
  ): Promise<T[]> {
    const base64Images = await this.filesToBase64(files);
    return base64Images.map(createDto);
  }

  /**
   * Validates multiple files
   */
  static validateFiles(
    files: FileList | File[],
    maxSizeInMB: number = 50,
    allowedTypes: string[] = ['image/jpeg', 'image/jpg', 'image/png', 'image/gif', 'image/webp']
  ): { isValid: boolean; errors: string[] } {
    const errors: string[] = [];
    
    Array.from(files).forEach((file, index) => {
      // Check file size
      const maxSizeInBytes = maxSizeInMB * 1024 * 1024;
      if (file.size > maxSizeInBytes) {
        errors.push(`File ${index + 1}: File size must be less than ${maxSizeInMB}MB`);
      }

      // Check file type
      if (!allowedTypes.includes(file.type)) {
        errors.push(`File ${index + 1}: File type ${file.type} is not allowed`);
      }
    });

    return { isValid: errors.length === 0, errors };
  }
}
