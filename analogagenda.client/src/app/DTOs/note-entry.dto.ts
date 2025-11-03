import { NoteEntryRuleDto } from './note-entry-rule.dto';
import { NoteEntryOverrideDto } from './note-entry-override.dto';

export interface NoteEntryDto {
  id: string;
  noteId: string;
  time: number;
  step: string;
  details: string;
  index: number;
  temperatureMin: number;
  temperatureMax?: number;
  rules: NoteEntryRuleDto[];
  overrides: NoteEntryOverrideDto[];
}