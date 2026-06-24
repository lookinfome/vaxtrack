/*
  ============================================================
  VAXTRACK  –  TEST SEED DATA  (v2)
  ============================================================

  PURPOSE
  -------
  Populates all four tables (Hospitals, Users, Bookings, UserRoleMappings)
  with consistent test records that cover every booking state and role type.

  THIS FILE IS SPLIT INTO TWO PARTS:
  -----------------------------------
  PART A  –  Run NOW (current schema, no Email/PasswordHash yet)
             Inserts: Hospitals, Users (profile only), Bookings, UserRoleMappings

  PART B  –  Run AFTER auth migration adds Email + PasswordHash columns
             Updates: sets Email and PasswordHash on the four seeded users

  SLOT MATH – WHY EACH SlotsAvailable VALUE WAS CHOSEN
  ------------------------------------------------------
  CityCare-17-e5  (TotalSlots = 50):
    Priya  Dose1 pending        →  -1  (slot held, not yet administered)
    Ananya Dose1 completed      →  -1  (slot used, administered, no restoration)
    Ananya Dose2 completed      →  -1  (slot used, administered, no restoration)
    SlotsAvailable              =  47  ✓

  GreenLeaf-17-f6 (TotalSlots = 50):
    Rahul  Dose1 completed      →  -1  (slot used, administered)
    Rahul  Dose2 pending        →  -1  (slot held, not yet administered)
    SlotsAvailable              =  48  ✓

  Apollo-17-a7    (TotalSlots = 50):
    Vivek  Dose1 canceled       →  -1 + 1 = 0  (slot restored on cancellation)
    SlotsAvailable              =  50  ✓

  DATA STORY
  ----------
  USER        STATE                                      ROLE
  Vivek       Dose1 CANCELED (Apollo). Admin account.   Platform-admin (global)
  Priya       Dose1 PENDING  (CityCare, Jul 5).         Regular user
  Rahul       Dose1 DONE, Dose2 PENDING (GreenLeaf).   Regular user
  Ananya      VACCINATION COMPLETE (both doses done).   Hospital-admin (CityCare)

  SEEDED IDENTIFIERS (keep handy for API testing)
  ------------------------------------------------
  Hospitals:
    CityCare-17-e5   →  HospitalUid: 55555555-5555-5555-5555-555555555555
    GreenLeaf-17-f6  →  HospitalUid: 66666666-6666-6666-6666-666666666666
    Apollo-17-a7     →  HospitalUid: 77777777-7777-7777-7777-777777777777

  Users:
    Vivek-17-a1      →  UserUid: 11111111-1111-1111-1111-111111111111  (admin)
    Priya-17-b2      →  UserUid: 22222222-2222-2222-2222-222222222222
    Rahul-17-c3      →  UserUid: 33333333-3333-3333-3333-333333333333
    Ananya-17-d4     →  UserUid: 44444444-4444-4444-4444-444444444444

  Bookings:
    Book_22222222-2222-2222-2222-222222222222-17-a1  (Priya,  D1 pending)
    Book_33333333-3333-3333-3333-333333333333-17-b2  (Rahul,  D1 done / D2 pending)
    Book_44444444-4444-4444-4444-444444444444-17-c3  (Ananya, vaccination complete)
    Book_11111111-1111-1111-1111-111111111111-17-d4  (Vivek,  D1 canceled)

  ROLLBACK / CLEANUP
  ------------------
  DELETE FROM [dbo].[UserRoleMappings] WHERE UserUid IN ('11111111-1111-1111-1111-111111111111','44444444-4444-4444-4444-444444444444');
  DELETE FROM [dbo].[Bookings]         WHERE UserUid IN ('11111111-1111-1111-1111-111111111111','22222222-2222-2222-2222-222222222222','33333333-3333-3333-3333-333333333333','44444444-4444-4444-4444-444444444444');
  DELETE FROM [dbo].[Users]            WHERE UserUid IN ('11111111-1111-1111-1111-111111111111','22222222-2222-2222-2222-222222222222','33333333-3333-3333-3333-333333333333','44444444-4444-4444-4444-444444444444');
  DELETE FROM [dbo].[Hospitals]        WHERE HospitalId IN ('CityCare-17-e5','GreenLeaf-17-f6','Apollo-17-a7');

  ============================================================
*/


-- ============================================================
-- PART A : RUN NOW  (current schema — no Email/PasswordHash)
-- ============================================================


-- ── A1: HOSPITALS ────────────────────────────────────────────
INSERT INTO [vaxtrack_sqlserver].[dbo].[Hospitals]
    (HospitalId, HospitalUid, HospitalName, HospitalAddress, HospitalPinCode,
     HospitalPhoneNumber, HospitalEmail, TotalSlots, SlotsAvailable, IsDeleted,
     RegisteredDate, UpdatedDate, RemovedDate)
