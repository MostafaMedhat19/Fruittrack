using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fruittrack.Migrations
{
    /// <inheritdoc />
    public partial class RemoveOldCashTransactionColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove old columns from CashReceiptTransactions table if they exist
            var cashReceiptColumns = migrationBuilder.Sql(@"
                SELECT COLUMN_NAME 
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_NAME = 'CashReceiptTransactions'
            ");

            // Check and remove PaidBackAmount column
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'CashReceiptTransactions' AND COLUMN_NAME = 'PaidBackAmount')
                BEGIN
                    ALTER TABLE CashReceiptTransactions DROP COLUMN PaidBackAmount
                END
            ");

            // Check and remove RemainingAmount column  
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'CashReceiptTransactions' AND COLUMN_NAME = 'RemainingAmount')
                BEGIN
                    ALTER TABLE CashReceiptTransactions DROP COLUMN RemainingAmount
                END
            ");

            // Add Notes column if it doesn't exist
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'CashReceiptTransactions' AND COLUMN_NAME = 'Notes')
                BEGIN
                    ALTER TABLE CashReceiptTransactions ADD Notes nvarchar(max) NULL DEFAULT ''
                END
            ");

            // Remove old columns from CashDisbursementTransactions table if they exist
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'CashDisbursementTransactions' AND COLUMN_NAME = 'Credit')
                BEGIN
                    ALTER TABLE CashDisbursementTransactions DROP COLUMN Credit
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'CashDisbursementTransactions' AND COLUMN_NAME = 'Debit')
                BEGIN
                    ALTER TABLE CashDisbursementTransactions DROP COLUMN Debit
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'CashDisbursementTransactions' AND COLUMN_NAME = 'Notes')
                BEGIN
                    ALTER TABLE CashDisbursementTransactions DROP COLUMN Notes
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Add back the removed columns for rollback
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'CashReceiptTransactions' AND COLUMN_NAME = 'PaidBackAmount')
                BEGIN
                    ALTER TABLE CashReceiptTransactions ADD PaidBackAmount decimal(18,2) NOT NULL DEFAULT 0
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'CashReceiptTransactions' AND COLUMN_NAME = 'RemainingAmount')
                BEGIN
                    ALTER TABLE CashReceiptTransactions ADD RemainingAmount decimal(18,2) NOT NULL DEFAULT 0
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'CashDisbursementTransactions' AND COLUMN_NAME = 'Credit')
                BEGIN
                    ALTER TABLE CashDisbursementTransactions ADD Credit decimal(18,2) NOT NULL DEFAULT 0
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'CashDisbursementTransactions' AND COLUMN_NAME = 'Debit')
                BEGIN
                    ALTER TABLE CashDisbursementTransactions ADD Debit decimal(18,2) NOT NULL DEFAULT 0
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'CashDisbursementTransactions' AND COLUMN_NAME = 'Notes')
                BEGIN
                    ALTER TABLE CashDisbursementTransactions ADD Notes nvarchar(max) NULL
                END
            ");

            // Remove Notes column from CashReceiptTransactions
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'CashReceiptTransactions' AND COLUMN_NAME = 'Notes')
                BEGIN
                    ALTER TABLE CashReceiptTransactions DROP COLUMN Notes
                END
            ");
        }
    }
}
