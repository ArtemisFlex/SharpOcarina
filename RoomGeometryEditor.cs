using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using OpenTK;

namespace SharpOcarina
{
    internal sealed class RoomGeometryEditor : Form
    {
        private readonly MainForm owner;
        private readonly ComboBox type = new ComboBox();
        private readonly TextBox name = new TextBox();
        private readonly TextBox material = new TextBox();
        private readonly TextBox texture = new TextBox();
        private readonly TextBox minX = new TextBox();
        private readonly TextBox minY = new TextBox();
        private readonly TextBox minZ = new TextBox();
        private readonly TextBox maxX = new TextBox();
        private readonly TextBox maxY = new TextBox();
        private readonly TextBox maxZ = new TextBox();
        private readonly TextBox polytype = new TextBox();
        private readonly CheckBox collision = new CheckBox();

        public RoomGeometryEditor(MainForm owner)
        {
            this.owner = owner;
            Text = "Room Geometry Authoring";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(430, 390);

            type.DropDownStyle = ComboBoxStyle.DropDownList;
            type.Items.AddRange(new object[] { "Cube", "Plane", "Triangle", "Ramp" });
            type.SelectedIndex = 0;

            name.Text = "Primitive";
            material.Text = "Default";
            minX.Text = "-50"; minY.Text = "0"; minZ.Text = "-50";
            maxX.Text = "50"; maxY.Text = "100"; maxZ.Text = "50";
            polytype.Text = "0";
            collision.Text = "Add matching collision geometry";
            collision.Checked = true;
            texture.Text = "Optional texture path (relative to the room model)";

            TableLayoutPanel table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 13,
                Padding = new Padding(10),
                AutoSize = false
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 145));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            AddRow(table, 0, "Primitive", type);
            AddRow(table, 1, "Name", name);
            AddRow(table, 2, "Material", material);
            AddRow(table, 3, "Texture", texture);
            AddRow(table, 4, "Min X / Y / Z", ThreeInputs(minX, minY, minZ));
            AddRow(table, 5, "Max X / Y / Z", ThreeInputs(maxX, maxY, maxZ));
            AddRow(table, 6, "Collision polytype", polytype);
            table.Controls.Add(collision, 0, 7);
            table.SetColumnSpan(collision, 2);

            Label hint = new Label
            {
                Text = "Plane uses Min/Max as its rectangle. Ramp rises from Min.Z to Max.Z.",
                AutoSize = true,
                ForeColor = Color.DimGray
            };
            table.Controls.Add(hint, 0, 8);
            table.SetColumnSpan(hint, 2);

            FlowLayoutPanel buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
            Button add = new Button { Text = "Add to Room", AutoSize = true };
            Button cancel = new Button { Text = "Close", AutoSize = true, DialogResult = DialogResult.Cancel };
            add.Click += Add_Click;
            buttons.Controls.Add(add);
            buttons.Controls.Add(cancel);
            table.Controls.Add(buttons, 0, 10);
            table.SetColumnSpan(buttons, 2);
            Controls.Add(table);
            AcceptButton = add;
            CancelButton = cancel;
        }

        private static Control ThreeInputs(Control a, Control b, Control c)
        {
            FlowLayoutPanel panel = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoSize = true };
            a.Width = b.Width = c.Width = 72;
            panel.Controls.Add(a); panel.Controls.Add(b); panel.Controls.Add(c);
            return panel;
        }

        private static void AddRow(TableLayoutPanel table, int row, string label, Control control)
        {
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 29));
            table.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
            control.Dock = DockStyle.Fill;
            table.Controls.Add(control, 1, row);
        }

        private void Add_Click(object sender, EventArgs e)
        {
            try
            {
                RoomPrimitiveSpec spec = new RoomPrimitiveSpec
                {
                    Type = (RoomPrimitiveType)type.SelectedIndex,
                    Name = name.Text,
                    MaterialName = material.Text,
                    TexturePath = string.IsNullOrWhiteSpace(texture.Text) || texture.Text.StartsWith("Optional ") ? null : texture.Text,
                    Min = new Vector3d(Read(minX), Read(minY), Read(minZ)),
                    Max = new Vector3d(Read(maxX), Read(maxY), Read(maxZ)),
                    CollisionPolyType = ParseInt(polytype.Text),
                    AddToCollision = collision.Checked
                };

                owner.AppendRoomPrimitive(spec);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Room Geometry", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static double Read(TextBox box)
        {
            double value;
            if (!double.TryParse(box.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                throw new InvalidOperationException("Enter valid numeric coordinates.");
            return value;
        }

        private static int ParseInt(string text)
        {
            int value;
            if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
                throw new InvalidOperationException("Collision polytype must be an integer.");
            return value;
        }
    }
}
