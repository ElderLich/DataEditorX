using System.Xml;

namespace DataEditorX.Common
{
    public class XMLReader
    {
        #region XML config operations
        /// <summary>
        /// SaveValue
        /// </summary>
        /// <param name="appKey"></param>
        /// <param name="appValue"></param>
        public static void Save(string appKey, string appValue)
        {
            XmlDocument xDoc = new();
            xDoc.Load(XMLConfigFile);

            XmlNode xNode = xDoc.SelectSingleNode("//appSettings");
            if (xNode == null)
            {
                XmlElement appSettings = xDoc.CreateElement("appSettings");
                _ = xDoc.DocumentElement.AppendChild(appSettings);
                xNode = appSettings;
            }

            XmlElement xElem = (XmlElement)xNode.SelectSingleNode("//add[@key='" + appKey + "']");
            if (xElem != null) //Update when the entry exists
            {
                xElem.SetAttribute("value", appValue);
            }
            else//Insert when the entry is missing
            {
                XmlElement xNewElem = xDoc.CreateElement("add");
                xNewElem.SetAttribute("key", appKey);
                xNewElem.SetAttribute("value", appValue);
                _ = xNode.AppendChild(xNewElem);
            }
            xDoc.Save(XMLConfigFile);
        }
        static string XMLConfigFile
        {
            get
            {
                return EnsureUserConfigFile();
            }
        }

        static string BundledConfigFile
        {
            get
            {
                string path = Application.ExecutablePath + ".config";
                if (!File.Exists(path))
                {
                    path = path.Replace(".exe", ".dll");
                }
                return path;
            }
        }

        static string UserConfigFile
        {
            get
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                return Path.Combine(appData, "DataEditorX", "DataEditorX.config");
            }
        }

        static string EnsureUserConfigFile()
        {
            string userConfig = UserConfigFile;
            if (File.Exists(userConfig))
            {
                return userConfig;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(userConfig));
            string bundledConfig = BundledConfigFile;
            if (File.Exists(bundledConfig))
            {
                File.Copy(bundledConfig, userConfig);
            }
            else
            {
                XmlDocument xDoc = new();
                XmlElement root = xDoc.CreateElement("configuration");
                _ = xDoc.AppendChild(root);
                _ = root.AppendChild(xDoc.CreateElement("appSettings"));
                xDoc.Save(userConfig);
            }
            return userConfig;
        }
        /// <summary>
        /// Get value
        /// </summary>
        /// <param name="appKey"></param>
        /// <returns></returns>
        public static string GetAppConfig(string appKey)
        {
            XmlDocument xDoc = new();
            xDoc.Load(XMLConfigFile);

            XmlNode xNode = xDoc.SelectSingleNode("//appSettings");
            XmlElement xElem = (XmlElement)xNode?.SelectSingleNode("//add[@key='" + appKey + "']");

            if (xElem != null)
            {
                return xElem.Attributes["value"].Value;
            }

            string bundledConfig = BundledConfigFile;
            if (File.Exists(bundledConfig))
            {
                XmlDocument bundledDoc = new();
                bundledDoc.Load(bundledConfig);
                XmlElement bundledElem = (XmlElement)bundledDoc.SelectSingleNode("//appSettings/add[@key='" + appKey + "']");
                if (bundledElem != null)
                {
                    return bundledElem.Attributes["value"].Value;
                }
            }

            return string.Empty;
        }
        #endregion
    }
}
