export interface CollectionDto {
  id: string;
  name: string;
  fromDate?: string | null;
  toDate?: string | null;
  location: string;
  /** Blob id for the card image */
  imageId: string;
  isOpen: boolean;
  owner: string;
  photoIds: string[];
  photoCount: number;
  imageUrl: string;
}

export interface CollectionOptionDto {
  id: string;
  name: string;
  imageUrl: string;
}
