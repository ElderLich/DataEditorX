using DataEditorX.Core;

namespace DataEditorX
{
    public partial class AddArchetypeForm : Form
    {
        public string name;
        public long code;
        readonly Dictionary<long, string> setcodes;

        public AddArchetypeForm(Dictionary<long, string> dic)
        {
            InitializeComponent();
            setcodes = dic ?? new Dictionary<long, string>();
            AcceptButton = btn_confirm;
            CancelButton = btn_cancel;
        }

        private void Btn_confirm_Click(object sender, EventArgs e)
        {
            if (!ArchetypeStringsService.TryParseSetcode(tb_archecode.Text, out code))
            {
                MessageBox.Show(this, "Enter a valid hexadecimal setcode ID, for example fae or 0xfae.",
                    "Add Archetype", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                tb_archecode.Focus();
                return;
            }

            if (setcodes.TryGetValue(code, out string existingName))
            {
                MessageBox.Show(this, $"{ArchetypeStringsService.FormatSetcode(code)} is already used by {existingName}.",
                    "Add Archetype", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                tb_archecode.Focus();
                return;
            }

            name = tb_archename.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show(this, "Enter an archetype name.", "Add Archetype",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                tb_archename.Focus();
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        private void AddArchetypeForm_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Enter:
                    Btn_confirm_Click(sender, e);
                    break;
                case Keys.Escape:
                    Close();
                    break;
            }
        }
    }
}
