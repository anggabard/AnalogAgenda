import { FileUploadHelper } from '../../helpers/file-upload.helper';
import { PhotoUploadDto } from '../../DTOs';

describe('FileUploadHelper', () => {
  describe('selectFiles', () => {
    let mockInput: HTMLInputElement;

    beforeEach(() => {
      // Mock HTML input element
      mockInput = document.createElement('input');
      spyOn(document, 'createElement').and.returnValue(mockInput);
      spyOn(document.body, 'appendChild');
      spyOn(document.body, 'removeChild');
    });

    it('should create file input with correct attributes for single file', async () => {
      // Arrange
      const clickSpy = spyOn(mockInput, 'click');
      
      // Act
      FileUploadHelper.selectFiles(false, 'image/*');
      
      // Assert
      expect(document.createElement).toHaveBeenCalledWith('input');
      expect(mockInput.type).toBe('file');
      expect(mockInput.accept).toBe('image/*');
      expect(mockInput.multiple).toBeFalsy();
      expect(clickSpy).toHaveBeenCalled();
    });

    it('should create file input with multiple attribute for multiple files', async () => {
      // Arrange
      const clickSpy = spyOn(mockInput, 'click');
      
      // Act
      FileUploadHelper.selectFiles(true, 'image/jpeg');
      
      // Assert
      expect(mockInput.multiple).toBeTruthy();
      expect(mockInput.accept).toBe('image/jpeg');
      expect(clickSpy).toHaveBeenCalled();
    });

    it('should return null when user cancels file selection', async () => {
      // Arrange
      spyOn(mockInput, 'click').and.callFake(() => {
        // Simulate user cancelling - no files selected
        Object.defineProperty(mockInput, 'files', {
          value: null,
          writable: true
        });
        mockInput.dispatchEvent(new Event('change'));
      });

      // Act
      const result = await FileUploadHelper.selectFiles();

      // Assert
      expect(result).toBeNull();
    });
  });

  describe('filesToPhotoUploadDtos', () => {
    it('should convert files to photo upload DTOs', async () => {
      // Arrange
      const mockFile = new File(['test'], 'test.jpg', { type: 'image/jpeg' });
      const base64Data = 'data:image/jpeg;base64,dGVzdA=='; // 'test' in base64
      
      // Mock filesToBase64 method
      spyOn(FileUploadHelper, 'filesToBase64').and.returnValue(Promise.resolve([base64Data]));

      const createDto = (imageBase64: string): PhotoUploadDto => ({
        imageBase64
      });

      // Act
      const result = await FileUploadHelper.filesToPhotoUploadDtos([mockFile], createDto);

      // Assert
      expect(result).toEqual([{ imageBase64: base64Data }]);
      expect(FileUploadHelper.filesToBase64).toHaveBeenCalledWith([mockFile]);
    });

    it('should handle multiple files', async () => {
      // Arrange
      const mockFile1 = new File(['test1'], 'test1.jpg', { type: 'image/jpeg' });
      const mockFile2 = new File(['test2'], 'test2.jpg', { type: 'image/jpeg' });
      const base64Data = ['data:image/jpeg;base64,dGVzdA==', 'data:image/jpeg;base64,dGVzdDI='];
      
      // Mock filesToBase64 method
      spyOn(FileUploadHelper, 'filesToBase64').and.returnValue(Promise.resolve(base64Data));

      const createDto = (imageBase64: string): PhotoUploadDto => ({
        imageBase64
      });

      // Act
      const result = await FileUploadHelper.filesToPhotoUploadDtos([mockFile1, mockFile2], createDto);

      // Assert
      expect(result).toEqual([
        { imageBase64: base64Data[0] },
        { imageBase64: base64Data[1] }
      ]);
    });
  });

  describe('validateFiles', () => {
    it('should return valid for acceptable files', () => {
      // Arrange
      const validFile = new File(['test'], 'test.jpg', { type: 'image/jpeg' });
      const fileList = [validFile];

      // Act
      const result = FileUploadHelper.validateFiles(fileList);

      // Assert
      expect(result.isValid).toBeTruthy();
      expect(result.errors.length).toBe(0);
    });

    it('should return invalid for files that are too large', () => {
      // Arrange
      const largeContent = 'x'.repeat(60 * 1024 * 1024); // 60MB
      const largeFile = new File([largeContent], 'large.jpg', { type: 'image/jpeg' });
      const fileList = [largeFile];

      // Act
      const result = FileUploadHelper.validateFiles(fileList, 50); // 50MB limit

      // Assert
      expect(result.isValid).toBeFalsy();
      expect(result.errors).toContain('File 1: File size must be less than 50MB'); // Based on actual implementation
    });

    it('should return invalid for unsupported file types', () => {
      // Arrange
      const unsupportedFile = new File(['test'], 'test.txt', { type: 'text/plain' });
      const fileList = [unsupportedFile];

      // Act
      const result = FileUploadHelper.validateFiles(fileList);

      // Assert
      expect(result.isValid).toBeFalsy();
      expect(result.errors).toContain('File 1: File type text/plain is not allowed'); // Based on actual implementation
    });

    it('should handle multiple validation errors', () => {
      // Arrange
      const largeContent = 'x'.repeat(60 * 1024 * 1024); // 60MB
      const largeFile = new File([largeContent], 'large.txt', { type: 'text/plain' });
      const fileList = [largeFile];

      // Act
      const result = FileUploadHelper.validateFiles(fileList, 50);

      // Assert
      expect(result.isValid).toBeFalsy();
      expect(result.errors.length).toBe(2);
      expect(result.errors).toContain('File 1: File size must be less than 50MB'); // Based on actual implementation
      expect(result.errors).toContain('File 1: File type text/plain is not allowed'); // Based on actual implementation
    });
  });
});
