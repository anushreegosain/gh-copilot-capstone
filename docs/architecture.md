# KAN-314: Authentication System Architecture

**Date:** 2026-07-07  
**Ticket:** KAN-314 (POC)  
**Scope:** JWT-based username/password authentication  
**Tech Stack:** .NET 8 Web API + React 18 TypeScript + Tailwind CSS  

---

## System Overview

```
┌─────────────────────────────────────────────────────────────┐
│                    React 18 Frontend                         │
│  ┌──────────────┐  ┌──────────────┐  ┌─────────────────┐   │
│  │  LoginPage   │  │ SignupPage   │  │  ProtectedRoute │   │
│  └──────┬───────┘  └──────┬───────┘  └────────┬────────┘   │
│         │                 │                    │            │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  AuthContext (JWT Storage & Current User State)     │  │
│  └──────────────────────────────────────────────────────┘  │
│                         │                                   │
└─────────────────────────┼───────────────────────────────────┘
                          │
              ┌───────────┴────────────┐
              │ HTTP REST (TLS)        │
              └───────────┬────────────┘
                          │
┌─────────────────────────┼───────────────────────────────────┐
│        .NET 8 Web API Backend                               │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  Auth Controller (/v1/auth)                          │  │
│  │  - POST /signup, /signin, /logout, /me               │  │
│  └─────────────────────┬────────────────────────────────┘  │
│                        │                                    │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  AuthService                                         │   │
│  │  - RegisterUser, AuthenticateUser                   │   │
│  │  - GenerateJWT, ValidateToken                       │   │
│  │  - Password hashing (BCrypt)                        │   │
│  └─────────────────────┬────────────────────────────────┘   │
│                        │                                    │
│  ┌─────────────────────┴────────────────────────────────┐   │
│  │  UserRepository (EF Core)                             │   │
│  │  - GetByUsernameAsync                                │   │
│  │  - CreateAsync, GetByIdAsync                         │   │
│  └─────────────────────┬────────────────────────────────┘   │
│                        │                                    │
│  ┌─────────────────────┴────────────────────────────────┐   │
│  │  Entity: User                                         │   │
│  │  - Id (PK), Username, PasswordHash, CreatedDate     │   │
│  └────────────────────────────────────────────────────┘   │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  SQL Database (AppDbContext)                         │   │
│  │  - Users table                                       │   │
│  └────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

---

## 1. Architecture Layers

### 1.1 Presentation Layer (Frontend)

**Responsibility:** Render UI forms, capture user input, manage local authentication state.

**Components:**
- `LoginPage.tsx` — Sign-in form (username/password)
- `SignupPage.tsx` — Sign-up form (username/password/confirm)
- `ProtectedRoute.tsx` — Route wrapper for authenticated pages
- `Header.tsx` — Navigation with Sign In/Sign Up buttons, user display

**State Management:**
- `AuthContext.tsx` — Centralized auth state (currentUser, isAuthenticated, token)
- `useAuth()` hook — Access auth state from any component

**Token Storage:**
- localStorage key: `auth_token` (JWT)
- Persists across page refreshes until expiry or manual logout

### 1.2 API Contract Layer (Controllers)

**Responsibility:** Handle HTTP requests, validate input, return envelope-wrapped responses.

**Controller:** `AuthController` (base route: `/v1/auth`)

**Endpoints:**

| Endpoint | Method | Purpose | Auth Required |
|----------|--------|---------|---------------|
| `/v1/auth/signup` | POST | Register new user | No |
| `/v1/auth/signin` | POST | Authenticate & get token | No |
| `/v1/auth/logout` | POST | Invalidate token (client cleanup) | Yes (JWT) |
| `/v1/auth/me` | GET | Get current user profile | Yes (JWT) |

**Input/Output DTOs:**
- `SignupRequestDto` — username, password, passwordConfirmation
- `SigninRequestDto` — username, password
- `AuthResponseDto` — token, expiresIn, user (id, username)
- `UserDto` — id, username, createdDate

---

## 2. Application Layer (Services & DTOs)

### 2.1 AuthService

**Responsibility:** Business logic for user registration, authentication, JWT generation, and password hashing.

**Key Methods:**

```csharp
// User registration with validation
Task<(bool Success, AuthResponseDto? Response, string? Error)> RegisterUserAsync(
    string username, string password, string passwordConfirmation, CancellationToken ct);

