/*
 * Created with SharpDevelop.
 * User: Acer
 * Date: May 18, Sunday
 * Time: 17:01
 * 
 */
using Microsoft.Data.Sqlite;
using System.Text;

namespace DataEditorX.Core
{
    /// <summary>
    /// SQLite helper methods.
    /// </summary>
    public static class DataBase
    {
        #region Defaults
        static readonly string _defaultSQL;
        static readonly string _defaultTableSQL;
        static readonly string _defaultOTableSQL;

        static DataBase()
        {
            _defaultSQL = "SELECT * FROM datas NATURAL JOIN texts WHERE datas.id > 0";
            StringBuilder st = new();
            _ = st.Append(@"CREATE TABLE texts(id integer primary key,name text,desc text");
            for (int i = 1; i <= 16; i++)
            {
                _ = st.Append(",str");
                _ = st.Append(i);
                _ = st.Append(" text");
            }
            _ = st.Append(");");
            _ = st.Append(@"CREATE TABLE datas(");
            _ = st.Append("id integer primary key,ot integer,alias integer,");
            _ = st.Append("setcode integer,type integer,atk integer,def integer,");
            _ = st.Append("level integer,race integer,attribute integer,category integer) ");
            _defaultTableSQL = st.ToString();
            _ = st.Remove(0, st.Length);
            StringBuilder ost = new();
            _ = ost.Append(@"CREATE TABLE IF NOT EXISTS texts(id integer primary key,name text,desc text");
            for (int i = 1; i <= 16; i++)
            {
                _ = ost.Append(",str");
                _ = ost.Append(i);
                _ = ost.Append(" text");
            }
            _ = ost.Append(");");
            _ = ost.Append(@"CREATE TABLE IF NOT EXISTS datas(id integer primary key default 0,
            ot integer default 0,alias integer default 0,setcode blob,
            type integer default 0,atk integer default 0,def integer default 0,level integer default 0,
            race integer default 0,attribute integer default 0,category integer default 0,
            genre integer default 0,script blob,support blob);");
            _defaultOTableSQL = ost.ToString();
            _ = ost.Remove(0, ost.Length);
        }
        #endregion

        #region Database creation
        /// <summary>
        /// Creates a new card database.
        /// </summary>
        /// <param name="Db">Destination database path.</param>
        public static bool Create(string Db)
        {
            if (File.Exists(Db))
            {
                File.Delete(Db);
            }
            using SqliteConnection con = new(@"Data Source=" + Db);
            con.Open();
            con.Close();
            SqliteConnection.ClearAllPools();
            try
            {
                _ = Db.EndsWith(".cdb", StringComparison.OrdinalIgnoreCase) ? Command(Db, _defaultTableSQL) : Command(Db, _defaultOTableSQL);
            }
            catch
            {
                return false;
            }
            return true;
        }
        public static bool CheckTable(string db)
        {
            if (!File.Exists(db))
            {
                return false;
            }

            try
            {
                using SqliteConnection con = new(@"Data Source=" + db);
                con.Open();
                using SqliteCommand cmd = con.CreateCommand();
                cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name IN ('datas','texts')";
                object count = cmd.ExecuteScalar();
                return Convert.ToInt64(count) == 2;
            }
            catch
            {
                return false;
            }
            finally
            {
                SqliteConnection.ClearAllPools();
            }
        }
        #endregion

        #region SQL execution
        /// <summary>
        /// Executes one or more SQL statements in a single transaction.
        /// </summary>
        /// <param name="DB">Database path.</param>
        /// <param name="SQLs">SQL statements to execute.</param>
        /// <returns>Total affected rows, or -1 when the transaction fails.</returns>
        public static int Command(string DB, params string[] SQLs)
        {
            int result = 0;
            if (File.Exists(DB) && SQLs != null)
            {
                using SqliteConnection con = new(@"Data Source=" + DB);
                con.Open();
                using (SqliteTransaction trans = con.BeginTransaction())
                {
                    try
                    {
                        foreach (string SQLstr in SQLs)
                        {
                            using SqliteCommand cmd = con.CreateCommand();
                            cmd.CommandText = SQLstr;
                            result += cmd.ExecuteNonQuery();
                        }
                    }
                    catch
                    {
                        trans.Rollback();
                        result = -1;
                    }
                    if (result != -1) trans.Commit();
                }
                con.Close();
                SqliteConnection.ClearAllPools();
            }
            return result;
        }
        #endregion