VALUES
    (
        'CityCare-17-e5',
        '55555555-5555-5555-5555-555555555555',
        'City Care Hospital',
        '14, MG Road, Bangalore, Karnataka',
        '560001',
        '08041234567',
        'info@citycarehosp.com',
        50, 47, 0,
        '2026-01-10 08:00:00', '2026-06-20 10:00:00', NULL
    ),
    (
        'GreenLeaf-17-f6',
        '66666666-6666-6666-6666-666666666666',
        'Green Leaf Medical Center',
        '22, 100 Feet Road, Indiranagar, Bangalore',
        '560038',
        '08044556677',
        'care@greenleafmedical.com',
        50, 48, 0,
        '2026-01-15 09:00:00', '2026-06-20 10:00:00', NULL
    ),
    (
        'Apollo-17-a7',
        '77777777-7777-7777-7777-777777777777',
        'Apollo Wellness Center',
        '5, Sarjapur Road, Koramangala, Bangalore',
        '560034',
        '08055667788',
        'wellness@apollocenter.com',
        50, 50, 0,
        '2026-01-20 10:00:00', '2026-06-20 10:00:00', NULL
    );


-- ── A2: USERS (no Email/PasswordHash — columns don't exist yet) ───────────────
INSERT INTO [vaxtrack_sqlserver].[dbo].[Users]
    (UserId, UserUid, UserName, UserBirthdate, UserAge, UserGender,
     UserPhone, UserAddress, UserPinCode, ProfilePicturePath,
     UserRole, IsDeleted, CreatedAt, UpdatedAt, DeletedAt)
VALUES
    (
        -- Platform admin
        'Vivek-17-a1',
        '11111111-1111-1111-1111-111111111111',
        'Vivek Kumar',
        '1990-05-15', 36, 'Male',
        '9876543210', '12, Whitefield, Bangalore', '560066', '',
        1,          -- UserRole = 1 (admin)
        0, '2026-01-05 10:00:00', NULL, NULL
    ),
    (
        -- Regular user: Dose 1 pending at CityCare
        'Priya-17-b2',
        '22222222-2222-2222-2222-222222222222',
        'Priya Sharma',
        '1995-08-22', 30, 'Female',
        '8765432109', '7, Koramangala, Bangalore', '560095', '',
        0,
        0, '2026-01-10 11:00:00', NULL, NULL
    ),
    (
        -- Regular user: Dose 1 completed, Dose 2 pending at GreenLeaf
        'Rahul-17-c3',
        '33333333-3333-3333-3333-333333333333',
        'Rahul Mehta',
        '1988-03-10', 38, 'Male',
        '7654321098', '33, Sadashivanagar, Bangalore', '560080', '',
        0,
        0, '2026-01-12 12:00:00', NULL, NULL
    ),
    (
        -- Hospital-admin user: vaccination complete
        'Ananya-17-d4',
        '44444444-4444-4444-4444-444444444444',
        'Ananya Patel',
        '1992-11-05', 33, 'Female',
        '6543210987', '21, Jayanagar, Bangalore', '560041', '',
        0,
        0, '2026-01-15 13:00:00', NULL, NULL
    );


-- ── A3: BOOKINGS ─────────────────────────────────────────────
-- NAMING NOTE:
--   Dose1HospitalUid / Dose2HospitalUid store the HospitalId (readable key),
--   NOT the HospitalUid GUID — known naming mismatch in the model.
--   UserUid is the FK to Users.UserUid (GUID), not the readable UserId.
INSERT INTO [vaxtrack_sqlserver].[dbo].[Bookings]
    (BookingId, BookingUid, UserUid,
     Dose1RequestedDateTime, Dose1SlotNumber, Dose1HospitalUid,
     IsDose1Completed, Dose1CompletedDateTime, IsD1RequestCanceled,
     Dose2RequestedDateTime, Dose2SlotNumber, Dose2HospitalUid,
     IsDose2Completed, Dose2CompletedDateTime, IsD2RequestCanceled,
     IsVaccinationCompleted, VaccinationCompletedDateTime,
     IsDeleted, CreatedAt, ModifiedAt, RemovedAt)
VALUES
    (
        -- Priya: Dose 1 PENDING at CityCare (Jul 5 2026, slot 5)
        'Book_22222222-2222-2222-2222-222222222222-17-a1',
        'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
        '22222222-2222-2222-2222-222222222222',
        '2026-07-05 10:00:00', 5, 'CityCare-17-e5',
        0, NULL, 0,
        NULL, 0, '',
        0, NULL, 0,
        0, NULL,
        0, '2026-06-01 09:00:00', '2026-06-01 09:00:00', NULL
    ),
    (
        -- Rahul: Dose 1 COMPLETED (Apr 20), Dose 2 PENDING at GreenLeaf (Jul 20, slot 7)
        'Book_33333333-3333-3333-3333-333333333333-17-b2',
        'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
        '33333333-3333-3333-3333-333333333333',
        '2026-04-15 11:00:00', 3, 'GreenLeaf-17-f6',
        1, '2026-04-20 11:30:00', 0,
        '2026-07-20 11:00:00', 7, 'GreenLeaf-17-f6',
        0, NULL, 0,
        0, NULL,
        0, '2026-04-10 08:00:00', '2026-05-01 09:00:00', NULL
    ),
    (
        -- Ananya: Both doses COMPLETED at CityCare. VACCINATION COMPLETE.
        'Book_44444444-4444-4444-4444-444444444444-17-c3',
        'cccccccc-cccc-cccc-cccc-cccccccccccc',
        '44444444-4444-4444-4444-444444444444',
        '2026-03-01 10:00:00', 10, 'CityCare-17-e5',
        1, '2026-03-05 10:30:00', 0,
        '2026-04-10 10:00:00', 12, 'CityCare-17-e5',
        1, '2026-04-15 10:30:00', 0,
        1, '2026-04-15 10:30:00',
        0, '2026-02-20 10:00:00', '2026-04-15 11:00:00', NULL
    ),
    (
        -- Vivek: Dose 1 CANCELED at Apollo (slot 2, May 10). Slot restored.
        'Book_11111111-1111-1111-1111-111111111111-17-d4',
        'dddddddd-dddd-dddd-dddd-dddddddddddd',
        '11111111-1111-1111-1111-111111111111',
        '2026-05-10 09:00:00', 2, 'Apollo-17-a7',
        0, NULL, 1,
        NULL, 0, '',
        0, NULL, 0,
        0, NULL,
        0, '2026-05-01 09:00:00', '2026-05-02 10:00:00', NULL
    );


-- ── A4: USER ROLE MAPPINGS ───────────────────────────────────
-- Id is auto-generated (IDENTITY column) — do not supply it.
INSERT INTO [vaxtrack_sqlserver].[dbo].[UserRoleMappings]
    (UserUid, RoleTag, ContextId, IsActive, CreatedAt, UpdatedAt)
VALUES
    (
        -- Ananya: hospital-admin scoped to CityCare-17-e5
        '44444444-4444-4444-4444-444444444444',
        'hospital-admin', 'CityCare-17-e5',
        1,
        '2026-01-16 09:00:00', '2026-01-16 09:00:00'
    ),
    (
        -- Vivek: platform-admin (global — ContextId empty means platform-wide)
        '11111111-1111-1111-1111-111111111111',
        'platform-admin', '',
        1,
        '2026-01-05 10:00:00', '2026-01-05 10:00:00'
    );


-- ============================================================
-- PART B : RUN AFTER AUTH MIGRATION
--          (after: dotnet ef migrations add AddEmailAndPasswordToUsers
--                  dotnet ef database update)
--
-- Adds login credentials to the four seeded users.
-- All users share password: Vaxtrack@Test1
--
-- HOW TO USE:
--   1. Complete auth implementation and run the EF Core migration.
--   2. Generate a BCrypt hash: BCrypt.Net.BCrypt.HashPassword("Vaxtrack@Test1", workFactor: 11)
--      Or use any online BCrypt generator (cost = 11). Same hash can be used for all four rows.
--   3. Replace <BCRYPT_HASH> below with the generated hash string.
--   4. Uncomment the four UPDATE statements and run them.
--
-- Kept commented out for now — Email and PasswordHash columns do not exist yet.
-- ============================================================

/*
UPDATE [vaxtrack_sqlserver].[dbo].[Users]
SET
    Email        = 'vivek@vaxtrack.com',
    PasswordHash = '<BCRYPT_HASH>'
WHERE UserId = 'Vivek-17-a1';

UPDATE [vaxtrack_sqlserver].[dbo].[Users]
SET
    Email        = 'priya@test.com',
    PasswordHash = '<BCRYPT_HASH>'
WHERE UserId = 'Priya-17-b2';

UPDATE [vaxtrack_sqlserver].[dbo].[Users]
SET
    Email        = 'rahul@test.com',
    PasswordHash = '<BCRYPT_HASH>'
WHERE UserId = 'Rahul-17-c3';

UPDATE [vaxtrack_sqlserver].[dbo].[Users]
SET
    Email        = 'ananya@test.com',
    PasswordHash = '<BCRYPT_HASH>'
WHERE UserId = 'Ananya-17-d4';
*/
