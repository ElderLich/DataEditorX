using DataEditorX.Config;
using DataEditorX.Language;
using System.Text;

namespace DataEditorX.Core
{
    public class CardEdit
    {
        readonly IDataForm dataform;
        public AddCommand addCard;
        public ModCommand modCard;
        public DelCommand delCard;
        public CopyCommand copyCard;

        public CardEdit(IDataForm dataform)
        {
            this.dataform = dataform;
            addCard = new AddCommand(this);
            modCard = new ModCommand(this);
            delCard = new DelCommand(this);
            copyCard = new CopyCommand(this);
        }

        #region Add
        //Add
        public class AddCommand : IBackableCommand
        {
            private string undoSQL;
            readonly IDataForm dataform;
            public AddCommand(CardEdit cardedit)
            {
                dataform = cardedit.dataform;
            }

            public bool Execute(params object[] args)
            {
                if (!dataform.CheckOpen())
                {
                    return false;
                }

                Card c = dataform.GetCard();
                if (c.id <= 0)//Card ID must be greater than 0
                {
                    MyMsg.Error(LMSG.CodeCanNotIsZero);
                    return false;
                }
                else if (c.id > 999999999)
                {
                    MyMsg.Error(LMSG.AddFail);
                    return false;
                }
                else if (c.omega[0] > 0)
                {
                    if (c.ot > 0x7)
                    {
                        MyMsg.Error(LMSG.AddFail);
                        return false;
                    }
                    else if (c.id > uint.MaxValue / 16)
                        MyMsg.Warning("Strings will fail to show up for this passcode.");
                }
                Card[] cards = dataform.GetCardList(false);
                foreach (Card ckey in cards)//Card ID already exists
                {
                    if (c.id == ckey.id)
                    {
                        MyMsg.Warning(LMSG.ItIsExists);
                        return false;
                    }
                }
                if (DataBase.Command(dataform.GetOpenFile(),
                    DataBase.IsOmegaDatabase(dataform.GetOpenFile()) ? DataBase.OmegaGetInsertSQL(c, true) : DataBase.GetInsertSQL(c, true)) >= 2)
                {
                    MyMsg.Show(LMSG.AddSucceed);
                    undoSQL = DataBase.GetDeleteSQL(c);
                    dataform.Search(true);
                    dataform.SetCard(c);
                    return true;
                }
                MyMsg.Error(LMSG.AddFail);
                return false;
            }
            public void Undo()
            {
                _ = DataBase.Command(dataform.GetOpenFile(), undoSQL);
            }

            public object Clone()
            {
                return MemberwiseClone();
            }
        }
        #endregion

        #region Modify
        //Modify
        public class ModCommand : IBackableCommand
        {
            private string undoSQL;
            private bool modifiled = false;
            private long oldid;
            private long newid;
            private bool delold;
            readonly IDataForm dataform;
            public ModCommand(CardEdit cardedit)
            {
                dataform = cardedit.dataform;
            }

