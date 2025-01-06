using Npgsql;
using System.Collections.Generic;
using System.Diagnostics;

namespace Postgres_
{
    public enum KnownInfo
    {
        Handynumber,
        UserId,
        Email,
        Nickname,
        None
    }

    public struct Error
    {
        public string Message;
        public DateTime Date;
        public int Id;

        public Error(string Message, DateTime Date, int Id)
        {
            this.Message = Message;
            this.Date = Date;
            this.Id = Id;
        }
    }

    public class Response
    {
        public long DurationInMs = 0;
        public List<int> ErrorIds = new List<int>();
        public DateTime Date = DateTime.Now;
        public object Object = null; // Sth like User, Userprofile, Settings etc

        public Response()
        {
        }

        public Response(object Object, long DurationInMs)
        {
            this.DurationInMs = DurationInMs;
            this.Object = Object;
            Date = DateTime.Now;
        }

        public Response(object Object, int ErrorId, long DurationInMs)
        {
            this.DurationInMs = DurationInMs;
            this.ErrorIds = new List<int>() { ErrorId };
            this.Object = Object;
            Date = DateTime.Now;
        }

        public Response(object Object, List<int> ErrorIds, long DurationInMs)
        {
            this.DurationInMs = DurationInMs;
            this.ErrorIds = ErrorIds;
            this.Object = Object;
            Date = DateTime.Now;
        }

        public Response(int ErrorId, long DurationInMs)
        {
            this.DurationInMs = DurationInMs;
            ErrorIds.Add(ErrorId);
            Object = false;
            Date = DateTime.Now;
        }

        public bool GetBool(ref string error)
        {
            error = "";
            if (Object is bool)
                return (bool)Object;

            error = "couldn't convert Bool";
            return false;
        }

        public DateTime GetDateTime(ref string error)
        {
            error = "";
            if (Object is DateTime)
                return (DateTime)Object;
            error = "couldn't convert Datetime";
            return new DateTime();
        }

       

        public List<List<string>>? GetRows()
        {
            return Object is List<List<string>> ? (List<List<string>>)Object : null;
        }

        public int GetInt(ref string error)
        {
            error = "";
            if (Object is int)
                return (int)Object;

            error = "couldn't convert Int";
            return 0;
        }


        public List<int> GetListInt()
        {
            return Object is List<int> ? (List<int>)Object : null;
        }


        public bool HasError()
        {
            return ErrorIds.Count != 0;
        }

        public void AddErrorId(int id)
        {
            ErrorIds.Add(id);
        }

        public void AddErrorIds(List<int> ids)
        {
            ErrorIds.AddRange(ids);
        }
    }

    public class LastJob
    {
        public string Name;
        public long Duration;
        public bool Successfull;
        public string Error;
        public DateTime Date;

        public LastJob(string name, long duration, bool successfull, string error)
        {
            Name = name;
            Duration = duration;
            Successfull = successfull;
            Error = error;
            Date = DateTime.Now;
        }
    }

    public class KnownInfoMapping
    {
        public KnownInfo KnownInfo;
        public string Info;

        public KnownInfoMapping(KnownInfo knownInfo, string info)
        {
            KnownInfo = knownInfo;
            Info = info;
        }
    }

    public class Postgres
    {
        private bool Testmode;
        private bool Connected = false;
        private NpgsqlConnection NpgsqlConnection;
        private const int MAXNUMBEROFERRORS = 200;
        private const int MAXNUMBEROFLASTJOBS = 200;
        private static List<Error> LastErrors = new List<Error>();
        private List<LastJob> LastJobs = new List<LastJob>();

        public Postgres(bool testmode)
        {
            Testmode = testmode;
        }

        public bool IsConnected()
        {
            return Connected;
        }

        public NpgsqlConnection GetNpgsqlConnection()
        {
            return NpgsqlConnection;
        }

