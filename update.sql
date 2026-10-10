CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928161648_InitialCreate') THEN
    CREATE TABLE "Customers" (
        "Id" uuid NOT NULL,
        "FullName" character varying(150) NOT NULL,
        "PhoneNumber" character varying(20) NOT NULL,
        "Email" character varying(200),
        CONSTRAINT "PK_Customers" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928161648_InitialCreate') THEN
    CREATE TABLE "Products" (
        "Id" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Description" character varying(4000) NOT NULL,
        "Price" numeric(18,2) NOT NULL,
        "StockQuantity" integer NOT NULL DEFAULT 0,
        "ImageUrl" character varying(500) NOT NULL,
        "IsActive" boolean NOT NULL DEFAULT TRUE,
        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (timezone('utc', now())),
        CONSTRAINT "PK_Products" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928161648_InitialCreate') THEN
    CREATE TABLE "Orders" (
        "Id" uuid NOT NULL,
        "OrderNumber" character varying(50) NOT NULL,
        "CustomerId" uuid NOT NULL,
        "TotalAmount" numeric(18,2) NOT NULL,
        "OrderStatus" character varying(30) NOT NULL,
        "PaymentMethod" character varying(30) NOT NULL,
        "Latitude" double precision NOT NULL,
        "Longitude" double precision NOT NULL,
        "AddressDetail" character varying(500) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (timezone('utc', now())),
        CONSTRAINT "PK_Orders" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Orders_Customers_CustomerId" FOREIGN KEY ("CustomerId") REFERENCES "Customers" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928161648_InitialCreate') THEN
    CREATE TABLE "OrderItems" (
        "Id" uuid NOT NULL,
        "OrderId" uuid NOT NULL,
        "ProductId" uuid NOT NULL,
        "Quantity" integer NOT NULL,
        "UnitPrice" numeric(18,2) NOT NULL,
        CONSTRAINT "PK_OrderItems" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_OrderItems_Orders_OrderId" FOREIGN KEY ("OrderId") REFERENCES "Orders" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_OrderItems_Products_ProductId" FOREIGN KEY ("ProductId") REFERENCES "Products" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928161648_InitialCreate') THEN
    CREATE TABLE "PaymentReceipts" (
        "Id" uuid NOT NULL,
        "OrderId" uuid NOT NULL,
        "ImagePath" character varying(500) NOT NULL,
        "TransactionReference" character varying(100),
        "UploadedAt" timestamp with time zone NOT NULL DEFAULT (timezone('utc', now())),
        "IsVerified" boolean NOT NULL DEFAULT FALSE,
        CONSTRAINT "PK_PaymentReceipts" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_PaymentReceipts_Orders_OrderId" FOREIGN KEY ("OrderId") REFERENCES "Orders" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928161648_InitialCreate') THEN
    CREATE INDEX "IX_Customers_PhoneNumber" ON "Customers" ("PhoneNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928161648_InitialCreate') THEN
    CREATE INDEX "IX_OrderItems_OrderId" ON "OrderItems" ("OrderId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928161648_InitialCreate') THEN
    CREATE INDEX "IX_OrderItems_ProductId" ON "OrderItems" ("ProductId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928161648_InitialCreate') THEN
    CREATE INDEX "IX_Orders_CustomerId" ON "Orders" ("CustomerId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928161648_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_Orders_OrderNumber" ON "Orders" ("OrderNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928161648_InitialCreate') THEN
    CREATE INDEX "IX_Orders_OrderStatus" ON "Orders" ("OrderStatus");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928161648_InitialCreate') THEN
    CREATE INDEX "IX_PaymentReceipts_OrderId" ON "PaymentReceipts" ("OrderId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928161648_InitialCreate') THEN
    CREATE INDEX "IX_Products_IsActive" ON "Products" ("IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928161648_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260928161648_InitialCreate', '8.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260930132944_AddProductColors') THEN
    ALTER TABLE "OrderItems" ADD "ColorName" character varying(100);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260930132944_AddProductColors') THEN
    ALTER TABLE "OrderItems" ADD "ProductColorId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260930132944_AddProductColors') THEN
    CREATE TABLE "ProductColors" (
        "Id" uuid NOT NULL,
        "ProductId" uuid NOT NULL,
        "Name" character varying(100) NOT NULL,
        "HexCode" character varying(20) NOT NULL,
        "StockQuantity" integer NOT NULL DEFAULT 0,
        "IsActive" boolean NOT NULL DEFAULT TRUE,
        CONSTRAINT "PK_ProductColors" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_ProductColors_Products_ProductId" FOREIGN KEY ("ProductId") REFERENCES "Products" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260930132944_AddProductColors') THEN
    CREATE INDEX "IX_OrderItems_ProductColorId" ON "OrderItems" ("ProductColorId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260930132944_AddProductColors') THEN
    CREATE INDEX "IX_ProductColors_ProductId" ON "ProductColors" ("ProductId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260930132944_AddProductColors') THEN
    ALTER TABLE "OrderItems" ADD CONSTRAINT "FK_OrderItems_ProductColors_ProductColorId" FOREIGN KEY ("ProductColorId") REFERENCES "ProductColors" ("Id") ON DELETE SET NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260930132944_AddProductColors') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260930132944_AddProductColors', '8.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260930213958_AddGenderAndSizeToOrderItem') THEN
    ALTER TABLE "OrderItems" ADD "Gender" character varying(20);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260930213958_AddGenderAndSizeToOrderItem') THEN
    ALTER TABLE "OrderItems" ADD "Size" character varying(20);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260930213958_AddGenderAndSizeToOrderItem') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260930213958_AddGenderAndSizeToOrderItem', '8.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261002104303_AddProductImages') THEN
    CREATE TABLE "ProductImages" (
        "Id" uuid NOT NULL,
        "ProductId" uuid NOT NULL,
        "ImageUrl" character varying(500) NOT NULL,
        "SortOrder" integer NOT NULL DEFAULT 0,
        CONSTRAINT "PK_ProductImages" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_ProductImages_Products_ProductId" FOREIGN KEY ("ProductId") REFERENCES "Products" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261002104303_AddProductImages') THEN
    CREATE INDEX "IX_ProductImages_ProductId" ON "ProductImages" ("ProductId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261002104303_AddProductImages') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261002104303_AddProductImages', '8.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003013529_AccountsPromosStockAndShipping') THEN
    ALTER TABLE "Products" DROP COLUMN "StockQuantity";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003013529_AccountsPromosStockAndShipping') THEN
    ALTER TABLE "ProductColors" DROP COLUMN "StockQuantity";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003013529_AccountsPromosStockAndShipping') THEN
    ALTER TABLE "ProductColors" ADD "ImageUrl" character varying(500);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003013529_AccountsPromosStockAndShipping') THEN
    ALTER TABLE "Orders" ADD "DiscountAmount" numeric(18,2) NOT NULL DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003013529_AccountsPromosStockAndShipping') THEN
    ALTER TABLE "Orders" ADD "PromoCodeText" character varying(50);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003013529_AccountsPromosStockAndShipping') THEN
    ALTER TABLE "Orders" ADD "ShippingAmount" numeric(18,2) NOT NULL DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003013529_AccountsPromosStockAndShipping') THEN
    ALTER TABLE "Orders" ADD "SubtotalAmount" numeric(18,2) NOT NULL DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003013529_AccountsPromosStockAndShipping') THEN
    ALTER TABLE "Customers" ADD "PasswordHash" character varying(300);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003013529_AccountsPromosStockAndShipping') THEN
    CREATE TABLE "ProductStocks" (
        "Id" uuid NOT NULL,
        "ProductId" uuid NOT NULL,
        "ProductColorId" uuid,
        "Size" character varying(10) NOT NULL,
        "Quantity" integer NOT NULL DEFAULT 0,
        CONSTRAINT "PK_ProductStocks" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_ProductStocks_ProductColors_ProductColorId" FOREIGN KEY ("ProductColorId") REFERENCES "ProductColors" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_ProductStocks_Products_ProductId" FOREIGN KEY ("ProductId") REFERENCES "Products" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003013529_AccountsPromosStockAndShipping') THEN
    CREATE TABLE "PromoCodes" (
        "Id" uuid NOT NULL,
        "Code" character varying(50) NOT NULL,
        "PercentOff" integer,
        "FreeShipping" boolean NOT NULL,
        "IsActive" boolean NOT NULL DEFAULT TRUE,
        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (timezone('utc', now())),
        CONSTRAINT "PK_PromoCodes" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003013529_AccountsPromosStockAndShipping') THEN
    CREATE TABLE "StoreSettings" (
        "Id" integer NOT NULL,
        "ShippingFee" numeric(18,2) NOT NULL,
        CONSTRAINT "PK_StoreSettings" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003013529_AccountsPromosStockAndShipping') THEN
    CREATE TABLE "PromoRedemptions" (
        "Id" uuid NOT NULL,
        "PromoCodeId" uuid NOT NULL,
        "CustomerId" uuid NOT NULL,
        "OrderId" uuid NOT NULL,
        "RedeemedAt" timestamp with time zone NOT NULL DEFAULT (timezone('utc', now())),
        CONSTRAINT "PK_PromoRedemptions" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_PromoRedemptions_Customers_CustomerId" FOREIGN KEY ("CustomerId") REFERENCES "Customers" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_PromoRedemptions_Orders_OrderId" FOREIGN KEY ("OrderId") REFERENCES "Orders" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_PromoRedemptions_PromoCodes_PromoCodeId" FOREIGN KEY ("PromoCodeId") REFERENCES "PromoCodes" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003013529_AccountsPromosStockAndShipping') THEN
    CREATE UNIQUE INDEX "IX_Customers_Email" ON "Customers" ("Email") WHERE "PasswordHash" IS NOT NULL AND "Email" IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003013529_AccountsPromosStockAndShipping') THEN
    CREATE INDEX "IX_ProductStocks_ProductColorId" ON "ProductStocks" ("ProductColorId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003013529_AccountsPromosStockAndShipping') THEN
    CREATE INDEX "IX_ProductStocks_ProductId_ProductColorId_Size" ON "ProductStocks" ("ProductId", "ProductColorId", "Size");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003013529_AccountsPromosStockAndShipping') THEN
    CREATE UNIQUE INDEX "IX_PromoCodes_Code" ON "PromoCodes" ("Code");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003013529_AccountsPromosStockAndShipping') THEN
    CREATE INDEX "IX_PromoRedemptions_CustomerId" ON "PromoRedemptions" ("CustomerId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003013529_AccountsPromosStockAndShipping') THEN
    CREATE INDEX "IX_PromoRedemptions_OrderId" ON "PromoRedemptions" ("OrderId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003013529_AccountsPromosStockAndShipping') THEN
    CREATE UNIQUE INDEX "IX_PromoRedemptions_PromoCodeId_CustomerId" ON "PromoRedemptions" ("PromoCodeId", "CustomerId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003013529_AccountsPromosStockAndShipping') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261003013529_AccountsPromosStockAndShipping', '8.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261004013639_ProductCatalog') THEN
    ALTER TABLE "Products" ADD "ShortDescription" character varying(200);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261004013639_ProductCatalog') THEN
    ALTER TABLE "Products" ADD "Slug" character varying(120) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261004013639_ProductCatalog') THEN
    ALTER TABLE "Products" ADD "SortOrder" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261004013639_ProductCatalog') THEN
    CREATE UNIQUE INDEX "IX_Products_Slug" ON "Products" ("Slug");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261004013639_ProductCatalog') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261004013639_ProductCatalog', '8.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005053423_ReviewsRemindersDiscountsAddress') THEN
    ALTER TABLE "Products" ADD "DiscountActive" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005053423_ReviewsRemindersDiscountsAddress') THEN
    ALTER TABLE "Products" ADD "DiscountPercent" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005053423_ReviewsRemindersDiscountsAddress') THEN
    ALTER TABLE "Orders" ADD "Area" character varying(100) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005053423_ReviewsRemindersDiscountsAddress') THEN
    ALTER TABLE "Orders" ADD "Governorate" character varying(50) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005053423_ReviewsRemindersDiscountsAddress') THEN
    CREATE TABLE "Reviews" (
        "Id" uuid NOT NULL,
        "ProductId" uuid NOT NULL,
        "CustomerId" uuid NOT NULL,
        "Rating" integer NOT NULL,
        "Comment" character varying(1000),
        "IsAnonymous" boolean NOT NULL,
        "AuthorName" character varying(150) NOT NULL,
        "IsHidden" boolean NOT NULL DEFAULT FALSE,
        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (timezone('utc', now())),
        CONSTRAINT "PK_Reviews" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Reviews_Customers_CustomerId" FOREIGN KEY ("CustomerId") REFERENCES "Customers" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Reviews_Products_ProductId" FOREIGN KEY ("ProductId") REFERENCES "Products" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005053423_ReviewsRemindersDiscountsAddress') THEN
    CREATE TABLE "StockReminders" (
        "Id" uuid NOT NULL,
        "ProductId" uuid NOT NULL,
        "ProductColorId" uuid,
        "Size" character varying(10) NOT NULL,
        "Email" character varying(254) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (timezone('utc', now())),
        "NotifiedAt" timestamp with time zone,
        CONSTRAINT "PK_StockReminders" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_StockReminders_ProductColors_ProductColorId" FOREIGN KEY ("ProductColorId") REFERENCES "ProductColors" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_StockReminders_Products_ProductId" FOREIGN KEY ("ProductId") REFERENCES "Products" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005053423_ReviewsRemindersDiscountsAddress') THEN
    CREATE INDEX "IX_Reviews_CustomerId" ON "Reviews" ("CustomerId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005053423_ReviewsRemindersDiscountsAddress') THEN
    CREATE UNIQUE INDEX "IX_Reviews_ProductId_CustomerId" ON "Reviews" ("ProductId", "CustomerId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005053423_ReviewsRemindersDiscountsAddress') THEN
    CREATE INDEX "IX_StockReminders_ProductColorId" ON "StockReminders" ("ProductColorId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005053423_ReviewsRemindersDiscountsAddress') THEN
    CREATE INDEX "IX_StockReminders_ProductId_ProductColorId_Size" ON "StockReminders" ("ProductId", "ProductColorId", "Size");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005053423_ReviewsRemindersDiscountsAddress') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261005053423_ReviewsRemindersDiscountsAddress', '8.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006045423_OrderStreetAndBuilding') THEN
    ALTER TABLE "Orders" ADD "BuildingNumber" character varying(30) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006045423_OrderStreetAndBuilding') THEN
    ALTER TABLE "Orders" ADD "Street" character varying(150) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006045423_OrderStreetAndBuilding') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261006045423_OrderStreetAndBuilding', '8.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261009125350_ReceiptRetention') THEN
    ALTER TABLE "StoreSettings" ADD "ReceiptRetentionDays" integer NOT NULL DEFAULT 60;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261009125350_ReceiptRetention') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261009125350_ReceiptRetention', '8.0.10');
    END IF;
END $EF$;
COMMIT;