            public bool Execute(params object[] args)
            {
                if (!dataform.CheckOpen())
                {
                    return false;
                }

                bool modfiles = (bool)args[0];

                Card c = dataform.GetCard();
                Card oldCard = dataform.GetOldCard();
                if (c.Equals(oldCard))//No changes to save
                {
                    MyMsg.Show(LMSG.ItIsNotChanged);
                    return false;
                }
                if (c.id <= 0)
                {
                    MyMsg.Error(LMSG.CodeCanNotIsZero);
                    return false;
                }
                string sql;
                bool oldCardDeleted = false;
                if (c.id != oldCard.id)//ID was changed
                {
                    sql = DataBase.IsOmegaDatabase(dataform.GetOpenFile()) ? DataBase.OmegaGetInsertSQL(c, false) : DataBase.GetInsertSQL(c, false);// Restore by inserting the card.
                    bool delold = MyMsg.Question(LMSG.IfDeleteCard);
                    if (delold)//Whether to delete the old card
                    {
                        if (DataBase.Command(dataform.GetOpenFile(),
                            DataBase.GetDeleteSQL(oldCard)) < 2)
                        {
                            //Delete failed
                            MyMsg.Error(LMSG.DeleteFail);
                            delold = false;
                        }
                        else
                        {//Delete succeeded; add restore SQL
                            undoSQL = DataBase.GetDeleteSQL(c) + (DataBase.IsOmegaDatabase(dataform.GetOpenFile()) ? DataBase.OmegaGetInsertSQL(oldCard, false) : DataBase.GetInsertSQL(oldCard, false));
                            oldCardDeleted = true;
                        }
                    }
                    else
                    {
                        undoSQL = DataBase.GetDeleteSQL(c);//Restore action is delete
                    }
                    //Rename resources when deleting the old card; otherwise copy resources
                    if (modfiles)
                    {
                        if (delold)
                        {
                            YGOUtil.CardRename(c.id, oldCard.id, dataform.GetPath());
                        }
                        else
                        {
                            YGOUtil.CardCopy(c.id, oldCard.id, dataform.GetPath());
                        }

                        modifiled = true;
                        oldid = oldCard.id;
                        newid = c.id;
                        this.delold = delold;
                    }
                }
                else
                {//Update data
                    sql = DataBase.IsOmegaDatabase(dataform.GetOpenFile()) ? DataBase.OmegaGetUpdateSQL(c) : DataBase.GetUpdateSQL(c);
                    undoSQL = DataBase.IsOmegaDatabase(dataform.GetOpenFile()) ? DataBase.OmegaGetUpdateSQL(oldCard) : DataBase.GetUpdateSQL(oldCard);
                }
                if (DataBase.Command(dataform.GetOpenFile(), sql) > 0)
                {
                    MyMsg.Show(LMSG.ModifySucceed);
                    dataform.SetCard(c);
                    dataform.RefreshModifiedCard(oldCard, c, oldCardDeleted);
                    return true;
                }
                else
                {
                    MyMsg.Error(LMSG.ModifyFail);
                }

                return false;
            }

            public void Undo()
            {
                _ = DataBase.Command(dataform.GetOpenFile(), undoSQL);
                if (modifiled)
                {
                    if (delold)
                    {
                        YGOUtil.CardRename(oldid, newid, dataform.GetPath());
                    }
                    else
                    {
                        YGOUtil.CardDelete(newid, dataform.GetPath());
                    }
                }
            }

            public object Clone()
            {
                return MemberwiseClone();
            }
        }
        #endregion

        #region Delete
        //Delete
        public class DelCommand : IBackableCommand
        {
            private string undoSQL;
            readonly IDataForm dataform;
            public DelCommand(CardEdit cardedit)
            {
                dataform = cardedit.dataform;
            }

            public bool Execute(params object[] args)
            {
                if (!dataform.CheckOpen())
                {
                    return false;
                }

                bool deletefiles = (bool)args[0];

                Card[] cards = dataform.GetCardList(true);
                if (cards == null || cards.Length == 0)
                {
                    return false;
                }

                string undo = "";
                if (!MyMsg.Question(LMSG.IfDeleteCard))
                {
                    return false;
                }

                List<string> sql = new();
                foreach (Card c in cards)
                {
                    sql.Add(DataBase.GetDeleteSQL(c));//Delete
                    undo += DataBase.IsOmegaDatabase(dataform.GetOpenFile()) ? DataBase.OmegaGetInsertSQL(c, true) : DataBase.GetInsertSQL(c, true);
                    //Load related-file deletion setting
                    if (deletefiles)
                    {
                        YGOUtil.CardDelete(c.id, dataform.GetPath());
                    }
                }
                if (DataBase.Command(dataform.GetOpenFile(), sql.ToArray()) >= (sql.Count * 2))
                {
                    MyMsg.Show(LMSG.DeleteSucceed);
                    dataform.Search(true);
                    undoSQL = undo;
                    return true;
                }
                else
                {
                    MyMsg.Error(LMSG.DeleteFail);
                    dataform.Search(true);
                }
                return false;
            }
            public void Undo()
            {
                _ = DataBase.Command(dataform.GetOpenFile(), undoSQL);
            }

            public object Clone()
            {
                return MemberwiseClone();
            }
        }
        #endregion

