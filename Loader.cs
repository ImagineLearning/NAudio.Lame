#region MIT license
// 
// MIT license
//
// Copyright (c) 2013 Corey Murtagh
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// 
#endregion
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;

namespace NAudio.Lame
{
	internal static class Loader
	{
		internal static bool Initialized = false;
		internal static string LoadedName;

        internal static bool unmanagedDllLoaded = false;

        [DllImport("kernel32.dll")]
        private static extern IntPtr LoadLibrary(string dllToLoad);


        private static void LoadUnmanagedDll()
	    {
	        if (unmanagedDllLoaded)
	            return;

	        var dllName = string.Format("libmp3lame.{0}.dll", Environment.Is64BitProcess ? "64" : "32");
            var dllPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(dllPath);
            var resourceName = Assembly.GetExecutingAssembly().GetManifestResourceNames().First(r => r.EndsWith(dllName, StringComparison.InvariantCultureIgnoreCase));

            using (var fileStream = new FileStream(Path.Combine(dllPath, dllName), FileMode.CreateNew))
            using (var resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                resourceStream.CopyTo(fileStream);
            }

            LoadLibrary(Path.Combine(dllPath, dllName));

            unmanagedDllLoaded = true;
	    }

		private static Assembly LoadLameWrapper(object sender, ResolveEventArgs args)
		{
			//Console.WriteLine("LoadLameWrapper(): {0}", args.Name);

			var asmName = new AssemblyName(args.Name).Name + ".dll";
			var srcAssembly = typeof(LameMP3FileWriter).Assembly;

			// Search resources for requested assembly
			byte[] src = null;
			foreach (string nxt in srcAssembly.GetManifestResourceNames())
			{
				int p1 = nxt.IndexOf(Environment.Is64BitProcess ? "x64" : "x86");
				int p2 = nxt.IndexOf(asmName);

				if (p1 < 0 || p2 < 0 || p1 >= p2)
					continue;

				LoadedName = nxt;
				
				// Load resource into byte array
				using (var strm = srcAssembly.GetManifestResourceStream(nxt))
				{
					src = new byte[strm.Length];
					strm.Read(src, 0, (int)strm.Length);
					break;
				}
			}
			if (src == null)
				return null;

			// Load assembly from byte array
			//Console.WriteLine("Loaded {0} bytes from resource", src.Length);
			try
			{
                LoadUnmanagedDll();
				var res = Assembly.Load(src, null, SecurityContextSource.CurrentAppDomain);
				return res;
			}
			catch //(Exception e)
			{
				//Console.WriteLine("LoadLameWrapper: Failed to create assembly from buffer.");
				//Console.WriteLine("Exception:");
				//Console.WriteLine("{0}", e.Message);
				throw;
			}
		}

		public static void Init()
		{
			if (!Initialized)
			{
				// Register assembly resolver
				AppDomain.CurrentDomain.AssemblyResolve += LoadLameWrapper;
				Initialized = true;
			}
		}
	}
}
