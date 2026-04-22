import { FilmType, UsernameType } from "../enums";

export interface FilmDto {
  id: string;
  name?: string;
  brand: string;
  iso: string;
  type: FilmType;
  numberOfExposures: number;
  cost: number;
  costCurrency?: string;
  purchasedBy: UsernameType;
  purchasedOn: string;
  imageUrl: string;
  description: string;
  developed: boolean;
  developedInSessionId?: string | null;
  developedWithDevKitId?: string | null;
  formattedExposureDate?: string;
  photoCount?: number;
  /** UTC from API; appended as `UpdatedDate` on film image display URLs. */
  updatedDate?: string | null;
}
