CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;
CREATE TABLE "Bars" (
    "Id" uuid NOT NULL,
    "State" integer NOT NULL,
    CONSTRAINT "PK_Bars" PRIMARY KEY ("Id")
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251011104826_InitialCreate', '9.0.10');

ALTER TABLE "Bars" ADD "CloseAt" interval NOT NULL DEFAULT INTERVAL '00:00:00';

ALTER TABLE "Bars" ADD "Name" text NOT NULL DEFAULT '';

ALTER TABLE "Bars" ADD "OpenAt" interval NOT NULL DEFAULT INTERVAL '00:00:00';

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251015073533_AddBarFields', '9.0.10');

COMMIT;

