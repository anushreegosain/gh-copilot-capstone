# Requirements - KAN-314

## Source
- Jira issue: KAN-314
- Title: Implement Simple Username/Password Signup & Sign-In (POC)

## Summary
Implement a lightweight proof-of-concept authentication flow that allows a user to create an account with a username and password, and then sign in with those same credentials. The implementation should fit the current .NET 8 starter backend and remain simple enough for local development and demo purposes.

## Functional Requirements
1. Provide a signup endpoint that accepts a username, password, and password confirmation.
2. Provide a sign-in endpoint that accepts a username and password.
3. Reject invalid input with clear validation messages for:
   - blank or missing username
   - blank or missing password
   - mismatched password confirmation during signup
   - duplicate usernames during signup
   - unknown username or incorrect password during sign-in
4. Persist credentials for the proof of concept using an in-memory store so the feature works without database setup.
5. Store passwords securely using a salted hash rather than plain text.
6. Return a clear success or failure response message appropriate for client display.

## API Shape
- Use the existing backend route conventions for the starter API.
- Expose endpoints under the pluralized auth resource path, following the current convention of `/v1/[resource]`.
- Keep the implementation consistent with the existing starter DTO and controller structure.
- Do not introduce a `/api/` prefix.

## Preserved Existing Behavior
- Existing health and root routes must continue to work.
- The application must continue to start successfully from the documented backend entry point.
- Existing Swagger access should remain available for local development.

## Error and Validation Behavior
- Validation errors should be returned as actionable API errors rather than generic server failures.
- Duplicate username registration should fail cleanly with a specific message.
- Invalid sign-in attempts should fail cleanly without exposing implementation details.

## Non-Goals for This POC
- No full authentication framework integration (for example, JWT/OIDC) is required.
- No persistent database-backed user store is required.
- No production-grade password policy enforcement beyond basic validation is required.

## Verification Expectations
- Backend tests should cover successful signup, duplicate signup, successful sign-in, and failed sign-in.
- Local startup should be verified from the backend project entry point.
- Swagger or direct API calls should be usable for exercising the new endpoints.

## Questions for Sign-Off
1. Should the POC remain backend-only, or should a minimal UI form also be added in this ticket?
2. Is in-memory persistence sufficient for the intended demo scenario?
3. Should successful sign-in return only a status message, or should it also include a session identifier or user identifier?
