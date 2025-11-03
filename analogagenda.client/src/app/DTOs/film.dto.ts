import { FilmType, UsernameType } from "../enums";
import { ExposureDateEntry } from "./exposure-date-entry.dto";

export interface FilmDto {
  id: string;
  name: string;
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
  exposureDates?: string;
}
