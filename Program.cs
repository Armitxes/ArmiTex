using System;

namespace ArmiTex {
	class Program {

		static void Main(string[] args)
		{

			ProgramArgs.Parse(args);
			if (ProgramArgs.RootFile == null)
			{
				Console.ReadKey();
				return;
			}

			ProgramArgs.RootFile.Parse();

			Console.WriteLine("Done. Parsed file located at " + ProgramArgs.OutputDir + ". Press any key to exit.");
			Console.ReadKey();
		}

	}
}
