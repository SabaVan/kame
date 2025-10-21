﻿using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class NullablePlaylist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bars_Playlist_CurrentPlaylistId",
                table: "Bars");

            migrationBuilder.AlterColumn<Guid>(
                name: "CurrentPlaylistId",
                table: "Bars",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_Bars_Playlist_CurrentPlaylistId",
                table: "Bars",
                column: "CurrentPlaylistId",
                principalTable: "Playlist",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bars_Playlist_CurrentPlaylistId",
                table: "Bars");

            migrationBuilder.AlterColumn<Guid>(
                name: "CurrentPlaylistId",
                table: "Bars",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Bars_Playlist_CurrentPlaylistId",
                table: "Bars",
                column: "CurrentPlaylistId",
                principalTable: "Playlist",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
