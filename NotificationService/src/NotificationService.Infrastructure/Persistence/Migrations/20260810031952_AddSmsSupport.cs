using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotificationService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSmsSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RecipientEmail",
                table: "Notifications",
                newName: "Recipient");

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "UserContacts",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            // Every Notification row created before this migration was, by definition, an email send
            // (SMS did not exist yet) – default to "Email" rather than an empty string so existing
            // rows still deserialize to a valid NotificationChannel value.
            migrationBuilder.AddColumn<string>(
                name: "Channel",
                table: "Notifications",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Email");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "UserContacts");

            migrationBuilder.DropColumn(
                name: "Channel",
                table: "Notifications");

            migrationBuilder.RenameColumn(
                name: "Recipient",
                table: "Notifications",
                newName: "RecipientEmail");
        }
    }
}
