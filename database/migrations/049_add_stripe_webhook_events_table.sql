-- Migration: 049_add_stripe_webhook_events_table
-- Adds persistent Stripe webhook event store for absolute idempotency and replay safety.

CREATE TABLE IF NOT EXISTS StripeWebhookEvents (
    StripeWebhookEventId INT AUTO_INCREMENT PRIMARY KEY,
    EventId VARCHAR(100) NOT NULL,
    EventType VARCHAR(80) NOT NULL,
    StripeCreatedAt DATETIME NOT NULL,
    ReceivedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ProcessingStatus VARCHAR(20) NOT NULL DEFAULT 'Received',
    AttemptCount INT NOT NULL DEFAULT 1,
    LastError TEXT NULL,
    ProcessedAt DATETIME NULL,
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    UNIQUE INDEX ux_swe_event_id (EventId),
    INDEX idx_swe_status_updated (ProcessingStatus, UpdatedAt)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Stores Stripe webhook events for idempotency and replay safety';

-- =============================================
-- Insert migration history
-- =============================================
INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('049_add_stripe_webhook_events_table', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = '1.0.0';
