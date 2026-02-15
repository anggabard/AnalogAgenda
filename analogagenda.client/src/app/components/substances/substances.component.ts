import { Component, inject, OnInit, ViewChild, TemplateRef } from "@angular/core";
import { Router } from "@angular/router";
import { DevKitDto, PagedResponseDto } from "../../DTOs";
import { DevKitService, UserSettingsService } from "../../services";

@Component({
    selector: 'app-substances',
    templateUrl: './substances.component.html',
    styleUrl: './substances.component.css',
    standalone: false
})

export class SubstancesComponent implements OnInit {
  private dk = inject(DevKitService);
  private router = inject(Router);
  private userSettingsService = inject(UserSettingsService);

  @ViewChild('devKitCardTemplate') devKitCardTemplate!: TemplateRef<any>;
  @ViewChild('availableDevKitRowTemplate') availableDevKitRowTemplate!: TemplateRef<any>;
  @ViewChild('expiredDevKitRowTemplate') expiredDevKitRowTemplate!: TemplateRef<any>;

  availableDevKitTableHeaders = ['Name', 'Type', 'Remaining', 'Mixed', 'Preview'];
  expiredDevKitTableHeaders = ['Name', 'Type', 'Remaining', 'Preview'];

  availableDevKits: DevKitDto[] = [];
  expiredDevKits: DevKitDto[] = [];

  // Pagination state
  availablePage = 1;
  expiredPage = 1;
  pageSize = 5;
  hasMoreAvailable = false;
  hasMoreExpired = false;
  loadingAvailable = false;
  loadingExpired = false;

  ngOnInit(): void {
    this.userSettingsService.getUserSettings().subscribe({
      next: (settings) => {
        this.pageSize = settings.entitiesPerPage ?? 5;
        this.loadAvailableDevKits();
        this.loadExpiredDevKits();
      },
      error: () => {
        this.loadAvailableDevKits();
        this.loadExpiredDevKits();
      }
    });
  }

  onNewKitClick() {
    this.router.navigate(['/substances/new']);
  }

  onKitSelected(id: string): void {
    this.router.navigate(['/substances/' + id]);
  }

  loadAvailableDevKits(): void {
    if (this.loadingAvailable) return;
    
    this.loadingAvailable = true;
    this.dk.getAvailableDevKitsPaged(this.availablePage, this.pageSize).subscribe({
      next: (response: PagedResponseDto<DevKitDto>) => {
        const newDevKits = response.data;
        this.availableDevKits.push(...newDevKits);
        
        // Update pagination state
        this.hasMoreAvailable = response.hasNextPage;
        this.availablePage++;
        this.loadingAvailable = false;
        
        // Backend now handles sorting, no need to sort here
      },
      error: (err) => {
        console.error(err);
        this.loadingAvailable = false;
      }
    });
  }

  loadExpiredDevKits(): void {
    if (this.loadingExpired) return;
    
    this.loadingExpired = true;
    this.dk.getExpiredDevKitsPaged(this.expiredPage, this.pageSize).subscribe({
      next: (response: PagedResponseDto<DevKitDto>) => {
        const newDevKits = response.data;
        this.expiredDevKits.push(...newDevKits);
        
        // Update pagination state
        this.hasMoreExpired = response.hasNextPage;
        this.expiredPage++;
        this.loadingExpired = false;
        
        // Backend now handles sorting, no need to sort here
      },
      error: (err) => {
        console.error(err);
        this.loadingExpired = false;
      }
    });
  }

  loadMoreAvailableDevKits(): void {
    this.loadAvailableDevKits();
  }

  loadMoreExpiredDevKits(): void {
    this.loadExpiredDevKits();
  }

}