        #region SQL reads
        static Card ReadCard(SqliteDataReader reader, bool reNewLine)
        {
            Card c = new(0)
            {
                id = (uint)reader.GetInt32(reader.GetOrdinal("id")),
                ot = reader.GetInt32(reader.GetOrdinal("ot")),
                alias = (uint)reader.GetInt64(reader.GetOrdinal("alias")),
                type = reader.GetInt64(reader.GetOrdinal("type")),
                atk = reader.GetInt32(reader.GetOrdinal("atk")),
                def = reader.GetInt32(reader.GetOrdinal("def")),
                level = reader.GetInt64(reader.GetOrdinal("level")),
                race = reader.GetInt64(reader.GetOrdinal("race")),
                attribute = reader.GetInt32(reader.GetOrdinal("attribute"))
            };
            try
            {
                byte[] setcode = reader.IsDBNull(reader.GetOrdinal("setcode")) ? Array.Empty<byte>() : (byte[])reader.GetValue(reader.GetOrdinal("setcode"));
                long setc = 0L;
                for (int i = setcode.Length; i > 0; --i) // (int i = 0; setcode >> i * 8 > 0; ++i)
                {
                    setc = (setc << 8) | setcode[i - 1] & 0xffu;
                }
                // for(ushort i = 0; i < 4;) Debug.WriteLine(setc & (0xffff << (i++ * 16)));
                c.setcode = setc;
                c.category = reader.GetInt64(reader.GetOrdinal("genre"));
                c.omega = new long[3];
                c.omega[0] = 1L;
                c.omega[1] = reader.GetInt64(reader.GetOrdinal("category"));
                string support = reader.IsDBNull(reader.GetOrdinal("support")) ? "\0" : reader.GetString(reader.GetOrdinal("support"));
                setc = 0L;
                foreach (byte sc in support.Reverse().Select(v => (byte)v)) setc = (setc << 8) | sc;
                c.omega[2] = setc;
                c.script = reader.IsDBNull(reader.GetOrdinal("script")) ? ""
                    : reader.GetString(reader.GetOrdinal("script"));
            }
            catch
            {
                c.setcode = reader.IsDBNull(reader.GetOrdinal("setcode")) ? 0L : reader.GetInt64(reader.GetOrdinal("setcode"));
                c.category = reader.GetInt64(reader.GetOrdinal("category"));
                c.omega = [0L, 0L, 0L];
                c.script = "";
            }
            c.name = reader.GetString(reader.GetOrdinal("name"));
            c.desc = reader.IsDBNull(reader.GetOrdinal("desc")) ? "" : reader.GetString(reader.GetOrdinal("desc"));
            if (reNewLine)
                c.desc = Retext(c.desc);
            for (int i = 1; i <= 0x10; i++)
                c.Str[i - 1] = reader.IsDBNull(reader.GetOrdinal("str" + i.ToString())) ? "" : reader.GetString(reader.GetOrdinal("str" + i.ToString())) ?? "";
            return c;
        }
        static string Retext(string text)
        {
            StringBuilder sr = new(text);
            _ = sr.Replace("\r\n", "\n");
            _ = sr.Replace("\n", Environment.NewLine);
            text = sr.ToString();
            _ = sr.Remove(0, sr.Length);
            return text;
        }

