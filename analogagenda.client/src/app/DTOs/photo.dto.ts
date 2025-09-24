import { HasImageDto } from ".";

export interface PhotoDto extends HasImageDto {
  rowKey: string;
  filmRowId: string;
  index: number;
}

export interface PhotoBulkUploadDto {
  filmRowId: string;
  photos: PhotoUploadDto[];
}

export interface PhotoUploadDto {
  imageBase64: string;
}

export interface PhotoCreateDto {
  filmRowId: string;
  imageBase64: string;
}
