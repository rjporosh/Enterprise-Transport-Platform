-- =============================================================================
-- Payment Service — Function Scripts
--
-- STATUS: supplemental / optional ops & reporting functions.
-- Like triggers-scripts.sql, these are NOT part of the EF Core migration
-- history and are never applied by `dotnet ef database update`. They exist
-- for DBA/ops use (dashboards, cleanup jobs, ad-hoc reconciliation queries)
-- against the schema created by schema-scripts.sql.
--
-- Apply manually:
--   psql "$PAYMENT_DB_CONNECTION_STRING" -f functions-script.sql
-- =============================================================================

-- -----------------------------------------------------------------------------
-- payment.fn_available_refund_amount(payment_id)
-- Mirrors the AvailableRefundAmount computed field already exposed in
-- PaymentDto (Application layer) — provided here so ops/BI can compute the
-- same figure directly in SQL (e.g. a finance dashboard) without going
-- through the API. Keep this formula in sync if the application-layer
-- calculation ever changes.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION payment.fn_available_refund_amount(p_payment_id uuid)
RETURNS numeric(18,2) AS $$
DECLARE
    v_amount           numeric(18,2);
    v_already_refunded numeric(18,2);
BEGIN
    SELECT "Amount" INTO v_amount
    FROM payment.payments
    WHERE "Id" = p_payment_id;

    IF NOT FOUND THEN
        RETURN NULL;
    END IF;

    SELECT COALESCE(SUM("Amount"), 0) INTO v_already_refunded
    FROM payment.payment_refunds
    WHERE "PaymentId" = p_payment_id
      AND "Status" <> 3; -- exclude Failed refunds

    RETURN v_amount - v_already_refunded;
END;
$$ LANGUAGE plpgsql STABLE;

-- -----------------------------------------------------------------------------
-- payment.fn_outbox_dead_letter_count(max_retries)
-- Counts outbox_messages that have exhausted retries and never processed —
-- intended for an alerting/health-dashboard query, not application code.
-- Default threshold of 5 matches the retry ceiling documented in
-- docs/programmers-guide/adr/0002-transactional-outbox.md; pass an explicit
-- value if that ceiling is ever changed there.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION payment.fn_outbox_dead_letter_count(p_max_retries integer DEFAULT 5)
RETURNS bigint AS $$
    SELECT COUNT(*)
    FROM payment.outbox_messages
    WHERE "ProcessedOnUtc" IS NULL
      AND "RetryCount" >= p_max_retries;
$$ LANGUAGE sql STABLE;

-- -----------------------------------------------------------------------------
-- payment.fn_purge_processed_outbox(older_than_days)
-- Deletes successfully processed outbox rows older than N days. Returns the
-- number of rows deleted. Intended to be run from a scheduled ops job (e.g.
-- a monthly maintenance window), NOT from application code — the app only
-- ever inserts/reads outbox_messages, it never purges them.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION payment.fn_purge_processed_outbox(p_older_than_days integer DEFAULT 90)
RETURNS bigint AS $$
DECLARE
    v_deleted bigint;
BEGIN
    DELETE FROM payment.outbox_messages
    WHERE "ProcessedOnUtc" IS NOT NULL
      AND "ProcessedOnUtc" < now() - (p_older_than_days || ' days')::interval;

    GET DIAGNOSTICS v_deleted = ROW_COUNT;
    RETURN v_deleted;
END;
$$ LANGUAGE plpgsql;
