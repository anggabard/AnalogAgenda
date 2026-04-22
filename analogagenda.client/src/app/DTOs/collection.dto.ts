export interface CollectionDto {
  id: string;
  name: string;
  fromDate?: string | null;
  toDate?: string | null;
  location: string;
  /** Optional; shown on public page */
  description?: string;
  /** Blob id for the card image */
  imageId: string;
  isOpen: boolean;
  isPublic?: boolean;
  /** Set when making collection public; never returned from GET */
  publicPassword?: string | null;
  owner: string;
  photoIds: string[];
  photoCount: number;
  imageUrl: string;
  /** UTC from API; cache-bust collection card image. */
  updatedDate?: string | null;
}

export interface CollectionOptionDto {
  id: string;
  name: string;
  imageUrl: string;
  updatedDate?: string | null;
}
