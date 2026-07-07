-- =============================================================================
-- VaxTrack v2 — Truncate All Tables
-- Wipes every table clean. No FK constraints are defined at the DB level
-- (relations are string-matched in application code), so order doesn't matter.
-- =============================================================================

TRUNCATE TABLE [vaxtrack_sqlserver].[dbo].[BookingAuditLogs];
TRUNCATE TABLE [vaxtrack_sqlserver].[dbo].[Bookings];
TRUNCATE TABLE [vaxtrack_sqlserver].[dbo].[HospitalAuditLogs];
TRUNCATE TABLE [vaxtrack_sqlserver].[dbo].[Hospitals];
TRUNCATE TABLE [vaxtrack_sqlserver].[dbo].[Notifications];
TRUNCATE TABLE [vaxtrack_sqlserver].[dbo].[PasswordResetTokens];
TRUNCATE TABLE [vaxtrack_sqlserver].[dbo].[RevokedTokens];
TRUNCATE TABLE [vaxtrack_sqlserver].[dbo].[UserAuditLogs];
TRUNCATE TABLE [vaxtrack_sqlserver].[dbo].[UserCredentials];
TRUNCATE TABLE [vaxtrack_sqlserver].[dbo].[UserRequests];
TRUNCATE TABLE [vaxtrack_sqlserver].[dbo].[UserRoleMappings];
TRUNCATE TABLE [vaxtrack_sqlserver].[dbo].[Users];