        public bool Connect(ref string error)
        {
            Response res = Connect();

            if (res.HasError())
                error = GetError(res.ErrorIds);

            string convError = "";
            bool b = res.GetBool(ref convError);
            if (convError == "")
                return b;
            error += "[" + convError + "]";
            return false;
        }

        private Response Connect()
        {
            bool success = true;
            Response response = new Response(success, 0);
            Stopwatch sw = new Stopwatch();
            sw.Start();
            try
            {
                NpgsqlConnectionStringBuilder builder = new NpgsqlConnectionStringBuilder();
                builder.Username = "admin";
                builder.Password = "xWIXaJP8viT1";
                builder.Host = "ep-icy-hat-239010.eu-central-1.aws.neon.tech";
                builder.Database = "topissues";
                NpgsqlConnection = new NpgsqlConnection(builder.ConnectionString);
                NpgsqlConnection.Open();
            }
            catch (Exception ex)
            {
                success = false;
                response.AddErrorId(AddError(ex.Message));
            }

            sw.Stop();
            response.Object = Connected = success;
            response.DurationInMs = sw.ElapsedMilliseconds;
            AddLastJob(new LastJob("Connect", sw.ElapsedMilliseconds, success, response.HasError() ? GetError(response.ErrorIds) : ""));
            return response;
        }

        private string GetResetQuery()
        {
            //return "DROP TABLE IF EXISTS Reports;" +
            //    "CREATE TABLE Reports (Id SERIAL PRIMARY KEY, FromUserId int, ToUserId int,Date Timestamp, Reason varchar(255),Description Text);" +
            //    "CREATE TABLE Users (Id SERIAL PRIMARY KEY);
            //    INSERT INTO predefinedelements(SubjectId, Content, ContentGerman, subject, SubjectGerman) VALUES(5,'Halal','Halal','EatingBehavior','Essverhalten');"

            return "DROP TABLE IF EXISTS Users;"+
                "DROP TABLE IF EXISTS Posts;"

            + "CREATE TABLE Users (Id SERIAL PRIMARY KEY);"
            + "CREATE TABLE Posts (Id SERIAL PRIMARY KEY, Title varchar(255), Content Text);"
            + "INSERT INTO Users(Id) VALUES(2);";
        }

        public Response ExecuteResetDBQuery()
        {
            string query = GetResetQuery();
            Stopwatch sw = Stopwatch.StartNew();

            Response response = new Response(true, 0);
            try
            {
                NpgsqlCommand command = new NpgsqlCommand(query, NpgsqlConnection);
                int res = command.ExecuteNonQuery();

                if (res == 0)
                    response.AddErrorId(AddError("ExecuteResetDBQuery: res == 0"));
            }
            catch (Exception ex)
            {
                string error = ex.Message;
                response.AddErrorId(AddError(error));
                if (error.Contains("A command is already in progres"))
                {
                }
                else if (error.Contains("Connection is not open"))
                {
                    string connError = "";
                    bool TryToConnect = Connect(ref connError);
                    if (!TryToConnect)
                    {
                        response = new Response(AddError(error), sw.ElapsedMilliseconds);
                    }
                }
                else
                {
                }
            }

            response.DurationInMs = sw.ElapsedMilliseconds;

            return response;
        }

        //public bool ResetDB(Asset asset, ref string error)
        //{
        //    string content = asset.GetContent().Replace("\r", "").Replace("\n", "");
        //    Response res = ExecuteResetDBQuery(content);

        //    if (res.HasError())
        //        error = GetError(res.ErrorIds);


        //    string convError = "";
        //    bool b = res.GetBool(ref convError);
        //    if (convError == "")
        //        return b;
        //    error += "," + convError;
        //    return false;
        //}

