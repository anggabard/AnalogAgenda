import { HasImageDto, NoteEntryDto } from "./index";

export interface NoteDto extends HasImageDto{
  id: string;
  name: string;
  sideNote: string;
  entries: NoteEntryDto[];
}