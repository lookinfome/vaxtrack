-- =============================================================================
-- VaxTrack v2 — Seed Data
-- =============================================================================
-- Passwords
--   Platform admin  : Admin@1234
--   All other users : Test@1234
--
-- ID convention     : {Name}-17-{xx}  (matches UtilityService.GenerateUniqueIdAsync)
-- Booking states covered:
--   BK-17-a1  Vivek   — Dose1 pending
--   BK-17-b2  Priya   — Dose1 completed, Dose2 pending
--   BK-17-c3  Rahul   — Both doses completed (fully vaccinated)
--   BK-17-d4  Anita   — Dose1 canceled
--   BK-17-e5  Kiran   — Dose1 completed, Dose2 canceled
--   BK-17-f6  Deepa   — Dose1 pending (different hospital than Vivek)
-- =============================================================================


-- =============================================================================
-- 1. USERS
-- =============================================================================
INSERT INTO [vaxtrack_sqlserver].[dbo].[Users]
    (UserId, UserUid, UserName, UserBirthdate, UserAge, UserGender, UserPhone, UserAddress, UserPinCode, ProfilePicturePath, UserRole, IsDeleted, CreatedAt, UpdatedAt, DeletedAt)
VALUES
    -- Platform Admin
    ('Admin-17-aa',
     '11111111-0000-0000-0000-000000000001',
     'Super Admin',
     '1985-04-10', 41, 'Male', '9000000001',
     '1 Admin Lane, Mumbai', '400001', '', 1, 0,
     '2026-01-01 08:00:00', NULL, NULL),

    -- Regular Users
    ('VivekK-17-b1',
     '22222222-0000-0000-0000-000000000002',
     'Vivek Kumar',
     '1995-06-15', 30, 'Male', '9111111111',
     '12 Park Street, Mumbai', '400002', '', 0, 0,
     '2026-01-05 09:00:00', NULL, NULL),

    ('PriyaS-17-c2',
     '33333333-0000-0000-0000-000000000003',
     'Priya Sharma',
     '1992-03-22', 34, 'Female', '9222222222',
     '45 Green Avenue, Delhi', '110001', '', 0, 0,
     '2026-01-08 10:00:00', NULL, NULL),

    ('RahulM-17-d3',
     '44444444-0000-0000-0000-000000000004',
     'Rahul Mehta',
     '1990-11-30', 35, 'Male', '9333333333',
     '78 MG Road, Bangalore', '560001', '', 0, 0,
     '2026-01-10 11:00:00', NULL, NULL),

    ('AnitaS-17-e4',
     '55555555-0000-0000-0000-000000000005',
     'Anita Singh',
     '1998-07-05', 27, 'Female', '9444444444',
     '22 Anna Salai, Chennai', '600001', '', 0, 0,
     '2026-01-12 12:00:00', NULL, NULL),

    ('KiranP-17-f5',
     '66666666-0000-0000-0000-000000000006',
     'Kiran Patel',
     '1993-09-18', 32, 'Male', '9555555555',
     '9 FC Road, Pune', '411001', '', 0, 0,
     '2026-01-15 13:00:00', NULL, NULL),

    ('DeepaR-17-a6',
     '77777777-0000-0000-0000-000000000007',
     'Deepa Reddy',
     '1997-02-28', 29, 'Female', '9666666666',
     '33 Banjara Hills, Hyderabad', '500001', '', 0, 0,
     '2026-01-18 14:00:00', NULL, NULL);


-- =============================================================================
-- 2. HOSPITALS
-- Slot accounting (only pending bookings occupy slots):
--   CityCare-17-a1   50 total — Vivek Dose1 pending (-1)       → 49 available
--   MediLife-17-b2   40 total — Priya Dose1 done (-1 used, not restored),
--                               Priya Dose2 pending (-1)        → 38 available
--   ApexHealth-17-c3 30 total — Rahul both done (no pending)   → 30 available
--   SunriseMed-17-d4 35 total — Deepa Dose1 pending (-1)       → 34 available
--   GlobalCure-17-e5 45 total — no current pending             → 45 available
-- =============================================================================
INSERT INTO [vaxtrack_sqlserver].[dbo].[Hospitals]
    (HospitalId, HospitalUid, HospitalName, HospitalAddress, HospitalPinCode, HospitalPhoneNumber, HospitalEmail, TotalSlots, SlotsAvailable, IsDeleted, RegisteredDate, UpdatedDate, RemovedDate)
