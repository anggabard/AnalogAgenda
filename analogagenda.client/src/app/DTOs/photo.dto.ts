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
  index?: number; // 0–999; client sets from upload order modal (API may assign if omitted)
}
