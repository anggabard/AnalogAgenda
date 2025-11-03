import { HasImageDto } from ".";

export interface PhotoDto extends HasImageDto {
  id: string;
  filmId: string;
  index: number;
}

export interface PhotoBulkUploadDto {
  filmId: string;
  photos: PhotoUploadDto[];
}

export interface PhotoUploadDto {
  imageBase64: string;
}

export interface PhotoCreateDto {
  filmId: string;
  imageBase64: string;
}
