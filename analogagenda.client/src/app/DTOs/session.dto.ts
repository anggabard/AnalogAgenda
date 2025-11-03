import { HasImageDto } from './subclasses/has-image.dto';

export interface SessionDto extends HasImageDto {
  id: string;
  sessionDate: string; // ISO date string
  location: string;
  participants: string; // JSON array as string
  description: string;
  usedSubstances: string; // Comma-separated DevKit Ids
  developedFilms: string; // Comma-separated Film Ids
  
  // Helper properties for frontend
  participantsList: string[];
  usedSubstancesList: string[];
  developedFilmsList: string[];
  
  // Dictionary mapping DevKit Id to list of Film Ids developed with that DevKit
  filmToDevKitMapping?: { [devKitId: string]: string[] };
}
