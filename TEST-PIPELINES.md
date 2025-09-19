# Test Pipelines for AnalogAgenda

This document explains the different ways to run tests for the AnalogAgenda application, which includes both backend (.NET) and frontend (Angular) test suites.

## 🎯 Overview

The application has two test suites:
- **Backend Tests**: 128 .NET unit tests for API controllers, services, validators, and helpers
- **Frontend Tests**: 67 Angular/Jasmine tests for components, services, and interceptors

## 🚀 CI/CD Pipelines

### GitHub Actions Pipeline
**File**: `.github/workflows/test-pipeline.yml`

**Manual Trigger Only**: Run manually from GitHub Actions tab when needed.

**Features**:
- Runs on Windows runners for consistency with development environment
- Parallel execution of backend and frontend tests
- Test result artifacts and code coverage
- Beautiful test summary in GitHub UI
- Fails pipeline if any tests fail

## 💻 Local Test Execution

### Manual Commands

#### Backend Tests Only
```bash
# From project root
dotnet restore
dotnet build --no-restore
dotnet test --no-build --verbosity normal
```

#### Frontend Tests Only
```bash
# From project root
cd analogagenda.client
npm ci
npm test -- --watch=false --browsers=ChromeHeadless
```

## 📊 Test Results

### Backend Test Coverage
- **Controllers**: API endpoints and error handling
- **Services**: Business logic and data access
- **Validators**: Input validation rules
- **Helpers**: Utility functions and extensions

### Frontend Test Coverage
- **Components**: UI logic and user interactions
- **Services**: HTTP clients and data management
- **Interceptors**: Request/response processing
- **Guards**: Route protection logic

## 🔧 Configuration

### Backend Test Configuration
- Test framework: **xUnit**
- Mocking: **Moq**
- Coverage: **Coverlet**
- Projects: `Tests/AnalogAgenda.Server.Tests/`, `Tests/Database.Tests/`

### Frontend Test Configuration
- Test framework: **Jasmine**
- Test runner: **Karma**
- Browser: **Chrome Headless**
- Configuration: `analogagenda.client/karma.conf.js`

## ⚡ Performance

### Typical Execution Times
- **Backend Tests**: ~3-5 seconds (128 tests)
- **Frontend Tests**: ~0.4 seconds (67 tests)
- **Total Pipeline**: ~2-3 minutes (including setup)

### Parallel Execution
Both CI/CD pipelines run backend and frontend tests in parallel for optimal speed.

## 🐛 Troubleshooting

### Common Issues

#### Backend Tests Fail
```bash
# Clear packages and rebuild
dotnet clean
dotnet restore
dotnet build
```

#### Frontend Tests Timeout
```bash
# Clear node_modules and reinstall
cd analogagenda.client
rm -rf node_modules package-lock.json
npm install
npm test -- --watch=false --browsers=ChromeHeadless
```

#### Chrome Not Found (Windows)
Chrome is typically pre-installed on GitHub Actions Windows runners. If issues occur, the pipeline will automatically handle browser installation.

### Environment Variables
No special environment variables required for test execution.

## 📈 Monitoring

### GitHub Actions
- Go to **Actions** tab in your repository
- Click **"Test Pipeline"** workflow
- Click **"Run workflow"** button to trigger manually
- View test results and download artifacts from completed runs

## ✅ Success Criteria

A successful test run requires:
- ✅ All 128 backend tests pass
- ✅ All 67 frontend tests pass  
- ✅ No build errors in either project
- ✅ No linting errors (warnings are acceptable)

## 🚀 How to Run

1. **GitHub Actions**: Go to Actions tab → Test Pipeline → Click "Run workflow"
2. **Local Testing**: Use the manual commands shown above
3. **After tests pass**: Deploy to staging/production as needed

---

**Questions?** Check the individual pipeline files for more detailed configuration options.
