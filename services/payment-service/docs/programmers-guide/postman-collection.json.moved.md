# Moved

The Postman collection (and its companion environment file) for this
service now live at:

- `services/payment-service/docs/scripts/postman/payment-service.postman-collection.json`
- `services/payment-service/docs/scripts/postman/payment-service.postman_environment.json`

This matches the `docs/scripts/postman/` convention already used by
auth-service, bus-service, and route-service. The old flat, out-of-date
collection that used to live at this path (`postman-collection.json`,
104 lines, missing 3/4 of the actual endpoints and no environment/scripts)
has been removed — see `docs/new-release-notes/release-notes.md` for details.
