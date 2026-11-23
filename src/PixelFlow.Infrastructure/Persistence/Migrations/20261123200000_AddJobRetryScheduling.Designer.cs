using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using PixelFlow.Infrastructure.Persistence;

#nullable disable

namespace PixelFlow.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261123200000_AddJobRetryScheduling")]
    partial class AddJobRetryScheduling
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.23")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("PixelFlow.Domain.Entities.Image", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_at");

                    b.Property<Guid?>("CurrentVersionId")
                        .HasColumnType("uuid")
                        .HasColumnName("current_version_id");

                    b.Property<Guid>("OwnerId")
                        .HasColumnType("uuid")
                        .HasColumnName("owner_id");

                    b.HasKey("Id")
                        .HasName("pk_images");

                    b.HasIndex("OwnerId")
                        .HasDatabaseName("ix_images_owner_id");

                    b.ToTable("images", (string)null);
                });

            modelBuilder.Entity("PixelFlow.Domain.Entities.ImageVersion", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<string>("ContentType")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)")
                        .HasColumnName("content_type");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_at");

                    b.Property<string>("FilePath")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("file_path");

                    b.Property<int>("Height")
                        .HasColumnType("integer")
                        .HasColumnName("height");

                    b.Property<Guid>("ImageId")
                        .HasColumnType("uuid")
                        .HasColumnName("image_id");

                    b.Property<int>("Number")
                        .HasColumnType("integer")
                        .HasColumnName("number");

                    b.Property<string>("Operation")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)")
                        .HasColumnName("operation");

                    b.Property<string>("Parameters")
                        .HasColumnType("jsonb")
                        .HasColumnName("parameters");

                    b.Property<Guid?>("ParentVersionId")
                        .HasColumnType("uuid")
                        .HasColumnName("parent_version_id");

                    b.Property<long>("Size")
                        .HasColumnType("bigint")
                        .HasColumnName("size");

                    b.Property<int>("Width")
                        .HasColumnType("integer")
                        .HasColumnName("width");

                    b.HasKey("Id")
                        .HasName("pk_image_versions");

                    b.HasIndex("ParentVersionId")
                        .HasDatabaseName("ix_image_versions_parent_version_id");

                    b.HasIndex("ImageId", "Number")
                        .IsUnique()
                        .HasDatabaseName("ix_image_versions_image_id_number");

                    b.ToTable("image_versions", (string)null);
                });

            modelBuilder.Entity("PixelFlow.Domain.Entities.Job", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<int>("Attempts")
                        .HasColumnType("integer")
                        .HasColumnName("attempts");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_at");

                    b.Property<string>("Error")
                        .HasMaxLength(2000)
                        .HasColumnType("character varying(2000)")
                        .HasColumnName("error");

                    b.Property<Guid>("ImageId")
                        .HasColumnType("uuid")
                        .HasColumnName("image_id");

                    b.Property<DateTimeOffset>("LastUpdated")
                        .IsConcurrencyToken()
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("last_updated");

                    b.Property<string>("LeaseOwner")
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)")
                        .HasColumnName("lease_owner");

                    b.Property<DateTimeOffset?>("LeaseUntil")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("lease_until");

                    b.Property<DateTimeOffset?>("NextAttemptAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("next_attempt_at");

                    b.Property<string>("Operation")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)")
                        .HasColumnName("operation");

                    b.Property<string>("Parameters")
                        .HasColumnType("jsonb")
                        .HasColumnName("parameters");

                    b.Property<int>("Progress")
                        .HasColumnType("integer")
                        .HasColumnName("progress");

                    b.Property<Guid?>("ResultVersionId")
                        .HasColumnType("uuid")
                        .HasColumnName("result_version_id");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("character varying(20)")
                        .HasColumnName("status");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid")
                        .HasColumnName("user_id");

                    b.HasKey("Id")
                        .HasName("pk_jobs");

                    b.HasIndex("ImageId")
                        .HasDatabaseName("ix_jobs_image_id");

                    b.HasIndex("UserId")
                        .HasDatabaseName("ix_jobs_user_id");

                    b.HasIndex("Status", "LeaseUntil")
                        .HasDatabaseName("ix_jobs_status_lease_until");

                    b.HasIndex("Status", "NextAttemptAt")
                        .HasDatabaseName("ix_jobs_status_next_attempt_at");

                    b.ToTable("jobs", (string)null);
                });

            modelBuilder.Entity("PixelFlow.Domain.Entities.ImageVersion", b =>
                {
                    b.HasOne("PixelFlow.Domain.Entities.Image", null)
                        .WithMany("Versions")
                        .HasForeignKey("ImageId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_image_versions_images_image_id");

                    b.HasOne("PixelFlow.Domain.Entities.ImageVersion", null)
                        .WithMany()
                        .HasForeignKey("ParentVersionId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .HasConstraintName("fk_image_versions_image_versions_parent_version_id");
                });

            modelBuilder.Entity("PixelFlow.Domain.Entities.Job", b =>
                {
                    b.HasOne("PixelFlow.Domain.Entities.Image", null)
                        .WithMany()
                        .HasForeignKey("ImageId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_jobs_images_image_id");
                });

            modelBuilder.Entity("PixelFlow.Domain.Entities.Image", b =>
                {
                    b.Navigation("Versions");
                });
#pragma warning restore 612, 618
        }
    }
}
