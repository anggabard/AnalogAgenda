export interface NoteEntryOverrideDto {
  id: string;
  noteEntryId: string;
  filmCountMin: number;
  filmCountMax: number;
  time?: number;
  step?: string;
  details?: string;
  temperatureMin?: number;
  temperatureMax?: number;
}
