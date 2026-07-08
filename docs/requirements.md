# KAN-314: Simple Username/Password Signup & Sign-In (POC)

**Date Created:** 2026-07-07  
**Status:** Draft - Pending Clarification  
**Jira Ticket:** [KAN-314](https://anusehgal.atlassian.net/browse/KAN-314)

---

## Overview

Implement a proof-of-concept authentication system that allows users to sign up with a username and password, and subsequently sign in using the same credentials. This feature provides the foundation for user account management and will be expanded in future iterations.

---

## Story Details

- **Jira Key:** KAN-314
- **Title:** Implement Simple Username/Password Signup & Sign-In (POC)
- **Type:** Story/Feature
- **Story Context:** As a user, I want to register an account and log in using a username and password, so that I can access the application securely.

---

## Functional Requirements

### Authentication - Sign Up (Registration)

1. **User Registration Form**
   - Display a registration page accessible from the application
   - Collect: username, password, password confirmation
   - Username must be unique in the system
   - Password must be non-empty and stored securely (hashed, not plaintext)

2. **Validation on Sign Up**
   - Username: minimum 3 characters, alphanumeric + underscores allowed
   - Password: minimum 8 characters required
   - Passwords must match (password == password confirmation)
   - Prevent duplicate usernames
   - Return clear error messages for validation failures

3. **Account Creation**
   - Successfully created accounts allow immediate sign-in
   - User receives confirmation (message/redirect) after successful registration

### Authentication - Sign In (Login)

1. **Sign-In Form**
   - Display a login page with username and password fields
   - Provide "Remember me" option (optional, can be added later)
   - Allow navigation to sign-up page from sign-in page

2. **Sign-In Validation**
   - Verify username exists
   - Verify password matches stored hash
   - Reject invalid credentials with generic message (security best practice)
   - Return clear error message on failure

3. **Session Management**
   - Upon successful sign-in, create a session/token for the user
   - Store session securely (JWT or session cookie)
   - User remains logged in while session is valid
   - Provide a way to log out (sign out) and destroy session

### Post-Authentication

1. **User State**
   - Application knows current logged-in user
   - Display user identity in UI (welcome message or header)
   - Prevent access to authenticated features when not logged in

2. **Navigation**
   - Provide logout functionality accessible from authenticated pages
   - Redirect unauthenticated users attempting to access protected routes to sign-in page
   - Allow users to navigate between signup and signin pages

---

## Non-Functional Requirements

1. **Security**
   - Passwords must never be stored in plaintext
   - Use bcrypt or PBKDF2 for password hashing
   - Validate all inputs on both client and server
   - No sensitive data in error messages (don't reveal whether username exists)
   - Use HTTPS in production

2. **Performance**
   - Authentication endpoints should respond within 500ms under normal load
   - Session validation should not block page rendering

3. **Usability**
   - Forms must be intuitive and mobile-friendly (Tailwind CSS responsive)
   - Clear, non-technical error messages
   - Consistent styling with existing application theme

4. **Testing**
   - Unit tests for service/repository logic
   - Integration tests for API endpoints
   - E2E tests validating sign-up → sign-in → authenticated access flow

---

## Architecture Scope (High-Level)

### Backend (.NET 8 Web API)

- **Entities:** User model with Id, Username, PasswordHash, CreatedDate
- **Database:** Store users in persistent storage (EF Core)
- **Endpoints:**
  - `POST /v1/auth/signup` — Register new user
  - `POST /v1/auth/signin` — Authenticate and create session
  - `POST /v1/auth/signout` — Destroy session (optional for POC)
  - `GET /v1/auth/me` — Get current user (optional, for UI to verify logged-in state)

- **Response Envelope:** All responses follow org's envelope pattern (ItemResponseDto, CollectionResponseDto)
- **Error Handling:** ErrorResponseDto with code/message/details

### Frontend (React 18 + TypeScript)

- **Pages:**
  - SignUpPage.tsx — Registration form
  - SignInPage.tsx — Login form
- **Components:**
  - Reusable form components for input handling
  - ErrorMessage component for displaying validation errors
- **Routes:**
  - `/signup` — Registration
  - `/signin` — Login
  - Protected route wrapper for authenticated content
- **State Management:**
  - Track current user in local state or context
  - Store auth token securely (localStorage or sessionStorage)
  - Provide auth context hook for checking logged-in status

---

## Acceptance Criteria

- [ ] **Sign-Up Works:** User can register with unique username and password
- [ ] **Sign-Up Validation:** System rejects invalid inputs with appropriate messages
- [ ] **Sign-In Works:** Registered user can successfully log in with correct credentials
- [ ] **Sign-In Rejection:** System rejects login attempts with wrong credentials
- [ ] **Session Persistence:** User stays logged in across page refreshes (until logout/session expiry)
- [ ] **Logout Works:** User can sign out and is redirected to sign-in page
- [ ] **Password Security:** Passwords are hashed (never stored as plaintext)
- [ ] **Route Protection:** Unauthenticated users cannot access protected routes
- [ ] **UI Consistency:** Forms match application styling and are responsive
- [ ] **Error Messages:** All error paths display user-friendly messages
- [ ] **Tests Passing:** Unit + integration tests cover happy path and error scenarios
- [ ] **API Documentation:** Endpoints documented in Swagger/OpenAPI

---

## Clarifications & Scope Decisions

### Questions for Stakeholder Sign-Off

**Q1: Session/Token Strategy**
- Should we use JWT tokens stored in localStorage, or server-side sessions with HTTP-only cookies?
- **Impact:** Affects security posture and CORS configuration
- **Proposal:** Use JWT tokens in localStorage for simplicity in POC; upgrade to HTTP-only cookies in production phase

**Q2: Password Reset**
- Should POC include "Forgot Password" functionality?
- **Impact:** Scope, backend, email integration
- **Proposal:** NOT in POC scope; capture as follow-up feature

**Q3: Email Verification**
- Should sign-up require email verification?
- **Impact:** Scope, email service integration
- **Proposal:** NOT in POC; only username/password required

**Q4: User Profile Data**
- Should we collect additional data on sign-up (email, full name, etc.)?
- **Impact:** Schema, form complexity
- **Proposal:** Username + password only for POC; expand in next iteration

**Q5: Session Timeout**
- Should sessions expire after inactivity? If yes, how long?
- **Impact:** Behavior, security
- **Proposal:** 24-hour default expiry for POC; configurable

**Q6: Navigation Entry Points**
- Where should sign-up and sign-in links appear? (landing page, header nav, etc.)
- **Impact:** UI flow, home page updates
- **Proposal:** Add Sign In / Sign Up buttons to main navigation header

**Q7: Post-Login Redirect**
- Where should users be redirected after successful sign-in?
- **Impact:** User experience, routing logic
- **Proposal:** Redirect to application dashboard/home; if no dashboard exists yet, redirect to health check page

---

## Requirements Checklist (for Implementation)

- [ ] Backend User entity with secure password storage (bcrypt)
- [ ] Signup endpoint with validation (username uniqueness, password strength)
- [ ] Signin endpoint with credential verification
- [ ] Session/token generation and validation
- [ ] Frontend signup form with client-side validation
- [ ] Frontend signin form with error handling
- [ ] Route protection middleware (frontend) to redirect unauthenticated users
- [ ] Auth context/state management for current user
- [ ] Logout functionality
- [ ] UI/UX consistency with Tailwind styling
- [ ] Unit tests for all service methods
- [ ] Integration tests for API endpoints
- [ ] Error path coverage (invalid inputs, duplicate users, wrong passwords)
- [ ] Documentation updates (API contract in Swagger)
- [ ] "Sign In" button visible from unauthenticated pages
- [ ] "Sign Out" button visible in header when authenticated

---

## Dependencies & Assumptions

- **Dependencies:**
  - EF Core is already configured for the application
  - Database migrations can be generated for User entity
  - Frontend build/dev environment is functional (Vite, React)

- **Assumptions:**
  - Users will authenticate before accessing app features
  - No multi-factor authentication required for POC
  - Sessions are valid until token expires or user logs out
  - Password hashing library (BCrypt) is available via NuGet

---

## Out of Scope (Explicitly)

- Multi-factor authentication (MFA)
- OAuth / Social login
- Password reset / recovery
- Email verification
- User profile management
- Account deactivation
- Role-based access control (RBAC) — basic auth only
- Audit logging of login attempts
- CAPTCHA or rate limiting (except basic validation)

---

## Timeline & Effort Estimate

- **Estimated Story Points:** 8-13 (depending on clarifications)
- **Proposed Timeline:** 2-3 sprints for POC implementation + testing

