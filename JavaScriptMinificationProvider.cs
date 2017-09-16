using System;
using System.IO;
using System.Web.Compilation;
using System.Web.Hosting;
using System.Reflection;
using Microsoft.Ajax.Utilities;
using System.CodeDom.Compiler;

namespace JavaScriptMinifier
{
	[BuildProviderAppliesTo(BuildProviderAppliesTo.Web)]
	public class JavaScriptMinificationProvider : System.Web.Compilation.BuildProvider
	{

		public override void GenerateCode(AssemblyBuilder assemblyBuilder)
		{
			if (VirtualPath.EndsWith(".min.js", StringComparison.OrdinalIgnoreCase))
			{
				base.GenerateCode(assemblyBuilder);
				return;
			}

			try
			{
				var virtualFile = HostingEnvironment.VirtualPathProvider.GetFile(VirtualPath);
				var type = virtualFile.GetType();
				if (type.Name == "MapPathBasedVirtualFile")
				{
					//dynamic type cannot access protected members.
					var physicalPathProperty = type.GetProperty("PhysicalPath", BindingFlags.NonPublic | BindingFlags.Instance);

					string physicalPath = (string) physicalPathProperty.GetValue(virtualFile);

					string minifiedFilePath = physicalPath.Substring(0, physicalPath.Length - ".js".Length) + ".min.js";
					System.IO.File.Copy(physicalPath, minifiedFilePath, true);
					//Minify(physicalPath);
				}
			}
			catch (Exception e)
			{
				
			}
		}

		private void Minify(string jsPath)
		{

			string minifiedPath = null;
			if (jsPath.EndsWith(".js", StringComparison.OrdinalIgnoreCase) == false)
			{
				minifiedPath = jsPath + ".min";
			}
			else
			{
				minifiedPath = jsPath.Substring(0, jsPath.Length - 3) + ".min.js";
			}

			var mapPath = jsPath.Replace(".js", ".min.js") + ".map";

			StreamWriter mapWriter = null;
			V3SourceMap sourceMap = null;
			try
			{
				var settings = new CodeSettings();
				//try
				//{
					//Be aware that the text encoding should be UTF-8 with no Byte-Order Mark (BOM) written at the beginning. 
					//V3 source map files that include a BOM will not be read by Chrome
					mapWriter = new StreamWriter(mapPath, false, new System.Text.UTF8Encoding(false));
					sourceMap = new V3SourceMap(mapWriter);
					sourceMap.StartPackage(minifiedPath, mapPath);
					// the first argument specifies "file" field in map file.
					// the second arugment specifies "//# sourceMappingURL=" in the output (minified js)
					settings.SymbolsMap = sourceMap;
				//}
				//catch (Exception ex)
				//{
				//}


				settings.TermSemicolons = true;
				Minifier minifier = new Minifier();
				minifier.FileName = jsPath;
				var content= minifier.MinifyJavaScript(File.ReadAllText(jsPath), settings);

				using (StreamWriter sw = new StreamWriter(minifiedPath))
				{
					sw.Write(content);
				}
			}
			finally
			{
				if (sourceMap != null)
				{
					sourceMap.EndPackage();
					sourceMap.Dispose();
				}
				mapWriter?.Dispose();
			}
		}
	}
}
