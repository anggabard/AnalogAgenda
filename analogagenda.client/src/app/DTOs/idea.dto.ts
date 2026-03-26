export interface IdeaSessionSummaryDto {
  id: string;
  displayLabel: string;
}

export interface IdeaDto {
  id: string;
  title: string;
  description: string;
  outcome?: string;
  connectedSessionIds?: string[];
  connectedSessions?: IdeaSessionSummaryDto[];
}
