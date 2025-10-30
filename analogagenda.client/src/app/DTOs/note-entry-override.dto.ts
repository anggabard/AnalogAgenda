export interface NoteEntryOverrideDto {
  rowKey: string;
  noteEntryRowKey: string;
  filmCountMin: number;
  filmCountMax: number;
  time?: number;
  step?: string;
  details?: string;
  temperatureMin?: number;
  temperatureMax?: number;
}