        public static Card[] Read(string DB, bool reNewLine, params long[] ids)
        {
            List<string> idlist = new();
            foreach (long id in ids)
            {
                idlist.Add(id.ToString());
            }
            return Read(DB, reNewLine, idlist.ToArray());
        }
        /// <summary>
        /// Reads cards by SQL, card IDs, or a name fragment.
        /// </summary>
        /// <param name="DB">Database path.</param>
        /// <param name="reNewLine">Normalize line endings for the current OS.</param>
        /// <param name="SQLs">SQL statements, card IDs, or name fragments.</param>
        public static Card[] Read(string DB, bool reNewLine, params string[] SQLs)
        {
            List<Card> list = new();
            HashSet<long> idlist = new();
            if (File.Exists(DB) && SQLs != null)
            {
                using SqliteConnection con = new(@"Data Source=" + DB);
                con.Open();
                foreach (string str in SQLs)
                {
                    _ = long.TryParse(str, out long tmp);
                    string SQLstr = _defaultSQL;
                    if (!string.IsNullOrEmpty(str))
                    {
                        if (tmp > 0)
                            SQLstr += " and datas.id=" + tmp.ToString();
                        else if (str.StartsWith("select", StringComparison.OrdinalIgnoreCase))
                            SQLstr = str;
                        else if (str.Contains("and ", StringComparison.CurrentCulture))
                            SQLstr += str;
                        else
                            SQLstr += " and texts.name like '%" + str + "%'";
                    }
                    using SqliteCommand cmd = con.CreateCommand();
                    cmd.CommandText = SQLstr;
                    using SqliteDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        Card c = ReadCard(reader, reNewLine);
                        if (idlist.Add(c.id))
                        {
                            list.Add(c);
                        }
                    }
                }
                con.Close();
                SqliteConnection.ClearAllPools();
            }
            if (list.Count == 0)
            {
                return null;
            }

            return list.ToArray();
        }
        #endregion

        #region Copy database
        /// <summary>
        /// Copy database
        /// </summary>
        /// <param name="DB">Destination database</param>
        /// <param name="cards">Card collection</param>
        /// <param name="ignore">Whether to keep existing records</param>
        /// <returns>Updated row count x2</returns>
        public static int CopyDB(string DB, bool ignore, params Card[] cards)
        {
            int result = 0;
            if (File.Exists(DB) && cards != null)
            {
                using SqliteConnection con = new(@"Data Source=" + DB);
                con.Open();
                using SqliteTransaction trans = con.BeginTransaction();
                foreach (Card c in cards)
                {
                    using SqliteCommand cmd = con.CreateCommand();
                    cmd.CommandText = DB.EndsWith(".cdb", StringComparison.OrdinalIgnoreCase) ? GetInsertSQL(c, ignore) : OmegaGetInsertSQL(c, ignore);
                    result += cmd.ExecuteNonQuery();
                }
                trans.Commit();
                con.Close();
                SqliteConnection.ClearAllPools();
            }
            return result;
        }
        #endregion

        #region Delete records
        public static int DeleteDB(string DB, params Card[] cards)
        {
            int result = 0;
            if (File.Exists(DB) && cards != null)
            {
                using SqliteConnection con = new(@"Data Source=" + DB);
                con.Open();
                using SqliteTransaction trans = con.BeginTransaction();
                foreach (Card c in cards)
                {
                    using SqliteCommand cmd = con.CreateCommand();
                    cmd.CommandText = GetDeleteSQL(c);
                    result += cmd.ExecuteNonQuery();
                }
                trans.Commit();
                con.Close();
                SqliteConnection.ClearAllPools();
            }
            return result;
        }
        #endregion

        #region Compact database
        public static void Compression(string db)
        {
            if (File.Exists(db))
            {
                using SqliteConnection con = new(@"Data Source=" + db);
                con.Open();
                using SqliteCommand cmd = con.CreateCommand();
                cmd.CommandText = "vacuum";
                _ = cmd.ExecuteNonQuery();
                con.Close();
                SqliteConnection.ClearAllPools();
            }
        }
        #endregion

