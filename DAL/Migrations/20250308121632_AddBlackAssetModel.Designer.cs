﻿// <auto-generated />
using DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace DAL.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20250308121632_AddBlackAssetModel")]
    partial class AddBlackAssetModel
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "9.0.2");

            modelBuilder.Entity("DAL.Models.BlackAsset", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(15)
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("BlackAssets");
                });

            modelBuilder.Entity("DAL.Models.User", b =>
                {
                    b.Property<long>("TelegramUserId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("AuthPhrase")
                        .HasColumnType("TEXT");

                    b.Property<string>("TelegramName")
                        .HasColumnType("TEXT");

                    b.HasKey("TelegramUserId");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("DAL.Models.UserConfiguration", b =>
                {
                    b.Property<long>("TelegramUserId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Budget")
                        .HasColumnType("INTEGER");

                    b.Property<decimal>("ExceptedProfit")
                        .HasColumnType("TEXT");

                    b.Property<byte>("MinChanceToBuy")
                        .HasColumnType("INTEGER");

                    b.Property<byte>("MinChangeToSell")
                        .HasColumnType("INTEGER");

                    b.HasKey("TelegramUserId");

                    b.ToTable("UserConfigurations");
                });

            modelBuilder.Entity("DAL.Models.UserConfiguration", b =>
                {
                    b.HasOne("DAL.Models.User", "User")
                        .WithOne("UserConfiguration")
                        .HasForeignKey("DAL.Models.UserConfiguration", "TelegramUserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("DAL.Models.User", b =>
                {
                    b.Navigation("UserConfiguration");
                });
#pragma warning restore 612, 618
        }
    }
}
