import { HasImageDto, NoteEntryDto } from "./index";

export interface NoteDto extends HasImageDto{
  rowKey: string;
  name: string;
  sideNote: string;
  entries: NoteEntryDto[];
}