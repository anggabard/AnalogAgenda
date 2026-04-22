import { PhotoDto } from './photo.dto';

export interface CollectionPublicCommentDto {
  id: string;
  authorName: string;
  body: string;
  /** ISO date from API */
  createdAt?: string;
}

export interface PublicCollectionPageDto {
  requiresPassword: boolean;
  id?: string;
  name?: string;
  fromDate?: string | null;
  toDate?: string | null;
  location?: string;
  description?: string | null;
  featuredImageUrl?: string;
  /** UTC when featured image is set; cache-bust featured `<img>`. */
  featuredImageUpdatedDate?: string | null;
  photos: PhotoDto[];
  comments: CollectionPublicCommentDto[];
}
