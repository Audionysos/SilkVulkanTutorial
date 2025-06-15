// Ignore Spelling: Shaders

using System.Diagnostics;

namespace _150_trying {
	public class ShadersCompiler {
		/// <summary>Writes compiler output and error streams.</summary>
		public static Action<string?> WL = s => Debug.WriteLine(s);
		/// <summary>Compiler path.</summary>
		public static string path = @"O:\programowanie\sdks\Vulkan 1.3.224.1\Bin\glslc.exe";

		public static async Task<object?> compile(string src, string output) {
			var p = new Process();
			p.StartInfo = new ProcessStartInfo() {
				FileName = path,
				Arguments = $@"{src} -o {output}",
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true
			};
			//if (p == null) return "Failed to start the process";
			var ob = new OutputBuffer(WL);
			p.OutputDataReceived += ob.output;
			p.ErrorDataReceived += ob.error;
			p.Start();
			p.BeginOutputReadLine();
			p.BeginErrorReadLine();
			await p.WaitForExitAsync();
			return ob.errors;
		}

		private static void OutputHandler(object sender, DataReceivedEventArgs e) {
			WL(e.Data);
		}
	}

	public class OutputBuffer {
		private Action<string?> WL = s => Debug.WriteLine(s);
		private List<string?>? std;
		public IReadOnlyList<string?>? outputs => std;
		private List<string?>? err;
		public IReadOnlyList<string?>? errors => err;

		public OutputBuffer(Action<string?> wL) {
			this.WL = wL;
		}

		public void output(object sender, DataReceivedEventArgs e) {
			if (e.Data == null) return;
			std ??= new();
			std.Add(e.Data);
			WL(e.Data);
		}

		public void error(object sender, DataReceivedEventArgs e) {
			if (e.Data == null) return;
			err ??= new();
			err.Add(e.Data);
			WL(e.Data);
		}
	}
}
