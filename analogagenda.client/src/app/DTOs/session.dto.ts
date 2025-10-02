import { HasImageDto } from './subclasses/has-image.dto';

export interface SessionDto extends HasImageDto {
  rowKey: string;
  sessionDate: string; // ISO date string
  location: string;
  participants: string; // JSON array as string
  description: string;
  usedSubstances: string; // JSON array of DevKit RowKeys
  developedFilms: string; // JSON array of Film RowKeys
  
  // Helper properties for frontend
  participantsList: string[];
  usedSubstancesList: string[];
  developedFilmsList: string[];
}
