using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using System.Data;
using System.Data.OleDb;

namespace inventory
{
    public static class Db
    {
        public static string DbPath
        {
            get
            {
                // para ang database sa proj ang gagamitin here
                string projectCopy = Path.GetFullPath(
                    Path.Combine(Application.StartupPath, @"..\..\userdb.accdb"));
                if (File.Exists(projectCopy)) return projectCopy;
                return Path.Combine(Application.StartupPath, "userdb.accdb");
            }
        }

        public static string ConnString
        {
            get { return "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + DbPath; }
        }

        // Hindi sine-save ang password nang plain text
        public static string Hash(string text)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(text));
                StringBuilder sb = new StringBuilder();
                foreach (byte b in bytes) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }
        public static void EnsureProductsTable()
        {
            using (OleDbConnection conn = new OleDbConnection(ConnString))
            {
                conn.Open();

                // Kung may Products table na, wala nang gagawin
                DataTable t = conn.GetSchema("Tables", new string[] { null, null, "Products", "TABLE" });
                if (t.Rows.Count > 0) return;

                string sql = "CREATE TABLE Products (" +
                    "ProductID COUNTER PRIMARY KEY, " +
                    "ProductName TEXT(100), " +
                    "Category TEXT(50), " +
                    "[Description] TEXT(255), " +
                    "UnitPrice CURRENCY, " +
                    "Quantity LONG, " +
                    "ReorderLevel LONG, " +
                    "CreatedAt DATETIME)";

                using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}