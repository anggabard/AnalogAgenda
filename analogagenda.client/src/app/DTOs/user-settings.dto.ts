export interface UserSettingsDto {
  userId: string;
  isSubscribed: boolean;
  currentFilmId?: string | null;
  tableView: boolean;
  entitiesPerPage: number;
  /** Preferred home dashboard section order; omit on PATCH when not changing layout. */
  homeSectionOrder?: string[] | null;
}

