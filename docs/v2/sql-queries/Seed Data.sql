-- =============================================================================
-- VaxTrack v2 — Fresh Start Seed Data
-- =============================================================================
-- Run "Truncate Commands.sql" first to wipe every table, then run this file.
--
-- What this creates:
--   1 Platform Admin account       (full platform privileges)
--   10 Hospital Admin accounts     (one per seeded hospital, scoped privileges)
--   1 Demo/Normal user account     (plain end-user, for smoke-testing only)
--   10 fresh Hospital records
--
-- No bookings are seeded on purpose — this DB is meant to be shared so real
-- users can register their own accounts and create their own bookings.
--
-- All system-created accounts use the @vaxtrack.system email domain, round
-- placeholder phone numbers, and "Other"/system-labelled names so they read
-- as system-provisioned rather than accounts a random end user signed up
-- with.
--
-- Passwords (BCrypt.Net-Next, work factor 11):
--   Platform admin   : Admin@12345
--   Hospital admins  : HospitalAdmin@12345   (same password for all 10)
--   Demo user        : Demo@12345
--
-- ID convention: {Name}-{xx}-{xx}  (matches UtilityService.GenerateUniqueIdAsync)
-- =============================================================================


-- =============================================================================
-- 1. HOSPITALS (10 fresh records)
-- =============================================================================
INSERT INTO [vaxtrack_sqlserver].[dbo].[Hospitals]
    (HospitalId, HospitalUid, HospitalName, HospitalAddress, HospitalPinCode, HospitalPhoneNumber, HospitalEmail, TotalSlots, SlotsAvailable, Status, StatusComment, IsDeleted, RegisteredDate, UpdatedDate, RemovedDate)
VALUES
    ('CityCare-17-c4', '2ffe79cd-7044-4c1b-ae94-eb53a724850f',
     'CityCare Hospital', 'Andheri West, Mumbai', '400053',
     '02244001100', 'contact@citycare.in',
     50, 50, 'Active', NULL, 0, '2026-07-08 08:00:00', '2026-07-08 08:00:00', NULL),

    ('MediLife-17-9e', '85809ac1-5dae-40c4-841b-f5de58cd7d0a',
     'MediLife Medical Center', 'Connaught Place, Delhi', '110001',
     '01144002200', 'info@medilife.in',
     50, 50, 'Active', NULL, 0, '2026-07-08 08:00:00', '2026-07-08 08:00:00', NULL),

    ('ApexHealth-17-fe', 'c4694b71-e4bb-4b67-bb6b-a3eb3ecbcbf3',
     'Apex Health Clinic', 'Koramangala, Bangalore', '560034',
     '08044003300', 'hello@apexhealth.in',
     50, 50, 'Active', NULL, 0, '2026-07-08 08:00:00', '2026-07-08 08:00:00', NULL),

    ('SunriseMed-17-7a', '8d267cd4-d71a-4f9f-9c61-b3e2e2a07194',
     'Sunrise Medical Center', 'T Nagar, Chennai', '600017',
     '04444004400', 'care@sunrisemed.in',
     50, 50, 'Active', NULL, 0, '2026-07-08 08:00:00', '2026-07-08 08:00:00', NULL),

    ('GlobalCure-17-3f', 'd17625a9-496a-496f-a4dc-87b10ebbfd06',
     'GlobalCure Hospital', 'Jubilee Hills, Hyderabad', '500033',
     '04044005500', 'support@globalcure.in',
     50, 50, 'Active', NULL, 0, '2026-07-08 08:00:00', '2026-07-08 08:00:00', NULL),

    ('TrustCare-17-64', '705d8829-5c14-45fe-b173-f134bbc0ac5b',
     'TrustCare Hospital', 'Kothrud, Pune', '411038',
     '02044006600', 'contact@trustcare.in',
     50, 50, 'Active', NULL, 0, '2026-07-08 08:00:00', '2026-07-08 08:00:00', NULL),

    ('LifeLine-17-25', 'bab3a894-321a-4344-8b27-cb4f1bb096ad',
     'LifeLine Medical Institute', 'Salt Lake, Kolkata', '700064',
     '03344007700', 'info@lifeline.in',
     50, 50, 'Active', NULL, 0, '2026-07-08 08:00:00', '2026-07-08 08:00:00', NULL),

    ('WellnessPlus-17-68', '4e641a5f-6006-4936-92b4-41f4aa6e1062',
     'WellnessPlus Hospital', 'Navrangpura, Ahmedabad', '380009',
     '07944008800', 'hello@wellnessplus.in',
     50, 50, 'Active', NULL, 0, '2026-07-08 08:00:00', '2026-07-08 08:00:00', NULL),

    ('CarePoint-17-23', 'e3237a3e-02ed-493b-a865-42ceaba4e1d4',
     'CarePoint Hospital', 'Malviya Nagar, Jaipur', '302017',
     '01414009900', 'care@carepoint.in',
     50, 50, 'Active', NULL, 0, '2026-07-08 08:00:00', '2026-07-08 08:00:00', NULL),

    ('UnityHealth-17-25', '55c852f7-63b7-4049-b633-7826f66bb4ba',
     'UnityHealth Hospital', 'Gomti Nagar, Lucknow', '226010',
     '05224001010', 'support@unityhealth.in',
     50, 50, 'Active', NULL, 0, '2026-07-08 08:00:00', '2026-07-08 08:00:00', NULL);


