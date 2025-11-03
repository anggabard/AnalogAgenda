import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { SessionsComponent } from '../../components/sessions/sessions.component';
import { SessionService, AccountService } from '../../services';
import { SessionDto, IdentityDto, PagedResponseDto } from '../../DTOs';
import { TestConfig } from '../test.config';

describe('SessionsComponent', () => {
  let component: SessionsComponent;
  let fixture: ComponentFixture<SessionsComponent>;
  let mockSessionService: jasmine.SpyObj<SessionService>;
  let mockAccountService: jasmine.SpyObj<AccountService>;
  let mockRouter: jasmine.SpyObj<Router>;

  const mockIdentity: IdentityDto = {
    username: 'Angel',
    email: 'angel@test.com'
  };

  const mockSession: SessionDto = {
    id: 'session-1',
    sessionDate: '2023-10-01',
    location: 'Test Location',
    participants: '["Angel", "Tudor"]',
    imageUrl: 'test-url',
    imageBase64: '',
    description: 'Test session',
    usedSubstances: '[]',
    developedFilms: '[]',
    participantsList: ['Angel', 'Tudor'],
    usedSubstancesList: [],
    developedFilmsList: []
  };

  const mockPagedResponse: PagedResponseDto<SessionDto> = {
    data: [mockSession],
    currentPage: 1,
    pageSize: 5,
    totalCount: 1,
    totalPages: 1,
    hasNextPage: false,
    hasPreviousPage: false
  };

  beforeEach(async () => {
    const sessionServiceSpy = jasmine.createSpyObj('SessionService', ['getPaged']);
    const accountServiceSpy = jasmine.createSpyObj('AccountService', ['whoAmI']);
    const routerSpy = TestConfig.createRouterSpy();

    accountServiceSpy.whoAmI.and.returnValue(of(mockIdentity));
    sessionServiceSpy.getPaged.and.returnValue(of(mockPagedResponse));

    await TestBed.configureTestingModule({
      declarations: [SessionsComponent],
      schemas: [NO_ERRORS_SCHEMA],
      providers: [
        { provide: SessionService, useValue: sessionServiceSpy },
        { provide: AccountService, useValue: accountServiceSpy },
        { provide: Router, useValue: routerSpy }
      ]
    }).compileComponents();

    mockSessionService = sessionServiceSpy;
    mockAccountService = accountServiceSpy;
    mockRouter = routerSpy;

    fixture = TestBed.createComponent(SessionsComponent);
    component = fixture.componentInstance;
  });

  afterEach(() => {
    if (fixture) {
      fixture.destroy();
    }
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load current user on init', () => {
    fixture.detectChanges();

    expect(mockAccountService.whoAmI).toHaveBeenCalled();
    expect(component.currentUsername).toBe('Angel');
  });

  it('should load sessions on init', () => {
    fixture.detectChanges();

    expect(mockSessionService.getPaged).toHaveBeenCalledWith(1, 5);
    expect(component.sessions.length).toBe(1);
    expect(component.sessions[0].id).toBe('session-1');
  });

  it('should handle whoAmI error', () => {
    mockAccountService.whoAmI.and.returnValue(throwError(() => 'Auth error'));
    spyOn(console, 'error');

    fixture.detectChanges();

    expect(console.error).toHaveBeenCalled();
  });

  it('should handle load sessions error', () => {
    mockSessionService.getPaged.and.returnValue(throwError(() => 'Load error'));
    spyOn(console, 'error');

    fixture.detectChanges();

    expect(console.error).toHaveBeenCalled();
    expect(component.loading).toBeFalsy();
  });

  it('should set hasMore flag correctly', () => {
    const pagedResponseWithMore: PagedResponseDto<SessionDto> = {
      ...mockPagedResponse,
      hasNextPage: true
    };
    mockSessionService.getPaged.and.returnValue(of(pagedResponseWithMore));

    fixture.detectChanges();

    expect(component.hasMore).toBeTruthy();
  });

  it('should load more sessions when loadMoreSessions is called', () => {
    fixture.detectChanges();
    
    const initialCount = component.sessions.length;
    const initialPage = component.currentPage;

    const secondPageResponse: PagedResponseDto<SessionDto> = {
      data: [{ ...mockSession, id: 'session-2' }],
      currentPage: 2,
      pageSize: 5,
      totalCount: 2,
      totalPages: 1,
      hasNextPage: false,
      hasPreviousPage: true
    };
    mockSessionService.getPaged.and.returnValue(of(secondPageResponse));

    component.loadMoreSessions();

    expect(mockSessionService.getPaged).toHaveBeenCalledWith(initialPage, 5);
    expect(component.sessions.length).toBe(initialCount + 1);
  });

  it('should not load sessions if already loading', () => {
    fixture.detectChanges();
    
    component.loading = true;
    const callCount = mockSessionService.getPaged.calls.count();

    component.loadSessions();

    expect(mockSessionService.getPaged.calls.count()).toBe(callCount);
  });

  it('should navigate to new session on onNewSessionClick', () => {
    component.onNewSessionClick();

    expect(mockRouter.navigate).toHaveBeenCalledWith(['/sessions/new']);
  });

  it('should navigate to session detail on onSessionSelected', () => {
    const sessionid = 'session-123';

    component.onSessionSelected(sessionRowKey);

    expect(mockRouter.navigate).toHaveBeenCalledWith(['/sessions/' + sessionRowKey]);
  });

  it('should parse participants JSON correctly', () => {
    const participantsJson = '["Angel", "Tudor"]';

    const result = component.parseParticipants(participantsJson);

    expect(result).toEqual(['Angel', 'Tudor']);
  });

  it('should handle invalid participants JSON', () => {
    const invalidJson = 'not valid json';

    const result = component.parseParticipants(invalidJson);

    expect(result).toEqual([]);
  });

  it('should handle empty participants JSON', () => {
    const result = component.parseParticipants('');

    expect(result).toEqual([]);
  });

  it('should format date correctly', () => {
    const dateString = '2023-10-01';

    const result = component.formatDate(dateString);

    expect(result).toBeTruthy();
    expect(typeof result).toBe('string');
  });

  it('should increment page number after loading sessions', () => {
    const initialPage = component.currentPage;

    fixture.detectChanges();

    expect(component.currentPage).toBe(initialPage + 1);
  });

  it('should append new sessions to existing list', () => {
    fixture.detectChanges();
    
    const firstSession = component.sessions[0];
    
    const secondPageResponse: PagedResponseDto<SessionDto> = {
      data: [{ ...mockSession, id: 'session-2' }],
      currentPage: 2,
      pageSize: 5,
      totalCount: 2,
      totalPages: 1,
      hasNextPage: false,
      hasPreviousPage: true
    };
    mockSessionService.getPaged.and.returnValue(of(secondPageResponse));

    component.loadMoreSessions();

    expect(component.sessions.length).toBe(2);
    expect(component.sessions[0]).toBe(firstSession);
    expect(component.sessions[1].id).toBe('session-2');
  });
});
