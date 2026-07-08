# Engineering Implementation Plan: KAN-314 Authentication POC

**Date:** 2026-07-07  
**Ticket:** KAN-314 (POC)  
**Scope:** JWT-based username/password authentication  
**Tech Stack:** .NET 8 Web API + React 18 TypeScript  

---

## 1. Dependency Graph (Mermaid)

```mermaid
gantt
    title KAN-314 Implementation Timeline
    dateFormat YYYY-MM-DD
    
    section Backend Infrastructure
    User Entity & Migrations           :active, task1, 2026-07-07, 2d
    DTOs (Auth Layer)                  :task2, after task1, 1d
    
    section Backend Services
    IUserRepository & Implementation   :task3, after task2, 1d
    IAuthService & Implementation      :task4, after task3, 2d
    JWT Configuration (appsettings)    :task5, after task2, 1d
    
    section Backend API Layer
    JwtMiddleware                      :task6, after task4, 1d
    AuthController                     :task7, after task4, 1d
    ServiceCollectionExtensions        :task8, after task3, after task4, 1d
    
    section Backend Testing
    AuthService Unit Tests             :task9, after task4, 2d
    AuthController Integration Tests   :task10, after task7, 2d
    UserRepository Tests               :task11, after task3, 1d
    
    section Frontend Auth Infrastructure
    AuthContext (State + API)          :task12, 2026-07-07, 2d
    useAuth Hook                       :task13, after task12, 1d
    
    section Frontend Components
    LoginPage Component                :task14, after task13, 1d
    SignupPage Component               :task15, after task13, 1d
    ProtectedRoute Wrapper             :task16, after task13, 1d
    Header Component (Auth Buttons)    :task17, after task13, 1d
    
    section Frontend Integration
    Token Persistence (localStorage)   :task18, after task12, 1d
    Token Validation on App Init       :task19, after task18, 1d
    Route Registration (/signin, /signup) :task20, after task14, after task15, 1d
    
    section Frontend Styling
    Tailwind CSS Forms & Pages         :task21, 2026-07-07, 3d
    
    section Cross-Functional
    CORS Configuration                 :task22, 2026-07-07, 1d
    GlobalExceptionMiddleware          :task23, 2026-07-08, 1d
    Local Startup Documentation        :task24, 2026-07-09, 1d
```

---

## 2. Task Breakdown & Prioritization

### PHASE 1: BACKEND INFRASTRUCTURE (Critical Path)

#### **Task 1.1: User Entity & Database Migrations**
- **Description:** Create User entity class and EF Core migration
- **Files to Create:**
  - `src/Api/Domain/Entities/User.cs`
  - `src/Api/Migrations/[timestamp]_CreateUsersTable.cs`
- **Success Criteria:**
  - User entity has: Id (PK), Username (unique, case-insensitive index), PasswordHash, CreatedDate, UpdatedDate
  - Migration creates Users table with UNIQUE constraint on Username
  - Database can be seeded with initial test user (optional for POC)
- **Regression Verification:** ✅ Existing entities (if any) remain untouched
- **Estimated Effort:** 2 days

#### **Task 1.2: Auth DTOs (Signup, Signin, Response)**
- **Description:** Create all input/output DTOs following envelope pattern
- **Files to Create:**
  - `src/Api/DTOs/Auth/SignupRequestDto.cs`
  - `src/Api/DTOs/Auth/SigninRequestDto.cs`
  - `src/Api/DTOs/Auth/AuthResponseDto.cs`
  - `src/Api/DTOs/Auth/UserDto.cs`
- **Success Criteria:**
  - All DTOs use [JsonPropertyName] for camelCase JSON
  - All properties have XML documentation
  - SignupRequestDto validates: username (3+ chars, alphanumeric + underscore), password (8+ chars), passwordConfirmation
  - SigninRequestDto validates: username, password (both required)
- **Compliance:** ✅ Matches copilot-instructions.md §2.2 (DTO standards)
- **Estimated Effort:** 1 day

---

### PHASE 2: BACKEND SERVICES (Critical Path)

#### **Task 2.1: IUserRepository & UserRepository**
- **Description:** Implement data access layer for User entity
- **Files to Create:**
  - `src/Api/Repositories/IUserRepository.cs`
  - `src/Api/Repositories/UserRepository.cs`
