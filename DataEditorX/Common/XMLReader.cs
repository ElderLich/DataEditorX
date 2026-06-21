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
                string path = Application.ExecutablePath + ".config";
                if (!File.Exists(path))
                    path = path.Replace(".exe", ".dll");
                return path;
            } 
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

            XmlElement xElem = (XmlElement)xNode.SelectSingleNode("//add[@key='" + appKey + "']");

            if (xElem != null)
            {
                return xElem.Attributes["value"].Value;
            }
            return string.Empty;
        }
        #endregion
    }
}
