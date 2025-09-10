import { NoteEntryDto } from "./index";

export interface NoteDto {
  rowKey: string;
  name: string;
  entries: NoteEntryDto[];
}