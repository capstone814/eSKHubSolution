-- PostgreSQL Migration Script for Neon DB (eSKHub)
-- Created automatically without binary blobs to avoid size limits

-- ========================================================
-- 1. SCHEMAS / TABLES CREATION
-- ========================================================

CREATE TABLE IF NOT EXISTS "Areas" (
    "Id" SERIAL PRIMARY KEY,
    "Name" VARCHAR(200) NOT NULL,
    "Type" VARCHAR(50) NOT NULL CHECK ("Type" IN ('Barangay', 'Municipality'))
);

CREATE TABLE IF NOT EXISTS "Users" (
    "Id" SERIAL PRIMARY KEY,
    "Username" VARCHAR(100) NOT NULL,
    "Password" VARCHAR(100) NOT NULL,
    "Image" VARCHAR(100) NULL,
    "Email" VARCHAR(100) NULL,
    "Role" VARCHAR(100) NULL,
    "Fullname" VARCHAR(200) NOT NULL,
    "Position" VARCHAR(100) NOT NULL,
    "Area" VARCHAR(100) NOT NULL,
    "ImageData" BYTEA NULL,
    "ImageMimeType" VARCHAR(50) NULL
);

CREATE TABLE IF NOT EXISTS "Announcements" (
    "Id" SERIAL PRIMARY KEY,
    "Subject" VARCHAR(500) NOT NULL,
    "Body" TEXT NOT NULL,
    "Attachment" VARCHAR(500) NULL,
    "AttachmentType" VARCHAR(50) NULL,
    "DateCreated" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "DateModified" TIMESTAMP NULL,
    "IsPublished" BOOLEAN NOT NULL DEFAULT FALSE,
    "Author" VARCHAR(200) NULL,
    "Audience" VARCHAR(50) NULL,
    "TargetBarangay" VARCHAR(100) NULL,
    "AttachmentData" BYTEA NULL,
    "AttachmentMimeType" VARCHAR(100) NULL,
    "ShortDescription" VARCHAR(160) NULL,
    "AuthorArea" VARCHAR(160) NULL,
    "AuthorPosition" VARCHAR(160) NULL
);

CREATE TABLE IF NOT EXISTS "Events" (
    "Id" SERIAL PRIMARY KEY,
    "Title" VARCHAR(200) NOT NULL,
    "Description" TEXT NULL,
    "EventDate" DATE NOT NULL,
    "StartTime" TIME NULL,
    "EndTime" TIME NULL,
    "Location" VARCHAR(300) NULL,
    "EventType" VARCHAR(50) NOT NULL,
    "Organizer" VARCHAR(200) NULL,
    "OrganizerPosition" VARCHAR(200) NULL,
    "OrganizerArea" VARCHAR(200) NULL,
    "DateCreated" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "DateModified" TIMESTAMP NULL,
    "IsPublished" BOOLEAN NOT NULL DEFAULT FALSE,
    "Audience" VARCHAR(50) NULL,
    "TargetBarangay" VARCHAR(200) NULL,
    "Notes" TEXT NULL
);

CREATE TABLE IF NOT EXISTS "NewsArticles" (
    "Id" SERIAL PRIMARY KEY,
    "Title" VARCHAR(255) NOT NULL,
    "Summary" VARCHAR(160) NOT NULL,
    "Content" TEXT NOT NULL,
    "NewsType" VARCHAR(50) NOT NULL,
    "Attachment" VARCHAR(255) NULL,
    "AttachmentType" VARCHAR(50) NULL,
    "AttachmentData" BYTEA NULL,
    "AttachmentMimeType" VARCHAR(100) NULL,
    "DateCreated" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "DateModified" TIMESTAMP NULL,
    "IsPublished" BOOLEAN NOT NULL DEFAULT FALSE,
    "Author" VARCHAR(255) NULL,
    "AuthorArea" VARCHAR(255) NULL,
    "AuthorPosition" VARCHAR(255) NULL
);

CREATE TABLE IF NOT EXISTS "ProgramsProjects" (
    "Id" SERIAL PRIMARY KEY,
    "Title" VARCHAR(200) NOT NULL,
    "Description" TEXT NOT NULL,
    "Objectives" TEXT NOT NULL,
    "TargetBeneficiaries" VARCHAR(500) NOT NULL,
    "ExpectedOutcomes" TEXT NOT NULL,
    "Budget" NUMERIC(18, 2) NOT NULL,
    "Timeline" VARCHAR(200) NOT NULL,
    "StartDate" TIMESTAMP NOT NULL,
    "EndDate" TIMESTAMP NOT NULL,
    "Category" VARCHAR(100) NOT NULL,
    "Venue" VARCHAR(300) NOT NULL,
    "Partners" TEXT NULL,
    "AttachmentType" VARCHAR(50) NULL,
    "AttachmentData" BYTEA NULL,
    "AttachmentMimeType" VARCHAR(100) NULL,
    "Attachment" VARCHAR(255) NULL,
    "DateCreated" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "DateModified" TIMESTAMP NULL,
    "ProposedBy" VARCHAR(200) NOT NULL,
    "ProposerPosition" VARCHAR(200) NOT NULL,
    "ProposerArea" VARCHAR(200) NOT NULL,
    "Status" VARCHAR(50) NOT NULL DEFAULT 'Pending',
    "AdminFeedback" TEXT NULL,
    "ReviewedBy" VARCHAR(200) NULL,
    "ReviewedDate" TIMESTAMP NULL,
    "IsPublished" BOOLEAN NOT NULL DEFAULT FALSE,
    "Audience" VARCHAR(50) NOT NULL DEFAULT 'Public',
    "TargetBarangay" VARCHAR(200) NULL,
    "EditAccessRequested" BOOLEAN NOT NULL DEFAULT FALSE,
    "EditAccessRequestedDate" TIMESTAMP NULL,
    "EditAccessGranted" BOOLEAN NOT NULL DEFAULT FALSE,
    "EditAccessGrantedDate" TIMESTAMP NULL,
    "EditAccessGrantedBy" VARCHAR(200) NULL
);