-- =============================================================================
-- 2. USERS
--    - 1 Platform Admin   (UserRole = 1)
--    - 10 Hospital Admins (UserRole = 0, privilege comes from UserRoleMappings)
--    - 1 Demo/Normal user (UserRole = 0, no role mapping)
-- =============================================================================
INSERT INTO [vaxtrack_sqlserver].[dbo].[Users]
    (UserId, UserUid, UserName, UserBirthdate, UserAge, UserGender, UserPhone, UserAddress, UserPinCode, ProfilePicturePath, UserRole, Status, StatusComment, IsDeleted, CreatedAt, UpdatedAt, DeletedAt)
VALUES
    -- Platform Admin
    ('SysAdmin-17-93', 'e0eb4ac9-5338-420b-a464-ba345178117b',
     'VaxTrack System Admin', '1985-01-01', 41, 'Other', '9000000000',
     'VaxTrack Head Office', '000001', '', 1, 'Active', NULL, 0,
     '2026-07-08 08:00:00', NULL, NULL),

    -- Hospital Admins (one per hospital)
    ('CityCareAdmin-17-1a', '8e497b3b-1d19-40a6-bc8f-e9c14d09c20c',
     'CityCare Hospital Admin', '1990-01-01', 36, 'Other', '9000000001',
     'Andheri West, Mumbai', '400053', '', 0, 'Active', NULL, 0,
     '2026-07-08 08:05:00', NULL, NULL),

    ('MediLifeAdmin-17-59', 'e09f89b3-16a0-499f-b589-0a969022561f',
     'MediLife Medical Center Admin', '1990-01-01', 36, 'Other', '9000000002',
     'Connaught Place, Delhi', '110001', '', 0, 'Active', NULL, 0,
     '2026-07-08 08:06:00', NULL, NULL),

    ('ApexHealthAdmin-17-9f', '9b38e0c6-9085-464b-b7b7-52afc8ba2574',
     'Apex Health Clinic Admin', '1990-01-01', 36, 'Other', '9000000003',
     'Koramangala, Bangalore', '560034', '', 0, 'Active', NULL, 0,
     '2026-07-08 08:07:00', NULL, NULL),

    ('SunriseMedAdmin-17-6a', '2c46f045-3e90-4c26-84ac-90f0d3b2c48c',
     'Sunrise Medical Center Admin', '1990-01-01', 36, 'Other', '9000000004',
     'T Nagar, Chennai', '600017', '', 0, 'Active', NULL, 0,
     '2026-07-08 08:08:00', NULL, NULL),

    ('GlobalCureAdmin-17-32', 'f4a62d9b-a115-4402-a184-a1c26e89c387',
     'GlobalCure Hospital Admin', '1990-01-01', 36, 'Other', '9000000005',
     'Jubilee Hills, Hyderabad', '500033', '', 0, 'Active', NULL, 0,
     '2026-07-08 08:09:00', NULL, NULL),

    ('TrustCareAdmin-17-6d', 'a1c82027-8e72-4b61-b589-03e081dad6ba',
     'TrustCare Hospital Admin', '1990-01-01', 36, 'Other', '9000000006',
     'Kothrud, Pune', '411038', '', 0, 'Active', NULL, 0,
     '2026-07-08 08:10:00', NULL, NULL),

    ('LifeLineAdmin-17-ca', 'a2814cd7-1373-4f4d-98e9-73f2e6e37128',
     'LifeLine Medical Institute Admin', '1990-01-01', 36, 'Other', '9000000007',
     'Salt Lake, Kolkata', '700064', '', 0, 'Active', NULL, 0,
     '2026-07-08 08:11:00', NULL, NULL),

    ('WellnessPlusAdmin-17-73', '2ae10cd8-7b59-4e64-81a9-c1dc2e016434',
     'WellnessPlus Hospital Admin', '1990-01-01', 36, 'Other', '9000000008',
     'Navrangpura, Ahmedabad', '380009', '', 0, 'Active', NULL, 0,
     '2026-07-08 08:12:00', NULL, NULL),

    ('CarePointAdmin-17-0d', 'a8c9203b-a42e-4241-bbeb-404474b66e34',
     'CarePoint Hospital Admin', '1990-01-01', 36, 'Other', '9000000009',
     'Malviya Nagar, Jaipur', '302017', '', 0, 'Active', NULL, 0,
     '2026-07-08 08:13:00', NULL, NULL),

    ('UnityHealthAdmin-17-31', '52ecf278-e3df-4400-bdbf-0647e9af50c0',
     'UnityHealth Hospital Admin', '1990-01-01', 36, 'Other', '9000000010',
     'Gomti Nagar, Lucknow', '226010', '', 0, 'Active', NULL, 0,
     '2026-07-08 08:14:00', NULL, NULL),

    -- Demo / Normal user (plain end-user, no elevated privileges, for smoke-testing only)
    ('DemoUser-17-f7', '61ddeb5c-9070-4358-b11e-d5668be8e2bc',
     'VaxTrack Demo User', '1995-01-01', 31, 'Other', '9000009999',
     'Demo Address, Sample City', '000000', '', 0, 'Active', NULL, 0,
     '2026-07-08 08:15:00', NULL, NULL);


