import { FileUploadHelper } from '../../helpers/file-upload.helper';

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

      const createDto = (imageBase64: string) => ({
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

      const createDto = (imageBase64: string) => ({
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

  describe('extractIndexFromFilename', () => {
    it('should extract valid index from numeric filename', () => {
      // Act & Assert
      expect(FileUploadHelper.extractIndexFromFilename('45.jpg')).toBe(45);
      expect(FileUploadHelper.extractIndexFromFilename('1.png')).toBe(1);
      expect(FileUploadHelper.extractIndexFromFilename('999.gif')).toBe(999);
      expect(FileUploadHelper.extractIndexFromFilename('0.webp')).toBe(0);
    });

    it('should extract valid index from filename with leading zeros', () => {
      // Act & Assert
      expect(FileUploadHelper.extractIndexFromFilename('002.jpg')).toBe(2);
      expect(FileUploadHelper.extractIndexFromFilename('045.png')).toBe(45);
      expect(FileUploadHelper.extractIndexFromFilename('000.gif')).toBe(0);
      expect(FileUploadHelper.extractIndexFromFilename('099.webp')).toBe(99);
    });

    it('should return null for non-numeric filenames', () => {
      // Act & Assert
      expect(FileUploadHelper.extractIndexFromFilename('photo-45.jpg')).toBeNull();
      expect(FileUploadHelper.extractIndexFromFilename('photo.jpg')).toBeNull();
      expect(FileUploadHelper.extractIndexFromFilename('DSC_1234.jpg')).toBeNull();
      expect(FileUploadHelper.extractIndexFromFilename('image_45.png')).toBeNull();
    });

    it('should return null for numbers out of range (0-999)', () => {
      // Act & Assert
      expect(FileUploadHelper.extractIndexFromFilename('1000.jpg')).toBeNull();
      expect(FileUploadHelper.extractIndexFromFilename('1234.png')).toBeNull();
      expect(FileUploadHelper.extractIndexFromFilename('9999.gif')).toBeNull();
    });

    it('should return null for filenames with alphanumeric characters', () => {
      // Act & Assert
      expect(FileUploadHelper.extractIndexFromFilename('45a.jpg')).toBeNull();
      expect(FileUploadHelper.extractIndexFromFilename('a45.png')).toBeNull();
      expect(FileUploadHelper.extractIndexFromFilename('4-5.gif')).toBeNull();
      expect(FileUploadHelper.extractIndexFromFilename('4 5.webp')).toBeNull();
    });

    it('should handle files without extensions', () => {
      // Act & Assert
      expect(FileUploadHelper.extractIndexFromFilename('45')).toBe(45);
      expect(FileUploadHelper.extractIndexFromFilename('002')).toBe(2);
      expect(FileUploadHelper.extractIndexFromFilename('photo')).toBeNull();
    });

    it('should handle various file extensions', () => {
      // Act & Assert
      expect(FileUploadHelper.extractIndexFromFilename('5.jpg')).toBe(5);
      expect(FileUploadHelper.extractIndexFromFilename('5.jpeg')).toBe(5);
      expect(FileUploadHelper.extractIndexFromFilename('5.png')).toBe(5);
      expect(FileUploadHelper.extractIndexFromFilename('5.gif')).toBe(5);
      expect(FileUploadHelper.extractIndexFromFilename('5.webp')).toBe(5);
      expect(FileUploadHelper.extractIndexFromFilename('5.tiff')).toBe(5);
    });

    it('should return null for negative numbers', () => {
      // Note: The regex should not match negative numbers, but the parseInt would handle them
      // Act & Assert
      expect(FileUploadHelper.extractIndexFromFilename('-5.jpg')).toBeNull();
      expect(FileUploadHelper.extractIndexFromFilename('-10.png')).toBeNull();
    });
  });
});
