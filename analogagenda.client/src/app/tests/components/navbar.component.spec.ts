import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { NavbarComponent } from '../../components/navbar/navbar.component';
import { AccountService } from '../../services';
import { IdentityDto } from '../../DTOs';

describe('NavbarComponent', () => {
  let component: NavbarComponent;
  let fixture: ComponentFixture<NavbarComponent>;
  let mockAccountService: jasmine.SpyObj<AccountService>;
  let mockRouter: jasmine.SpyObj<Router>;

  beforeEach(async () => {
    const accountServiceSpy = jasmine.createSpyObj('AccountService', ['whoAmI', 'logout']);
    const routerSpy = jasmine.createSpyObj('Router', ['navigate']);

    // Set up default return values to avoid subscription errors
    accountServiceSpy.whoAmI.and.returnValue(of({ username: 'testuser', email: 'test@example.com' }));
    accountServiceSpy.logout.and.returnValue(of({}));

    await TestBed.configureTestingModule({
      declarations: [NavbarComponent],
      providers: [
        { provide: AccountService, useValue: accountServiceSpy },
        { provide: Router, useValue: routerSpy }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(NavbarComponent);
    component = fixture.componentInstance;
    mockAccountService = TestBed.inject(AccountService) as jasmine.SpyObj<AccountService>;
    mockRouter = TestBed.inject(Router) as jasmine.SpyObj<Router>;
  });

  afterEach(() => {
    fixture.destroy();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });


  it('should load username on initialization', () => {
    // Arrange
    const mockIdentity: IdentityDto = { username: 'testuser', email: 'test@example.com' };
    mockAccountService.whoAmI.and.returnValue(of(mockIdentity));

    // Act
    fixture.detectChanges(); // Triggers ngOnInit

    // Assert
    expect(mockAccountService.whoAmI).toHaveBeenCalled();
    expect(component.username).toBe('testuser');
  });

  it('should handle whoAmI error gracefully', () => {
    // Arrange
    mockAccountService.whoAmI.and.returnValue(throwError(() => 'error'));
    
    // Act - Reinitialize component with error condition
    fixture = TestBed.createComponent(NavbarComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    // Assert - Should handle error gracefully
    expect(component.username).toBe(''); // Should remain empty on error
  });

  it('should toggle sidebar when toggleSidebar is called', () => {
    // Arrange
    spyOn(component.isOpenEvent, 'emit');
    expect(component.isSidebarOpen).toBeFalse();

    // Act
    component.toggleSidebar();

    // Assert
    expect(component.isSidebarOpen).toBeTrue();
    expect(component.isOpenEvent.emit).toHaveBeenCalledWith(true);

    // Act again
    component.toggleSidebar();

    // Assert
    expect(component.isSidebarOpen).toBeFalse();
    expect(component.isOpenEvent.emit).toHaveBeenCalledWith(false);
  });

  it('should navigate to notes and close sidebar when onNotesClick is called', () => {
    // Arrange
    spyOn(component.isOpenEvent, 'emit');
    component.isSidebarOpen = true;

    // Act
    component.onNotesClick();

    // Assert
    expect(mockRouter.navigate).toHaveBeenCalledWith(['/notes']);
    expect(component.isSidebarOpen).toBeFalse();
    expect(component.isOpenEvent.emit).toHaveBeenCalledWith(false);
  });

  it('should navigate to home and close sidebar when onHomeClick is called', () => {
    // Arrange
    spyOn(component.isOpenEvent, 'emit');
    component.isSidebarOpen = true;

    // Act
    component.onHomeClick();

    // Assert
    expect(mockRouter.navigate).toHaveBeenCalledWith(['/home']);
    expect(component.isSidebarOpen).toBeFalse();
    expect(component.isOpenEvent.emit).toHaveBeenCalledWith(false);
  });

  it('should navigate to substances and close sidebar when onSubstancesClick is called', () => {
    // Arrange
    spyOn(component.isOpenEvent, 'emit');
    component.isSidebarOpen = true;

    // Act
    component.onSubstancesClick();

    // Assert
    expect(mockRouter.navigate).toHaveBeenCalledWith(['/substances']);
    expect(component.isSidebarOpen).toBeFalse();
    expect(component.isOpenEvent.emit).toHaveBeenCalledWith(false);
  });

  it('should navigate to change password and close sidebar when onChangePasswordClick is called', () => {
    // Arrange
    spyOn(component.isOpenEvent, 'emit');
    component.isSidebarOpen = true;

    // Act
    component.onChangePasswordClick();

    // Assert
    expect(mockRouter.navigate).toHaveBeenCalledWith(['/change-password']);
    expect(component.isSidebarOpen).toBeFalse();
    expect(component.isOpenEvent.emit).toHaveBeenCalledWith(false);
  });

  it('should logout and navigate to login on successful logout', () => {
    // Arrange
    mockAccountService.logout.and.returnValue(of({}));

    // Act
    component.onLogoutClick();

    // Assert
    expect(mockAccountService.logout).toHaveBeenCalled();
    expect(mockRouter.navigate).toHaveBeenCalledWith(['/login']);
  });

  it('should navigate to login on logout error', () => {
    // Arrange
    mockAccountService.logout.and.returnValue(throwError(() => 'logout error'));

    // Act
    component.onLogoutClick();

    // Assert
    expect(mockAccountService.logout).toHaveBeenCalled();
    expect(mockRouter.navigate).toHaveBeenCalledWith(['/login']);
  });

});