-- =============================================================================
-- 3. USER CREDENTIALS
-- =============================================================================
INSERT INTO [vaxtrack_sqlserver].[dbo].[UserCredentials]
    (UserUid, Email, PasswordHash, IsDeleted, DeletedAt, CreatedAt, UpdatedAt)
VALUES
    -- Platform admin — Admin@12345
    ('e0eb4ac9-5338-420b-a464-ba345178117b', 'admin@vaxtrack.system',
     '$2a$11$nW3EpZInTBNKa0C4YIh5b.vK0CPMYo6pTklWsAMpFYzaoJLvYy7dO',
     0, NULL, '2026-07-08 08:00:00', '2026-07-08 08:00:00'),

    -- Hospital admins — HospitalAdmin@12345 (same password for all 10)
    ('8e497b3b-1d19-40a6-bc8f-e9c14d09c20c', 'admin@citycare.vaxtrack.system',
     '$2a$11$d3AIIcOsrExFai6EYGrnBe6hnI.EK0sWOXuozw92o0siVcB2CWG3y',
     0, NULL, '2026-07-08 08:05:00', '2026-07-08 08:05:00'),

    ('e09f89b3-16a0-499f-b589-0a969022561f', 'admin@medilife.vaxtrack.system',
     '$2a$11$d3AIIcOsrExFai6EYGrnBe6hnI.EK0sWOXuozw92o0siVcB2CWG3y',
     0, NULL, '2026-07-08 08:06:00', '2026-07-08 08:06:00'),

    ('9b38e0c6-9085-464b-b7b7-52afc8ba2574', 'admin@apexhealth.vaxtrack.system',
     '$2a$11$d3AIIcOsrExFai6EYGrnBe6hnI.EK0sWOXuozw92o0siVcB2CWG3y',
     0, NULL, '2026-07-08 08:07:00', '2026-07-08 08:07:00'),

    ('2c46f045-3e90-4c26-84ac-90f0d3b2c48c', 'admin@sunrisemed.vaxtrack.system',
     '$2a$11$d3AIIcOsrExFai6EYGrnBe6hnI.EK0sWOXuozw92o0siVcB2CWG3y',
     0, NULL, '2026-07-08 08:08:00', '2026-07-08 08:08:00'),

    ('f4a62d9b-a115-4402-a184-a1c26e89c387', 'admin@globalcure.vaxtrack.system',
     '$2a$11$d3AIIcOsrExFai6EYGrnBe6hnI.EK0sWOXuozw92o0siVcB2CWG3y',
     0, NULL, '2026-07-08 08:09:00', '2026-07-08 08:09:00'),

    ('a1c82027-8e72-4b61-b589-03e081dad6ba', 'admin@trustcare.vaxtrack.system',
     '$2a$11$d3AIIcOsrExFai6EYGrnBe6hnI.EK0sWOXuozw92o0siVcB2CWG3y',
     0, NULL, '2026-07-08 08:10:00', '2026-07-08 08:10:00'),

    ('a2814cd7-1373-4f4d-98e9-73f2e6e37128', 'admin@lifeline.vaxtrack.system',
     '$2a$11$d3AIIcOsrExFai6EYGrnBe6hnI.EK0sWOXuozw92o0siVcB2CWG3y',
     0, NULL, '2026-07-08 08:11:00', '2026-07-08 08:11:00'),

    ('2ae10cd8-7b59-4e64-81a9-c1dc2e016434', 'admin@wellnessplus.vaxtrack.system',
     '$2a$11$d3AIIcOsrExFai6EYGrnBe6hnI.EK0sWOXuozw92o0siVcB2CWG3y',
     0, NULL, '2026-07-08 08:12:00', '2026-07-08 08:12:00'),

    ('a8c9203b-a42e-4241-bbeb-404474b66e34', 'admin@carepoint.vaxtrack.system',
     '$2a$11$d3AIIcOsrExFai6EYGrnBe6hnI.EK0sWOXuozw92o0siVcB2CWG3y',
     0, NULL, '2026-07-08 08:13:00', '2026-07-08 08:13:00'),

    ('52ecf278-e3df-4400-bdbf-0647e9af50c0', 'admin@unityhealth.vaxtrack.system',
     '$2a$11$d3AIIcOsrExFai6EYGrnBe6hnI.EK0sWOXuozw92o0siVcB2CWG3y',
     0, NULL, '2026-07-08 08:14:00', '2026-07-08 08:14:00'),

    -- Demo user — Demo@12345
    ('61ddeb5c-9070-4358-b11e-d5668be8e2bc', 'demo.user@vaxtrack.system',
     '$2a$11$EtPC.yixk43w7/2v9SybJenph8RW9F4O5ki.4JiPh64.41ZRZrYKm',
     0, NULL, '2026-07-08 08:15:00', '2026-07-08 08:15:00');


