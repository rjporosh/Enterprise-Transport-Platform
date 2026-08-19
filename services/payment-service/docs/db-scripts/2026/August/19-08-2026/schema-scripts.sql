-- =============================================================================
-- Payment Service — Schema Scripts
-- Generated from EF Core migration history (source of truth: dotnet ef).
-- Migrations covered:
--   20260810133345_InitialCreate           (outbox_messages, payments, payment_refunds)
--   20260810195845_AddAgentPaymentMethod   (agent_payment_methods)
-- Target: PostgreSQL. Schema: "payment".
--
-- IMPORTANT: this file is a human-readable snapshot for review/DBA sign-off.
-- It is NOT how migrations are applied — the actual apply path is always
-- `dotnet ef database update` (see /guide.md at the repo root), which reads
-- the real .cs migration files, not this SQL. Regenerate this snapshot after
-- every new migration using:
--   dotnet ef migrations script --project src/PaymentService.Infrastructure \
--     --startup-project src/PaymentService.Api --idempotent -o docs/db-scripts/<date>/schema-scripts.sql
-- (that command needs the .NET 10 SDK; it could not be run in this sandbox —
-- see ai-hanover.md for why — so this snapshot was hand-derived from the
-- migration source instead and should be treated as unverified against a
-- real `dotnet ef` run until someone with the SDK regenerates it.)
-- =============================================================================

CREATE SCHEMA IF NOT EXISTS payment;

-- -----------------------------------------------------------------------------
-- outbox_messages — transactional outbox (see docs/programmers-guide/adr/0002-transactional-outbox.md)
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS payment.outbox_messages (
    "Id"             uuid                      NOT NULL,
    "EventType"      character varying(500)    NOT NULL,
    "Payload"        text                      NOT NULL,
    "OccurredOnUtc"  timestamp with time zone  NOT NULL,
    "ProcessedOnUtc" timestamp with time zone  NULL,
    "Error"          character varying(2000)   NULL,
    "RetryCount"     integer                   NOT NULL,
    "CorrelationId"  character varying(100)    NULL,
    CONSTRAINT "PK_outbox_messages" PRIMARY KEY ("Id")
);

CREATE INDEX IF NOT EXISTS "IX_outbox_messages_OccurredOnUtc"  ON payment.outbox_messages ("OccurredOnUtc");
CREATE INDEX IF NOT EXISTS "IX_outbox_messages_ProcessedOnUtc" ON payment.outbox_messages ("ProcessedOnUtc");
CREATE INDEX IF NOT EXISTS "IX_outbox_messages_RetryCount"     ON payment.outbox_messages ("RetryCount");

-- -----------------------------------------------------------------------------
-- payments — core payment aggregate
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS payment.payments (
    "Id"                 uuid                      NOT NULL,
    "TenantId"            uuid                      NOT NULL,
    "CompanyId"           uuid                      NULL,
    "OrganizationId"      uuid                      NULL,
    "CustomerId"          uuid                      NOT NULL,
    "OrderReference"      character varying(200)    NOT NULL,
    "IdempotencyKey"      character varying(200)    NOT NULL,
    "ProviderReference"   character varying(500)    NULL,
    "ProviderPaymentId"   character varying(500)    NULL,
    "Status"              integer                   NOT NULL,
    "PaymentMethod"       integer                   NOT NULL,
    "Currency"            character varying(3)      NOT NULL,
    "FeeAmount"           numeric(18,2)             NULL,
    "TaxAmount"           numeric(18,2)             NULL,
    "FailureReason"       character varying(1000)   NULL,
    "FailureCode"         character varying(100)    NULL,
    "Metadata"            text                      NULL,
    "ExpiresAtUtc"        timestamp with time zone  NOT NULL,
    "CreatedAtUtc"        timestamp with time zone  NOT NULL,
    "UpdatedAtUtc"        timestamp with time zone  NULL,
    "ProcessedAtUtc"      timestamp with time zone  NULL,
    "Amount"              numeric(18,2)             NOT NULL,
    CONSTRAINT "PK_payments" PRIMARY KEY ("Id")
);