        #region Open script
        //Open script
        public bool OpenScript(bool openinthis, string defaultScriptName)
        {
            if (!dataform.CheckOpen())
            {
                return false;
            }

            Card c = dataform.GetCard();
            uint id = c.id;
            string lua;
            if (c.id > 0)
            {
                lua = dataform.GetPath().GetScript(id);
                if (c.omega[0] > 0 && (DataBase.IsOmegaDatabase(dataform.GetOpenFile()) || !File.Exists(lua)))
                {
                    lua = MyPath.Combine(dataform.GetPath().gamepath, "../Scripts", "c" + id + ".lua");
                    if (c.omega[0] > 0 && !string.IsNullOrEmpty(c.script)
                        && !byte.TryParse(c.script, out _) && !File.Exists(lua) && openinthis)
                    {
                        DEXConfig.OpenFileInThis(id.ToString() + "```" + c.script);
                        return true;
                    }
                }
            }
            else if (!string.IsNullOrWhiteSpace(defaultScriptName))
            {
                lua = dataform.GetPath().GetModuleScript(defaultScriptName);
            }
            else
            {
                return false;
            }
            if (!File.Exists(lua))
            {
                if (c.omega[0] > 0 && !string.IsNullOrEmpty(c.script) && !byte.TryParse(c.script, out _)
                    && !openinthis)
                {
                    MyPath.CreateDirByFile(lua);
                    using FileStream fs = new(lua,
                        FileMode.OpenOrCreate, FileAccess.Write);
                    StreamWriter sw = new(fs, new UTF8Encoding(false));
                    sw.Write(c.script);
                    sw.Close();
                    fs.Close();
                }
                if (MyMsg.Question(LMSG.IfCreateScript))//Ask whether to create the script
                {
                    MyPath.CreateDirByFile(lua);
                    using FileStream fs = new(lua,
                        FileMode.OpenOrCreate, FileAccess.Write);
                    StreamWriter sw = new(fs, new UTF8Encoding(false));
                    if (c.id > 0)
                    {
                        sw.WriteLine("--" + c.name);
                        sw.WriteLine("local s,id,o=GetID()");
                        sw.WriteLine("function s.initial_effect(c)");
                        sw.WriteLine("\t");
                        sw.WriteLine("end");
                    }
                    else
                    {
                        sw.WriteLine("--" + Path.GetFileNameWithoutExtension(lua));
                    }
                    sw.Close();
                    fs.Close();
                }
            }
            if (File.Exists(lua))//Open the file when it exists
            {
                if (openinthis)//Whether to open with this program
                {
                    DEXConfig.OpenFileInThis(lua);
                }
                else
                {
                    return DataEditorX.Common.MyUtils.OpenShellTarget(lua);
                }

                return true;
            }
            return false;
        }
        #endregion

        #region Copy cards
        public class CopyCommand : IBackableCommand
        {
            bool copied = false;
            Card[] newCards;
            bool replace;
            Card[] oldCards;
            readonly CardEdit cardedit;
            readonly IDataForm dataform;
            public CopyCommand(CardEdit cardedit)
            {
                this.cardedit = cardedit;
                dataform = cardedit.dataform;
            }

            public bool Execute(params object[] args)
            {
                if (!dataform.CheckOpen())
                    return false;

                Card[] cards = (Card[])args[0];

                if (cards == null || cards.Length == 0)
                    return false;
                foreach (Card c in cards) c.omega[0] = DataBase.IsOmegaDatabase(dataform.GetOpenFile()) ? 1 : 0;

                bool replace = false;
                Card[] oldcards = DataBase.Read(dataform.GetOpenFile(), true, "");
                if (oldcards != null && oldcards.Length != 0)
                {
                    int i = 0;
                    foreach (Card oc in oldcards) {
                        foreach (Card c in cards) {
                            if (c.id == oc.id) {
                                if (++i == 1)
                                {
                                    replace = MyMsg.Question(LMSG.IfReplaceExistingCard);
                                    break;
                                }
                            }
                        }
                        if (i > 0) break;
                    }
                }
                _ = DataBase.CopyDB(dataform.GetOpenFile(), !replace, cards);
                copied = true;
                newCards = cards;
                this.replace = replace;
                oldCards = oldcards;
                return true;
            }
            public void Undo()
            {
                _ = DataBase.DeleteDB(dataform.GetOpenFile(), newCards);
                _ = DataBase.CopyDB(dataform.GetOpenFile(), !replace, oldCards);
            }

            public object Clone()
            {
                CopyCommand replica = new(cardedit)
                {
                    copied = copied,
                    newCards = (Card[])newCards.Clone(),
                    replace = replace
                };
                if (oldCards != null)
                {
                    replica.oldCards = (Card[])oldCards.Clone();
                }

                return replica;
            }
        }
        #endregion
    }
}
