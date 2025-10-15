import { HasImageDto } from ".";
import { FilmType, UsernameType } from "../enums";

export interface FilmDto extends HasImageDto{
  rowKey: string;
  name: string;
  iso: string;
  type: FilmType;
  numberOfExposures: number;
  cost: number;
  purchasedBy: UsernameType;
  purchasedOn: string;
  description: string;
  developed: boolean;
  developedInSessionRowKey?: string | null;
  developedWithDevKitRowKey?: string | null;
}
