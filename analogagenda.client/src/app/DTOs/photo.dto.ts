import { HasImageDto } from ".";

export interface PhotoDto extends HasImageDto {
  id: string;
  filmId: string;
  index: number;
  restricted?: boolean;
}

export interface PhotoCreateDto {
  filmId: string;
  imageBase64: string;
  index?: number; // Optional index (0-999). If not provided, next available index is used
}
