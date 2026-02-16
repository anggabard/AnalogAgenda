import { NO_ERRORS_SCHEMA } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { LoginComponent } from '../../components/login/login.component';
import { AccountService } from '../../services/implementations/account.service';

describe('LoginComponent', () => {
  let component: LoginComponent;
  let fixture: ComponentFixture<LoginComponent>;
  let mockAccountService: jasmine.SpyObj<AccountService>;
  let mockRouter: jasmine.SpyObj<Router>;

  beforeEach(async () => {
    const accountServiceSpy = jasmine.createSpyObj('AccountService', ['login']);
    const routerSpy = jasmine.createSpyObj('Router', ['navigate']);

    await TestBed.configureTestingModule({
      declarations: [LoginComponent],
      imports: [ReactiveFormsModule],
      schemas: [NO_ERRORS_SCHEMA],
      providers: [
        FormBuilder,
        { provide: AccountService, useValue: accountServiceSpy },
        { provide: Router, useValue: routerSpy }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    mockAccountService = TestBed.inject(AccountService) as jasmine.SpyObj<AccountService>;
    mockRouter = TestBed.inject(Router) as jasmine.SpyObj<Router>;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should initialize form with empty values', () => {
    expect(component.form.get('username')?.value).toBe('');
    expect(component.form.get('password')?.value).toBe('');
  });

  it('should mark form as invalid when username is empty', () => {
    component.form.patchValue({ username: '', password: 'password123' });
    expect(component.form.invalid).toBeTruthy();
  });

  it('should mark form as invalid when password is empty', () => {
    component.form.patchValue({ username: 'testuser', password: '' });
    expect(component.form.invalid).toBeTruthy();
  });

  it('should mark form as valid when both fields are filled', () => {
    component.form.patchValue({ username: 'testuser', password: 'password123' });
    expect(component.form.valid).toBeTruthy();
  });

  it('should call AccountService.login when form is valid and submitted', () => {
    // Arrange
    component.form.patchValue({ username: 'testuser', password: 'password123' });
    mockAccountService.login.and.returnValue(of({}));

    // Act
    component.submit();

    // Assert
    expect(mockAccountService.login).toHaveBeenCalledWith('testuser', 'password123');
  });

  it('should navigate to home on successful login', () => {
    // Arrange
    component.form.patchValue({ username: 'testuser', password: 'password123' });
    mockAccountService.login.and.returnValue(of({}));

    // Act
    component.submit();

    // Assert
    expect(mockRouter.navigate).toHaveBeenCalledWith(['/home']);
  });


  it('should set errorMessage on login failure', () => {
    // Arrange
    component.form.patchValue({ username: 'testuser', password: 'wrongpassword' });
    mockAccountService.login.and.returnValue(throwError(() => new Error('Login failed')));

    // Act
    component.submit();

    // Assert
    expect(component.errorMessage).toBe('Login failed. Please check your credentials.');
    expect(component.loading).toBeFalsy();
  });

  it('should clear errorMessage on new login attempt', () => {
    // Arrange
    component.errorMessage = 'Previous error';
    component.form.patchValue({ username: 'testuser', password: 'password123' });
    mockAccountService.login.and.returnValue(of({}));

    // Act
    component.submit();

    // Assert
    expect(component.errorMessage).toBeNull();
  });

  it('should not submit when form is invalid', () => {
    // Arrange
    component.form.patchValue({ username: '', password: '' });

    // Act
    component.submit();

    // Assert
    expect(mockAccountService.login).not.toHaveBeenCalled();
    expect(mockRouter.navigate).not.toHaveBeenCalled();
  });
});