CREATE TABLE IF NOT EXISTS "Youths" (
    "Id" SERIAL PRIMARY KEY,
    "FirstName" VARCHAR(100) NOT NULL,
    "MiddleName" VARCHAR(100) NULL,
    "LastName" VARCHAR(100) NOT NULL,
    "DateOfBirth" TIMESTAMP NOT NULL,
    "Gender" VARCHAR(20) NOT NULL,
    "CivilStatus" VARCHAR(20) NOT NULL,
    "Email" VARCHAR(255) NOT NULL,
    "ContactNumber" VARCHAR(50) NOT NULL,
    "Barangay" VARCHAR(100) NOT NULL,
    "Address" VARCHAR(500) NOT NULL,
    "EducationalAttainment" VARCHAR(50) NULL,
    "CurrentSchool" VARCHAR(255) NULL,
    "Occupation" VARCHAR(255) NULL,
    "IsVoter" BOOLEAN NOT NULL DEFAULT FALSE,
    "IsSKVoter" BOOLEAN NOT NULL DEFAULT FALSE,
    "Skills" TEXT NULL,
    "Interests" TEXT NULL,
    "WillingToVolunteer" BOOLEAN NOT NULL DEFAULT FALSE,
    "ImageData" BYTEA NULL,
    "ImageMimeType" VARCHAR(100) NULL,
    "DateRegistered" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "DateModified" TIMESTAMP NULL,
    "Status" VARCHAR(20) NOT NULL DEFAULT 'Active'
);

CREATE TABLE IF NOT EXISTS "ProgramParticipations" (
    "Id" SERIAL PRIMARY KEY,
    "ProgramId" INT NOT NULL REFERENCES "ProgramsProjects"("Id") ON DELETE CASCADE,
    "YouthId" INT NOT NULL REFERENCES "Youths"("Id") ON DELETE CASCADE,
    "YouthName" TEXT NOT NULL,
    "YouthEmail" TEXT NOT NULL,
    "YouthContact" TEXT NULL,
    "YouthBarangay" TEXT NOT NULL,
    "DateRegistered" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "Status" TEXT NOT NULL,
    "Notes" TEXT NULL
);

CREATE TABLE IF NOT EXISTS "ChatMessages" (
    "Id" SERIAL PRIMARY KEY,
    "SenderUsername" TEXT NOT NULL,
    "ReceiverUsername" TEXT NOT NULL,
    "Message" TEXT NOT NULL,
    "Timestamp" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "IsRead" BOOLEAN NOT NULL DEFAULT FALSE,
    "ReadAt" TIMESTAMP NULL
);

CREATE TABLE IF NOT EXISTS "Feedbacks" (
    "Id" SERIAL PRIMARY KEY,
    "Name" VARCHAR(100) NOT NULL,
    "Email" VARCHAR(100) NOT NULL,
    "ContactNumber" VARCHAR(20) NULL,
    "Subject" VARCHAR(100) NOT NULL,
    "Message" TEXT NOT NULL,
    "Category" VARCHAR(50) NOT NULL,
    "DateSubmitted" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "IsRead" BOOLEAN NOT NULL DEFAULT FALSE,
    "AdminNotes" TEXT NULL,
    "DateRead" TIMESTAMP NULL,
    "IsForwarded" BOOLEAN NOT NULL DEFAULT FALSE,
    "ForwardedToBarangay" VARCHAR(255) NULL,
    "ForwardedDate" TIMESTAMP NULL,
    "IsForwardedSolved" BOOLEAN NOT NULL DEFAULT FALSE,
    "DateForwardedSolved" TIMESTAMP NULL,
    "ForwardedToBarangays" TEXT NULL,
    "ReadByUsers" TEXT NULL,
    "SolvedByBarangays" TEXT NULL
);

CREATE TABLE IF NOT EXISTS "PageContents" (
    "Id" SERIAL PRIMARY KEY,
    "PageName" VARCHAR(50) NOT NULL,
    "Content" TEXT NOT NULL,
    "DateModified" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "ModifiedBy" TEXT NULL
);