        #region SQL builders
        #region Query builders
        static string ToInt(long l)
        {
            unchecked
            {
                return ((int)l).ToString();
            }
        }
        static int ToDatabaseStat(int value)
        {
            return value == -1 ? 0 : value;
        }
        static void AppendTypeCondition(StringBuilder sb, long type)
        {
            if (type <= 0)
            {
                return;
            }

            long normalSpell = (long)(Info.CardType.TYPE_NORMAL | Info.CardType.TYPE_SPELL);
            long normalTrap = (long)(Info.CardType.TYPE_NORMAL | Info.CardType.TYPE_TRAP);
            if (type == normalSpell)
            {
                _ = sb.Append(" and datas.type = " + ToInt((long)Info.CardType.TYPE_SPELL));
            }
            else if (type == normalTrap)
            {
                _ = sb.Append(" and datas.type = " + ToInt((long)Info.CardType.TYPE_TRAP));
            }
            else
            {
                _ = sb.Append(" and datas.type & " + ToInt(type) + " = " + ToInt(type));
            }
        }
        static void AppendAtkCondition(StringBuilder sb, int atk)
        {
            if (atk != -1)
            {
                _ = sb.Append(" and datas.type & 1 = 1 and datas.atk = " + atk.ToString());
            }
        }
        static void AppendDefCondition(StringBuilder sb, Card c)
        {
            if (c.IsType(Info.CardType.TYPE_LINK))
            {
                if (c.def > 0)
                {
                    _ = sb.Append(" and datas.def & " + c.def.ToString() + " = " + c.def.ToString());
                }
                return;
            }

            if (c.def != -1)
            {
                _ = sb.Append(" and datas.type & 1 = 1 and datas.def = " + c.def.ToString());
            }
        }
        public static string OmegaGetSelectSQL(Card c)
        {
            StringBuilder sb = new();
            _ = sb.Append("SELECT * FROM datas NATURAL JOIN texts WHERE datas.id > 0");
            if (c == null)
            {
                return sb.ToString();
            }

            if (!string.IsNullOrEmpty(c.name))
            {
                if (c.name.Contains("%%", StringComparison.CurrentCulture))
                {
                    c.name = c.name.Replace("%%", "%");
                }
                else
                {
                    c.name = "%" + c.name.Replace("%", "/%").Replace("_", "/_") + "%";
                }

                _ = sb.Append(" and texts.name like '" + c.name.Replace("'", "''") + "' ");
            }
            if (!string.IsNullOrEmpty(c.desc))
            {
                _ = sb.Append(" and texts.desc like '%" + c.desc.Replace("'", "''") + "%' ");
            }

            if (c.ot > 0)
            {
                _ = sb.Append(" and datas.ot = " + c.ot.ToString());
            }

            if (c.attribute > 0)
            {
                _ = sb.Append(" and datas.attribute = " + c.attribute.ToString());
            }

            if ((c.level & 0xff) > 0)
            {
                _ = sb.Append(" and (datas.level & 255) = " + ToInt(c.level & 0xff));
            }

            if ((c.level & 0xff000000) > 0)
            {
                _ = sb.Append(" and (datas.level & 4278190080) = " + ToInt(c.level & 0xff000000));
            }

            if ((c.level & 0xff0000) > 0)
            {
                _ = sb.Append(" and (datas.level & 16711680) = " + ToInt(c.level & 0xff0000));
            }

            if (c.race > 0)
            {
                _ = sb.Append(" and datas.race = " + ToInt(c.race));
            }

            AppendTypeCondition(sb, c.type);

            if (c.category > 0)
                _ = sb.Append(" and datas.genre & " + ToInt(c.category) + " = " + ToInt(c.category));

            if (c.omega != null && c.omega[0] > 0)
            {
                if (c.omega[1] > 0)
                    _ = sb.Append(" and datas.category & " + ToInt(c.omega[1]) + " = " + ToInt(c.omega[1]));
                if (c.omega[2] > 0)
                    _ = sb.Append(" and datas.support & " + ToInt(c.omega[2]) + " = " + ToInt(c.omega[2]));
            }

            AppendAtkCondition(sb, c.atk);
            AppendDefCondition(sb, c);

            if (c.id > 0 && c.alias > 0)
            {
                _ = sb.Append(" and datas.id BETWEEN " + c.alias.ToString() + " and " + c.id.ToString());
            }
            else if (c.id > 0)
            {
                _ = sb.Append(" and ( datas.id=" + c.id.ToString() + " or datas.alias=" + c.id.ToString() + ") ");
            }
            else if (c.alias > 0)
            {
                _ = sb.Append(" and datas.alias= " + c.alias.ToString());
            }

            return sb.ToString();

        }
        public static string GetSelectSQL(Card c)
        {
            StringBuilder sb = new();
            _ = sb.Append("SELECT datas.*,texts.* FROM datas,texts WHERE datas.id=texts.id ");
            if (c == null)
            {
                return sb.ToString();
            }

            if (!string.IsNullOrEmpty(c.name))
            {
                if (c.name.Contains("%%", StringComparison.CurrentCulture))
                {
                    c.name = c.name.Replace("%%", "%");
                }
                else
                {
                    c.name = "%" + c.name.Replace("%", "/%").Replace("_", "/_") + "%";
                }

                _ = sb.Append(" and texts.name like '" + c.name.Replace("'", "''") + "' ");
            }
            if (!string.IsNullOrEmpty(c.desc))
            {
                _ = sb.Append(" and texts.desc like '%" + c.desc.Replace("'", "''") + "%' ");
            }

            if (c.ot > 0)
            {
                _ = sb.Append(" and datas.ot = " + c.ot.ToString());
            }

            if (c.attribute > 0)
            {
                _ = sb.Append(" and datas.attribute = " + c.attribute.ToString());
            }

            if ((c.level & 0xff) > 0)
            {
                _ = sb.Append(" and (datas.level & 255) = " + ToInt(c.level & 0xff));
            }

            if ((c.level & 0xff000000) > 0)
            {
                _ = sb.Append(" and (datas.level & 4278190080) = " + ToInt(c.level & 0xff000000));
            }

            if ((c.level & 0xff0000) > 0)
            {
                _ = sb.Append(" and (datas.level & 16711680) = " + ToInt(c.level & 0xff0000));
            }

            if (c.race > 0)
            {
                _ = sb.Append(" and datas.race = " + ToInt(c.race));
            }

            AppendTypeCondition(sb, c.type);

            if (c.category > 0)
            {
                _ = sb.Append(" and datas.category & " + ToInt(c.category) + " = " + ToInt(c.category));
            }

            AppendAtkCondition(sb, c.atk);
            AppendDefCondition(sb, c);

            if (c.id > 0 && c.alias > 0)
            {
                _ = sb.Append(" and datas.id BETWEEN " + c.alias.ToString() + " and " + c.id.ToString());
            }
            else if (c.id > 0)
            {
                _ = sb.Append(" and ( datas.id=" + c.id.ToString() + " or datas.alias=" + c.id.ToString() + ") ");
            }
            else if (c.alias > 0)
            {
                _ = sb.Append(" and datas.alias= " + c.alias.ToString());
            }

            return sb.ToString();

        }
        #endregion

