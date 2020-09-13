using System;
using System.Collections.Generic;
using System.IO;

namespace ArmiTex {

	internal static class ProgramArgs {

		public static string[] RawArgs { get; private set; }
		public static TexFile RootFile { get; private set; }
		public static string OutputDir { get; private set; }
		public static TexFile OutputFile { get; private set; }
		public static Dictionary<string, dynamic> TexVars { get; private set; } = new Dictionary<string, dynamic>();

		public static void Parse(string[] args)
		{
			RawArgs = args;
			if (args.Length == 0)
			{
				PrintHelp();
				return;
			}

			string currentArg = "";
			foreach (string arg in args)
			{
				if (arg.Trim().Length < 1)
					continue;

				if (arg.Contains("-")) {
					currentArg = arg;
					continue;
				}

				if (currentArg == "" || currentArg == "-f" || currentArg == "--file")
					RootFile = new TexFile(arg);
				else if (currentArg == "-o" || currentArg == "--output")
				{
					Directory.CreateDirectory(arg);
					OutputDir = arg;
				}
				else if (currentArg == "-h" || currentArg == "--help")
					PrintHelp();

				currentArg = "";
			}

			if (RootFile == null)
				return;

			if (OutputDir == null)
				OutputDir = Path.Combine(RootFile.DirectoryPath, "ArmiTex");

			OutputFile = new TexFile(true);
		}

		public static void PrintHelp()
		{
			Console.WriteLine("{0,-20} {1,-10}", "Argument", "Description");
			
			Console.WriteLine("{0,-20} {1,-10}", "-f | --file", "(mandatory) Specify the root .tex file to be parsed.");
			Console.WriteLine("{0,-20} {1,-10}", "-h | --help", "Displays this help list.");
			Console.WriteLine("{0,-20} {1,-10}", "-o | --output", "Specify a directory where the parsed .tex file shall be generated.");
			Console.WriteLine("{0,-20} {1,-10}", "", "If not specified, output will be generated in a subfolder at the root file location.");
		}

	}

}
