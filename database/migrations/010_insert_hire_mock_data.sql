-- Migration: 010_insert_hire_mock_data
-- Description: Insert mock data for Hire table
-- Date: 2026-02-01

-- =============================================
-- HIRE MOCK DATA
-- =============================================
-- Using existing users from mock data:
-- Owners:
--   Shinkiri (550e8400-e29b-41d4-a716-446655440001)
--   Ngan (550e8400-e29b-41d4-a716-446655440002)
-- Employees:
--   Tran Van A (550e8400-e29b-41d4-a716-446655440003)
--   Nguyen Thi B (550e8400-e29b-41d4-a716-446655440004)
--   Le Van Thanh C (550e8400-e29b-41d4-a716-446655440005)

INSERT INTO Hire (OwnerId, EmployeeId, IsActive, StartAt, EndAt) VALUES
-- Shinkiri hired Tran Van A (active)
('550e8400-e29b-41d4-a716-446655440001', '550e8400-e29b-41d4-a716-446655440003', TRUE, '2024-01-15 09:00:00', NULL),

-- Ngan hired Nguyen Thi B (active)
('550e8400-e29b-41d4-a716-446655440002', '550e8400-e29b-41d4-a716-446655440004', TRUE, '2024-02-01 08:30:00', NULL),

-- Ngan hired Le Van Thanh C (active)
('550e8400-e29b-41d4-a716-446655440002', '550e8400-e29b-41d4-a716-446655440005', TRUE, '2024-03-01 09:00:00', NULL);

-- Insert this migration
INSERT INTO __MigrationHistory (MigrationId, ProductVersion) 
VALUES ('010_insert_hire_mock_data', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = ProductVersion;