- **Methods Required:**
  - `GetByUsernameAsync(string username, CancellationToken ct)` — Case-insensitive lookup
  - `GetByIdAsync(int id, CancellationToken ct)`
  - `CreateAsync(User user, CancellationToken ct)`
  - `UsernameExistsAsync(string username, CancellationToken ct)` — For pre-insert check (RSK-004 mitigation)
- **Success Criteria:**
  - All methods use `AsNoTracking()` for read operations
  - Case-insensitive comparison: `.Where(u => u.Username.ToLower() == username.ToLower())`
  - CancellationToken passed to all async calls
  - Handles duplicate key constraint gracefully
- **Compliance:** ✅ Matches copilot-instructions.md §2.3 (Repository Pattern)
- **Estimated Effort:** 1 day

#### **Task 2.2: IAuthService & AuthService**
- **Description:** Implement authentication business logic
- **Files to Create:**
  - `src/Api/Services/IAuthService.cs`
  - `src/Api/Services/AuthService.cs`
- **Methods Required:**
  ```csharp
  Task<(bool Success, AuthResponseDto? Response, string? Error)> RegisterUserAsync(
      string username, string password, string passwordConfirmation, CancellationToken ct);
  
  Task<(bool Success, AuthResponseDto? Response, string? Error)> AuthenticateUserAsync(
      string username, string password, CancellationToken ct);
  
  string GenerateJwt(User user);
  
  Task<User?> ValidateTokenAsync(string token, CancellationToken ct);
  
  Task LogoutAsync(int userId, CancellationToken ct);
  ```
