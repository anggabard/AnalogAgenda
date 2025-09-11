export interface NoteEntryDto {
  rowKey: string;
  noteRowKey: string;
  time: number; // serialized as "hh:mm:ss"
  process: string;
  film: string;
  details: string;
}