-- =============================================================================
-- Payment Service — Trigger Scripts
--
-- STATUS: supplemental / optional DBA-maintained scripts.
-- These are NOT part of the EF Core migration history (no C# migration adds
-- them) and `dotnet ef database update` will never apply or remove them.
-- The application does not currently depend on any of these triggers to
-- function correctly — UpdatedAtUtc, for example, is already set from
-- application code (see PaymentDbContext SaveChanges interceptor / entity
-- setters). They exist as defense-in-depth for the case where a row is
-- modified outside the application (manual DBA fix, a future ETL job, etc.)
-- and are safe to apply or skip.
--
-- Apply manually against the "payment" schema after schema-scripts.sql:
--   psql "$PAYMENT_DB_CONNECTION_STRING" -f triggers-scripts.sql
-- =============================================================================

-- -----------------------------------------------------------------------------
-- Generic "touch UpdatedAtUtc on any UPDATE" function + triggers.
-- Belt-and-braces only: if application code already set UpdatedAtUtc in the
-- same statement, this trigger just re-confirms the same value's freshness.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION payment.fn_touch_updated_at_utc()
RETURNS trigger AS $$
BEGIN
    NEW."UpdatedAtUtc" := now();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_payments_touch_updated_at ON payment.payments;
CREATE TRIGGER trg_payments_touch_updated_at
    BEFORE UPDATE ON payment.payments
    FOR EACH ROW
    EXECUTE FUNCTION payment.fn_touch_updated_at_utc();

DROP TRIGGER IF EXISTS trg_payment_refunds_touch_updated_at ON payment.payment_refunds;
CREATE TRIGGER trg_payment_refunds_touch_updated_at
    BEFORE UPDATE ON payment.payment_refunds
    FOR EACH ROW
    EXECUTE FUNCTION payment.fn_touch_updated_at_utc();

DROP TRIGGER IF EXISTS trg_agent_payment_methods_touch_updated_at ON payment.agent_payment_methods;
CREATE TRIGGER trg_agent_payment_methods_touch_updated_at
    BEFORE UPDATE ON payment.agent_payment_methods
    FOR EACH ROW
    EXECUTE FUNCTION payment.fn_touch_updated_at_utc();

-- -----------------------------------------------------------------------------
-- Guard trigger: a payment_refunds row can never push a payment's total
-- refunded amount above that payment's Amount. The application already
-- enforces this in RefundPaymentHandler via availableRefundAmount — this is
-- a second, DB-level line of defense against a refund inserted by anything
-- that bypasses the application (manual SQL, a future batch job, etc.).
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION payment.fn_guard_refund_does_not_exceed_payment()
RETURNS trigger AS $$
DECLARE
    v_payment_amount   numeric(18,2);
    v_already_refunded numeric(18,2);
BEGIN
    SELECT "Amount" INTO v_payment_amount
    FROM payment.payments
    WHERE "Id" = NEW."PaymentId";

    SELECT COALESCE(SUM("Amount"), 0) INTO v_already_refunded
    FROM payment.payment_refunds
    WHERE "PaymentId" = NEW."PaymentId"
      AND "Id" <> NEW."Id"
      AND "Status" <> 3; -- 3 = Failed (see PaymentService.Domain.Enums.RefundStatus) — failed refunds don't count against the balance

    IF v_already_refunded + NEW."Amount" > v_payment_amount THEN
        RAISE EXCEPTION 'Refund total (%) would exceed payment amount (%) for PaymentId %',
            v_already_refunded + NEW."Amount", v_payment_amount, NEW."PaymentId";
    END IF;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_guard_refund_does_not_exceed_payment ON payment.payment_refunds;
CREATE TRIGGER trg_guard_refund_does_not_exceed_payment
    BEFORE INSERT OR UPDATE ON payment.payment_refunds
    FOR EACH ROW
    EXECUTE FUNCTION payment.fn_guard_refund_does_not_exceed_payment();
