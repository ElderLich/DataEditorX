/*
 * Created with SharpDevelop.
 * User: Acer
 * Date: 2014-10-12
 * Time: 19:43
 * 
 */
using DataEditorX.Common;
using DataEditorX.Config;
using DataEditorX.Language;
using System.ComponentModel;
using System.IO.Compression;

namespace DataEditorX.Core
{
    /// <summary>
    /// Task
    /// </summary>
    public class TaskHelper
    {
        #region Member
        /// <summary>
        /// Current task
        /// </summary>
        private MyTask nowTask = MyTask.NONE;
        /// <summary>
        /// Previous task
        /// </summary>
        private MyTask lastTask = MyTask.NONE;
        /// <summary>
        /// Card list
        /// </summary>
        private Card[] cardlist;
        /// <summary>
        /// Card list
        /// </summary>
        public Card[] CardList
        {
            get { return cardlist; }
        }
        /// <summary>
        /// Task parameters
        /// </summary>
        private string[] mArgs;
        /// <summary>
        /// Image settings
        /// </summary>
        private readonly ImageSet imgSet;
        /// <summary>
        /// Whether cancelled
        /// </summary>
        private bool isCancel = false;
        /// <summary>
        /// Whether running
        /// </summary>
        private bool isRun = false;
        /// <summary>
        /// Background worker
        /// </summary>
        private readonly BackgroundWorker worker;

        public TaskHelper(string datapath, BackgroundWorker worker)
        {
            Datapath = datapath;
            this.worker = worker;
            imgSet = new ImageSet();
        }
        public bool IsRuning()
        {
            return isRun;
        }
        public bool IsCancel()
        {
            return isCancel;
        }
        public void Cancel()
        {
            isRun = false;
            isCancel = true;
        }
        public MyTask GetLastTask()
        {
            return lastTask;
        }

        #endregion

        #region Other
        //Set task
        public void SetTask(MyTask myTask, Card[] cards, params string[] args)
        {
            nowTask = myTask;
            cardlist = cards;
            mArgs = args;
        }
        //Convert images
        //public void ToImg(string img, string saveimg1, string saveimg2)
        public void ToImg(string img, string saveimg1)
        {
            if (!File.Exists(img))
            {
                return;
            }

            Bitmap bmp = new(img);
            _ = MyBitmap.SaveAsJPEG(MyBitmap.Zoom(bmp, imgSet.W, imgSet.H),
                                saveimg1, imgSet.quality);
            //MyBitmap.SaveAsJPEG(MyBitmap.Zoom(bmp, imgSet.w, imgSet.h),
            //					saveimg2, imgSet.quilty);
            bmp.Dispose();
        }
        #endregion

        #region Check for updates
        public static void CheckVersion(bool showNew)
        {
            string updateUrl = DEXConfig.ReadString(DEXConfig.TAG_UPDATE_URL);
            string newver = CheckUpdate.GetNewVersion(updateUrl);
            if (newver == CheckUpdate.DEFAULT)
            {   //Version check failed
                if (!showNew)
                {
                    return;
                }

                MyMsg.Error(GetUpdateInfoFailureMessage(updateUrl));
                return;
            }

            if (CheckUpdate.CheckVersion(newver, Application.ProductVersion))
            {//New version is available
                if (!MyMsg.Question(LMSG.HaveNewVersion))
                {
                    return;
                }
            }
            else
            {//Already on the latest version
                if (!showNew)
                {
                    return;
                }

                MyMsg.Show(LMSG.NowIsNewVersion);
                return;
            }
            string downloadDir = MyPath.Combine(Path.GetTempPath(), "DataEditorX");
            MyPath.CreateDir(downloadDir);
            string zipFile = MyPath.Combine(downloadDir, "DataEditorX_" + newver + ".zip");
            //Download file
            if (CheckUpdate.DownLoad(zipFile))
            {
                if (MyMsg.Question("Update downloaded.\n\nInstall it now? DataEditorX will close and restart."))
                {
                    if (CheckUpdate.InstallUpdate(zipFile))
                    {
                        Application.Exit();
                    }
                    else
                    {
                        MyMsg.Show(GetInstallFailureMessage());
                    }
                }
                else
                {
                    MyMsg.Show("Update downloaded to:\n" + zipFile);
                }
            }
            else
            {
                MyMsg.Show(GetDownloadFailureMessage());
            }
        }

