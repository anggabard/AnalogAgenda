import { FilmType, UsernameType } from "../enums";

export interface FilmDto {
  id: string;
  name?: string;
  brand: string;
  iso: string;
  type: FilmType;
  numberOfExposures: number;
  cost: number;
  purchasedBy: UsernameType;
  purchasedOn: string;
  imageUrl: string;
  description: string;
  developed: boolean;
  developedInSessionId?: string | null;
  developedWithDevKitId?: string | null;
  formattedExposureDate?: string;
  photoCount?: number;
}