        private void AddLastJob(LastJob job)
        {
            if (LastJobs.Count == MAXNUMBEROFLASTJOBS)
                LastJobs.RemoveAt(0);

            if (LastJobs.Count > 30)
            {
                var list = LastJobs.OrderByDescending(i => i.Duration).ToList();
                var failed = list.Where(i => i.Successfull == false && !i.Error.ToLower().Contains("other query")).ToList();

                // Workload
                DateTime start = LastJobs.OrderBy(i => i.Date).First().Date;
                DateTime end = LastJobs.OrderByDescending(i => i.Date).First().Date;
                double ms = (end - start).TotalMilliseconds;
                long sum = LastJobs.Sum(i => i.Duration);
                double percent = (sum / ms) * 100;

                if (failed.Count > 0)
                {
                }
            }

            LastJobs.Add(job);
        }

        public string GetWorkLoad()
        {
            DateTime start = LastJobs.OrderBy(i => i.Date).First().Date;
            DateTime end = LastJobs.OrderByDescending(i => i.Date).First().Date;
            double ms = (end - start).TotalMilliseconds;
            long sum = LastJobs.Sum(i => i.Duration);
            double percent = (sum / ms) * 100;
            return (sum / 1000) + "sec/" + Math.Round((ms / 1000), 0) + "sec  " + Math.Round(percent, 2) + "%";
        }

        public List<string> GetLastJobs(int minDuration, int number = 30)
        {
            List<string> jobs = new List<string>();
            int a = LastJobs.Count - 1;
            while(jobs.Count < number && a != -1)
            {
                if (LastJobs[a].Duration > minDuration)
                    jobs.Add(DateTime.Now.ToShortTimeString() + " " + LastJobs[a].Name + " " +
                        LastJobs[a].Successfull + " " + LastJobs[a].Duration + "ms");
                a--;
            }

            return jobs;
        }

        public List<string> GetFailsSortedDesc()
        {
            var list = LastJobs.OrderByDescending(i => i.Duration).ToList();
            var failed = list.Where(i => i.Successfull == false).ToList();
            Dictionary<string, int> fails = new Dictionary<string, int>();

            lock (failed)
            {
                foreach (LastJob job in failed)
                {
                    if (fails.ContainsKey(job.Error))
                        fails[job.Error]++;
                    else
                        fails.Add(job.Error, 1);
                }
            }

            List<string> sorted = new List<string>();
            foreach (var pair in fails.OrderByDescending(i => i.Value))
            {
                sorted.Add(pair.Key + ": " + pair.Value);
            }

            return sorted;
        }

        private int AddError(string error)
        {
            if (LastErrors.Count == MAXNUMBEROFERRORS)
                LastErrors.RemoveAt(0);

            Error newError = new Error(error, DateTime.Now, LastErrors.Count);
            LastErrors.Add(newError);
            return newError.Id;
        }

        public static string GetError(List<int> ErrorIds)
        {
            string error = "";
            foreach (int id in ErrorIds)
            {
                Error err = LastErrors.FirstOrDefault(i => i.Id == id);
                if (err.Equals(null) || error.Contains(err.Message))
                    continue;

                if (error.Length > 0)
                    error += ",";
                error += err.Message;
            }

            return error;
        }

        #region inserts

       

        public Response ExecuteInsertManyQueries(List<string> queries, int maxSingleQueryLength = 10000)
        {
            string queryChain = "";
            int count = 0;

            Stopwatch sw = Stopwatch.StartNew();
            foreach (string query in queries)
            {
                if (queryChain.Length + query.Length > maxSingleQueryLength)
                {
                    // make sure queryChain is never empty, even if one atomic query is longer than maxSingleQueryLength longer than
                    if (query.Length > maxSingleQueryLength && queryChain == "")
                        queryChain = query;

                    Response res = ExecuteInsertQuery(queryChain);
                    count++;

                    string convError = "";
                    if (res.HasError())
                        res.AddErrorIds(res.ErrorIds);

                    if (!res.GetBool(ref convError) || convError != "")
                        return res;

                    queryChain = "";
                }
                else
                    queryChain += query;
            }

            long tt = sw.ElapsedMilliseconds;
            long per = tt / count;

            return new Response(true, sw.ElapsedMilliseconds);
        }