-- =============================================================================
-- 4. USER ROLE MAPPINGS
-- hospital-admin assignments (ContextId = HospitalId, the readable PK)
-- =============================================================================
INSERT INTO [vaxtrack_sqlserver].[dbo].[UserRoleMappings]
    (UserUid, RoleTag, ContextId, IsActive, CreatedAt, UpdatedAt)
VALUES
    ('8e497b3b-1d19-40a6-bc8f-e9c14d09c20c', 'hospital-admin', 'CityCare-17-c4', 1, '2026-07-08 08:05:00', '2026-07-08 08:05:00'),
    ('e09f89b3-16a0-499f-b589-0a969022561f', 'hospital-admin', 'MediLife-17-9e', 1, '2026-07-08 08:06:00', '2026-07-08 08:06:00'),
    ('9b38e0c6-9085-464b-b7b7-52afc8ba2574', 'hospital-admin', 'ApexHealth-17-fe', 1, '2026-07-08 08:07:00', '2026-07-08 08:07:00'),
    ('2c46f045-3e90-4c26-84ac-90f0d3b2c48c', 'hospital-admin', 'SunriseMed-17-7a', 1, '2026-07-08 08:08:00', '2026-07-08 08:08:00'),
    ('f4a62d9b-a115-4402-a184-a1c26e89c387', 'hospital-admin', 'GlobalCure-17-3f', 1, '2026-07-08 08:09:00', '2026-07-08 08:09:00'),
    ('a1c82027-8e72-4b61-b589-03e081dad6ba', 'hospital-admin', 'TrustCare-17-64', 1, '2026-07-08 08:10:00', '2026-07-08 08:10:00'),
    ('a2814cd7-1373-4f4d-98e9-73f2e6e37128', 'hospital-admin', 'LifeLine-17-25', 1, '2026-07-08 08:11:00', '2026-07-08 08:11:00'),
    ('2ae10cd8-7b59-4e64-81a9-c1dc2e016434', 'hospital-admin', 'WellnessPlus-17-68', 1, '2026-07-08 08:12:00', '2026-07-08 08:12:00'),
    ('a8c9203b-a42e-4241-bbeb-404474b66e34', 'hospital-admin', 'CarePoint-17-23', 1, '2026-07-08 08:13:00', '2026-07-08 08:13:00'),
    ('52ecf278-e3df-4400-bdbf-0647e9af50c0', 'hospital-admin', 'UnityHealth-17-25', 1, '2026-07-08 08:14:00', '2026-07-08 08:14:00');
