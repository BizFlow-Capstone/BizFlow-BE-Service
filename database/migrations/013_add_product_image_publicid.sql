-- Migration: 013_add_product_image_publicid
-- Description: Add ImagePublicId column to Products table for Cloudinary image management
-- Date: 2026-02-05

-- =============================================
-- ADD ImagePublicId COLUMN TO PRODUCTS TABLE
-- =============================================
ALTER TABLE Products
ADD COLUMN ImagePublicId VARCHAR(255) NULL COMMENT 'Cloudinary public ID for image deletion' AFTER ImageUrl;

-- Add index for faster lookup during cleanup jobs
CREATE INDEX idx_product_image_publicid ON Products(ImagePublicId);
