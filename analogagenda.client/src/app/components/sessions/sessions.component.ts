import { Component, inject, OnInit, ViewChild, TemplateRef } from "@angular/core";
import { Router } from "@angular/router";
import { SessionService, AccountService, UserSettingsService } from "../../services";
import { SessionDto, IdentityDto, PagedResponseDto } from "../../DTOs";

@Component({
    selector: 'app-sessions',
    templateUrl: './sessions.component.html',
    styleUrl: './sessions.component.css',
    standalone: false
})
export class SessionsComponent implements OnInit {
  private router = inject(Router);
  private sessionService = inject(SessionService);
  private accountService = inject(AccountService);
  private userSettingsService = inject(UserSettingsService);

  @ViewChild('sessionCardTemplate') sessionCardTemplate!: TemplateRef<any>;
  @ViewChild('sessionRowTemplate') sessionRowTemplate!: TemplateRef<any>;

  sessionTableHeaders = ['Location', 'Date', 'Participants', 'Description', 'Preview'];

  sessions: SessionDto[] = [];
  currentUsername: string = '';

  // Pagination state
  currentPage = 1;
  pageSize = 5;
  hasMore = false;
  loading = false;

  ngOnInit(): void {
    this.userSettingsService.getUserSettings().subscribe({
      next: (settings) => {
        this.pageSize = settings.entitiesPerPage ?? 5;
        this.accountService.whoAmI().subscribe({
          next: (identity: IdentityDto) => {
            this.currentUsername = identity.username;
            this.loadSessions();
          },
          error: (err) => {
            console.error(err);
          }
        });
      },
      error: () => {
        this.accountService.whoAmI().subscribe({
          next: (identity: IdentityDto) => {
            this.currentUsername = identity.username;
            this.loadSessions();
          },
          error: (err) => {
            console.error(err);
          }
        });
      }
    });
  }

  loadSessions(): void {
    if (this.loading) return;
    
    this.loading = true;
    this.sessionService.getPaged(this.currentPage, this.pageSize).subscribe({
      next: (response: PagedResponseDto<SessionDto>) => {
        this.sessions.push(...response.data);
        this.hasMore = response.hasNextPage;
        this.currentPage++;
        this.loading = false;
      },
      error: (err) => {
        console.error(err);
        this.loading = false;
      }
    });
  }

  loadMoreSessions(): void {
    this.loadSessions();
  }

  onNewSessionClick() {
    this.router.navigate(['/sessions/new']);
  }

  onSessionSelected(id: string) {
    this.router.navigate(['/sessions/' + id]);
  }

  parseParticipants(participantsJson: string): string[] {
    try {
      return JSON.parse(participantsJson || '[]');
    } catch {
      return [];
    }
  }

  formatDate(dateString: string): string {
    return new Date(dateString).toLocaleDateString();
  }
}