// User authentication with credential validation
Task<(bool Success, AuthResponseDto? Response, string? Error)> AuthenticateUserAsync(
    string username, string password, CancellationToken ct);

// JWT token generation
string GenerateJwt(User user);

// Token validation (for ProtectedRoute)
Task<User?> ValidateTokenAsync(string token, CancellationToken ct);

// Logout (primarily client-side, but server can track blacklist)
Task LogoutAsync(int userId, CancellationToken ct);
```

**Business Rules:**
- Username: minimum 3 chars, alphanumeric + underscores
- Password: minimum 8 chars, hashed with BCrypt (cost=12)
- Usernames must be unique (case-insensitive)
- JWT expires after 24 hours
- Generic error messages (don't reveal if username exists)

### 2.2 DTOs

**Location:** `DTOs/Auth/`

```csharp
// Request
public class SignupRequestDto
{
    [JsonPropertyName("username")]
    public string Username { get; set; }
    
    [JsonPropertyName("password")]
    public string Password { get; set; }
    
    [JsonPropertyName("passwordConfirmation")]
    public string PasswordConfirmation { get; set; }
}

public class SigninRequestDto
{
    [JsonPropertyName("username")]
    public string Username { get; set; }
    
    [JsonPropertyName("password")]
    public string Password { get; set; }
}

// Response
public class AuthResponseDto
{
    [JsonPropertyName("token")]
    public string Token { get; set; }
    
    [JsonPropertyName("expiresIn")]
    public int ExpiresIn { get; set; }  // seconds
    
    [JsonPropertyName("user")]
    public UserDto User { get; set; }
}

public class UserDto
{
    [JsonPropertyName("userId")]
    public int UserId { get; set; }
    
    [JsonPropertyName("username")]
    public string Username { get; set; }
    
    [JsonPropertyName("createdDate")]
    public DateTime CreatedDate { get; set; }
}
```

---

## 3. Domain Layer (Entities)

### 3.1 User Entity

**Location:** `Domain/Entities/User.cs`

```csharp
public class User
{
    /// <summary>Unique identifier</summary>
    public int Id { get; set; }
    
    /// <summary>Unique username (3+ alphanumeric + underscores)</summary>
    public string Username { get; set; }
    
    /// <summary>BCrypt-hashed password (never plaintext)</summary>
    public string PasswordHash { get; set; }
    
    /// <summary>Account creation timestamp</summary>
    public DateTime CreatedDate { get; set; }
    
    /// <summary>Account last updated (for future extensions)</summary>
    public DateTime UpdatedDate { get; set; }
}
```

**Business Logic:**
- Username uniqueness enforced at database level (unique index)
- PasswordHash computed via BCrypt before persistence
- CreatedDate set at registration time (server clock)

---

## 4. Data Access Layer (Repositories)

### 4.1 IUserRepository Interface

**Location:** `Repositories/IUserRepository.cs`

```csharp
public interface IUserRepository
{
    /// <summary>Retrieve user by username (case-insensitive)</summary>
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken);
    
    /// <summary>Retrieve user by ID</summary>
    Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken);
    
    /// <summary>Create and persist new user</summary>
    Task<User> CreateAsync(User user, CancellationToken cancellationToken);
    
    /// <summary>Check if username already exists</summary>
    Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken);
}
```

### 4.2 UserRepository Implementation

**Location:** `Repositories/UserRepository.cs`

- Use EF Core with AsNoTracking() for queries
- Support case-insensitive username lookup
- Handle unique constraint violations gracefully
- Pass CancellationToken to all async methods

---

## 5. JWT Token Structure

**Header:**
```json
{
  "alg": "HS256",
  "typ": "JWT"
}
```

**Payload (Claims):**
```json
{
  "sub": "1",                              // User ID
  "username": "john_doe",                  // Username
  "iat": 1720000000,                       // Issued at
  "exp": 1720086400,                       // Expires (24 hours later)
  "iss": "dotnet-react-starter"            // Issuer
}
```

**Signing:**
- Algorithm: HS256 (HMAC-SHA256)
- Secret: Read from `appsettings.json` (Jwt:Secret)
- Min 32 chars for security

---

## 6. Frontend Authentication Flow

### 6.1 AuthContext & useAuth Hook

**File:** `src/contexts/AuthContext.tsx`

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
}

// Provider component manages state, token persistence, and API calls
// useAuth() hook provides access from any component
```

