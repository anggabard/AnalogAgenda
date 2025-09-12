import { HasImageDto } from ".";
import { DevKitType, UsernameType } from "../enums";

export interface DevKitDto extends HasImageDto{
  rowKey: string;
  name: string;
  url: string;
  type: DevKitType;
  purchasedBy: UsernameType;
  purchasedOn: string;
  mixedOn: string;
  validForWeeks: number;
  validForFilms: number;
  filmsDeveloped: number;
  description: string;
  expired: boolean
}