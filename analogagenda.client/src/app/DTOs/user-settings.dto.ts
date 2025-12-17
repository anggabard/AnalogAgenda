export interface UserSettingsDto {
  userId: string;
  isSubscribed: boolean;
  currentFilmId?: string | null;
  tableView: boolean;
  entitiesPerPage: number;
}