**Initialization:**
- On mount, check localStorage for token
- If token exists, call `/v1/auth/me` to validate and restore user
- Handle token expiry gracefully (redirect to /signin)

### 6.2 Page Flow

**SignupPage:**
1. Render form (username, password, confirm password)
2. On submit: call `signup()` from AuthContext
3. On success: redirect to `/` (home)
4. On error: display error message in form

**SigninPage:**
1. Render form (username, password)
2. On submit: call `signin()` from AuthContext
3. On success: redirect to `/` (home)
4. On error: display error message in form

**Header:**
1. Check `isAuthenticated` from useAuth()
2. If authenticated: show username + Logout button
3. If not: show Sign In / Sign Up buttons

**ProtectedRoute:**
1. Wrap routes that require authentication
2. Check `isAuthenticated`
3. If not authenticated: redirect to `/signin`
4. If authenticated: render component

---

## 7. Backend API Response Patterns

### 7.1 Success Responses

**Sign-up Success (201 Created):**
```json
{
  "item": {
    "token": "eyJhbGc...",
    "expiresIn": 86400,
    "user": {
      "userId": 1,
      "username": "john_doe",
      "createdDate": "2026-07-07T12:00:00Z"
    }
  },
  "metadata": {
    "timestamp": "2026-07-07T12:00:01Z",
    "transactionId": "txn-uuid-here"
  },
  "links": {
    "self": "/v1/auth/signin"
  }
}
```

**Sign-in Success (200 OK):**
Same as Sign-up

**Get Current User (200 OK):**
```json
{
  "item": {
    "userId": 1,
    "username": "john_doe",
    "createdDate": "2026-07-07T12:00:00Z"
  },
  "metadata": {
    "timestamp": "2026-07-07T12:00:01Z",
    "transactionId": "txn-uuid-here"
  },
  "links": {
    "self": "/v1/auth/me"
  }
}
```

### 7.2 Error Responses

**Validation Error (400 Bad Request):**
```json
{
  "code": "ORG-VAL-001",
  "message": "Validation failed",
  "details": [
    {
      "field": "username",
      "message": "Username must be at least 3 characters"
    },
    {
      "field": "password",
      "message": "Password must be at least 8 characters"
    }
  ]
}
```

**Duplicate Username (400 Bad Request):**
```json
{
  "code": "ORG-VAL-001",
  "message": "The provided credentials are invalid"  // Generic message
}
```

**Wrong Credentials (401 Unauthorized):**
```json
{
  "code": "ORG-AUT-001",
  "message": "The provided credentials are invalid"  // Generic message
}
```

**Unauthorized (401):**
```json
{
  "code": "ORG-AUT-001",
  "message": "Authorization required"
}
```

---

## 8. Security Considerations

| Concern | Mitigation |
|---------|-----------|
| **Password storage** | BCrypt hashing (cost=12), never plaintext |
| **Token theft** | 24-hour expiry, HTTPS-only in production, localStorage (no HttpOnly due to POC constraints) |
| **CSRF** | CORS properly configured, SameSite cookies (if used later) |
| **Brute force** | Generic error messages, rate limiting (future enhancement) |
| **SQL injection** | EF Core parameterized queries |
| **XSS** | React automatic escaping, no innerHTML |
| **Username enumeration** | Generic "invalid credentials" message |

