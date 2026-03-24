export interface DevKitSessionAssignmentRowDto {
  id: string;
  sessionDate: string;
  location: string;
  participantsPreview: string;
  isSelected: boolean;
}

export interface DevKitFilmAssignmentRowDto {
  id: string;
  name: string;
  brand: string;
  iso: string;
  type: string;
  formattedExposureDate: string;
  isSelected: boolean;
}

export interface IdListDto {
  ids: string[];
}
