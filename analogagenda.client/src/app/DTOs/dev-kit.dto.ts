import { DevKitType, UsernameType } from "../enums";

export interface DevKitDto {
  id: string;
  name: string;
  url: string;
  type: DevKitType;
  purchasedBy: UsernameType;
  purchasedOn: string;
  mixedOn: string;
  validForWeeks: number;
  validForFilms: number;
  filmsDeveloped: number;
  imageUrl: string;
  description: string;
  expired: boolean
}