VALUES
    ('CityCare-17-a1',
     'aaaaaaaa-0000-0000-0000-000000000001',
     'CityCare Hospital',
     'Andheri West, Mumbai', '400053',
     '02244001100', 'contact@citycare.in',
     50, 49, 0,
     '2026-01-02 08:00:00', '2026-01-02 08:00:00', NULL),

    ('MediLife-17-b2',
     'bbbbbbbb-0000-0000-0000-000000000002',
     'MediLife Medical Center',
     'Connaught Place, Delhi', '110001',
     '01144002200', 'info@medilife.in',
     40, 38, 0,
     '2026-01-03 08:00:00', '2026-01-03 08:00:00', NULL),

    ('ApexHealth-17-c3',
     'cccccccc-0000-0000-0000-000000000003',
     'Apex Health Clinic',
     'Koramangala, Bangalore', '560034',
     '08044003300', 'hello@apexhealth.in',
     30, 30, 0,
     '2026-01-04 08:00:00', '2026-01-04 08:00:00', NULL),

    ('SunriseMed-17-d4',
     'dddddddd-0000-0000-0000-000000000004',
     'Sunrise Medical Center',
     'T Nagar, Chennai', '600017',
     '04444004400', 'care@sunrisemed.in',
     35, 34, 0,
     '2026-01-05 08:00:00', '2026-01-05 08:00:00', NULL),

    ('GlobalCure-17-e5',
     'eeeeeeee-0000-0000-0000-000000000005',
     'GlobalCure Hospital',
     'Jubilee Hills, Hyderabad', '500033',
     '04044005500', 'support@globalcure.in',
     45, 45, 0,
     '2026-01-06 08:00:00', '2026-01-06 08:00:00', NULL);


-- =============================================================================
-- 3. USER CREDENTIALS
-- Passwords hashed with BCrypt.Net-Next, work factor 11
--   Admin@1234  →  $2a$11$zmyt9RtdCVKbMtj99XgDNeAu6tEUY2.hfepcTvdC/rYW7eHFr0Meq
--   Test@1234   →  $2a$11$VfBywofr9hZJFOku51W.CeMOVPz8NoT/L5e33L.D.P9GvGAPzgI7y  (Vivek)
--               →  $2a$11$waashP/JoOJR.SpgKon.Y.Z4Y97MHxBGDoa2qc7PSBMSmB3OABCqS  (Priya)
--               →  $2a$11$egy4UtyoaGVQKqVjX/qDu.WbZMp42d2z5s.j/iqp6L3dZOEDPBeKi  (Rahul)
--               →  $2a$11$Ef7.OsA984014co48.7JEu/LUJOud7mmmBjpbpuEygW6dNeoopW/y  (Anita)
--               →  $2a$11$0EXIUss8vBSt5YoW3BY1C.fJhuPiHDJ7FZckubK7nPA2zuK.TfAG2  (Kiran)
--               →  $2a$11$CZicAtw/yJxjONJaWncg9.lnGIe9OT2o/VsPEmLgCP53Lm4fWpNwm  (Deepa)
-- =============================================================================
INSERT INTO [vaxtrack_sqlserver].[dbo].[UserCredentials]
    (UserUid, Email, PasswordHash, IsDeleted, DeletedAt, CreatedAt, UpdatedAt)