        #region Insert SQL builders
        /// <summary>
        /// Build insert SQL
        /// </summary>
        /// <param name="c">Card data</param>
        /// <param name="ignore"></param>
        /// <returns>SQL statement.</returns>
        public static string OmegaGetInsertSQL(Card c, bool ignore, bool hex = false)
        {
            StringBuilder st = new();
            if (ignore)
            {
                _ = st.Append("INSERT or ignore into datas values(");
            }
            else
            {
                _ = st.Append("INSERT or replace into datas values(");
            }

            _ = st.Append(c.id); _ = st.Append(',');
            _ = st.Append(c.ot); _ = st.Append(',');
            _ = st.Append(c.alias); _ = st.Append(',');
            if (c.omega[0] > 0)
            {
                byte[] set = Array.Empty<byte>();
                for (ushort i = 0; c.setcode >> i * 8 > 0; ++i)
                {
                    Array.Resize(ref set, i + 1);
                    set[i] = (byte)((c.setcode >> i * 8) & 0xff);
                }
                if (set.Length > 0 && c.setcode > 0)
                {
                    _ = st.Append("x'");
                    foreach (byte sc in set)
                    {
                        _ = st.Append(sc.ToString("x02").Replace("'", "''"));
                    }
                    _ = st.Append('\'');
                }
                else _ = st.Append("null");
            }
            else
            {
                if (hex) _ = st.Append("0x" + c.setcode.ToString("x")); else st.Append(c.setcode);
            }
            _ = st.Append(',');
            if (hex) _ = st.Append("0x" + c.type.ToString("x")); else _ = st.Append(c.type);
            _ = st.Append(',');
            _ = st.Append(ToDatabaseStat(c.atk)); _ = st.Append(',');
            _ = st.Append(ToDatabaseStat(c.def)); _ = st.Append(',');
            if (hex)
            {
                _ = st.Append("0x" + c.level.ToString("x")); _ = st.Append(',');
                _ = st.Append("0x" + c.race.ToString("x")); _ = st.Append(',');
                _ = st.Append("0x" + c.attribute.ToString("x")); _ = st.Append(',');
                if (c.omega[0] > 0) _ = st.Append("0x" + c.omega[1].ToString("x")); else _ = st.Append("0x0");
                _ = st.Append(',');
                _ = st.Append("0x" + c.category.ToString("x"));
                if (c.omega[0] > 0)
                {
                    _ = st.Append(',');
                    _ = st.Append(string.IsNullOrEmpty(c.script) ? "null" : "'" + c.script.Replace("'", "''") + "'");
                    _ = st.Append(',');
                    byte[] set = Array.Empty<byte>();
                    for (ushort i = 0; c.omega[2] >> i * 8 > 0; ++i)
                    {
                        Array.Resize(ref set, i + 1);
                        set[i] = (byte)((c.omega[2] >> i * 8) & 0xff);
                    }
                    if (set.Length > 0 && c.omega[2] > 0)
                    {
                        _ = st.Append("x'");
                        foreach (byte sc in set)
                        {
                            _ = st.Append(sc.ToString("x02").Replace("'", "''"));
                        }
                        _ = st.Append('\'');
                    } else st.Append("null");
                } else _ = st.Append(",null,null");
            }
            else
            {
                _ = st.Append(c.level); _ = st.Append(',');
                _ = st.Append(c.race); _ = st.Append(',');
                _ = st.Append(c.attribute); _ = st.Append(',');
                if (c.omega[0] > 0) _ = st.Append(c.omega[1]); else _ = st.Append('0');
                _ = st.Append(',');
                _ = st.Append(c.category);
                if (c.omega[0] > 0)
                {
                    _ = st.Append(',');
                    _ = st.Append(string.IsNullOrEmpty(c.script) ? "null" : "'" + c.script.Replace("'", "''") + "'");
                    _ = st.Append(',');
                    byte[] set = Array.Empty<byte>();
                    for (ushort i = 0; c.omega[2] >> i * 8 > 0; ++i)
                    {
                        Array.Resize(ref set, i + 1);
                        set[i] = (byte)((c.omega[2] >> i * 8) & 0xff);
                    }
                    if (set.Length > 0 && c.omega[2] > 0)
                    {
                        _ = st.Append("x'");
                        foreach (byte sc in set)
                        {
                            _ = st.Append(sc.ToString("x02").Replace("'", "''"));
                        }
                        _ = st.Append('\'');
                    }
                    else st.Append("null");
                }
                else _ = st.Append(",null,null");
            }
            _ = st.Append(')');
            if (ignore)
            {
                _ = st.Append(";\nINSERT or ignore into texts values(");
            }
            else
            {
                _ = st.Append(";\nINSERT or replace into texts values(");
            }

            _ = st.Append(c.id); _ = st.Append(",'");
            _ = st.Append(c.name.Replace("'", "''")); _ = st.Append("','");
            _ = st.Append(c.desc.Replace("'", "''"));
            for (int i = 0; i < 0x10; i++)
            {
                _ = st.Append("','"); _ = st.Append(c.Str[i].Replace("'", "''"));
            }
            _ = st.Append("');");
            string sql = st.ToString();
            return sql;
        }
        /// <summary>
        /// Build insert SQL
        /// </summary>
        /// <param name="c">Card data</param>
        /// <param name="ignore"></param>
        /// <returns>SQL statement.</returns>
        public static string GetInsertSQL(Card c, bool ignore, bool hex = false)
        {
            StringBuilder st = new();
            if (ignore)
            {
                _ = st.Append("INSERT or ignore into datas values(");
            }
            else
            {
                _ = st.Append("INSERT or replace into datas values(");
            }

            _ = st.Append(c.id); _ = st.Append(',');
            _ = st.Append(c.ot); _ = st.Append(',');
            _ = st.Append(c.alias); _ = st.Append(',');
            if (hex)
            {
                _ = st.Append("0x" + c.setcode.ToString("x")); _ = st.Append(',');
                _ = st.Append("0x" + c.type.ToString("x")); _ = st.Append(',');
            }
            else
            {
                _ = st.Append(c.setcode); _ = st.Append(',');
                _ = st.Append(c.type); _ = st.Append(',');
            }
            _ = st.Append(ToDatabaseStat(c.atk)); ; _ = st.Append(',');
            _ = st.Append(ToDatabaseStat(c.def)); _ = st.Append(',');
            if (hex)
            {
                _ = st.Append("0x" + c.level.ToString("x")); _ = st.Append(',');
                _ = st.Append("0x" + c.race.ToString("x")); _ = st.Append(',');
                _ = st.Append("0x" + c.attribute.ToString("x")); _ = st.Append(',');
                _ = st.Append("0x" + c.category.ToString("x"));
            }
            else
            {
                _ = st.Append(c.level); _ = st.Append(',');
                _ = st.Append(c.race); _ = st.Append(',');
                _ = st.Append(c.attribute); _ = st.Append(',');
                _ = st.Append(c.category);
            }
            _ = st.Append(')');
            if (ignore)
            {
                _ = st.Append(";\nINSERT or ignore into texts values(");
            }
            else
            {
                _ = st.Append(";\nINSERT or replace into texts values(");
            }

            _ = st.Append(c.id); _ = st.Append(",'");
            _ = st.Append(c.name.Replace("'", "''")); _ = st.Append("','");
            _ = st.Append(c.desc.Replace("'", "''"));
            for (int i = 0; i < 0x10; i++)
            {
                _ = st.Append("','"); _ = st.Append(c.Str[i].Replace("'", "''"));
            }
            _ = st.Append("');");
            string sql = st.ToString();
            return sql;
        }
        #endregion