        private static string GetUpdateInfoFailureMessage(string updateUrl)
        {
            string reason = CheckUpdate.LastInfoStatus switch
            {
                CheckUpdate.UpdateInfoStatus.EmptyUrl => "The update metadata URL is empty.",
                CheckUpdate.UpdateInfoStatus.InvalidFormat => "The update metadata file exists, but it does not contain a valid DataEditorX version and release URL.",
                CheckUpdate.UpdateInfoStatus.Unavailable => "The update metadata file could not be reached.",
                _ => "The update metadata could not be checked."
            };
            string detail = string.IsNullOrWhiteSpace(CheckUpdate.LastInfoError)
                ? ""
                : "\n\nDetails: " + CheckUpdate.LastInfoError;

            return reason
                + "\n\nDataEditorX checks this file before looking for release zips:"
                + "\n" + updateUrl
                + "\n\nIf you have not pushed the repo/update metadata yet, or the GitHub repo is private, this is expected."
                + detail;
        }

        private static string GetDownloadFailureMessage()
        {
            string message = LanguageHelper.GetMsg(LMSG.DownloadFail);
            return string.IsNullOrWhiteSpace(CheckUpdate.LastDownloadError)
                ? message
                : message + "\n\nDetails: " + CheckUpdate.LastDownloadError;
        }

        private static string GetInstallFailureMessage()
        {
            return "Update downloaded, but the installer could not start."
                + "\n\nYou can still install manually from the downloaded archive.";
        }

        public static void OnCheckUpdate(bool showNew)
        {
            CheckVersion(showNew);
        }
        #endregion

        public string Datapath { get; }

        #region Export data
        public void ExportData(string path, string zipname, string _cdbfile, string modulescript)
        {
            int i = 0;
            Card[] cards = cardlist;
            if (cards == null || cards.Length == 0)
            {
                return;
            }

            int count = cards.Length;
            YgoPath ygopath = new(path);
            string name = Path.GetFileNameWithoutExtension(zipname);
            //Database
            string cdbfile = zipname + ".cdb";
            //Readme
            string readme = MyPath.Combine(path, name + ".txt");
            //New-card YDK
            string deckydk = ygopath.GetYdk(name);
            //module scripts
            string extra_script = "";
            if (modulescript.Length > 0)
            {
                extra_script = ygopath.GetModuleScript(modulescript);
            }

            File.Delete(cdbfile);
            _ = DataBase.Create(cdbfile);
            _ = DataBase.CopyDB(cdbfile, false, cardlist);
            if (File.Exists(zipname))
            {
                File.Delete(zipname);
            }

            using (ZipStorer zips = ZipStorer.Create(zipname, ""))
            {
                zips.AddFile(cdbfile, Path.GetFileNameWithoutExtension(_cdbfile) + ".cdb", "");
                if (File.Exists(readme))
                {
                    zips.AddFile(readme, "readme_" + name + ".txt", "");
                }

                if (File.Exists(deckydk))
                {
                    zips.AddFile(deckydk, "deck/" + name + ".ydk", "");
                }

                if (modulescript.Length > 0 && File.Exists(extra_script))
                {
                    zips.AddFile(extra_script, extra_script.Replace(path, ""), "");
                }

                foreach (Card c in cards)
                {
                    i++;
                    worker.ReportProgress(i / count, string.Format("{0}/{1}", i, count));
                    string[] files = ygopath.GetCardfiles(c.id);
                    foreach (string file in files)
                    {
                        if (!string.Equals(file, extra_script) && File.Exists(file))
                        {
                            zips.AddFile(file, file.Replace(path, ""), "");
                        }
                    }
                }
            }
            File.Delete(cdbfile);
        }
        #endregion

        #region Run
        public void Run()
        {
            isCancel = false;
            isRun = true;
            bool showNew;
            switch (nowTask)
            {
                case MyTask.ExportData:
                    if (mArgs != null && mArgs.Length >= 3)
                    {
                        ExportData(mArgs[0], mArgs[1], mArgs[2], mArgs[3]);
                    }
                    break;
                case MyTask.CheckUpdate:
                    showNew = false;
                    if (mArgs != null && mArgs.Length >= 1)
                    {
                        showNew = mArgs[0] == bool.TrueString;
                    }
                    OnCheckUpdate(showNew);
                    break;
            }
            isRun = false;
            lastTask = nowTask;
            nowTask = MyTask.NONE;
            cardlist = null;

            mArgs = null;
        }
        #endregion
    }

}
