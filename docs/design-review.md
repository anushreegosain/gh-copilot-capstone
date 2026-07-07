# Design Review - KAN-314

## Review Summary
The proposed authentication POC is appropriately scoped for a starter application. The architecture is simple, testable, and aligns with the current backend structure.

## Decisions
1. Use an in-memory repository for the POC.
   - This keeps the implementation lightweight and avoids introducing a database dependency for the demo.
2. Keep auth logic in a dedicated service layer.
   - This preserves separation of concerns between controllers and business logic.
3. Use password hashing rather than plaintext storage.
   - This addresses the most important security concern for a basic auth flow.

## Risks
- In-memory state is ephemeral and will be reset on restart.
- Password hashing must be implemented carefully to avoid introducing a security weakness.
- Validation behavior must remain consistent and user-friendly.

## Mitigations
- Document the in-memory repository as a deliberate POC constraint.
- Use a standard salted-hash approach and verify it with tests.
- Add explicit validation and error handling for duplicate usernames and failed sign-in attempts.

## Recommendation
Proceed to implementation with the current design. The design is low risk and appropriate for the stated POC scope.
