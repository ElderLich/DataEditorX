using DataEditorX.Common;
using DataEditorX.Config;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace DataEditorX
{
    public class AboutForm : Form
    {
        private readonly Action checkForUpdates;
        private readonly List<LinkLabel> detailLinks = new();

        public AboutForm(Action checkForUpdates = null)
        {
            this.checkForUpdates = checkForUpdates;
            InitializeComponent();
            ApplyTheme();
        }

        public static void ShowVersionInfo(IWin32Window owner, Action checkForUpdates = null)
        {
            using AboutForm form = new(checkForUpdates);
            _ = form.ShowDialog(owner);
        }

        private void InitializeComponent()
        {
            Text = "Version Info";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowIcon = true;
            Icon = SystemIcons.Information;
            ClientSize = new Size(580, 370);

            TableLayoutPanel root = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(18)
            };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            TableLayoutPanel header = new()
            {
                AutoSize = true,
                ColumnCount = 2,
                Dock = DockStyle.Top,
                Margin = new Padding(0, 0, 0, 16)
            };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 56));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            PictureBox icon = new()
            {
                Image = SystemIcons.Information.ToBitmap(),
                Size = new Size(40, 40),
                SizeMode = PictureBoxSizeMode.CenterImage,
                Margin = new Padding(0, 4, 16, 0)
            };

            Label title = new()
            {
                AutoSize = true,
                Font = new Font(Font.FontFamily, 18, FontStyle.Bold),
                Text = "DataEditorX",
                Margin = new Padding(0)
            };

            Label subtitle = new()
            {
                AutoSize = true,
                Text = "MDPro3 card database and Lua script editor",
                Margin = new Padding(0, 4, 0, 0)
            };

            FlowLayoutPanel titlePanel = new()
            {
                AutoSize = true,
                FlowDirection = FlowDirection.TopDown,
                Dock = DockStyle.Fill,
                Margin = new Padding(0)
            };
            titlePanel.Controls.Add(title);
            titlePanel.Controls.Add(subtitle);
            header.Controls.Add(icon, 0, 0);
            header.Controls.Add(titlePanel, 1, 0);

            TableLayoutPanel details = new()
            {
                AutoSize = true,
                ColumnCount = 2,
                Dock = DockStyle.Top,
                Margin = new Padding(0)
            };
            details.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            details.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            AddDetail(details, "Version", CleanVersion(Application.ProductVersion));
            AddDetail(details, "Maintainer", "ElderLich");
            AddDetail(details, "Project Manager", "Co-created with BullyWiiPlaza");
            AddDetail(details, "Original Author", "purerosefallen/DataEditorX", "https://github.com/purerosefallen/DataEditorX");
            AddDetail(details, "Based On", "Lyris12 fork", "https://github.com/Lyris12/DataEditorX");
            AddDetail(details, "Repository", DEXConfig.ReadString(DEXConfig.TAG_SOURCE_URL), DEXConfig.ReadString(DEXConfig.TAG_SOURCE_URL));
            AddDetail(details, "Runtime", RuntimeInformation.FrameworkDescription);
            AddDetail(details, "Process", Environment.Is64BitProcess ? "64-bit" : "32-bit");

            FlowLayoutPanel buttons = new()
            {
                AutoSize = true,
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Margin = new Padding(0, 18, 0, 0)
            };

            Button closeButton = new()
            {
                Text = "Close",
                DialogResult = DialogResult.OK,
                Size = new Size(96, 28)
            };
            Button sourceButton = new()
            {
                Text = "Source Code",
                Size = new Size(104, 28),
                Margin = new Padding(8, 0, 0, 0)
            };
            Button updateButton = new()
            {
                Text = "Check Updates",
                Size = new Size(116, 28),
                Margin = new Padding(8, 0, 0, 0),
                Enabled = checkForUpdates != null
            };

            sourceButton.Click += delegate { OpenRepository(); };
            updateButton.Click += delegate { checkForUpdates?.Invoke(); };

            AcceptButton = closeButton;
            buttons.Controls.Add(closeButton);
            buttons.Controls.Add(updateButton);
            buttons.Controls.Add(sourceButton);

            root.Controls.Add(header, 0, 0);
            root.Controls.Add(details, 0, 1);
            root.Controls.Add(buttons, 0, 2);
            Controls.Add(root);
        }

        private void AddDetail(TableLayoutPanel details, string label, string value, string linkUrl = null)
        {
            int row = details.RowCount++;
            details.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            Label nameLabel = new()
            {
                AutoSize = true,
                Font = new Font(Font, FontStyle.Bold),
                Text = label,
                Margin = new Padding(0, 0, 12, 10)
            };
            details.Controls.Add(nameLabel, 0, row);

            Control valueControl;
            if (!string.IsNullOrWhiteSpace(linkUrl))
            {
                LinkLabel link = new()
                {
                    AutoSize = true,
                    Text = value,
                    Margin = new Padding(0, 0, 0, 10)
                };
                link.LinkClicked += delegate { OpenLink(linkUrl); };
                detailLinks.Add(link);
                valueControl = link;
            }
            else
            {
                valueControl = new Label()
                {
                    AutoSize = true,
                    Text = value,
                    Margin = new Padding(0, 0, 0, 10)
                };
            }

            details.Controls.Add(valueControl, 1, row);
        }

        private void ApplyTheme()
        {
            ThemeManager.ApplyControlTree(this);
            if (ThemeManager.IsDarkTheme)
            {
                foreach (LinkLabel link in detailLinks)
                {
                    link.LinkColor = Color.LightSkyBlue;
                    link.ActiveLinkColor = Color.White;
                    link.VisitedLinkColor = Color.LightSteelBlue;
                }
            }
        }

        private static string CleanVersion(string version)
        {
            return Regex.Replace(version ?? "", "\\+.*", "");
        }

        private static void OpenRepository()
        {
            OpenLink(DEXConfig.ReadString(DEXConfig.TAG_SOURCE_URL));
        }

        private static void OpenLink(string url)
        {
            _ = MyUtils.OpenShellTarget(url);
        }
    }
}
