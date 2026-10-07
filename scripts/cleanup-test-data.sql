-- Removes test orders (and what hangs off them) before going live.  PostgreSQL.
--
-- READ FIRST
--   1. Make a backup:   pg_dump -Fc -h <host> -U <user> -d <database> -f backup-before-cleanup.dump
--   2. Run this whole file once as it is. It ends with ROLLBACK, so NOTHING is changed: it only shows counts.
--   3. If the counts look right, change the last line from ROLLBACK to COMMIT and run it again.
--
-- This deletes ALL orders. To keep real orders and only remove older test ones, replace each
-- `DELETE FROM "Orders";`-style filter below with a date cut-off, for example:
--     WHERE "CreatedAt" < TIMESTAMPTZ '2026-10-15 00:00:00+00'
--
-- It does NOT touch products, colours, promo codes, settings or customer accounts (people who signed up).

BEGIN;

SELECT 'orders' AS what, COUNT(*) FROM "Orders"
UNION ALL SELECT 'order items', COUNT(*) FROM "OrderItems"
UNION ALL SELECT 'payment receipts', COUNT(*) FROM "PaymentReceipts"
UNION ALL SELECT 'promo uses', COUNT(*) FROM "PromoRedemptions";

-- Promo uses first: deleting them makes each promo code usable again for those customers.
DELETE FROM "PromoRedemptions";
DELETE FROM "PaymentReceipts";
DELETE FROM "OrderItems";
DELETE FROM "Orders";

-- Guest customers (never created an account) who no longer have any order or review.
DELETE FROM "Customers" c
WHERE c."PasswordHash" IS NULL
  AND NOT EXISTS (SELECT 1 FROM "Orders" o WHERE o."CustomerId" = c."Id")
  AND NOT EXISTS (SELECT 1 FROM "Reviews" r WHERE r."CustomerId" = c."Id")
  AND NOT EXISTS (SELECT 1 FROM "PromoRedemptions" pr WHERE pr."CustomerId" = c."Id");

-- OPTIONAL: test reviews and "remind me" sign-ups. Remove the two leading dashes to run them.
-- DELETE FROM "Reviews";
-- DELETE FROM "StockReminders";

SELECT 'orders left' AS what, COUNT(*) FROM "Orders"
UNION ALL SELECT 'customers left', COUNT(*) FROM "Customers";

-- Change ROLLBACK to COMMIT only after you have checked the numbers above.
ROLLBACK;

-- AFTER COMMIT:
--   * The units your test orders took out of stock are NOT put back. Enter the real stock in
--     Admin > Products > (product) > Stock by size.
--   * Delete the old test receipt images by hand:  src/ECommerceStore.Web/wwwroot/uploads/receipts/
