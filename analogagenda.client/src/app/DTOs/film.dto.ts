import { FilmType, UsernameType } from "../enums";

export interface FilmDto {
  rowKey: string;
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
  developedInSessionRowKey?: string | null;
  developedWithDevKitRowKey?: string | null;
}
