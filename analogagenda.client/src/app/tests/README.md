# Frontend Tests

This folder contains all the unit tests for the Angular frontend application, organized in a structured way.

## Structure

```
tests/
├── components/          # Component tests
│   ├── home.component.spec.ts
│   └── login.component.spec.ts
├── test.config.ts      # Test utilities and configuration
└── README.md          # This file
```

## Running Tests

To run all tests:
```bash
npm test
```

To run tests in watch mode:
```bash
npm test -- --watch
```

To run tests with code coverage:
```bash
npm test -- --code-coverage
```

## Test Guidelines

### Component Tests
- Test component creation and basic functionality
- Mock all external dependencies (services, router, etc.)
- Test user interactions and form validations
- Test component outputs and expected behaviors

### Service Tests
- Mock HTTP calls using HttpClientTestingModule
- Test all public methods
- Test error handling scenarios

### Best Practices
1. Use descriptive test names that explain what is being tested
2. Follow the Arrange-Act-Assert pattern
3. Mock all external dependencies
4. Test both success and error scenarios
5. Keep tests isolated and independent

## Test Utilities

The `test.config.ts` file provides common utilities:
- `TestConfig.createRouterSpy()` - Creates a router spy for testing
- `TestConfig.configureTestBed()` - Common TestBed configuration

## Common Issues

### Router Navigation Tests
When testing components that use router navigation, always mock the Router:
```typescript
const routerSpy = jasmine.createSpyObj('Router', ['navigate']);
```


### Form Tests
For reactive forms, test:
- Initial form state
- Validation rules
- Form submission with valid/invalid data
- Error display
