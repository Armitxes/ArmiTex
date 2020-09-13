using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ArmiTex {

	class TexFile {

		public bool Valid { get; private set; } = false;
		public string FileName { get; private set; }
		public string FullPath { get; private set; }
		public string DirectoryPath { get; private set; }

		private string Buffer { get; set; }

		public TexFile(string fullPath)
		{

			Console.WriteLine("Loading file: \"" + fullPath + "\"");

			if (!File.Exists(fullPath))
			{
				Console.WriteLine(" -> TexFile not found.");
				return;
			}

			if (!fullPath.Contains(".tex"))
			{
				Console.WriteLine(" -> File is not a valid .tex file.");
				return;
			}

			FullPath = fullPath;
			DirectoryPath = Path.GetDirectoryName(FullPath);
			FileName = Path.GetFileName(fullPath);
			Valid = true;
		}

		public TexFile(bool outputFile = false)
		{
			if (!outputFile)
				return;

			DirectoryPath = ProgramArgs.OutputDir;
			Directory.CreateDirectory(DirectoryPath);

			FileName = ProgramArgs.RootFile.FileName;
			FullPath = Path.Combine(DirectoryPath, FileName);
			File.Create(FullPath).Close();
		}

		public void Parse(StreamWriter writer = null)
		{
			bool subparse = true;
			if (writer == null)
			{
				writer = new StreamWriter(ProgramArgs.OutputFile.FullPath);
				subparse = false;
			}

			int idx;

			StreamReader reader = new StreamReader(FullPath);

			while ((idx = reader.Read()) != -1)
			{
				char c = (char)idx;
				Buffer += c;

				if (HandleComment(reader, writer))
					continue;

				if (HandleTexCommand(reader, writer))
					continue;

				if (HandleArmiCommand(reader, writer))
					continue;
 
				WriteBuffer(writer);
			}

			if (!subparse)
				writer.Close();

			reader.Close();
		}


		public void ParseString(StreamWriter writer, string texString, Encoding encoding)
		{
			int idx;

			byte[] byteArray = encoding.GetBytes(texString);
			MemoryStream stream = new MemoryStream(byteArray);
			StreamReader reader = new StreamReader(stream);

			while ((idx = reader.Read()) != -1)
			{
				char c = (char)idx;
				Buffer += c;

				if (HandleComment(reader, writer))
					continue;

				if (HandleTexCommand(reader, writer))
					continue;

				if (HandleArmiCommand(reader, writer))
					continue;

				WriteBuffer(writer);
			}

			reader.Close();
			stream.Close();
		}

		private void WriteBuffer(StreamWriter writer)
		{
			writer.Write(Buffer);
			Buffer = "";
			writer.Flush();
		}

		private bool HandleComment(StreamReader reader, StreamWriter writer)
		{
			if (Buffer.Length < 1)
				return false;

			if (Buffer[^1..] == "%")
			{
				int idx;
				while ((idx = reader.Read()) != -1)
				{
					char c = (char)idx;
					Buffer += c;

					WriteBuffer(writer);
					if (c == '\r' || c == '\n')
						return true;
				}
			}

			return false;
		}

		private bool HandleTexCommand(StreamReader reader, StreamWriter writer)
		{
			if (Buffer.Length < 1)
				return false;

			if (Buffer[^1..] == "\\")
			{
				int idx;
				string skipped = null;
				string texCommand = "";
				while ((idx = reader.Read()) != -1)
				{
					char c = (char)idx;
					if (c == '{' || c == '[' || idx == ' ' || c == '\r' || c == '\n')
					{
						skipped += c;
						break;
					}
					texCommand += c;
				}

				if (texCommand == "include")
				{
					HandleInclude(reader, writer);
					return true;
				}

				Buffer += texCommand + skipped;
				return false;
			}
			return false;
		}

		private void HandleInclude(StreamReader reader, StreamWriter writer)
		{
			int idx;
			string subFile = "";
			while ((idx = reader.Read()) != -1)
			{
				char c = (char)idx;
				if (c == '{')
					continue;

				if (c == '}')
					break;

				subFile += c;
			}

			subFile = subFile.Trim();
			if (!Path.HasExtension(subFile))
				subFile += ".tex";

			string subPath = Path.Combine(ProgramArgs.RootFile.DirectoryPath, subFile);
			new TexFile(subPath).Parse(writer);
		}

		private bool HandleArmiCommand(StreamReader reader, StreamWriter writer)
		{
			if (Buffer.Length < 1)
				return false;

			if (Buffer[^1..] == "!")
			{
				int idx;
				string skipped = null;
				string armiCommand = "";

				bool confirmed = false;
				while ((idx = reader.Read()) != -1)
				{
					char c = (char)idx;
					if (c == '!')
					{
						confirmed = true;
						continue;
					}

					if (!confirmed)
					{
						skipped += c;
						break;
					}

					if (!Char.IsLetterOrDigit(c) && c != '.')
					{
						skipped += c;
						break;
					}
					armiCommand += c;
				}

				if (confirmed)
				{
					// Remove the ! from output.
					if (Buffer.Length > 0 && Buffer[^1..] == "!")
						Buffer = Buffer[..^1];

					if (armiCommand == "mysqlcon")
						MySqlHandler.CmdMySqlCon(reader);
					else if (armiCommand == "mysqlsingle")
						MySqlHandler.CmdMySqlQuery(reader, true);
					else if (armiCommand == "mysqlquery")
						MySqlHandler.CmdMySqlQuery(reader);
					else if (armiCommand == "mysqlexecute")
						MySqlHandler.CmdMySqlExecute(reader);
					else if (armiCommand == "mysqlforeach")
						MySqlHandler.CmdMySqlForeach(reader, writer);
					else if (HandleArmiVariable(reader, writer, armiCommand))
						Buffer += skipped;
					else
						Console.WriteLine("Unknown command: " + armiCommand);

					return true;
				}

				Buffer += armiCommand + skipped;
				return false;
			}
			return false;
		}

		private bool HandleArmiVariable(StreamReader reader, StreamWriter writer, string varName)
		{
			if (!varName.Contains("!!"))
				varName = "!!" + varName;
			varName = varName.Trim();

			string subProp = "";
			string suffix = "";
			string[] props = varName.Split('.');
			if (props.Length > 1)
			{
				varName = props[0];
				subProp = props[1];
			}
			if (props.Length > 0 && props[^1] == "")
				suffix = ".";

			bool isVarReal = ProgramArgs.TexVars.ContainsKey(varName);
			if (!isVarReal)
				return HandleArmiVariable(
					writer,
					"<" + varName + " is not defined>" + suffix
				);

			bool isValidVarType = ProgramArgs.TexVars[varName].GetType() == typeof(Dictionary<string, object>);
			if (!isValidVarType)
				return HandleArmiVariable(
					writer,
					"<Variable \"" + varName + "\" is an iterable type>" + suffix
				);

			Dictionary<string, object> variable = ProgramArgs.TexVars[varName];
			if (!variable.ContainsKey(subProp))
				return HandleArmiVariable(
					writer,
					"<Variable \"" + varName + "\" has no property \"" + subProp + "\">" + suffix
				);

			Buffer = variable[subProp].ToString() + suffix;
			WriteBuffer(writer);
			return true;
		}

		private bool HandleArmiVariable(StreamWriter writer, string message)
		{
			Buffer += message;
			Console.WriteLine(message);
			WriteBuffer(writer);
			return false;
		}

	}
}
