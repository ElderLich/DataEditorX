namespace DataEditorX.Config
{
    public class YgoPath
    {
        public YgoPath(string gamepath)
        {
            SetPath(gamepath);
        }
        public void SetPath(string gamepath)
        {
            this.gamepath = gamepath;
            picpath = MyPath.Combine(gamepath, "pics");
            fieldpath = MyPath.Combine(picpath, "field");
            picpath2 = MyPath.Combine(picpath, "thumbnail");
            luapath = MyPath.Combine(gamepath, "script");
            ydkpath = MyPath.Combine(gamepath, "deck");
            replaypath = MyPath.Combine(gamepath, "replay");
        }
        /// <summary>Game directory</summary>
        public string gamepath;
        /// <summary>Full-size image directory</summary>
        public string picpath;
        /// <summary>Thumbnail directory</summary>
        public string picpath2;
        /// <summary>Field image directory</summary>
        public string fieldpath;
        /// <summary>Script directory</summary>
        public string luapath;
        /// <summary>Deck directory</summary>
        public string ydkpath;
        /// <summary>Replay directory</summary>
        public string replaypath;

        public string GetImage(long id)
        {
            return GetImage(id.ToString());
        }
        //public string GetImageThum(long id)
        //{
        //	return GetImageThum(id.ToString());
        //}
        public string GetImageField(long id)
        {
            return GetImageField(id.ToString());//Field image
        }
        public string GetScript(long id)
        {
            return GetScript(id.ToString());
        }
        public string GetYdk(string name)
        {
            return MyPath.Combine(ydkpath, name + ".ydk");
        }
        //String ID
        public string GetImage(string id)
        {
            string img = MyPath.Combine(picpath, id + ".png");
            if (!File.Exists(img)) img = MyPath.Combine(picpath, id + ".jpg");
            return img;
        }
        //public string GetImageThum(string id)
        //{
        //	return MyPath.Combine(picpath2, id + ".jpg");
        //}
        public string GetImageField(string id)
        {
            return MyPath.Combine(fieldpath, id + ".png");//Field image
        }
        public string GetScript(string id)
        {
            return MyPath.Combine(luapath, "c" + id + ".lua");
        }
        public string GetModuleScript(string modulescript)
        {
            return MyPath.Combine(luapath, modulescript + ".lua");
        }

        public string[] GetCardfiles(long id)
        {
            string[] files = [
                GetImage(id),//Full-size image
				//GetImageThum(id),//Thumbnail
				GetImageField(id),//Field image
				GetScript(id)
           ];
            return files;
        }
        public string[] GetCardfiles(string id)
        {
            string[] files = [
                GetImage(id),//Full-size image
				//GetImageThum(id),//Thumbnail
				GetImageField(id),//Field image
				GetScript(id)
           ];
            return files;
        }
    }
}