        public bool InsertPost(string title, string content)
        {
            string query = "INSERT INTO Posts (title, content) VALUES('" + title + "','" + content + "');";

            Response res = ExecuteInsertQuery(query);
            string error = "";
            return res.GetBool(ref error);
        }

        private Response ExecuteInsertQuery(string query)
        {
            Stopwatch sw = Stopwatch.StartNew();
            Response response = new Response();
            try
            {
                NpgsqlCommand command = new NpgsqlCommand(query, NpgsqlConnection);
                int res = command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                string error = ex.Message;
                response.AddErrorId(AddError(error));
                if (error.Contains("process"))
                {
                }
                else if (error.Contains("Connection is not open"))
                {
                    string connError = "";
                    bool TryToConnect = Connect(ref connError);
                    if (!TryToConnect)
                    {
                        response = new Response(AddError(error), sw.ElapsedMilliseconds);
                    }
                    else
                        response = ExecuteInsertQuery(query);
                }
                else
                {
                }
            }

            response.DurationInMs = sw.ElapsedMilliseconds;
            response.Object = !response.HasError();
            return response;
        }

        #endregion

        #region delete

        private Response ExecuteDeleteQuery(string query)
        {
            Stopwatch sw = Stopwatch.StartNew();
            Response response = new Response(true, 0);
            try
            {
                NpgsqlCommand command = new NpgsqlCommand(query, NpgsqlConnection);
                int res = command.ExecuteNonQuery();

                if (res == 0)
                {
                }
                else if (res < 0)
                {
                    response.AddErrorId(AddError("ExecuteDeleteQuery: res < 0"));
                }
            }
            catch (Exception ex)
            {
                string error = ex.Message;
                response.AddErrorId(AddError(error));
                if (error.Contains("A command is already in progres"))
                {
                }
                else if (error.Contains("Connection is not open"))
                {
                    string connError = "";
                    bool TryToConnect = Connect(ref connError);
                    if (!TryToConnect)
                    {
                        response = new Response(AddError(error), sw.ElapsedMilliseconds);
                    }
                    else
                        response = ExecuteDeleteQuery(query);
                }
                else
                {
                }
            }

            response.DurationInMs = sw.ElapsedMilliseconds;
            response.Object = !response.HasError();
            return response;
        }



        #endregion

        #region update

        private Response ExecuteUpdateQuery(string query)
        {
            Stopwatch sw = Stopwatch.StartNew();
            Response response = new Response(true, 0);
            try
            {
                NpgsqlCommand command = new NpgsqlCommand(query, NpgsqlConnection);
                int res = command.ExecuteNonQuery();

                if (res < 0)
                    response.AddErrorId(AddError("ExecuteUpdateQuery: res < 0"));
            }
            catch (Exception ex)
            {
                string error = ex.Message;
                response.AddErrorId(AddError(error));
                if (error.Contains("A command is already in progres"))
                {
                }
                else if (error.Contains("Connection is not open"))
                {
                    string connError = "";
                    bool TryToConnect = Connect(ref connError);
                    if (!TryToConnect)
                    {
                        response = new Response(AddError(error), sw.ElapsedMilliseconds);
                    }
                    else
                        response = ExecuteUpdateQuery(query);
                }
                else
                {
                }
            }

            response.DurationInMs = sw.ElapsedMilliseconds;

            return response;
        }


        #endregion

        #region selects

        private Response ExecuteSelectQuery(string query)
        {           
            List<List<string>> rows = new List<List<string>>();
            Stopwatch sw = Stopwatch.StartNew();
            Response response = new Response(false, 0);
            NpgsqlDataReader reader = null;
            try
            {
                NpgsqlCommand command = new NpgsqlCommand(query, NpgsqlConnection);
                reader = command.ExecuteReader();
                while (reader.Read())
                {
                    rows.Add(new List<string>());
                    for (int a = 0; a < reader.FieldCount; a++)
                        rows.Last().Add(reader.GetValue(a).ToString());
                }

                response.Object = rows;
            }
            catch (Exception ex)
            {
                string error = ex.Message.Replace("\n","");
                response.AddErrorId(AddError(error));
                if (error.Contains("A command is already in progres"))
                {
                }
                else if (error.Contains("Connection is not open"))
                {
                    string connError = "";
                    bool TryToConnect = Connect(ref connError);
                    if (!TryToConnect)
                    {
                        response = new Response(AddError(error), sw.ElapsedMilliseconds);
                    }
                    else
                        response = ExecuteSelectQuery(query);
                }
                else
                {
                }
            }
            finally
            {
                reader?.Close();
            }

            response.DurationInMs = sw.ElapsedMilliseconds;
            return response;
        }