        #region Update SQL builders
        /// <summary>
        /// Build update SQL
        /// </summary>
        /// <param name="c">Card data</param>
        /// <returns>SQL statement.</returns>
        public static string OmegaGetUpdateSQL(Card c)
        {
            StringBuilder st = new();
            _ = st.Append("update datas set ot="); _ = st.Append(c.ot);
            _ = st.Append(",alias="); _ = st.Append(c.alias);
            _ = st.Append(",setcode=");
            if (c.omega[0] > 0) {
                byte[] set = Array.Empty<byte>();
                for (ushort i = 0; c.setcode >> i * 8 > 0; ++i) {
                    Array.Resize(ref set, i + 1);
                    set[i] = (byte)((c.setcode >> i * 8) & 0xff);
                } if (set.Length > 0 && c.setcode > 0) {
                    _ = st.Append("x'");
                    foreach (byte sc in set)
                        _ = st.Append(sc.ToString("x02"));
                    _ = st.Append('\'');
                } else _ = st.Append("null");
            } else _ = st.Append(c.setcode);
            _ = st.Append(",type="); _ = st.Append(c.type);
            _ = st.Append(",atk="); _ = st.Append(ToDatabaseStat(c.atk));
            _ = st.Append(",def="); _ = st.Append(ToDatabaseStat(c.def));
            _ = st.Append(",level="); _ = st.Append(c.level);
            _ = st.Append(",race="); _ = st.Append(c.race);
            _ = st.Append(",attribute="); _ = st.Append(c.attribute);
            _ = st.Append(",category=");
            if (c.omega[0] > 0)
            {
                _ = st.Append(c.omega[1]);
                _ = st.Append(",script="); _ = st.Append(string.IsNullOrEmpty(c.script) ? "null" : "'" + c.script.Replace("'", "''") + "'");
                _ = st.Append(",support=");
                byte[] set = Array.Empty<byte>();
                for (ushort i = 0; c.omega[2] >> i * 8 > 0; ++i) {
                    Array.Resize(ref set, i + 1);
                    set[i] = (byte)((c.omega[2] >> i * 8) & 0xff);
                } if (set.Length > 0 && c.omega[2] > 0) {
                    _ = st.Append("x'");
                    foreach (byte sc in set)
                        _ = st.Append(sc.ToString("x02"));
                    _ = st.Append('\'');
                } else st.Append("null");
                    _ = st.Append(",genre=");
            } _ = st.Append(c.category);
            _ = st.Append(" where id="); _ = st.Append(c.id);
            _ = st.Append("; update texts set name='"); _ = st.Append(c.name.Replace("'", "''"));
            _ = st.Append("',desc='"); _ = st.Append(c.desc.Replace("'", "''")); _ = st.Append("', ");
            for (int i = 0; i < 0x10; i++) {
                _ = st.Append("str"); _ = st.Append(i + 1); _ = st.Append("='");
                _ = st.Append(c.Str[i].Replace("'", "''"));
                if (i < 15)
                    _ = st.Append("',");
            } _ = st.Append("' where id="); _ = st.Append(c.id);
            _ = st.Append(';');
            return st.ToString();
        }
        /// <summary>
        /// Build update SQL
        /// </summary>
        /// <param name="c">Card data</param>
        /// <returns>SQL statement.</returns>
        public static string GetUpdateSQL(Card c)
        {
            StringBuilder st = new();
            _ = st.Append("update datas set ot="); _ = st.Append(c.ot);
            _ = st.Append(",alias="); _ = st.Append(c.alias);
            _ = st.Append(",setcode="); _ = st.Append(c.setcode);
            _ = st.Append(",type="); _ = st.Append(c.type);
            _ = st.Append(",atk="); _ = st.Append(ToDatabaseStat(c.atk));
            _ = st.Append(",def="); _ = st.Append(ToDatabaseStat(c.def));
            _ = st.Append(",level="); _ = st.Append(c.level);
            _ = st.Append(",race="); _ = st.Append(c.race);
            _ = st.Append(",attribute="); _ = st.Append(c.attribute);
            _ = st.Append(",category="); _ = st.Append(c.category);
            _ = st.Append(" where id="); _ = st.Append(c.id);
            _ = st.Append("; update texts set name='"); _ = st.Append(c.name.Replace("'", "''"));
            _ = st.Append("',desc='"); _ = st.Append(c.desc.Replace("'", "''")); _ = st.Append("', ");
            for (int i = 0; i < 0x10; i++)
            {
                _ = st.Append("str"); _ = st.Append(i + 1); _ = st.Append("='");
                _ = st.Append(c.Str[i].Replace("'", "''"));
                if (i < 15)
                {
                    _ = st.Append("',");
                }
            }
            _ = st.Append("' where id="); _ = st.Append(c.id);
            _ = st.Append(';');
            string sql = st.ToString();
            return sql;
        }
        #endregion

        #region Delete
        /// <summary>
        /// Build delete SQL
        /// </summary>
        /// <param name="c">Card ID</param>
        /// <returns>SQL statement.</returns>
        public static string GetDeleteSQL(Card c)
        {
            string id = c.id.ToString();
            return "Delete from datas where id=" + id + ";Delete from texts where id=" + id + ";";
        }
        #endregion
        #endregion

    }
}
