import { HasImageDto } from './subclasses/has-image.dto';

export interface MergedNoteDto extends HasImageDto {
  compositeId: string;
  name: string;
  sideNote: string;
  entries: MergedNoteEntryDto[];
}

export interface MergedNoteEntryDto {
  rowKey: string;
  noteRowKey: string;
  time: number;
  step: string;
  details: string;
  index: number;
  temperatureMin: number;
  temperatureMax?: number;
  substance: string;
  startTime: number;
}
