using DataEditorX.Core;

namespace DataEditorX
{
    public partial class AddArchetypeForm : Form
    {
        public string name;
        public long code;
        readonly Dictionary<long, string> setcodes;
        readonly string dialogTitle;
        readonly string invalidIdMessage;
        readonly string emptyNameMessage;

        public AddArchetypeForm(Dictionary<long, string> dic)
            : this(
                dic,
                "Add Archetype",
                "Setcode ID:",
                "Archetype Name:",
                "Enter a valid hexadecimal setcode ID, for example fae or 0xfae.",
                "Enter an archetype name.")
        {
        }

        public AddArchetypeForm(
            Dictionary<long, string> dic,
            string title,
            string idLabel,
            string nameLabel,
            string invalidIdMessage,
            string emptyNameMessage)
        {
            InitializeComponent();
            setcodes = dic ?? new Dictionary<long, string>();
            dialogTitle = title;
            this.invalidIdMessage = invalidIdMessage;
            this.emptyNameMessage = emptyNameMessage;
            Text = title;
            label_setcode.Text = idLabel;
            label_name.Text = nameLabel;
            AcceptButton = btn_confirm;
            CancelButton = btn_cancel;
        }

        private void Btn_confirm_Click(object sender, EventArgs e)
        {
            if (!ArchetypeStringsService.TryParseHexId(tb_archecode.Text, out code))
            {
                MessageBox.Show(this, invalidIdMessage, dialogTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                tb_archecode.Focus();
                return;
            }

            if (setcodes.TryGetValue(code, out string existingName))
            {
                MessageBox.Show(this, $"{ArchetypeStringsService.FormatHexId(code)} is already used by {existingName}.",
                    dialogTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                tb_archecode.Focus();
                return;
            }

            name = tb_archename.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show(this, emptyNameMessage, dialogTitle,
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
