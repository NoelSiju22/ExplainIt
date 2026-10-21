-- ExplainIt feature expansion. Run this once against the existing Supabase/PostgreSQL database.
ALTER TABLE "Documents" ADD COLUMN IF NOT EXISTS "Status" varchar(50) NOT NULL DEFAULT 'Needs attention';
ALTER TABLE "Documents" ADD COLUMN IF NOT EXISTS "SimpleSummary" varchar(4000);
ALTER TABLE "Documents" ADD COLUMN IF NOT EXISTS "WhatIsIt" varchar(4000);
ALTER TABLE "Documents" ADD COLUMN IF NOT EXISTS "WhyReceived" varchar(4000);
ALTER TABLE "Documents" ADD COLUMN IF NOT EXISTS "WhatToDo" varchar(4000);
ALTER TABLE "Documents" ADD COLUMN IF NOT EXISTS "Consequences" varchar(4000);
ALTER TABLE "Documents" ADD COLUMN IF NOT EXISTS "SourceText" text;
ALTER TABLE "Documents" ADD COLUMN IF NOT EXISTS "LastViewedAt" timestamptz;
ALTER TABLE "Documents" ADD COLUMN IF NOT EXISTS "DeletedAt" timestamptz;
CREATE TABLE IF NOT EXISTS "DocumentVersions" ("Id" uuid PRIMARY KEY,"DocumentId" uuid NOT NULL REFERENCES "Documents"("Id") ON DELETE CASCADE,"VersionNumber" integer NOT NULL,"Title" varchar(255) NOT NULL,"Description" varchar(2000),"FilePath" varchar(500) NOT NULL,"ChangeSummary" varchar(1000),"CreatedAt" timestamptz NOT NULL);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_DocumentVersions_DocumentId_VersionNumber" ON "DocumentVersions"("DocumentId","VersionNumber");
CREATE TABLE IF NOT EXISTS "DocumentTags" ("Id" uuid PRIMARY KEY,"DocumentId" uuid NOT NULL REFERENCES "Documents"("Id") ON DELETE CASCADE,"Name" varchar(80) NOT NULL);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_DocumentTags_DocumentId_Name" ON "DocumentTags"("DocumentId","Name");
CREATE TABLE IF NOT EXISTS "DocumentEvents" ("Id" uuid PRIMARY KEY,"DocumentId" uuid NOT NULL REFERENCES "Documents"("Id") ON DELETE CASCADE,"Type" varchar(80) NOT NULL,"Description" varchar(1000) NOT NULL,"CreatedAt" timestamptz NOT NULL);
CREATE INDEX IF NOT EXISTS "IX_DocumentEvents_DocumentId" ON "DocumentEvents"("DocumentId");
CREATE TABLE IF NOT EXISTS "DocumentReminders" ("Id" uuid PRIMARY KEY,"DocumentId" uuid NOT NULL REFERENCES "Documents"("Id") ON DELETE CASCADE,"DeadlineId" uuid,"ActionId" uuid,"Title" varchar(255) NOT NULL,"RemindAt" timestamptz NOT NULL,"IsRead" boolean NOT NULL DEFAULT false,"CreatedAt" timestamptz NOT NULL);
CREATE INDEX IF NOT EXISTS "IX_DocumentReminders_DocumentId_RemindAt" ON "DocumentReminders"("DocumentId","RemindAt");
CREATE TABLE IF NOT EXISTS "DocumentShares" ("Id" uuid PRIMARY KEY,"DocumentId" uuid NOT NULL REFERENCES "Documents"("Id") ON DELETE CASCADE,"Token" varchar(120) NOT NULL,"CreatedAt" timestamptz NOT NULL,"ExpiresAt" timestamptz,"Revoked" boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_DocumentShares_Token" ON "DocumentShares"("Token");
CREATE TABLE IF NOT EXISTS "DocumentQuestions" ("Id" uuid PRIMARY KEY,"DocumentId" uuid NOT NULL REFERENCES "Documents"("Id") ON DELETE CASCADE,"Question" varchar(2000) NOT NULL,"Answer" varchar(10000) NOT NULL,"Mode" varchar(30) NOT NULL,"CreatedAt" timestamptz NOT NULL);
CREATE INDEX IF NOT EXISTS "IX_DocumentQuestions_DocumentId" ON "DocumentQuestions"("DocumentId");

-- AI-guided document understanding: SourceText stores extracted PDF text for grounded analysis.
CREATE INDEX IF NOT EXISTS "IX_Documents_UserId_DeletedAt" ON "Documents"("UserId","DeletedAt");
