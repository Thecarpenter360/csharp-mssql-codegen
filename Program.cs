using System;
using System.IO;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace csmsqqlcodegen
{
    class Program
    {
        static string ProjectName = "", ConnectionString_Database = "", SqlConnectionStr = "", UniqueRunID = Guid.NewGuid().ToString();

        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            PrintWelcomeMessage();
            GetInfoFromUser();
            CreateClassFiles(GetTableCols(CreateTableList(GetTables())));
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("Complete!");
            Console.ReadKey();
        }

        static void PrintWelcomeMessage()
        {
            Console.WriteLine(@"
####################################################################################################
  ###          #   #   ###    ###    ###   #       ###              #                             
 #   #         #   #  #   #  #   #  #   #  #      #   #             #                             
 #       ###   ## ##  #      #      #   #  #      #       ###    ## #   ###    ## #   ###   # ##  
 #      #      # # #   ###    ###   #   #  #      #      #   #  #  ##  #   #  #  #   #   #  ##  # 
 #       ###   #   #      #      #  #   #  #      #      #   #  #   #  #####   ##    #####  #   # 
 #   #      #  #   #  #   #  #   #  # # #  #      #   #  #   #  #  ##  #      #      #      #   # 
  ###   ####   #   #   ###    ###    ###   #####   ###    ###    ## #   ###    ###    ###   #   # 
                                        #                                     #   #               
####################################################################################################                            
            ");
        }
        
        static void GetInfoFromUser()
        {
            Console.WriteLine("\nProject name (namespace):");
            Console.ForegroundColor = ConsoleColor.White;
            ProjectName = Console.ReadLine();

            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("\nConnectionStringBuilder (requires a valid SQL Server login with sufficient permissions):");

            Console.WriteLine("Server=");
            Console.ForegroundColor = ConsoleColor.White;
            string ConnectionString_Server = Console.ReadLine();

            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("Database=");
            Console.ForegroundColor = ConsoleColor.White;
            ConnectionString_Database = Console.ReadLine();

            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("User Id=");
            Console.ForegroundColor = ConsoleColor.White;
            string ConnectionString_UserID = Console.ReadLine();

            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("Password=");
            Console.ForegroundColor = ConsoleColor.White;
            string ConnectionString_Password = Console.ReadLine();
            
            SqlConnectionStr = $"Server={ConnectionString_Server};Database={ConnectionString_Database};User Id={ConnectionString_UserID};Password={ConnectionString_Password};";
            
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("\nStarting build: ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(UniqueRunID);
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($" at {DateTime.Now.ToString()}\n");
        }

        static void CreateClassFiles(List<DataTable> TableData)
        {
            try
            {
                foreach (DataTable dt in TableData)
                {
                    CreateClassFileWithContent(CreateClassFileContent(dt), dt.Rows[0][2].ToString());
                }
            }
            catch (Exception e)
            {
                ErrorDisplay(e.ToString());
            }
        }

        static void CreateClassFileWithContent(string FileContent, string FileName)
        {
            try
            {
                string location = Path.Join($@"C:\Users\{Environment.UserName}\Documents\", UniqueRunID);
                if (!Directory.Exists(location))
                {
                    Directory.CreateDirectory(location);
                }

                string NewFileName = Path.Join(location, FileName + ".cs");
                var newfile = File.Create(NewFileName);
                newfile.Close();

                File.WriteAllText(NewFileName, FileContent);
            }
            catch (Exception e)
            {
                ErrorDisplay(e.ToString());
            }
        }

        static string CreateClassFileContent(DataTable dt)
        {
            try
            {
                List<string> noSize = new List<string> {"int", "text", "decimal", "datetime", "smallint", "bit", "real", "numeric"};

                string NewClassName = dt.Rows[0][2].ToString().Replace(" #", "").Replace("#", "").Replace(" ", "_").Replace("-", "_");
                List<string[]> ClassProps = new List<string[]>();

                foreach (DataRow row in dt.Rows)
                {
                    ClassProps.Add(new string[] {
                        row[3].ToString().Replace(" #", "").Replace("#", "").Replace(" ", "_").Replace("-", "_"), //PropName
                        row[7].ToString(), //PropType
                        row[8].ToString() //PropSize (optional)
                    });
                }
            
                string ClassFileImportsSection = 
@"using System;
using System.IO;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
            ";

                string ClassFileHeaderSection = @"
namespace " + ProjectName + @" {
    class " + NewClassName + " {";

                string ClassFileFooterSection = @"
    }
}
            ";

                string ClassFilePropsSection = "";

            

                List<string[]> PropArray = new List<string[]>();
                List<string[]> SqlParamArray = new List<string[]>();

                foreach (string[] prop in ClassProps)
                { 
                    ClassFilePropsSection += BuildProp(prop[0], GetCsDataTypeFromDBType(prop[1]));
                    PropArray.Add(new string[] {prop[0], GetCsDataTypeFromDBType(prop[1])});
                    SqlParamArray.Add(new string[] {prop[0], prop[1], prop[2]});
                    //string PropSize = prop[2];
                }

                string ClassFileBodySection = @"
        " + BuildInsertUpdateDeleteMethods(NewClassName, SqlParamArray);

                string ClassFileCtorSection = BuildCtor(NewClassName, PropArray);

                return ClassFileImportsSection + ClassFileHeaderSection + ClassFilePropsSection + ClassFileCtorSection + ClassFileBodySection + ClassFileFooterSection;
            }
            catch (Exception e)
            {
                ErrorDisplay(e.ToString());
                return "";
            }
        }

        static string BuildInsertUpdateDeleteMethods(string NewClassName, List<string[]> SqlParamArray)
        {
            try
            {
                return BuildSingleInsertStatement(NewClassName, SqlParamArray) + BuildSingleUpdateStatement(NewClassName, SqlParamArray) + BuildSingleDeleteStatement(NewClassName, SqlParamArray);
            }
            catch (Exception e)
            {
                ErrorDisplay(e.ToString());
                return "";
            }
        }

        static string BuildSingleInsertStatement(string NewClassName, List<string[]> SqlParamArray)
        {
            try
            {
                string ParamSection = "", ColVarSegment = "", ColSegment = "";

            foreach (string[] param in SqlParamArray)
            {
                if (!param[0].ToLower().Contains("id") && !param[0].ToLower().Contains("int"))
                {
                    ColSegment += "[" + param[0] + "]" + ((SqlParamArray.IndexOf(param) < SqlParamArray.Count - 1) ? ", " : "");
                    ColVarSegment += '@' + param[0] + ((SqlParamArray.IndexOf(param) < SqlParamArray.Count - 1) ?  ", " : "");
                    ParamSection += @"
                    new SqlParameter(" + '"' + '@' + param[0] + '"' + ", SqlDbType." + ReplaceWithCorrectSqlParam(param[1]) + ") {Value = _" + NewClassName + "." + param[0] + ( CheckIfNeedsSizeForVars(param[1]) ? (", Size = " + param[2]) : "") + "},";
                }
            }

            return @"
        public bool InsertRecord" + NewClassName + "(" + NewClassName + " _" + NewClassName + @") 
        {
            bool inserted = false;

            try 
            { 
                List<SqlParameter> SqlParams = new List<SqlParameter>
                {
                    " + ParamSection + 
                    @"
                };

                using (SqlConnection conn = new SqlConnection(" + '"' + SqlConnectionStr + '"' + @"))
                {
                    conn.Open();
                    using (SqlCommand comm = new SqlCommand(" + '"' + "INSERT INTO [" + NewClassName + "] (" + ColSegment + ") VALUES (" + ColVarSegment + ");" + '"' + @", conn))
                    {
                        foreach (SqlParameter s in SqlParams)
                        {
                           comm.Parameters.Add(s);
                        }
                        if (comm.ExecuteNonQuery() > 0) { 
                           inserted = true;
                        }
                   }
                }
            }
            catch (Exception ex) 
            { 
                ex.ToString();
            }

            return inserted;
        }
        ";
            }
            catch (Exception e)
            {
                ErrorDisplay(e.ToString());
                return "";
            }
        }

        static string BuildSingleUpdateStatement(string NewClassName, List<string[]> SqlParamArray)
        {
            try
            {
                string ParamSection = "", ColSegment = "", ColVarSegment = "", UpdateSetsSection = "";
            string PossibleIDVar = SqlParamArray[0][0];

            foreach (string[] param in SqlParamArray)
            {
                if (!param[0].ToLower().Contains("id") && !param[0].ToLower().Contains("int"))
                {
                    ColSegment += "[" + param[0] + "]" + ((SqlParamArray.IndexOf(param) < SqlParamArray.Count - 1) ? ", " : "");
                ColVarSegment += '@' + param[0] + ((SqlParamArray.IndexOf(param) < SqlParamArray.Count - 1) ?  ", " : "");
                UpdateSetsSection += " SET [" + param[0] + "] = @" + param[0] + ((SqlParamArray.IndexOf(param) < SqlParamArray.Count - 1) ? ", " : "");
                ParamSection += @"
                new SqlParameter(" + '"' + '@' + param[0] + '"' + ", SqlDbType." + ReplaceWithCorrectSqlParam(param[1]) + ") {Value = _" + NewClassName + "." + param[0] + ( CheckIfNeedsSizeForVars(param[1]) ? (", Size = " + param[2]) : "") + "},";
                }
            }

            string ReturnMethod = @"
        public bool UpdateRecord" + NewClassName +  "(" + NewClassName + " _" + NewClassName + @") 
        {
            bool updated = false;

            try 
            { 
                List<SqlParameter> SqlParams = new List<SqlParameter>
                {
                    " + ParamSection + 
                    @"
                };

                using (SqlConnection conn = new SqlConnection(" + '"' + SqlConnectionStr + '"' + @"))
                {
                    conn.Open();
                    using (SqlCommand comm = new SqlCommand(" + '"' + "UPDATE [" + NewClassName + "] " + UpdateSetsSection + " WHERE [" + PossibleIDVar + "] = @" + PossibleIDVar + ";" + '"' + @", conn))
                    {
                        foreach (SqlParameter s in SqlParams)
                        {
                           comm.Parameters.Add(s);
                        }
                        if (comm.ExecuteNonQuery() > 0) { 
                           updated = true;
                        }
                   }
                }
            }
            catch (Exception ex) 
            { 
                ex.ToString();
            }

            return updated;
        }
        ";

            if (!SqlParamArray[0][0].ToLower().Contains("id") && !SqlParamArray[0][0].ToLower().Contains("int"))
            {
                ReturnMethod = @"
        /* possible missing PK or col[0] mismatch, commenting out generated delete statement
        
        " +  ReturnMethod + @"
        */
            ";
            }

            return ReturnMethod;
            }
            catch (Exception e)
            {
                ErrorDisplay(e.ToString());
                return "";
            }
        }

        static string BuildSingleDeleteStatement(string NewClassName, List<string[]> SqlParamArray)
        {
            try
            {
                string PossibleIDVar = SqlParamArray[0][0];
            string ReturnMethod = @"
        public bool DeleteRecord" + NewClassName + @"(int _ID) 
        {
            bool deleted = false;

            try 
            { 
                SqlParameter pID = new SqlParameter(" + '"' + "@ID" + '"' + @", SqlDbType.Int) { Value = _ID };

                using (SqlConnection conn = new SqlConnection(" + '"' + SqlConnectionStr + '"' + @"))
                {
                    conn.Open();
                    using (SqlCommand comm = new SqlCommand(" + '"' + "DELETE FROM [" + NewClassName + "] WHERE [" + PossibleIDVar + "] = @ID;" + '"' + @", conn))
                    {
                        comm.Parameters.Add(pID);
                        if (comm.ExecuteNonQuery() > 0) { 
                           deleted = true;
                        }
                   }
                }
            }
            catch (Exception ex) 
            { 
                ex.ToString();
            }

            return deleted;
        }
        ";
            if (!SqlParamArray[0][0].ToLower().Contains("id") && !SqlParamArray[0][0].ToLower().Contains("int"))
            {
                ReturnMethod = @"
        /* possible missing PK or col[0] mismatch, commenting out generated delete statement
        
        " +  ReturnMethod + @"
        */
            ";
            }

            return ReturnMethod;
            }
            catch (Exception e)
            {
                ErrorDisplay(e.ToString());
                return "";
            }
        }

        static string ReplaceWithCorrectSqlParam(string Param)
        {
            switch (Param)
            {
                case "varchar":
                    return "VarChar";
                case "int":
                    return "Int";
                case "datetime":
                    return "DateTime";
                case "date":
                    return "Date";
                case "nvarchar":
                    return "NVarChar";
                case "smallint":
                    return "SmallInt";
                case "bit":
                    return "Bit";
                case "nchar":
                    return "NChar";
                case "decimal":
                    return "Decimal";
                case "text":
                    return "Text";
                case "real":
                    return "Real";
                case "float":
                    return "Float";
                case "numeric":
                    return "Decimal";
                case "char":
                    return "Char";
                case "money":
                    return "Money";
                case "varbinary":
                    return "VarBinary";
                case "uniqueidentifier":
                    return "UniqueIdentifier";
                case "ntext":
                    return "NText";
                case "bigint":
                    return "BigInt";
                case "image":
                    return "Image";
                default:
                    return Param;
            }
        }

        static string BuildProp(string PropName, string PropType)
        {
            return @"
        public " + PropType + " " + PropName + @" { get; set; }";
        }

        static string BuildCtor(string NewClassName, List<string[]> PropArray)
        {
            try
            {
                string ParenSection = "";
            string ValSection = "";

            int TotalProps = PropArray.Count;
            ParenSection += "(";
            foreach(string[] prop in PropArray)
            {
                ParenSection += prop[1] + " _" + prop[0] + ((PropArray.IndexOf(prop) < (TotalProps - 1)) ? ", " : "");
                ValSection += @"
            " + prop[0] + " = _" + prop[0] + ";";
            }
            ParenSection += ")";

            return @"

        public " + NewClassName + @"() {}

        public " + NewClassName + ParenSection + @"
        {" + ValSection + @"
        }
            ";
            }
            catch (Exception e)
            {
                ErrorDisplay(e.ToString());
                return "";
            }
        }

        static bool CheckIfNeedsSizeForVars(string DBType)
        {
            DBType = DBType.ToLower();
            List<string> NeedsSize = new List<string>() {"char", "varchar", "nvarchar", "binary", "varbinary"}; //"decimal","numeric", "smallmoney", "money", "float",
            return NeedsSize.Contains(DBType);
        }

        static string GetCsDataTypeFromDBType(string DBType)
        {
            DBType = DBType.ToLower();
            List<string> StringType = new List<string>() {"char", "varchar", "text", "nchar", "nvarchar", "ntext", "binary", "varbinary", "image", "uniqueidentifier"};
            List<string> NumericTypeInt = new List<string>() {"tinyint", "smallint", "int", "bigint"};
            List<string> NumericTypeFloat = new List<string>() {"decimal", "numeric", "smallmoney", "money", "float", "real"};
            List<string> DateTimeType = new List<string>() {"datetime", "datetime2", "smalldatetime", "date", "time", "datetimeoffset", "timestamp"};

            if (StringType.Contains(DBType))
            {
                return "string";
            }

            if (NumericTypeInt.Contains(DBType))
            {
                return "int";
            }

            if (NumericTypeFloat.Contains(DBType))
            {
                return "double";
            }

            if (DateTimeType.Contains(DBType))
            {
                return "DateTime";
            }

            if (DBType == "bit")
            {
                return "bool";
            }

            return "string";
        }

        static List<DataTable> GetTableCols(List<string> tables)
        {
            List<DataTable> tablecols = new List<DataTable>();
            try
            {
                using (SqlConnection conn = new SqlConnection(SqlConnectionStr))
                {
                    conn.Open();
                    foreach (string table in tables)
                    {
                        tablecols.Add(conn.GetSchema("Columns", new[] { ConnectionString_Database, null, table }));
                    }
                }
            }
            catch (Exception e)
            {
                ErrorDisplay(e.ToString());
            }

            return tablecols;
        }

        static List<string> CreateTableList(DataTable dt) 
        {
            List<string> tables = new List<string>();

            try
            {
                foreach (DataRow row in dt.Rows)
                {
                    foreach (DataColumn col in dt.Columns)
                    {
                        string colStr = row[col].ToString();
                        if (!colStr.Contains("aspnet_") && colStr != "BASE TABLE" && col.Ordinal > 0) //colStr != "dbo"
                        {
                            tables.Add(colStr);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                ErrorDisplay(e.ToString());
            }
            
            return tables;
        }

        static DataTable GetTables() 
        { 
            DataTable dt = new DataTable();

            try
            {
                using (SqlConnection conn = new SqlConnection(SqlConnectionStr))
                {
                    conn.Open();
                    dt = conn.GetSchema("Tables");
                }
            }
            catch (Exception e)
            {
                ErrorDisplay(e.ToString());
            }
            
            return dt;
        }

        static void ErrorDisplay(string ErrMsg)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n{ErrMsg}");
        }
    }
}