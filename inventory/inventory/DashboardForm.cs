using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.OleDb;

namespace inventory
{
    public partial class DashboardForm : Form
    {
        private int selectedId = 0;

        public DashboardForm(string fullName)
        {
            InitializeComponent();
            lblWelcome.Text = "Welcome, " + fullName + "!";

            dgvProducts.ReadOnly = true;
            dgvProducts.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvProducts.AllowUserToAddRows = false;

            btnAddProduct.Click += (s, e) => AddProduct();
            btnUpdate.Click += (s, e) => UpdateProduct();
            btnDelete.Click += (s, e) => DeleteProduct();
            btnClear.Click += (s, e) => ClearFields();
            btnRefresh.Click += (s, e) => { txtSearch.Clear(); LoadProducts(); };
            txtSearch.TextChanged += (s, e) => LoadProducts();
            dgvProducts.CellClick += dgvProducts_CellClick;

            LoadProducts();
        }
        private void LoadProducts()
        {
            try
            {
                using (OleDbConnection conn = new OleDbConnection(Db.ConnString))
                {
                    conn.Open();
                    string search = txtSearch.Text.Trim();
                    string sql = "SELECT ProductID, ProductName, Category, [Description], " +
                                 "UnitPrice, Quantity, ReorderLevel, CreatedAt FROM Products";
                    if (search != "")
                        sql += " WHERE ProductName LIKE ? OR Category LIKE ? OR [Description] LIKE ?";
                    sql += " ORDER BY ProductID DESC";

                    using (OleDbDataAdapter da = new OleDbDataAdapter(sql, conn))
                    {
                        if (search != "")
                        {
                            string like = "%" + search + "%";
                            da.SelectCommand.Parameters.AddWithValue("?", like);
                            da.SelectCommand.Parameters.AddWithValue("?", like);
                            da.SelectCommand.Parameters.AddWithValue("?", like);
                        }
                        DataTable dt = new DataTable();
                        da.Fill(dt);
                        dgvProducts.DataSource = dt;
                    }

                    lblTotalProducts.Text = CountProducts(conn, "").ToString();
                    lblInStock.Text = CountProducts(conn, " WHERE Quantity > ReorderLevel").ToString();
                    lblLowStock.Text = CountProducts(conn, " WHERE Quantity > 0 AND Quantity <= ReorderLevel").ToString();
                    lblOutOfStock.Text = CountProducts(conn, " WHERE Quantity <= 0").ToString();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private int CountProducts(OleDbConnection conn, string where)
        {
            using (OleDbCommand cmd = new OleDbCommand("SELECT COUNT(*) FROM Products" + where, conn))
            {
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        private bool ReadInputs(out string name, out string category, out string desc,
                                out decimal price, out int qty, out int reorder)
        {
            name = txtProductName.Text.Trim();
            category = cmbCategory.Text.Trim();
            desc = txtDescription.Text.Trim();
            price = 0; qty = 0; reorder = 0;

            if (name == "" || category == "")
            {
                MessageBox.Show("Please enter a product name and choose a category.");
                return false;
            }
            if (!decimal.TryParse(txtUnitPrice.Text.Trim(), out price) || price < 0)
            {
                MessageBox.Show("Unit Price must be a valid number.");
                return false;
            }
            if (!int.TryParse(txtQuantity.Text.Trim(), out qty) || qty < 0)
            {
                MessageBox.Show("Quantity must be a whole number.");
                return false;
            }
            if (!int.TryParse(txtReorderLevel.Text.Trim(), out reorder) || reorder < 0)
            {
                MessageBox.Show("Reorder Level must be a whole number.");
                return false;
            }
            return true;
        }

        private void AddProduct()
        {
            string name, category, desc; decimal price; int qty, reorder;
            if (!ReadInputs(out name, out category, out desc, out price, out qty, out reorder)) return;

            try
            {
                using (OleDbConnection conn = new OleDbConnection(Db.ConnString))
                {
                    conn.Open();
                    using (OleDbCommand cmd = new OleDbCommand(
                        "INSERT INTO Products (ProductName, Category, [Description], UnitPrice, " +
                        "Quantity, ReorderLevel, CreatedAt) VALUES (?, ?, ?, ?, ?, ?, ?)", conn))
                    {
                        cmd.Parameters.Add("?", OleDbType.VarWChar).Value = name;
                        cmd.Parameters.Add("?", OleDbType.VarWChar).Value = category;
                        cmd.Parameters.Add("?", OleDbType.VarWChar).Value = desc;
                        cmd.Parameters.Add("?", OleDbType.Currency).Value = price;
                        cmd.Parameters.Add("?", OleDbType.Integer).Value = qty;
                        cmd.Parameters.Add("?", OleDbType.Integer).Value = reorder;
                        cmd.Parameters.Add("?", OleDbType.Date).Value = DateTime.Now;
                        cmd.ExecuteNonQuery();
                    }
                }
                MessageBox.Show("Product added!");
                ClearFields();
                LoadProducts();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private void UpdateProduct()
        {
            if (selectedId == 0)
            {
                MessageBox.Show("Select a product from the list first.");
                return;
            }
            string name, category, desc; decimal price; int qty, reorder;
            if (!ReadInputs(out name, out category, out desc, out price, out qty, out reorder)) return;

            try
            {
                using (OleDbConnection conn = new OleDbConnection(Db.ConnString))
                {
                    conn.Open();
                    using (OleDbCommand cmd = new OleDbCommand(
                        "UPDATE Products SET ProductName = ?, Category = ?, [Description] = ?, " +
                        "UnitPrice = ?, Quantity = ?, ReorderLevel = ? WHERE ProductID = ?", conn))
                    {
                        cmd.Parameters.Add("?", OleDbType.VarWChar).Value = name;
                        cmd.Parameters.Add("?", OleDbType.VarWChar).Value = category;
                        cmd.Parameters.Add("?", OleDbType.VarWChar).Value = desc;
                        cmd.Parameters.Add("?", OleDbType.Currency).Value = price;
                        cmd.Parameters.Add("?", OleDbType.Integer).Value = qty;
                        cmd.Parameters.Add("?", OleDbType.Integer).Value = reorder;
                        cmd.Parameters.Add("?", OleDbType.Integer).Value = selectedId;
                        cmd.ExecuteNonQuery();
                    }
                }
                MessageBox.Show("Product updated!");
                ClearFields();
                LoadProducts();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private void DeleteProduct()
        {
            if (selectedId == 0)
            {
                MessageBox.Show("Select a product from the list first.");
                return;
            }
            if (MessageBox.Show("Delete this product?", "Confirm",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

            try
            {
                using (OleDbConnection conn = new OleDbConnection(Db.ConnString))
                {
                    conn.Open();
                    using (OleDbCommand cmd = new OleDbCommand(
                        "DELETE FROM Products WHERE ProductID = ?", conn))
                    {
                        cmd.Parameters.Add("?", OleDbType.Integer).Value = selectedId;
                        cmd.ExecuteNonQuery();
                    }
                }
                MessageBox.Show("Product deleted.");
                ClearFields();
                LoadProducts();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private void ClearFields()
        {
            selectedId = 0;
            txtProductName.Clear();
            txtDescription.Clear();
            txtUnitPrice.Clear();
            txtQuantity.Clear();
            txtReorderLevel.Clear();
            cmbCategory.SelectedIndex = -1;
            dgvProducts.ClearSelection();
        }

        private void dgvProducts_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            DataGridViewRow row = dgvProducts.Rows[e.RowIndex];

            selectedId = Convert.ToInt32(row.Cells["ProductID"].Value);
            txtProductName.Text = Convert.ToString(row.Cells["ProductName"].Value);
            cmbCategory.Text = Convert.ToString(row.Cells["Category"].Value);
            txtDescription.Text = Convert.ToString(row.Cells["Description"].Value);
            txtUnitPrice.Text = Convert.ToString(row.Cells["UnitPrice"].Value);
            txtQuantity.Text = Convert.ToString(row.Cells["Quantity"].Value);
            txtReorderLevel.Text = Convert.ToString(row.Cells["ReorderLevel"].Value);
        }

        private void btnLogout_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
