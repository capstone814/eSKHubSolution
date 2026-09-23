-- Complete PostgreSQL INSERT Query for ProgramsProjects (With PDF Hex Attachment Data)

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
    'A comprehensive 3-month training program designed to equip the youth of Barangay 1 - Poblacion with essential leadership skills and digital competencies. The program aims to bridge the digital divide and empower young residents aged 15-30 to become effective community leaders and digitally-literate citizens capable of contributing to local development initiatives.', 
    '1. Enhance leadership capabilities of at least 50 youth participants through structured workshops and mentorship programs
2. Provide basic to intermediate digital literacy training including computer operations, internet navigation, and social media management
3. Develop critical thinking and problem-solving skills among participants
4. Create a pool of young volunteers ready to assist in barangay programs and initiatives
5. Foster collaboration and networking among youth in the community', 
    'Out-of-school youth, working youth, and students aged 15-30 years old residing in Barangay 1 - Poblacion, Bacacay, Albay (Target: 50 participants per batch)', 
    '1. At least 80% of participants will complete the full training program
2. Participants will demonstrate improved leadership competencies through pre and post-assessment
3. All participants will gain basic computer literacy certification
4. Formation of a Youth Volunteer Corps with at least 30 active members
5. Establishment of a community learning hub for continued digital education
6. Participants will organize at least one community outreach project applying their learned skills', 
    75000.00, 
    'March 2026 to May 2026 (3 months)', 
    '2026-03-01 00:00:00', 
    '2026-05-31 00:00:00', 
    'Youth Development', 
    'Barangay 1 - Poblacion Multi-Purpose Hall and SK Office, Bacacay, Albay', 
    'Department of Information and Communications Technology (DICT) Albay, Bacacay Municipal Youth Development Office, Bicol University College of Education, Local Internet Café Partners', 
    'document', 
    DECODE('255044462D312E340A25C3A2C3A30A312030206F626A0A3C3C0A2F5469746C652028290A2F43726561746F722028FEFF0077006B00680074006D006C0074006F00700064006600200030002E00310032002E0035290A2F50726F64756365722028FEFF0051007400200035002E00310032002E0038290A2F4372656174696F6E446174652028443A32303235313231383032313033332B303127303027290A3E3E0A656E646F626A', 'hex'), 
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
) ON CONFLICT ("Id") DO UPDATE SET 
    "Title" = EXCLUDED."Title",
    "Description" = EXCLUDED."Description",
    "Objectives" = EXCLUDED."Objectives",
    "TargetBeneficiaries" = EXCLUDED."TargetBeneficiaries",
    "ExpectedOutcomes" = EXCLUDED."ExpectedOutcomes",
    "Budget" = EXCLUDED."Budget",
    "Timeline" = EXCLUDED."Timeline",
    "StartDate" = EXCLUDED."StartDate",
    "EndDate" = EXCLUDED."EndDate",
    "Category" = EXCLUDED."Category",
    "Venue" = EXCLUDED."Venue",
    "Partners" = EXCLUDED."Partners",
    "AttachmentType" = EXCLUDED."AttachmentType",
    "AttachmentData" = EXCLUDED."AttachmentData",
    "ProposedBy" = EXCLUDED."ProposedBy",
    "ProposerPosition" = EXCLUDED."ProposerPosition",
    "ProposerArea" = EXCLUDED."ProposerArea",
    "Status" = EXCLUDED."Status",
    "IsPublished" = EXCLUDED."IsPublished",
    "Audience" = EXCLUDED."Audience";

-- Sync sequence ID for PostgreSQL auto-increment
SELECT setval(pg_get_serial_sequence('"ProgramsProjects"', 'Id'), coalesce(max("Id"), 1)) FROM "ProgramsProjects";