        public DateTime GetDBReferenceDate(ref string error)
        {
            Stopwatch sw = Stopwatch.StartNew();
            Response res = GetDBReferenceDate();
            if (res.HasError())
                error = GetError(res.ErrorIds);
            AddLastJob(new LastJob("GetDBReferenceDate", sw.ElapsedMilliseconds, error == "", error));

            string convError = "";
            DateTime dt = res.GetDateTime(ref convError);
            if (dt == new DateTime())
            {
                error += "," + convError;
                return DateTime.MinValue;
            }
            return dt;
        }

        private Response GetDBReferenceDate()
        {
            Stopwatch sw = Stopwatch.StartNew();
            string query = "SELECT now()";
            NpgsqlDataReader reader = null;
            Response response = new Response(true, 0);
            try
            {
                NpgsqlCommand command = new NpgsqlCommand(query, NpgsqlConnection);
                reader = command.ExecuteReader();
                reader.Read();
                response.Object = reader.GetDateTime(0);
            }
            catch (Exception ex)
            {
                response.AddErrorId(AddError(ex.Message));
            }
            finally
            {
                reader?.Close();
            }

            sw.Stop();
            response.DurationInMs = sw.ElapsedMilliseconds;
            return response;
        }



        #endregion

        #region Exists

        private Response ExecuteExistsQuery(string query)
        {
            bool exists = false;
            Stopwatch sw = Stopwatch.StartNew();
            Response response = new Response();
            NpgsqlDataReader reader = null;
            try
            {
                NpgsqlCommand command = new NpgsqlCommand(query, NpgsqlConnection);
                reader = command.ExecuteReader();
                if (reader.Read())
                {
                    exists = reader.GetBoolean(0);
                }

                response.Object = exists;
            }
            catch (Exception ex)
            {
                string error = ex.Message;
                response.AddErrorId(AddError(error));
                if (error.Contains("A command is already in progres"))
                {
                }
                else if (error.Contains("Connection is not open"))
                {
                    string connError = "";
                    bool TryToConnect = Connect(ref connError);
                    if (!TryToConnect)
                    {
                        response = new Response(AddError(error), sw.ElapsedMilliseconds);
                    }
                    else
                        response = ExecuteSelectQuery(query);
                }
                else
                {
                }
            }
            finally
            {
                reader?.Close();
            }

            response.DurationInMs = sw.ElapsedMilliseconds;
            return response;
        }

        public bool CheckUserExists(string info, KnownInfo knownInfo, ref string error)
        {
            Stopwatch sw = Stopwatch.StartNew();
            Response res = CheckUserExists(info, knownInfo);
            AddLastJob(new LastJob("CheckUserExists", sw.ElapsedMilliseconds, !res.HasError(), GetError(res.ErrorIds)));
            if (res.HasError())
            {
                error = GetError(res.ErrorIds);
                return false;
            }

            string convError = "";
            bool b = res.GetBool(ref convError);
            if (convError != "")
                error += "," + convError;
            return b;
        }

        private Response CheckUserExists(string info, KnownInfo knownInfo)
        {
            Response res = new Response();

            if (knownInfo == KnownInfo.Handynumber)
                res = HandynumberExists(info);
            else if (knownInfo == KnownInfo.Email)
                res = EmailExists(info);
            else if (knownInfo == KnownInfo.Nickname)
                res = NicknameExists(info);

            return res;
        }

