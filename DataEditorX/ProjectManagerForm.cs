using DataEditorX.Config;
using DataEditorX.Core;
using WeifenLuo.WinFormsUI.Docking;

namespace DataEditorX
{
    public sealed class ProjectManagerForm : DockContent
    {
        private readonly ProjectManagerService service;
        private readonly List<Control> actionControls = new();

        private TextBox tbMdPro3Directory;
        private TextBox tbMdPro3DataDirectory;
        private TextBox tbCustomProjectDirectory;
        private TextBox tbVoicePackDirectory;
        private CheckBox chkActiveSync;
        private ListView lvResolvedPaths;
        private RichTextBox logBox;
        private ProjectFolderSynchronizer synchronizer;

        public ProjectManagerForm()
        {
            Name = "ProjectManagerForm";
            Text = "Project Manager";
            DockAreas = DockAreas.Document | DockAreas.DockLeft | DockAreas.DockRight | DockAreas.DockBottom;

            service = new ProjectManagerService(Log);

            InitializeComponent();
            LoadSettings();
            RefreshResolvedPaths();
            ApplyTheme();
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            TableLayoutPanel root = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(8)
            };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 65));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 35));

            root.Controls.Add(CreateEnvironmentGroup(), 0, 0);
            root.Controls.Add(CreateTabs(), 0, 1);
            root.Controls.Add(CreateLogGroup(), 0, 2);

            Controls.Add(root);
            ClientSize = new Size(900, 620);
            MinimumSize = new Size(760, 500);

            ResumeLayout(false);
        }

        private Control CreateEnvironmentGroup()
        {
            GroupBox group = new()
            {
                Name = "gb_environment",
                Text = "Environment",
                Dock = DockStyle.Top,
                AutoSize = true
            };

            TableLayoutPanel layout = CreateDirectoryLayout();
            tbMdPro3Directory = CreateDirectoryTextBox();
            tbMdPro3Directory.TextChanged += MdPro3DirectoryTextChanged;
            AddDirectoryRow(layout, 0, "MDPro3 Directory:", tbMdPro3Directory);
            tbMdPro3DataDirectory = CreateDirectoryTextBox();
            tbMdPro3DataDirectory.TextChanged += SharedDirectoryTextChanged;
            AddDirectoryRow(layout, 1, "MDPro3 Data Directory:", tbMdPro3DataDirectory);

            group.Controls.Add(layout);
            return group;
        }

        private Control CreateTabs()
        {
            TabControl tabs = new()
            {
                Name = "tabs",
                Dock = DockStyle.Fill
            };

            tabs.TabPages.Add(CreateProjectTab());
            tabs.TabPages.Add(CreateVoicePackTab());
            return tabs;
        }

        private TabPage CreateProjectTab()
        {
            TabPage tab = new()
            {
                Name = "tab_project",
                Text = "Custom Project",
                Padding = new Padding(8)
            };

            TableLayoutPanel layout = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3
            };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            TableLayoutPanel directoryLayout = CreateDirectoryLayout();
            tbCustomProjectDirectory = CreateDirectoryTextBox();
            tbCustomProjectDirectory.TextChanged += SharedDirectoryTextChanged;
            AddDirectoryRow(directoryLayout, 0, "Custom Project Directory:", tbCustomProjectDirectory);
            layout.Controls.Add(directoryLayout, 0, 0);

            FlowLayoutPanel actions = new()
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                Padding = new Padding(0, 6, 0, 6),
                WrapContents = true
            };

            Button installProject = CreateActionButton("Install", InstallProjectClick);
            Button uninstallProject = CreateActionButton("Uninstall", UninstallProjectClick);
            Button restartMdPro3 = CreateActionButton("(Re-)start MDPro3", RestartMdPro3Click);
            Button openCustomCdb = CreateActionButton("Open .cdb", OpenCustomCdbClick);
            chkActiveSync = new CheckBox
            {
                Name = "chk_activeSync",
                Text = "Active Sync",
                AutoSize = true,
                Margin = new Padding(12, 7, 3, 3)
            };
            chkActiveSync.CheckedChanged += ActiveSyncCheckedChanged;

            actions.Controls.Add(installProject);
            actions.Controls.Add(uninstallProject);
            actions.Controls.Add(restartMdPro3);
            actions.Controls.Add(openCustomCdb);
            actions.Controls.Add(chkActiveSync);
            layout.Controls.Add(actions, 0, 1);

            GroupBox pathsGroup = new()
            {
                Name = "gb_resolvedPaths",
                Text = "Resolved Paths",
                Dock = DockStyle.Fill
            };

            lvResolvedPaths = new ListView
            {
                Name = "lv_resolvedPaths",
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                HideSelection = false
            };
            lvResolvedPaths.Columns.Add("Asset", 120);
            lvResolvedPaths.Columns.Add("Custom Project", 320);
            lvResolvedPaths.Columns.Add("MDPro3", 320);
            lvResolvedPaths.Resize += (_, _) => ResizePathColumns();
            pathsGroup.Controls.Add(lvResolvedPaths);
            layout.Controls.Add(pathsGroup, 0, 2);

            tab.Controls.Add(layout);
            return tab;
        }

        private TabPage CreateVoicePackTab()
        {
            TabPage tab = new()
            {
                Name = "tab_voicePack",
                Text = "Voice Pack",
                Padding = new Padding(8)
            };

            TableLayoutPanel layout = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            TableLayoutPanel directoryLayout = CreateDirectoryLayout();
            tbVoicePackDirectory = CreateDirectoryTextBox();
            AddDirectoryRow(directoryLayout, 0, "Voice Pack Directory:", tbVoicePackDirectory);
            layout.Controls.Add(directoryLayout, 0, 0);

            FlowLayoutPanel actions = new()
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                Padding = new Padding(0, 6, 0, 6),
                WrapContents = true
            };
            actions.Controls.Add(CreateActionButton("Install Voice Pack", InstallVoicePackClick));
            actions.Controls.Add(CreateActionButton("Uninstall Voice Pack", UninstallVoicePackClick));
            layout.Controls.Add(actions, 0, 1);

            tab.Controls.Add(layout);
            return tab;
        }

        private Control CreateLogGroup()
        {
            GroupBox group = new()
            {
                Name = "gb_log",
                Text = "Log",
                Dock = DockStyle.Fill
            };

            TableLayoutPanel layout = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            logBox = new RichTextBox
            {
                Name = "logBox",
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BorderStyle = BorderStyle.FixedSingle,
                DetectUrls = false
            };
            layout.Controls.Add(logBox, 0, 0);

            FlowLayoutPanel actions = new()
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true
            };
            Button clearLog = new()
            {
                Name = "btn_clearLog",
                Text = "Clear Log",
                AutoSize = true,
                Margin = new Padding(3, 6, 3, 3)
            };
            clearLog.Click += (_, _) => logBox.Clear();
            actions.Controls.Add(clearLog);
            layout.Controls.Add(actions, 0, 1);

            group.Controls.Add(layout);
            return group;
        }

        private static TableLayoutPanel CreateDirectoryLayout()
        {
            TableLayoutPanel layout = new()
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 3,
                Padding = new Padding(6)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            return layout;
        }

        private void AddDirectoryRow(TableLayoutPanel layout, int row, string labelText, TextBox textBox)
        {
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            Label label = new()
            {
                Text = labelText,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(3, 7, 8, 3)
            };

            Button browse = new()
            {
                Text = "Browse...",
                AutoSize = true,
                Anchor = AnchorStyles.Right
            };
            browse.Click += (_, _) => BrowseDirectory(textBox);

            layout.Controls.Add(label, 0, row);
            layout.Controls.Add(textBox, 1, row);
            layout.Controls.Add(browse, 2, row);
        }

        private static TextBox CreateDirectoryTextBox()
        {
            return new TextBox
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Margin = new Padding(3, 4, 3, 3)
            };
        }

        private Button CreateActionButton(string text, EventHandler onClick)
        {
            Button button = new()
            {
                Text = text,
                AutoSize = true,
                Margin = new Padding(3, 3, 6, 3)
            };
            button.Click += onClick;
            actionControls.Add(button);
            return button;
        }

        private void BrowseDirectory(TextBox target)
        {
            using FolderBrowserDialog dialog = new();
            if (Directory.Exists(target.Text))
            {
                dialog.SelectedPath = target.Text;
            }

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                target.Text = dialog.SelectedPath;
                SaveSettings();
            }
        }

        private void LoadSettings()
        {
            tbMdPro3Directory.Text = DEXConfig.ReadString(DEXConfig.TAG_PROJECT_MANAGER_MDPRO3_DIR);
            tbMdPro3DataDirectory.Text = DEXConfig.ReadString(DEXConfig.TAG_PROJECT_MANAGER_MDPRO3_DATA_DIR);
            if (string.IsNullOrWhiteSpace(tbMdPro3DataDirectory.Text) && !string.IsNullOrWhiteSpace(tbMdPro3Directory.Text))
            {
                tbMdPro3DataDirectory.Text = Path.Combine(tbMdPro3Directory.Text, "Data");
            }

            tbCustomProjectDirectory.Text = DEXConfig.ReadString(DEXConfig.TAG_PROJECT_MANAGER_CUSTOM_PROJECT_DIR);
            tbVoicePackDirectory.Text = DEXConfig.ReadString(DEXConfig.TAG_PROJECT_MANAGER_VOICE_PACK_DIR);
        }

        private void SaveSettings()
        {
            DEXConfig.Save(DEXConfig.TAG_PROJECT_MANAGER_MDPRO3_DIR, tbMdPro3Directory.Text);
            DEXConfig.Save(DEXConfig.TAG_PROJECT_MANAGER_MDPRO3_DATA_DIR, tbMdPro3DataDirectory.Text);
            DEXConfig.Save(DEXConfig.TAG_PROJECT_MANAGER_CUSTOM_PROJECT_DIR, tbCustomProjectDirectory.Text);
            DEXConfig.Save(DEXConfig.TAG_PROJECT_MANAGER_VOICE_PACK_DIR, tbVoicePackDirectory.Text);
        }

        private void MdPro3DirectoryTextChanged(object sender, EventArgs e)
        {
            string dataDirectory = string.IsNullOrWhiteSpace(tbMdPro3Directory.Text)
                ? string.Empty
                : Path.Combine(tbMdPro3Directory.Text, "Data");
            if (string.IsNullOrWhiteSpace(tbMdPro3DataDirectory.Text)
                || !Directory.Exists(tbMdPro3DataDirectory.Text))
            {
                tbMdPro3DataDirectory.Text = dataDirectory;
            }

            SharedDirectoryTextChanged(sender, e);
        }

        private void SharedDirectoryTextChanged(object sender, EventArgs e)
        {
            if (chkActiveSync != null && chkActiveSync.Checked)
            {
                chkActiveSync.Checked = false;
                Log("Active sync was stopped because a project path changed.", ProjectManagerLogLevel.Warning);
            }

            RefreshResolvedPaths();
        }

        private async void InstallProjectClick(object sender, EventArgs e)
        {
            await RunOperationAsync(() => service.InstallProject(tbMdPro3Directory.Text, tbCustomProjectDirectory.Text));
        }

        private async void UninstallProjectClick(object sender, EventArgs e)
        {
            await RunOperationAsync(() => service.UninstallProject(tbMdPro3Directory.Text, tbCustomProjectDirectory.Text));
        }

        private async void RestartMdPro3Click(object sender, EventArgs e)
        {
            await RunOperationAsync(() => service.RestartMdPro3(tbMdPro3Directory.Text));
        }

        private void OpenCustomCdbClick(object sender, EventArgs e)
        {
            SaveSettings();

            string expansionsDirectory = ProjectManagerService.BuildPaths(tbCustomProjectDirectory.Text).Expansions;
            if (!Directory.Exists(expansionsDirectory))
            {
                _ = MessageBox.Show(this, $"Custom project Expansions folder was not found:\n{expansionsDirectory}",
                    "Project Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string[] cdbFiles = Directory.GetFiles(expansionsDirectory, "*.cdb", SearchOption.TopDirectoryOnly);
            string fileToOpen = null;
            if (cdbFiles.Length == 1)
            {
                fileToOpen = cdbFiles[0];
            }
            else
            {
                using OpenFileDialog dialog = new()
                {
                    InitialDirectory = expansionsDirectory,
                    Filter = "Card databases (*.cdb)|*.cdb|All files (*.*)|*.*",
                    Title = "Open custom project database"
                };

                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                fileToOpen = dialog.FileName;
            }

            if (DockPanel?.FindForm() is MainForm mainForm)
            {
                mainForm.Open(fileToOpen);
                Log($"Opened custom database: {fileToOpen}", ProjectManagerLogLevel.Success);
            }
            else
            {
                Log("Could not find the main workspace window.", ProjectManagerLogLevel.Error);
            }
        }

        private async void InstallVoicePackClick(object sender, EventArgs e)
        {
            await RunOperationAsync(() => service.InstallVoicePack(tbVoicePackDirectory.Text, tbMdPro3Directory.Text));
        }

        private async void UninstallVoicePackClick(object sender, EventArgs e)
        {
            await RunOperationAsync(() => service.UninstallVoicePack(tbVoicePackDirectory.Text, tbMdPro3Directory.Text));
        }

        private async Task RunOperationAsync(Action operation)
        {
            SaveSettings();
            SetBusy(true);
            try
            {
                await Task.Run(operation);
            }
            catch (Exception ex)
            {
                Log(ex.Message, ProjectManagerLogLevel.Error);
                _ = MessageBox.Show(this, ex.Message, "Project Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void ActiveSyncCheckedChanged(object sender, EventArgs e)
        {
            if (chkActiveSync.Checked)
            {
                SaveSettings();
                synchronizer = new ProjectFolderSynchronizer(tbCustomProjectDirectory.Text, tbMdPro3Directory.Text, Log);
                if (!synchronizer.Start())
                {
                    chkActiveSync.CheckedChanged -= ActiveSyncCheckedChanged;
                    chkActiveSync.Checked = false;
                    chkActiveSync.CheckedChanged += ActiveSyncCheckedChanged;
                    synchronizer.Dispose();
                    synchronizer = null;
                }
            }
            else
            {
                StopSynchronization();
            }
        }

        private void StopSynchronization()
        {
            synchronizer?.Dispose();
            synchronizer = null;
        }

        private void SetBusy(bool busy)
        {
            foreach (Control control in actionControls)
            {
                control.Enabled = !busy;
            }

            chkActiveSync.Enabled = !busy;
            Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
        }

        private void RefreshResolvedPaths()
        {
            if (lvResolvedPaths == null)
            {
                return;
            }

            ProjectInstallPaths source = ProjectManagerService.BuildPaths(tbCustomProjectDirectory.Text);
            ProjectInstallPaths destination = ProjectManagerService.BuildPaths(tbMdPro3Directory.Text);
            (string Name, string Source, string Destination)[] rows =
            {
                ("Expansions", source.Expansions, destination.Expansions),
                ("Scripts", source.Scripts, destination.Scripts),
                ("Closeups", source.Closeups, destination.Closeups),
                ("Art2", source.Art2, destination.Art2),
                ("OverFrame", source.OverFrame, destination.OverFrame),
                ("MonsterCutin2", source.MonsterCutin2, destination.MonsterCutin2),
                ("SpecialCards.json", "", Path.Combine(tbMdPro3DataDirectory.Text, "SpecialCards.json"))
            };

            lvResolvedPaths.BeginUpdate();
            lvResolvedPaths.Items.Clear();
            for (int i = 0; i < rows.Length; i++)
            {
                ListViewItem item = new(rows[i].Name)
                {
                    Tag = i
                };
                item.SubItems.Add(rows[i].Source);
                item.SubItems.Add(rows[i].Destination);
                lvResolvedPaths.Items.Add(item);
                ThemeManager.ApplyListViewItem(item);
            }
            lvResolvedPaths.EndUpdate();
            ResizePathColumns();
        }

        private void ResizePathColumns()
        {
            if (lvResolvedPaths.Columns.Count < 3)
            {
                return;
            }

            int available = Math.Max(320, lvResolvedPaths.ClientSize.Width - SystemInformation.VerticalScrollBarWidth);
            lvResolvedPaths.Columns[0].Width = 120;
            int pathWidth = Math.Max(180, (available - 120) / 2);
            lvResolvedPaths.Columns[1].Width = pathWidth;
            lvResolvedPaths.Columns[2].Width = pathWidth;
        }

        private void Log(string message, ProjectManagerLogLevel level)
        {
            if (IsDisposed)
            {
                return;
            }

            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(new Action(() => Log(message, level)));
                }
                catch
                {
                }
                return;
            }

            Color color = LogColor(level);
            logBox.SelectionStart = logBox.TextLength;
            logBox.SelectionLength = 0;
            logBox.SelectionColor = color;
            logBox.AppendText($"{DateTime.Now:HH:mm:ss} {level}: {message}{Environment.NewLine}");
            logBox.SelectionColor = logBox.ForeColor;
            logBox.ScrollToCaret();
        }

        private static Color LogColor(ProjectManagerLogLevel level)
        {
            ThemeManager.ThemePalette palette = ThemeManager.CurrentPalette;
            return level switch
            {
                ProjectManagerLogLevel.Success => palette.IsDark ? Color.FromArgb(110, 220, 140) : Color.ForestGreen,
                ProjectManagerLogLevel.Warning => palette.IsDark ? Color.FromArgb(245, 200, 100) : Color.DarkGoldenrod,
                ProjectManagerLogLevel.Error => palette.IsDark ? Color.FromArgb(255, 120, 120) : Color.Firebrick,
                _ => palette.TextColor
            };
        }

        public void ApplyTheme()
        {
            ThemeManager.ApplyControlTree(this);
            ThemeManager.ApplyListViewItems(lvResolvedPaths);

            ThemeManager.ThemePalette palette = ThemeManager.CurrentPalette;
            logBox.BackColor = palette.InputBackColor;
            logBox.ForeColor = palette.TextColor;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            SaveSettings();
            StopSynchronization();
            base.OnFormClosing(e);
        }
    }
}
