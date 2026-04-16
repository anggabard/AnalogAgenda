import { HasImageDto } from ".";

export interface PhotoDto extends HasImageDto {
  id: string;
  /** Blob id in the photos container (for collection card image). */
  imageId?: string;
  filmId: string;
  index: number;
  /** 1-based order in a collection when applicable */
  collectionIndex?: number | null;
  restricted?: boolean;
}

export interface PhotoCreateDto {
  filmId: string;
  imageBase64: string;
  index?: number; // 0–999; client sets from upload order modal (API may assign if omitted)
}