- **Business Logic Details:**
  - **RegisterUserAsync:**
    - Validate input (username length, password length, confirmation match)
    - Check `UsernameExistsAsync()` first (fast fail)
    - Hash password with BCrypt (cost=12)
    - Create User entity
    - Save to repository
    - Return JWT token + UserDto (negative path: return error tuple)
  - **AuthenticateUserAsync:**
    - Find user by username (case-insensitive)
    - If not found: return generic "invalid credentials" error (don't leak username existence)
    - Verify password hash with BCrypt
    - If mismatch: return generic "invalid credentials" error
    - Generate JWT and return AuthResponseDto (negative path: return error)
  - **GenerateJwt:**
    - Create JWT with claims: sub (userId), username, iat, exp (now + 24h), iss
    - Sign with HS256 using secret from config
    - Return token string
  - **ValidateTokenAsync:**
    - Parse and validate JWT signature
    - Check expiry
    - Extract userId from claims
    - Load user from repository (return null if not found)
    - Return User or null
  - **LogoutAsync:**
    - Optional for POC (client-side token deletion sufficient)
    - Can be no-op or add to blacklist (Phase 2)
- **Error Messages (Generic for Security):**
  - "Username must be at least 3 characters"
  - "Password must be at least 8 characters"
  - "Passwords do not match"
  - "The provided credentials are invalid" (for duplicate username, wrong password, user not found)
- **Security Requirements:** ✅ Matches design-review.md security audit
- **Compliance:** ✅ Matches copilot-instructions.md §2.4 (Service Pattern)
- **Estimated Effort:** 2 days

#### **Task 2.3: JWT Configuration (appsettings.json)**
- **Description:** Add JWT settings to configuration
- **File to Update:** `src/Api/appsettings.json`, `src/Api/appsettings.Development.json`
- **Configuration:**
  ```json
  {
    "Jwt": {
      "Secret": "your-super-secret-key-minimum-32-characters-here",
      "ExpiryMinutes": 1440,
      "Issuer": "dotnet-react-starter"
    }
  }
  ```
- **Success Criteria:**
  - Secret is ≥32 characters (enforced)
  - Development config has safe default; production config documented as requiring override
  - AuthService reads these values via IConfiguration or strongly-typed options
- **Regression Verification:** ✅ Existing appsettings unchanged (additive only)
- **Estimated Effort:** 1 day

---

### PHASE 3: BACKEND API LAYER (Critical Path)

#### **Task 3.1: JwtMiddleware**
- **Description:** Extract and validate bearer token on all requests
- **Files to Create:** `src/Api/Middleware/JwtMiddleware.cs`
- **Functionality:**
  - Read Authorization header (Bearer {token})
  - Call AuthService.ValidateTokenAsync()
  - If valid: add claims to HttpContext.User (set ClaimsPrincipal)
  - If invalid/expired: allow request to pass (let [Authorize] attribute handle rejection)
  - Log invalid tokens (for audit trail)
- **Success Criteria:**
  - Middleware registers in Program.cs before route mapping
  - Doesn't block requests (all requests can proceed)
  - Controllers can check User.Identity.IsAuthenticated
- **Compliance:** ✅ Matches design-review.md ADR-005 (JwtMiddleware specification)
- **Estimated Effort:** 1 day

#### **Task 3.2: AuthController**
- **Description:** Create API endpoints for authentication
- **Files to Create:** `src/Api/Controllers/AuthController.cs`
- **Endpoints:**
  ```csharp
  [Route("v1/[controller]")]
  [ApiController]
  public class AuthController : ControllerBase
  {
      /// POST /v1/auth/signup
      [HttpPost("signup")]
      [ProducesResponseType(typeof(ItemResponseDto<AuthResponseDto>), StatusCodes.Status201Created)]
      [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
      public async Task<ActionResult<ItemResponseDto<AuthResponseDto>>> Signup(
          [FromBody] SignupRequestDto request,
          CancellationToken cancellationToken);
      
      /// POST /v1/auth/signin
      [HttpPost("signin")]
      [ProducesResponseType(typeof(ItemResponseDto<AuthResponseDto>), StatusCodes.Status200OK)]
      [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
      public async Task<ActionResult<ItemResponseDto<AuthResponseDto>>> Signin(
          [FromBody] SigninRequestDto request,
          CancellationToken cancellationToken);
      
      /// GET /v1/auth/me (Protected)
      [HttpGet("me")]
      [Authorize]
      [ProducesResponseType(typeof(ItemResponseDto<UserDto>), StatusCodes.Status200OK)]
      [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
      public async Task<ActionResult<ItemResponseDto<UserDto>>> GetCurrentUser(
          CancellationToken cancellationToken);
      
      /// POST /v1/auth/logout (Protected, optional for POC)
      [HttpPost("logout")]
      [Authorize]
      [ProducesResponseType(StatusCodes.Status200OK)]
      [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
      public async Task<IActionResult> Logout(CancellationToken cancellationToken);
  }
  ```
- **Response Wrapping:**
  - Signup/Signin: Return `ItemResponseDto<AuthResponseDto>` with token + user + metadata
  - Me: Return `ItemResponseDto<UserDto>` with user + metadata
  - Errors: Return `ErrorResponseDto` with code (ORG-VAL-001, ORG-AUT-001, ORG-INT-001)
  - All responses include transactionId (from middleware or request context)
- **Input Validation:**
  - Server-side validation of request DTOs (redundant to frontend but required by standards)
  - Return 400 Bad Request with field-level error details
- **Error Paths:**
  - **Signup duplicate username:** 400 Bad Request, generic "credentials invalid" message
  - **Signup weak password:** 400 Bad Request, specific field message
  - **Signin wrong credentials:** 401 Unauthorized, generic message
  - **Me without token:** 401 Unauthorized
  - **Me with expired token:** 401 Unauthorized
- **Compliance:**
  - ✅ copilot-instructions.md §1.1 (Route /v1/auth, no /api/ prefix)
  - ✅ copilot-instructions.md §1.2 (Response envelope)
  - ✅ copilot-instructions.md §2.6 (Controller standards)
- **Estimated Effort:** 1 day

#### **Task 3.3: ServiceCollectionExtensions**
- **Description:** Register auth services in DI container
- **Files to Update:** `src/Api/Extensions/ServiceCollectionExtensions.cs`
- **Services to Register:**
  ```csharp
  public static IServiceCollection AddApplicationServices(this IServiceCollection services)
  {
      services.AddScoped<IUserRepository, UserRepository>();
      services.AddScoped<IAuthService, AuthService>();
      return services;
  }
  ```
- **Program.cs Integration:**
  ```csharp
  builder.Services.AddApplicationServices();
  app.UseMiddleware<JwtMiddleware>();
  app.UseAuthentication();
  app.UseAuthorization();
  ```
- **Compliance:** ✅ copilot-instructions.md §2.5 (DI registration)
- **Estimated Effort:** 1 day

---

### PHASE 4: BACKEND TESTING

#### **Task 4.1: AuthService Unit Tests**
- **Description:** Test business logic (registration, authentication, JWT generation)
- **Files to Create:** `tests/Api.Tests/Services/AuthServiceTests.cs`
- **Test Scenarios:**
  - [x] RegisterUserAsync_WithValidInput_ReturnsToken (happy path)
  - [x] RegisterUserAsync_WithDuplicateUsername_ReturnsError (negative path)
  - [x] RegisterUserAsync_WithWeakPassword_ReturnsError (negative path)
  - [x] RegisterUserAsync_WithNonMatchingPasswords_ReturnsError (negative path)
  - [x] AuthenticateUserAsync_WithCorrectCredentials_ReturnsToken (happy path)
  - [x] AuthenticateUserAsync_WithWrongPassword_ReturnsGenericError (negative path)
  - [x] AuthenticateUserAsync_WithNonexistentUser_ReturnsGenericError (negative path + security)
  - [x] GenerateJwt_CreatesValidToken_WithCorrectClaims (unit test)
  - [x] ValidateTokenAsync_WithValidToken_ReturnsUser (happy path)
  - [x] ValidateTokenAsync_WithExpiredToken_ReturnsNull (negative path)
  - [x] ValidateTokenAsync_WithInvalidSignature_ReturnsNull (negative path)
- **Mock Setup:** IUserRepository, IConfiguration
- **Compliance:** ✅ copilot-instructions.md §3.2-3.5 (Test standards, AAA pattern, TestDataBuilders)
- **Estimated Effort:** 2 days

#### **Task 4.2: AuthController Integration Tests**
- **Description:** Test HTTP endpoints (signup, signin, me, logout)
- **Files to Create:** `tests/Api.Tests/Controllers/AuthControllerTests.cs`
- **Test Scenarios:**
  - [x] POST /v1/auth/signup (201) — Happy path, returns token
  - [x] POST /v1/auth/signup (400) — Duplicate username
  - [x] POST /v1/auth/signup (400) — Weak password
  - [x] POST /v1/auth/signin (200) — Happy path, returns token
  - [x] POST /v1/auth/signin (401) — Wrong password
  - [x] GET /v1/auth/me (200) — With valid token, returns user
  - [x] GET /v1/auth/me (401) — Without token
  - [x] GET /v1/auth/me (401) — With expired token
  - [x] POST /v1/auth/logout (200) — With valid token
- **Setup:** CustomWebApplicationFactory with InMemory database
- **Compliance:** ✅ copilot-instructions.md §3.6-3.7 (Integration tests)
- **Estimated Effort:** 2 days

#### **Task 4.3: UserRepository Unit Tests**
- **Description:** Test data access layer
- **Files to Create:** `tests/Api.Tests/Repositories/UserRepositoryTests.cs`
- **Test Scenarios:**
  - [x] GetByUsernameAsync_WithExistingUsername_ReturnsUser (case-insensitive)
  - [x] GetByUsernameAsync_WithNonexistentUsername_ReturnsNull
  - [x] CreateAsync_WithValidUser_PersistsUser
  - [x] UsernameExistsAsync_WithExistingUsername_ReturnsTrue
  - [x] UsernameExistsAsync_WithNonexistentUsername_ReturnsFalse
- **Setup:** InMemory DbContext
- **Compliance:** ✅ copilot-instructions.md §3.6 (Repository tests with InMemory)
- **Estimated Effort:** 1 day

---

### PHASE 5: FRONTEND AUTHENTICATION INFRASTRUCTURE

#### **Task 5.1: AuthContext**
- **Description:** Centralized authentication state management
- **Files to Create:** `src/contexts/AuthContext.tsx`
- **Context Interface:**
  ```typescript
  interface AuthContextType {
    currentUser: UserDto | null;
    isAuthenticated: boolean;
    token: string | null;
    signup: (username: string, password: string, passwordConfirmation: string) => Promise<void>;
    signin: (username: string, password: string) => Promise<void>;
    logout: () => Promise<void>;
    loading: boolean;
    error: string | null;
    clearError: () => void;
  }
  ```
- **Responsibilities:**
  - Manage auth state (currentUser, token, isAuthenticated)
  - Persist token to localStorage (key: `auth_token`)
  - Call backend APIs (/v1/auth/signup, /signin, /logout, /me)
  - Handle token expiry and restoration on app init
  - Provide error state for UI feedback
- **Implementation Details:**
  - Use React Context API + useReducer for state management
  - On mount: Load token from localStorage, validate with `/v1/auth/me`
  - On invalid token: Clear localStorage and set isAuthenticated = false
  - Handle 401 responses by clearing auth state
- **Success Criteria:**
  - Token persists across page refreshes
  - Token clears on logout
  - Error messages from backend are accessible in UI
  - Loading state prevents double-submit on forms
- **Estimated Effort:** 2 days

#### **Task 5.2: useAuth Hook**
- **Description:** Custom hook to access auth context from any component
- **Files to Create:** `src/hooks/useAuth.ts`
- **Function:**
  ```typescript
  export function useAuth(): AuthContextType {
    const context = useContext(AuthContext);
    if (!context) {
      throw new Error('useAuth must be used within AuthProvider');
    }
    return context;
  }
  ```
- **Usage:** `const { currentUser, isAuthenticated, signin, logout } = useAuth();`
- **Estimated Effort:** 1 day

#### **Task 5.3: ProtectedRoute Component**
- **Description:** Route wrapper that redirects unauthenticated users to signin
- **Files to Create:** `src/components/ProtectedRoute.tsx`
- **Implementation:**
  ```typescript
  interface ProtectedRouteProps {
    children: ReactNode;
  }
  
  export function ProtectedRoute({ children }: ProtectedRouteProps) {
    const { isAuthenticated, loading } = useAuth();
    const navigate = useNavigate();
    
    if (loading) return <LoadingSpinner />;
    
    if (!isAuthenticated) {
      navigate('/signin');
      return null;
    }
    
    return <>{children}</>;
  }
  ```
- **Usage:** `<ProtectedRoute><DashboardPage /></ProtectedRoute>`
- **Estimated Effort:** 1 day

---

### PHASE 6: FRONTEND COMPONENTS

#### **Task 6.1: LoginPage Component**
- **Description:** Sign-in form page
- **Files to Create:** `src/pages/LoginPage.tsx`
- **UI Elements:**
  - Username input field
  - Password input field
  - "Sign In" button
  - Error message display (from AuthContext)
  - "Don't have an account? Sign up" link to /signup
  - Loading state on button while signin in progress
- **Form Handling:**
  - Validate: username and password non-empty
  - Call `signin(username, password)` from useAuth()
  - On success: redirect to `/` (home)
  - On error: display error message from context
- **Styling:** Tailwind CSS (responsive, mobile-friendly)
- **Success Criteria:**
  - Form submission prevents default
  - Button disabled during API call
  - Error messages display below form
  - Link to signup page is functional
  - Page is responsive on mobile
- **Regression Verification:** ✅ Existing pages unaffected
- **Estimated Effort:** 1 day

#### **Task 6.2: SignupPage Component**
- **Description:** Registration form page
- **Files to Create:** `src/pages/SignupPage.tsx`
- **UI Elements:**
  - Username input field
  - Password input field
  - Confirm Password input field
  - "Sign Up" button
  - Error message display (per field if validation fails)
  - "Already have an account? Sign in" link to /signin
  - Loading state on button
- **Form Handling:**
  - Validate: username (3+ chars), password (8+ chars), passwords match
  - Show field-level error messages before submission
  - Call `signup(username, password, passwordConfirmation)` from useAuth()
  - On success: redirect to `/` (home)
  - On error: display error message from context
- **Styling:** Tailwind CSS (responsive, mobile-friendly)
- **Success Criteria:**
  - Client-side validation shows feedback immediately
  - Server-side errors displayed in form
  - Button disabled during API call
  - Link to signin page is functional
  - Page is responsive on mobile
- **Regression Verification:** ✅ Existing pages unaffected
- **Estimated Effort:** 1 day

#### **Task 6.3: Update Header Component**
- **Description:** Add authentication UI to existing header
- **Files to Update:** `src/components/Header/Header.tsx` (or create if not exists)
- **Conditional UI:**
  - If `isAuthenticated === true`:
    - Display username: "Welcome, {currentUser.username}"
    - Display "Logout" button (call logout() on click)
  - If `isAuthenticated === false`:
    - Display "Sign In" link (navigate to /signin)
    - Display "Sign Up" link (navigate to /signup)
- **Styling:** Tailwind CSS, align with existing design
- **Success Criteria:**
  - Auth state reflected in header (no page reload needed)
  - Buttons/links are properly clickable
  - Responsive on mobile
- **Regression Verification:** ✅ Existing header content preserved
- **Estimated Effort:** 1 day

#### **Task 6.4: Token Persistence (localStorage)**
- **Description:** Save/load JWT from browser storage
- **Implementation Location:** AuthContext useEffect
- **Functionality:**
  - On signin/signup: Save token to `localStorage.setItem('auth_token', token)`
  - On logout: Remove token: `localStorage.removeItem('auth_token')`
  - On app init (useEffect): Load token and validate with `/v1/auth/me`
- **Success Criteria:**
  - Refresh page → user remains logged in (token loaded from localStorage)
  - Logout → token removed → refresh page → redirected to signin
  - Expired token (after 24h) → cleared on next page refresh
- **Estimated Effort:** 1 day

#### **Task 6.5: Token Validation on App Init**
- **Description:** Restore user session on page load
- **Implementation Location:** App.tsx or AuthContext useEffect
- **Functionality:**
  ```typescript
  useEffect(() => {
    const token = localStorage.getItem('auth_token');
    if (token) {
      // Call /v1/auth/me to validate token and get user
      fetchApi<UserDto>('/v1/auth/me')
        .then(response => {
          setCurrentUser(response.item);
          setIsAuthenticated(true);
        })
        .catch(() => {
          // Token invalid/expired
          localStorage.removeItem('auth_token');
          setIsAuthenticated(false);
        });
    }
  }, []);
  ```
- **Success Criteria:**
  - Page refresh with valid token → user data restored immediately
  - Page refresh with expired token → redirected to signin
  - Loading state prevents flash of unauth UI
- **Estimated Effort:** 1 day

#### **Task 6.6: Route Registration**
- **Description:** Add /signin and /signup routes to React Router
- **Files to Update:** `src/App.tsx`
- **Routes to Add:**
  ```typescript
  <Routes>
    <Route path="/" element={<HomePage />} />
    <Route path="/signin" element={<LoginPage />} />
    <Route path="/signup" element={<SignupPage />} />
    <Route path="/protected" element={<ProtectedRoute><ProtectedPage /></ProtectedRoute>} />
    // ... existing routes
  </Routes>
  ```
- **Entry Points:**
  - User navigates to /signin or /signup directly (URL bar)
  - User clicks "Sign In" / "Sign Up" in header
  - User clicks links on LoginPage / SignupPage
  - Unauthenticated user navigates to protected route → redirected to /signin
- **Redirect Behavior:**
  - On successful signin/signup → redirect to `/` (home)
  - Unauthenticated access to /protected → redirect to `/signin`
  - If already authenticated → cannot access /signin or /signup (optional: redirect to home)
- **Success Criteria:**
  - All routes accessible and render correct component
  - Redirects work as documented
  - Protected routes block unauth users
- **Regression Verification:** ✅ Existing routes (/health, etc.) preserved
- **Estimated Effort:** 1 day

---

### PHASE 7: CROSS-FUNCTIONAL TASKS

#### **Task 7.1: CORS Configuration**
- **Description:** Ensure frontend can call backend APIs
- **Files to Update:** `src/Api/Program.cs`
- **Configuration:**
  ```csharp
  builder.Services.AddCors(options =>
  {
      options.AddPolicy("AllowFrontend", policy =>
      {
          policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
                .AllowAnyMethod()
                .AllowAnyHeader();
      });
  });
  
  app.UseCors("AllowFrontend");
  ```
- **Success Criteria:**
  - Frontend can POST to /v1/auth/signup without CORS errors
  - Frontend can POST to /v1/auth/signin without CORS errors
  - Preflight OPTIONS requests succeed
- **Regression Verification:** ✅ Existing API endpoints unaffected
- **Estimated Effort:** 1 day

#### **Task 7.2: GlobalExceptionMiddleware**
- **Description:** Catch unhandled exceptions and return consistent error responses
- **Files to Create:** `src/Api/Middleware/GlobalExceptionMiddleware.cs`
- **Functionality:**
  ```csharp
  public async Task InvokeAsync(HttpContext context)
  {
      try
      {
          await _next(context);
      }
      catch (Exception ex)
      {
          _logger.LogError(ex, "Unhandled exception");
          context.Response.StatusCode = StatusCodes.Status500InternalServerError;
          context.Response.ContentType = "application/json";
          
          var response = new ErrorResponseDto
          {
              Code = "ORG-INT-001",
              Message = "Internal server error",
              Details = null
          };
          
          await context.Response.WriteAsJsonAsync(response);
      }
  }
  ```
- **Register in Program.cs:** `app.UseMiddleware<GlobalExceptionMiddleware>();`
- **Success Criteria:**
  - Unhandled exceptions return 500 with consistent error format
  - Error details not exposed to client (logged server-side only)
  - transactionId included if middleware has access to request context
- **Estimated Effort:** 1 day

#### **Task 7.3: Local Startup & Configuration Documentation**
- **Description:** Document how to run backend locally with auth enabled
- **Files to Update/Create:** `README.md`, `docs/BUILD_INSTRUCTIONS.md`
- **Documentation:**
  ```markdown
  ## Backend Setup (Authentication Enabled)
  
  1. **Prerequisites:**
     - .NET 8 SDK installed
     - SQL Server LocalDB or other local instance
  
  2. **Database Migration:**
     ```bash
     cd backend
     dotnet ef database update
     ```
  
  3. **JWT Configuration:**
     - Edit `src/Api/appsettings.Development.json`
     - Set `Jwt:Secret` to a ≥32 character string (can be any string for dev)
     ```json
     {
       "Jwt": {
         "Secret": "your-dev-secret-key-minimum-32-characters",
         "ExpiryMinutes": 1440,
         "Issuer": "dotnet-react-starter"
       }
     }
     ```
  
  4. **Run Backend:**
     ```bash
     cd backend/src/Api
     dotnet run
     ```
     - API starts at `http://localhost:5000`
     - Swagger UI at `http://localhost:5000/swagger`
  
  5. **Frontend Setup:**
     ```bash
     cd frontend
     npm install
     npm run dev
     ```
     - Frontend starts at `http://localhost:5173`
  
  6. **Test Authentication:**
     - Navigate to http://localhost:5173/signup
     - Create account (username: test_user, password: password123)
     - Redirected to home page (logged in)
     - Refresh page (token persists from localStorage)
     - Click "Logout" in header (token removed)
  
  7. **Verify Backend Health:**
     ```bash
     curl http://localhost:5000/health
     ```
     Should return 200 OK with health status.
  ```
- **Estimated Effort:** 1 day

---

## 3. Requirement Traceability Matrix

| Requirement | Implementation Task(s) | Verification |
|:---|:---|:---|
| **Signup form** | Task 6.2 | Manual: Form renders, accepts input |
| **Signup validation** | Task 1.2, Task 2.2 (service), Task 4.1 | Test: SignupRequestDto, AuthService.RegisterUserAsync tests |
| **Unique username** | Task 1.1 (constraint), Task 2.1 (UsernameExistsAsync), Task 2.2 (logic) | Test: AuthServiceTests.RegisterUserAsync_WithDuplicateUsername_ReturnsError |
| **Secure password hashing** | Task 2.2 (BCrypt in AuthService) | Test: AuthServiceTests verify BCrypt usage |
| **Signin form** | Task 6.1 | Manual: Form renders, accepts input |
| **Signin validation** | Task 2.2 (AuthService.AuthenticateUserAsync), Task 3.2 (AuthController) | Test: AuthServiceTests.AuthenticateUserAsync, AuthControllerTests |
| **Generic error messages** | Task 2.2 (service error returns), Task 3.2 (controller error mapping) | Test: AuthServiceTests negative paths verify generic messages |
| **JWT token generation** | Task 2.2 (AuthService.GenerateJwt), Task 2.3 (config) | Test: AuthServiceTests.GenerateJwt_CreatesValidToken |
| **Token storage (localStorage)** | Task 5.1 (AuthContext), Task 6.4 (persistence) | Manual: Inspect DevTools, verify token in localStorage |
| **Token persistence** | Task 5.1, Task 6.5 (app init validation) | Manual: Refresh page, verify user still logged in |
| **24-hour expiry** | Task 2.3 (ExpiryMinutes: 1440) | Manual: Verify JWT iat/exp claims |
| **Logout functionality** | Task 3.2 (Logout endpoint), Task 5.1 (logout method) | Test: AuthControllerTests POST /logout |
| **Current user endpoint (/v1/auth/me)** | Task 3.2 (AuthController.GetCurrentUser), Task 2.2 (ValidateTokenAsync) | Test: AuthControllerTests GET /me |
| **Route protection** | Task 5.3 (ProtectedRoute), Task 6.6 (route registration) | Manual: Try accessing protected route unauth → redirect to signin |
| **Header auth UI** | Task 6.3 (Header component update) | Manual: Inspect header for Sign In/Sign Up buttons, username display |
| **Signup → Signin flow** | Task 6.1, 6.2, 6.6 | Manual: End-to-end test signup→signin→homepage |
| **Error messaging** | Task 3.2 (error responses), Task 6.1, 6.2 (display) | Manual: Submit invalid data, verify error messages display |
| **HTTPS enforcement** | Documented in architecture (production requirement) | Not testable in dev; documented for deployment |
| **API envelope pattern** | Task 1.2 (DTOs), Task 3.2 (controller responses) | Test: AuthControllerTests verify response structure |

---

## 4. Preserved Behavior & Regression Tests

| Existing Feature | Regression Test | Success Criteria |
|:---|:---|:---|
| **Health endpoint** | `GET /health` | Returns 200 OK, continues to work | 
| **Swagger UI** | `GET /swagger` | Accessible, documents new /v1/auth/* endpoints |
| **CORS** | Frontend can call backend | No CORS errors on auth requests |
| **Error response format** | Existing ErrorResponseDto structure | Auth endpoints follow same pattern |
| **Existing routes** | All existing routes still accessible | No breaking changes to routing |

---

## 5. Test Coverage Requirements

| Component | Minimum Coverage | Test File |
|:---|:---|:---|
| **AuthService** | 90% | tests/Api.Tests/Services/AuthServiceTests.cs |
| **UserRepository** | 85% | tests/Api.Tests/Repositories/UserRepositoryTests.cs |
| **AuthController** | 80% (integration) | tests/Api.Tests/Controllers/AuthControllerTests.cs |
| **DTOs** | Validation only | Part of AuthControllerTests |

---

## 6. Critical Path & Bottlenecks

**Critical Path:** Task 1.1 → Task 1.2 → Task 2.1 → Task 2.2 → Task 3.2 → Task 4.1

**Blockers:**
- **Task 2.2 cannot start** until Task 2.1 (IUserRepository interface)
- **Task 3.2 cannot start** until Task 2.2 (AuthService) and Task 2.3 (JWT config)
- **Task 4.1 cannot start** until Task 2.2 (AuthService implementation)

**Parallelization Opportunities:**
- Frontend tasks (6.1-6.4) can run in parallel with backend tasks (1-4) once APIs are stubbed
- Task 5.1-5.3 (Auth infrastructure) can start after Task 5.1 design is finalized
- Task 7.1-7.3 can run in parallel with any backend task

---

## 7. Acceptance Checklist

- [ ] **Sign-Up Works:** User can register with unique username/password
- [ ] **Sign-Up Validation:** System rejects invalid inputs with messages
- [ ] **Sign-In Works:** Registered user can log in
- [ ] **Sign-In Rejection:** System rejects wrong credentials
- [ ] **Token Persists:** User stays logged in after page refresh
- [ ] **Logout Works:** User can log out and is redirected to signin
- [ ] **Password Security:** Passwords hashed with BCrypt (never plaintext)
- [ ] **Route Protection:** Unauth users cannot access protected routes
- [ ] **UI Consistency:** Forms match existing design
- [ ] **Error Messages:** All error paths display friendly messages
- [ ] **Tests Passing:** 90%+ coverage on services, 80%+ on controllers
- [ ] **API Documentation:** Swagger reflects new endpoints
- [ ] **Backend Runs Locally:** `dotnet run` from backend/src/Api starts service
- [ ] **Frontend Runs Locally:** `npm run dev` from frontend starts app
- [ ] **End-to-End:** Signup → Signin → Protected Route → Logout flow works

---

**Next Step:** Stage 5 - Code Implementation (Engineer Agent)
