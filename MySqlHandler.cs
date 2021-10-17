using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using MySql.Data.MySqlClient;

namespace ArmiTex {

	static class MySqlHandler {

		private static MySqlConnection _dbConnection;
		private static string ConnectionString { get; set; }

		private static void InitConnection()
		{
			if (_dbConnection == null || _dbConnection.State == ConnectionState.Closed)
			{
				_dbConnection = new MySqlConnection(ConnectionString);
				_dbConnection.Open();
			}
		}
		private static void CloseConnection()
		{
			if (_dbConnection != null || _dbConnection.State != ConnectionState.Closed)
			{
				_dbConnection.Close();
			}
		}

		public static List<Dictionary<string, object>> Query(string queryString)
		{
			List<Dictionary<string, object>> result = new List<Dictionary<string, object>>();

			InitConnection();
			MySqlCommand cmd = new MySqlCommand(queryString, _dbConnection);
			MySqlDataReader rdr = cmd.ExecuteReader();

			while (rdr.Read())
			{
				Dictionary<string, object> row = new Dictionary<string, object>();
				for (int i = 0; i < rdr.FieldCount; i++) {
					object value = rdr.GetValue(i);
					if (value is string)
						value = value.ToString().Replace("&", "\\&");
					row.Add(rdr.GetName(i), value);
				}
				result.Add(row);
			}
			rdr.Close();

			CloseConnection();
			return result;
		}

		public static void CmdMySqlCon(StreamReader reader)
		{
			int idx;

			ConnectionString = "";
			while ((idx = reader.Read()) != -1)
			{
				char c = (char)idx;
				if (c == '}' || c == '\r' || c == '\n')
					break;
				ConnectionString += c;
			}

			Console.WriteLine("MySQL Connection: " + ConnectionString);
		}

		public static void CmdMySqlQuery(TexFile file, StreamReader reader, bool single = false)
		{
			int idx;

			string texVariable = "";
			string subVariable = "";
			string query = "";
			bool isQuery = false;
			bool isSubVariable = false;

			while ((idx = reader.Read()) != -1)
			{
				char c = (char)idx;
				if (c == '{')
					continue;

				if (c == '}' || c == '\r' || c == '\n')
				{
					if (isQuery)
						break;
					isQuery = true;
					continue;
				}

				if (isQuery)
				{
					if (c == '!' && query.Length > 0 && query[^1..] == "!")
					{
						query = query[..^1];
						subVariable = "";
						isSubVariable = true;
						continue;
					}

					if (isSubVariable)
					{
						if (!Char.IsLetterOrDigit(c) && c != '.')
						{
							isSubVariable = false;
							query += file.HandleArmiVariable(subVariable);
						} else {
							subVariable += c;
							continue;
						}
					}

					query += c;
					continue;
				}
				texVariable += c;
			}
			Console.WriteLine(texVariable + " <- " + query);
			List<Dictionary<string, object>> results = Query(query);

			if (single)
			{
				Console.WriteLine(texVariable + " -> " + "Query with " + results.Count + " result(s) trimmed to 1 result.");
				ProgramArgs.TexVars.Add(texVariable, results.FirstOrDefault());
				return;
			}

			Console.WriteLine(texVariable + " -> " + "Query with " + results.Count + " result(s).");
			ProgramArgs.TexVars[texVariable] = results;
		}

		public static void CmdMySqlExecute(StreamReader reader)
		{
			int idx;
			string query = "";

			while ((idx = reader.Read()) != -1)
			{
				char c = (char)idx;
				if (c == '{')
					continue;

				if (c == '}' || c == '\r' || c == '\n')
					break;

				query += c;
			}

			Query(query);
			Console.WriteLine("Executed " + query);
		}

		public static void CmdMySqlForeach(StreamReader reader, StreamWriter writer)
		{
			int idx;
			int curly = 0;
			bool hasLoopVar = false;
			string loopVar = "";
			string loopVarInto = "";

			while ((idx = reader.Read()) != -1)
			{
				char c = (char)idx;
				if (c == '{' || c == ' ' || c == '\t')
					continue;
				else if (c == '|')
				{
					hasLoopVar = true;
					continue;
				}	
				else if (c == '}')
				{
					Console.WriteLine("Iterating " + loopVar + " into " + loopVarInto);
					break;
				}

				if (!hasLoopVar)
					loopVar += c;
				else
					loopVarInto += c;
			}

			if (!ProgramArgs.TexVars.ContainsKey(loopVar))
				Console.WriteLine("Unknown variable \"" + loopVar + "\"");

			var results = ProgramArgs.TexVars.Where(x => x.Key == loopVar).FirstOrDefault().Value;
			bool isValidVarType = results.GetType() == typeof(List<Dictionary<string, object>>);
			if (!isValidVarType)
				Console.WriteLine("The variable \"" + loopVar + "\" is a non iterable type.");

			string toParse = "";
			while ((idx = reader.Read()) != -1)
			{
				char c = (char)idx;
				if (c == '{')
				{
					curly += 1;
					if (curly == 1)
						continue;
				}
				else if (c == '}')
				{
					curly -= 1;
					if (curly == 0)
						break;
				}

				if (isValidVarType)
					toParse += c;
			}

			if (!isValidVarType)
				return;

			foreach (Dictionary<string, object> result in results)
			{
				ProgramArgs.TexVars[loopVarInto] = result;
				new TexFile().ParseString(writer, toParse, reader.CurrentEncoding);
			}
		}

	}

}
