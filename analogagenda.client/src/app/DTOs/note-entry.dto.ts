export interface NoteEntryDto {
  noteRowKey: string;
  time: number; // serialized as "hh:mm:ss"
  process: string;
  film: string;
  details: string;
}