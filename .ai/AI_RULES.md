Never violate Clean Architecture.
Never bypass Vertical Slice Architecture.
Never access the database directly from controllers.
Never use synchronous I/O.
Every endpoint requires validation.
Every mutation publishes domain events when appropriate.
Every exception must be logged with correlation IDs.
Every public API must be documented.
Every feature must include tests.
Never duplicate business logic.
Never introduce circular dependencies.
Preserve backward compatibility unless an ADR explicitly approves a breaking change.
