import { DevKitType, UsernameType } from "../enums";

export interface DevKitDto {
  name: string;
  url: string;
  type: DevKitType,
  purchasedBy: UsernameType,
  purchasedOn: string,
  mixedOn: string,
  validForWeeks: number,
  validForFilms: number,
  filmsDeveloped: number,
  imageAsBase64: string,
  description: string;
}