CREATE INDEX IF NOT EXISTS "IX_payments_CreatedAtUtc"       ON payment.payments ("CreatedAtUtc");
CREATE INDEX IF NOT EXISTS "IX_payments_CustomerId"         ON payment.payments ("CustomerId");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_payments_IdempotencyKey" ON payment.payments ("IdempotencyKey");
CREATE INDEX IF NOT EXISTS "IX_payments_OrderReference"     ON payment.payments ("OrderReference");
CREATE INDEX IF NOT EXISTS "IX_payments_ProviderPaymentId"  ON payment.payments ("ProviderPaymentId");
CREATE INDEX IF NOT EXISTS "IX_payments_Status"             ON payment.payments ("Status");
CREATE INDEX IF NOT EXISTS "IX_payments_TenantId"           ON payment.payments ("TenantId");

-- -----------------------------------------------------------------------------
-- payment_refunds — child of payments, cascade delete
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS payment.payment_refunds (
    "Id"                       uuid                      NOT NULL,
    "PaymentId"                uuid                      NOT NULL,
    "TenantId"                 uuid                      NOT NULL,
    "Amount"                   numeric(18,2)             NOT NULL,
    "Currency"                 character varying(3)      NOT NULL,
    "Reason"                   character varying(500)    NOT NULL,
    "ProviderRefundReference"  character varying(500)    NULL,
    "Status"                   integer                   NOT NULL,
    "FailureReason"            character varying(1000)   NULL,
    "FailureCode"              character varying(100)    NULL,
    "InitiatedByUserId"        character varying(200)    NULL,
    "CreatedAtUtc"             timestamp with time zone  NOT NULL,
    "UpdatedAtUtc"             timestamp with time zone  NULL,
    "ProcessedAtUtc"           timestamp with time zone  NULL,
    CONSTRAINT "PK_payment_refunds" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_payment_refunds_payments_PaymentId"
        FOREIGN KEY ("PaymentId") REFERENCES payment.payments ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_payment_refunds_CreatedAtUtc" ON payment.payment_refunds ("CreatedAtUtc");
CREATE INDEX IF NOT EXISTS "IX_payment_refunds_PaymentId"    ON payment.payment_refunds ("PaymentId");
CREATE INDEX IF NOT EXISTS "IX_payment_refunds_Status"       ON payment.payment_refunds ("Status");
CREATE INDEX IF NOT EXISTS "IX_payment_refunds_TenantId"     ON payment.payment_refunds ("TenantId");

-- -----------------------------------------------------------------------------
-- agent_payment_methods — added in AddAgentPaymentMethod migration
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS payment.agent_payment_methods (
    "Id"                  uuid                      NOT NULL,
    "AgentId"              uuid                      NOT NULL,
    "MethodType"           integer                   NOT NULL,
    "Provider"             character varying(50)     NOT NULL,
    "AccountNumber"        character varying(100)    NOT NULL,
    "AccountName"          character varying(200)    NULL,
    "IsDefault"            boolean                   NOT NULL,
    "IsVerified"           boolean                   NOT NULL,
    "VerificationToken"    character varying(200)    NULL,
    "Metadata"             text                      NULL,
    "CreatedAtUtc"         timestamp with time zone  NOT NULL,
    "UpdatedAtUtc"         timestamp with time zone  NULL,
    CONSTRAINT "PK_agent_payment_methods" PRIMARY KEY ("Id")
);

CREATE INDEX IF NOT EXISTS "IX_agent_payment_methods_AgentId"    ON payment.agent_payment_methods ("AgentId");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_agent_payment_methods_AgentId_Provider_AccountNumber"
    ON payment.agent_payment_methods ("AgentId", "Provider", "AccountNumber");
CREATE INDEX IF NOT EXISTS "IX_agent_payment_methods_IsVerified" ON payment.agent_payment_methods ("IsVerified");
CREATE INDEX IF NOT EXISTS "IX_agent_payment_methods_Provider"   ON payment.agent_payment_methods ("Provider");