VALUES
    ('11111111-0000-0000-0000-000000000001',
     'admin@vaxtrack.in',
     '$2a$11$zmyt9RtdCVKbMtj99XgDNeAu6tEUY2.hfepcTvdC/rYW7eHFr0Meq',
     0, NULL, '2026-01-01 08:00:00', '2026-01-01 08:00:00'),

    ('22222222-0000-0000-0000-000000000002',
     'vivek.kumar@example.com',
     '$2a$11$VfBywofr9hZJFOku51W.CeMOVPz8NoT/L5e33L.D.P9GvGAPzgI7y',
     0, NULL, '2026-01-05 09:00:00', '2026-01-05 09:00:00'),

    ('33333333-0000-0000-0000-000000000003',
     'priya.sharma@example.com',
     '$2a$11$waashP/JoOJR.SpgKon.Y.Z4Y97MHxBGDoa2qc7PSBMSmB3OABCqS',
     0, NULL, '2026-01-08 10:00:00', '2026-01-08 10:00:00'),

    ('44444444-0000-0000-0000-000000000004',
     'rahul.mehta@example.com',
     '$2a$11$egy4UtyoaGVQKqVjX/qDu.WbZMp42d2z5s.j/iqp6L3dZOEDPBeKi',
     0, NULL, '2026-01-10 11:00:00', '2026-01-10 11:00:00'),

    ('55555555-0000-0000-0000-000000000005',
     'anita.singh@example.com',
     '$2a$11$Ef7.OsA984014co48.7JEu/LUJOud7mmmBjpbpuEygW6dNeoopW/y',
     0, NULL, '2026-01-12 12:00:00', '2026-01-12 12:00:00'),

    ('66666666-0000-0000-0000-000000000006',
     'kiran.patel@example.com',
     '$2a$11$0EXIUss8vBSt5YoW3BY1C.fJhuPiHDJ7FZckubK7nPA2zuK.TfAG2',
     0, NULL, '2026-01-15 13:00:00', '2026-01-15 13:00:00'),

    ('77777777-0000-0000-0000-000000000007',
     'deepa.reddy@example.com',
     '$2a$11$CZicAtw/yJxjONJaWncg9.lnGIe9OT2o/VsPEmLgCP53Lm4fWpNwm',
     0, NULL, '2026-01-18 14:00:00', '2026-01-18 14:00:00');


-- =============================================================================
-- 4. USER ROLE MAPPINGS
-- hospital-admin assignments (ContextId = HospitalId, the readable PK)
--   Vivek  → hospital-admin at CityCare-17-a1
--   Priya  → hospital-admin at MediLife-17-b2
--   Rahul  → hospital-admin at ApexHealth-17-c3 AND SunriseMed-17-d4 (multi-scope)
-- =============================================================================
INSERT INTO [vaxtrack_sqlserver].[dbo].[UserRoleMappings]
    (UserUid, RoleTag, ContextId, IsActive, CreatedAt, UpdatedAt)
VALUES
    ('22222222-0000-0000-0000-000000000002',
     'hospital-admin', 'CityCare-17-a1',
     1, '2026-01-06 10:00:00', '2026-01-06 10:00:00'),

    ('33333333-0000-0000-0000-000000000003',
     'hospital-admin', 'MediLife-17-b2',
     1, '2026-01-09 10:00:00', '2026-01-09 10:00:00'),

    ('44444444-0000-0000-0000-000000000004',
     'hospital-admin', 'ApexHealth-17-c3',
     1, '2026-01-11 10:00:00', '2026-01-11 10:00:00'),

    ('44444444-0000-0000-0000-000000000004',
     'hospital-admin', 'SunriseMed-17-d4',
     1, '2026-01-11 10:30:00', '2026-01-11 10:30:00'),

    -- Revoked mapping example (Kiran had GlobalCure admin, was revoked)
    ('66666666-0000-0000-0000-000000000006',
     'hospital-admin', 'GlobalCure-17-e5',
     0, '2026-01-16 10:00:00', '2026-02-01 09:00:00');