        public bool HandynumberExists(string handynumber, ref string error)
        {
            Stopwatch sw = Stopwatch.StartNew();
            Response res = HandynumberExists(handynumber);
            AddLastJob(new LastJob("HandynumberExists", sw.ElapsedMilliseconds, !res.HasError(), GetError(res.ErrorIds)));
            if (res.HasError())
            {
                error = GetError(res.ErrorIds);
                return false;
            }

            string convError = "";
            bool b = res.GetBool(ref convError);
            if (convError != "")
                error += "," + convError;
            return b;
        }

        private Response HandynumberExists(string handynumber)
        {
            Response response = ExecuteExistsQuery("select '" + handynumber + "' in (select handynumber from metainfos)");

            return response;
        }

        public bool EmailExists(string email, ref string error)
        {
            Stopwatch sw = Stopwatch.StartNew();
            Response res = EmailExists(email);
            AddLastJob(new LastJob("EmailExists", sw.ElapsedMilliseconds, !res.HasError(), GetError(res.ErrorIds)));
            if (res.HasError())
            {
                error = GetError(res.ErrorIds);
                return false;
            }

            string convError = "";
            bool b = res.GetBool(ref convError);
            if (convError != "")
                error += "," + convError;
            return b;
        }

        private Response EmailExists(string email)
        {

            Response response = ExecuteExistsQuery("select '" + email + "' in (select email from metainfos)");


            return response;
        }

        public bool NicknameExists(string nickname, ref string error)
        {
            Stopwatch sw = Stopwatch.StartNew();
            Response res = NicknameExists(nickname);
            AddLastJob(new LastJob("NicknameExists", sw.ElapsedMilliseconds, !res.HasError(), GetError(res.ErrorIds)));
            if (res.HasError())
            {
                error = GetError(res.ErrorIds);
                return false;
            }

            string convError = "";
            bool b = res.GetBool(ref convError);
            if (convError != "")
                error += "," + convError;
            return b;
        }

        private Response NicknameExists(string nickname)
        {

            Response response = ExecuteExistsQuery("select '" + nickname + "' in (select nickname from metainfos)");

            return response;
        }


        public bool PictureExists(string userpic, ref string error)
        {
            Stopwatch sw = Stopwatch.StartNew();
            Response res = PictureExists(userpic);
            AddLastJob(new LastJob("PictureExists", sw.ElapsedMilliseconds, !res.HasError(), GetError(res.ErrorIds)));
            if (res.HasError())
            {
                error = GetError(res.ErrorIds);
                return false;
            }

            string convError = "";
            bool b = res.GetBool(ref convError);
            if (convError != "")
                error += "," + convError;
            return b;
        }

        private Response PictureExists(string userpic)
        {

            string query = "select * from media where ftppath like '%" + userpic + "%'";
            Response res = ExecuteSelectQuery(query);
            var rows = res.GetRows();

            res = new Response(rows.Count > 0, res.DurationInMs);


            return res;
        }



        #endregion

        #region count

        public int CountUsers(ref string error)
        {
            Stopwatch sw = Stopwatch.StartNew();
            Response res = CountUsers();
            if (res.HasError())
                error = GetError(res.ErrorIds);
            AddLastJob(new LastJob("CountUsers", sw.ElapsedMilliseconds, error == "", error));
            string convError = "";
            int i = res.GetInt(ref convError);

            if (convError != "")
            {
                error += "," + convError;
            }
            return i;
        }

        private Response NextMediaID()
        {
            return Count("media");
        }

        private Response CountUsers()
        {
            return Count("users");
        }

        private Response Count(string table)
        {
            Stopwatch sw = Stopwatch.StartNew();
            Response response = ExecuteSelectQuery("select count (id) from " + table + ";");
            if (response.GetRows() == null)
            {
                return response;
            }

            int count = Convert.ToInt32(response.GetRows().First().First());

            response.Object = count;
            response.DurationInMs = sw.ElapsedMilliseconds;

            return response;
        }

        #endregion
    }
}