CREATE TABLE IF NOT EXISTS "Polls" (
    "Id" SERIAL PRIMARY KEY,
    "Question" VARCHAR(200) NOT NULL,
    "Options" TEXT NOT NULL,
    "Votes" TEXT NULL,
    "DateCreated" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "DateExpires" TIMESTAMP NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedBy" VARCHAR(200) NOT NULL,
    "CreatorArea" VARCHAR(200) NOT NULL,
    "Audience" VARCHAR(50) NOT NULL DEFAULT 'Public',
    "TargetBarangay" VARCHAR(200) NULL,
    "AllowMultipleVotes" BOOLEAN NOT NULL DEFAULT FALSE,
    "VoterIds" TEXT NULL
);

CREATE TABLE IF NOT EXISTS "TransparencyDocuments" (
    "Id" SERIAL PRIMARY KEY,
    "Title" VARCHAR(200) NOT NULL,
    "Category" VARCHAR(50) NOT NULL,
    "Description" TEXT NULL,
    "FileData" BYTEA NULL,
    "FileMimeType" VARCHAR(100) NULL,
    "FileName" VARCHAR(200) NULL,
    "FiscalYear" INT NULL,
    "Quarter" VARCHAR(10) NULL,
    "DateUploaded" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UploadedBy" VARCHAR(100) NULL,
    "IsPublished" BOOLEAN NOT NULL DEFAULT FALSE,
    "ViewCount" INT DEFAULT 0,
    "DateModified" TIMESTAMP NULL
);

-- ========================================================
-- 2. INITIAL SEED DATA
-- ========================================================

-- Insert Announcements
INSERT INTO "Announcements" (
    "Id", "Subject", "Body", "Attachment", "AttachmentType", 
    "DateCreated", "DateModified", "IsPublished", "Author", "Audience"
) VALUES (
    16, 
    'SK Community Wellness Day Set for January 28', 
    'The SK Federation is inviting all youth and community members to join the upcoming Community Wellness Day happening on January 28, an initiative designed to promote healthier lifestyles and accessible health services across the barangay.', 
    'd4766485-4438-47b2-b739-25be7aebab01.png', 
    'image', 
    '2025-12-12 10:26:33.1259835', 
    '2025-12-16 16:13:04.7211573', 
    TRUE, 
    'John Vincent Bobier', 
    'Public'
) ON CONFLICT ("Id") DO NOTHING;

-- Insert Programs/Projects
INSERT INTO "ProgramsProjects" (
    "Id", "Title", "Description", "Objectives", "TargetBeneficiaries", 
    "ExpectedOutcomes", "Budget", "Timeline", "StartDate", "EndDate", 
    "Category", "Venue", "Partners", "AttachmentType", "AttachmentData", 
    "AttachmentMimeType", "Attachment", "DateCreated", "DateModified", 
    "ProposedBy", "ProposerPosition", "ProposerArea", "Status", "AdminFeedback", 
    "ReviewedBy", "ReviewedDate", "IsPublished", "Audience", "TargetBarangay", 
    "EditAccessRequested", "EditAccessRequestedDate", "EditAccessGranted", 
    "EditAccessGrantedDate", "EditAccessGrantedBy"
) VALUES (
    1, 
    'Youth Leadership and Digital Literacy Training Program 2026', 
    'A comprehensive 3-month training program designed to equip the youth of Barangay 1 - Poblacion with essential leadership skills and digital competencies.', 
    '1. Enhance leadership capabilities of at least 50 youth participants through structured workshops and mentorship programs', 
    'Out-of-school youth, working youth, and students aged 15-30 years old residing in Barangay 1 - Poblacion, Bacacay, Albay', 
    '1. At least 80% of participants will complete the full training program', 
    75000.00, 
    'March 2026 to May 2026 (3 months)', 
    '2026-03-01 00:00:00', 
    '2026-05-31 00:00:00', 
    'Youth Development', 
    'Barangay 1 - Poblacion Multi-Purpose Hall and SK Office, Bacacay, Albay', 
    'Department of Information and Communications Technology (DICT) Albay', 
    'document', 
    NULL, 
    NULL, 
    NULL, 
    CURRENT_TIMESTAMP, 
    NULL, 
    'John Vincent Bobier', 
    'SK Chairman', 
    'Barangay 1 - Poblacion', 
    'Pending', 
    NULL, 
    NULL, 
    NULL, 
    FALSE, 
    'Public', 
    NULL, 
    FALSE, 
    NULL, 
    FALSE, 
    NULL, 
    NULL
) ON CONFLICT ("Id") DO NOTHING;

-- Synchronize sequences
SELECT setval(pg_get_serial_sequence('"Announcements"', 'Id'), coalesce(max("Id"), 1)) FROM "Announcements";
SELECT setval(pg_get_serial_sequence('"ProgramsProjects"', 'Id'), coalesce(max("Id"), 1)) FROM "ProgramsProjects";