-- =============================================================================
-- 5. BOOKINGS
-- Note: Dose1HospitalUid / Dose2HospitalUid store the readable HospitalId
--       (used by service for slot updates and hospital-admin role checks)
-- =============================================================================
INSERT INTO [vaxtrack_sqlserver].[dbo].[Bookings]
    (BookingId, BookingUid, UserUid,
     Dose1RequestedDateTime, Dose1SlotNumber, Dose1HospitalUid,
     IsDose1Completed, Dose1CompletedDateTime, IsD1RequestCanceled,
     Dose2RequestedDateTime, Dose2SlotNumber, Dose2HospitalUid,
     IsDose2Completed, Dose2CompletedDateTime, IsD2RequestCanceled,
     IsVaccinationCompleted, VaccinationCompletedDateTime,
     IsDeleted, CreatedAt, ModifiedAt, RemovedAt)
VALUES

    -- BK-17-a1 | Vivek | Dose1 PENDING at CityCare
    ('BK-17-a1',
     'b0000001-0000-0000-0000-000000000001',
     '22222222-0000-0000-0000-000000000002',
     '2026-07-10 10:00:00', 5, 'CityCare-17-a1',
     0, NULL, 0,
     NULL, 0, '',
     0, NULL, 0,
     0, NULL,
     0, '2026-06-20 09:00:00', '2026-06-20 09:00:00', NULL),

    -- BK-17-b2 | Priya | Dose1 COMPLETED, Dose2 PENDING at MediLife
    ('BK-17-b2',
     'b0000002-0000-0000-0000-000000000002',
     '33333333-0000-0000-0000-000000000003',
     '2026-05-15 11:00:00', 12, 'MediLife-17-b2',
     1, '2026-05-15 11:30:00', 0,
     '2026-07-20 11:00:00', 8, 'MediLife-17-b2',
     0, NULL, 0,
     0, NULL,
     0, '2026-05-10 10:00:00', '2026-05-15 11:35:00', NULL),

    -- BK-17-c3 | Rahul | BOTH DOSES COMPLETED — fully vaccinated
    ('BK-17-c3',
     'b0000003-0000-0000-0000-000000000003',
     '44444444-0000-0000-0000-000000000004',
     '2026-03-01 09:00:00', 3, 'ApexHealth-17-c3',
     1, '2026-03-01 09:45:00', 0,
     '2026-05-01 09:00:00', 7, 'ApexHealth-17-c3',
     1, '2026-05-01 09:50:00', 0,
     1, '2026-05-01 09:50:00',
     0, '2026-02-25 08:00:00', '2026-05-01 10:00:00', NULL),

    -- BK-17-d4 | Anita | Dose1 CANCELED
    ('BK-17-d4',
     'b0000004-0000-0000-0000-000000000004',
     '55555555-0000-0000-0000-000000000005',
     '2026-04-10 14:00:00', 9, 'SunriseMed-17-d4',
     0, NULL, 1,
     NULL, 0, '',
     0, NULL, 0,
     0, NULL,
     0, '2026-04-05 12:00:00', '2026-04-08 15:00:00', NULL),

    -- BK-17-e5 | Kiran | Dose1 COMPLETED, Dose2 CANCELED
    ('BK-17-e5',
     'b0000005-0000-0000-0000-000000000005',
     '66666666-0000-0000-0000-000000000006',
     '2026-04-20 10:00:00', 6, 'GlobalCure-17-e5',
     1, '2026-04-20 10:30:00', 0,
     '2026-06-20 10:00:00', 4, 'GlobalCure-17-e5',
     0, NULL, 1,
     0, NULL,
     0, '2026-04-15 09:00:00', '2026-06-18 11:00:00', NULL),

    -- BK-17-f6 | Deepa | Dose1 PENDING at SunriseMed
    ('BK-17-f6',
     'b0000006-0000-0000-0000-000000000006',
     '77777777-0000-0000-0000-000000000007',
     '2026-07-05 15:00:00', 2, 'SunriseMed-17-d4',
     0, NULL, 0,
     NULL, 0, '',
     0, NULL, 0,
     0, NULL,
     0, '2026-06-22 10:00:00', '2026-06-22 10:00:00', NULL);
