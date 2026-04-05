using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mytril.Audit.Domain.Entities;
using Mytril.Audit.Domain.Enums;
using Mytril.Audit.Domain.ValueObjects;

namespace Mytril.Audit.Infrastructure.Persistence.Configurations;

public sealed class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ToTable("audit_log");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id");

        builder.Property(e => e.EventId)
            .HasColumnName("event_id");

        builder.HasIndex(e => e.EventId)
            .IsUnique()
            .HasDatabaseName("uidx_audit_event_id");

        builder.Property(e => e.EventType)
            .HasColumnName("event_type")
            .HasColumnType("varchar(200)")
            .HasConversion(
                v => v.Value,
                v => new EventTypeLabel(v));

        builder.Property(e => e.EventVersion)
            .HasColumnName("event_version")
            .HasColumnType("varchar(10)")
            .HasDefaultValue("1.0");

        builder.Property(e => e.Source)
            .HasColumnName("source")
            .HasColumnType("varchar(100)")
            .HasConversion(
                v => v.Value,
                v => new EventSource(v));

        builder.Property(e => e.Severity)
            .HasColumnName("severity")
            .HasColumnType("smallint")
            .HasConversion<short>(
                v => (short)v,
                v => (AuditSeverity)v);

        builder.Property(e => e.UserId)
            .HasColumnName("user_id");

        builder.Property(e => e.ActorId)
            .HasColumnName("actor_id");

        builder.Property(e => e.TenantId)
            .HasColumnName("tenant_id");

        builder.Property(e => e.TenantProductId)
            .HasColumnName("tenant_product_id");

        builder.Property(e => e.IpAddress)
            .HasColumnName("ip_address");

        builder.Property(e => e.UserAgent)
            .HasColumnName("user_agent");

        builder.Property(e => e.TraceId)
            .HasColumnName("trace_id")
            .HasColumnType("varchar(64)");

        builder.HasIndex(e => e.TraceId)
            .HasDatabaseName("idx_audit_trace_id");

        builder.Property(e => e.CorrelationId)
            .HasColumnName("correlation_id")
            .HasColumnType("varchar(64)");

        builder.Property(e => e.Payload)
            .HasColumnName("payload")
            .HasColumnType("jsonb")
            .HasConversion(
                v => v.Value,
                v => new SanitizedPayload(v));

        builder.Property(e => e.OccurredAt)
            .HasColumnName("occurred_at");

        builder.Property(e => e.ReceivedAt)
            .HasColumnName("received_at")
            .ValueGeneratedOnAdd();

        // Composite indexes
        builder.HasIndex(e => new { e.Source, e.OccurredAt })
            .IsDescending(false, true)
            .HasDatabaseName("idx_audit_source_occurred");

        builder.HasIndex(e => new { e.EventType, e.OccurredAt })
            .IsDescending(false, true)
            .HasDatabaseName("idx_audit_event_type");
    }
}
