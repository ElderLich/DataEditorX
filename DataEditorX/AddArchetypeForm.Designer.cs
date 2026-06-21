namespace DataEditorX
{
    partial class AddArchetypeForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            label_setcode = new Label();
            tb_archecode = new TextBox();
            label_name = new Label();
            tb_archename = new TextBox();
            btn_confirm = new Button();
            btn_cancel = new Button();
            SuspendLayout();
            //
            // label_setcode
            //
            label_setcode.AutoSize = true;
            label_setcode.Location = new Point(12, 15);
            label_setcode.Name = "label_setcode";
            label_setcode.Size = new Size(77, 12);
            label_setcode.TabIndex = 0;
            label_setcode.Text = "Setcode ID:";
            //
            // tb_archecode
            //
            tb_archecode.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            tb_archecode.Location = new Point(112, 11);
            tb_archecode.Name = "tb_archecode";
            tb_archecode.Size = new Size(260, 21);
            tb_archecode.TabIndex = 1;
            //
            // label_name
            //
            label_name.AutoSize = true;
            label_name.Location = new Point(12, 45);
            label_name.Name = "label_name";
            label_name.Size = new Size(95, 12);
            label_name.TabIndex = 2;
            label_name.Text = "Archetype Name:";
            //
            // tb_archename
            //
            tb_archename.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            tb_archename.Location = new Point(112, 41);
            tb_archename.Name = "tb_archename";
            tb_archename.Size = new Size(260, 21);
            tb_archename.TabIndex = 3;
            //
            // btn_confirm
            //
            btn_confirm.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btn_confirm.Location = new Point(216, 74);
            btn_confirm.Name = "btn_confirm";
            btn_confirm.Size = new Size(75, 23);
            btn_confirm.TabIndex = 4;
            btn_confirm.Text = "Confirm";
            btn_confirm.Click += Btn_confirm_Click;
            //
            // btn_cancel
            //
            btn_cancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btn_cancel.DialogResult = DialogResult.Cancel;
            btn_cancel.Location = new Point(297, 74);
            btn_cancel.Name = "btn_cancel";
            btn_cancel.Size = new Size(75, 23);
            btn_cancel.TabIndex = 5;
            btn_cancel.Text = "Cancel";
            //
            // AddArchetypeForm
            //
            AutoScaleDimensions = new SizeF(6F, 12F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(384, 109);
            Controls.Add(btn_cancel);
            Controls.Add(btn_confirm);
            Controls.Add(tb_archename);
            Controls.Add(label_name);
            Controls.Add(tb_archecode);
            Controls.Add(label_setcode);
            Font = new Font("SimSun", 9F);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            KeyPreview = true;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "AddArchetypeForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Add Archetype";
            KeyDown += AddArchetypeForm_KeyDown;
            ResumeLayout(false);
            PerformLayout();
        }

        private Label label_setcode;
        private TextBox tb_archecode;
        private Label label_name;
        private TextBox tb_archename;
        private Button btn_confirm;
        private Button btn_cancel;

        #endregion
    }
}