---

## 9. Database Schema

**Table: Users**

```sql
CREATE TABLE [Users] (
    [Id] INT PRIMARY KEY IDENTITY(1,1),
    [Username] NVARCHAR(256) NOT NULL UNIQUE,
    [PasswordHash] NVARCHAR(MAX) NOT NULL,
    [CreatedDate] DATETIME2 NOT NULL,
    [UpdatedDate] DATETIME2 NOT NULL
);

CREATE INDEX [IX_Users_Username] ON [Users]([Username]);
```

---

## 10. Configuration & Dependencies

### 10.1 Backend appsettings.json

```json
{
  "Jwt": {
    "Secret": "your-super-secret-key-min-32-chars",
    "ExpiryMinutes": 1440,
    "Issuer": "dotnet-react-starter"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=dotnet_react_starter;Trusted_Connection=true;TrustServerCertificate=true;"
  }
}
```

### 10.2 NuGet Dependencies

- `System.IdentityModel.Tokens.Jwt` — JWT generation & validation
- `BCrypt.Net-Core` — Password hashing
- `Microsoft.IdentityModel.Tokens` — Token handling

### 10.3 Frontend Dependencies

- `react-router-dom` — Already in project (for routing)
- No additional auth libraries (POC uses context + localStorage)

---

## 11. File Structure

**Backend:**
```
src/Api/
├── Controllers/
│   └── AuthController.cs
├── Domain/
│   └── Entities/
│       └── User.cs
├── DTOs/
│   └── Auth/
│       ├── SignupRequestDto.cs
│       ├── SigninRequestDto.cs
│       ├── AuthResponseDto.cs
│       └── UserDto.cs
├── Repositories/
│   ├── IUserRepository.cs
│   └── UserRepository.cs
├── Services/
│   ├── IAuthService.cs
│   └── AuthService.cs
└── Middleware/
    └── JwtMiddleware.cs (optional, for validation)

tests/Api.Tests/
├── Controllers/
│   └── AuthControllerTests.cs
├── Services/
│   └── AuthServiceTests.cs
└── Repositories/
    └── UserRepositoryTests.cs
```

**Frontend:**
```
src/
├── contexts/
│   └── AuthContext.tsx
├── hooks/
│   └── useAuth.ts
├── pages/
│   ├── LoginPage.tsx
│   └── SignupPage.tsx
├── components/
│   ├── ProtectedRoute.tsx
│   └── Header.tsx
└── types/
    └── auth.ts
```

---

## 12. Integration Points

| Layer | Integration | Method |
|-------|-----------|--------|
| **Frontend ↔ Backend** | REST API | HTTP POST/GET with JSON + JWT |
| **Backend ↔ Database** | EF Core | DbContext with migrations |
| **Token Persistence** | Browser Storage | localStorage (POC) |
| **Password Security** | BCrypt | External library |

---

## 13. Risks & Mitigations

| Risk | Severity | Mitigation |
|------|----------|-----------|
| **Token in localStorage** | Medium | POC scope; move to HttpOnly in production |
| **No rate limiting** | Low | Add in Phase 2 |
| **No token blacklist** | Low | Implement if extended features needed |
| **No multi-device logout** | Low | Out of POC scope |

---

## 14. Future Enhancements (Post-POC)

- [ ] Password reset / forgot password flow
- [ ] Email verification for sign-up
- [ ] Refresh tokens for extended sessions
- [ ] Multi-factor authentication (MFA)
- [ ] Social login (OAuth)
- [ ] Role-based access control (RBAC)
- [ ] HttpOnly secure cookies instead of localStorage
- [ ] Token blacklist / revocation
- [ ] Audit logging for auth events

---

**Next Step:** Stage 3 - Design Review (security audit, anti-pattern check)
