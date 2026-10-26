// IVSDKScriptCompiler 0.3 - EXPERIMENTAL IV-SDK .NET script. MIT License, see LICENSE.
// https://github.com/bassisthip63-ui/IVSDKScriptCompiler
// Lets IV-SDK .NET run ScriptHookDotNet scripts straight from source, like ScriptHookDotNet 1.7.1.9 did:
// put  MyScript.cs  /  MyScript.vb  (or MyScript.net.cs / .net.vb) in the GTA IV "scripts" folder, as with
// ScriptHookDotNet 1.7.1.9. In IVSDKDotNet\scripts only *.net.cs / *.net.vb are compiled.
//
// How: IV-SDK constructs its own .ivsdk scripts BEFORE it looks for ScriptHookDotNet scripts. So in its constructor
// this script compiles each source script into  MyScript.compiled.net.dll  next to the source, and IV-SDK then
// finds and loads that like any other .net.dll. A script is only recompiled when its source (or this tool) changed.
//
// References added automatically: mscorlib, System, System.Core, System.Drawing, System.Windows.Forms, System.Xml,
// System.Data, and IV-SDK's ScriptHookDotNet.dll. Extra ones: a line  //#reference SomeLibrary.dll  in the source
// (a file name is looked for in the script's folder, the game folder, IVSDKDotNet\bin and the .NET Framework folder).
//
// The .NET Framework 4 compilers understand C# 5 / VB 11 (old ScriptHookDotNet scripts are fine; C# 6+ syntax such as
// $"..." or ?. is not). Errors go to the IV-SDK console (F4) and to IVSDKDotNet\IVSDKScriptCompiler.log.
//
// Written for the C# 5 compiler that ships with Windows (.NET Framework 4).

using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using IVSDKDotNet;

[assembly: AssemblyTitle("IVSDKScriptCompiler")]
[assembly: AssemblyDescription("IV-SDK .NET script that compiles ScriptHookDotNet .cs / .vb source scripts into .net.dll files at game start")]
[assembly: AssemblyProduct("IVSDKScriptCompiler")]
[assembly: AssemblyCompany("bassisthip63-ui")]
[assembly: AssemblyCopyright("Copyright (c) 2026 bassisthip63-ui. MIT License.")]
[assembly: AssemblyVersion("0.3.0.0")]
[assembly: AssemblyFileVersion("0.3.0.0")]
[assembly: AssemblyInformationalVersion("0.3.0 (experimental) - https://github.com/bassisthip63-ui/IVSDKScriptCompiler")]
[assembly: Guid("9c3d7a51-2e64-4f0b-8a1d-6b5e0f27c4d3")]

namespace IVSDKScriptCompiler
{
    public class Main : Script
    {
        const string OutSuffix = ".compiled.net.dll";
        static readonly string[] DefaultRefs =
        {
            "System.dll", "System.Core.dll", "System.Drawing.dll", "System.Windows.Forms.dll", "System.Xml.dll", "System.Data.dll"
        };

        readonly StringBuilder log = new StringBuilder();
        string gameDir, logPath;

        public Main()
        {
            try { Run(); }
            catch (Exception ex) { Say("IVSDKScriptCompiler: unexpected error: " + ex); }
            finally
            {
                try { if (logPath != null) File.WriteAllText(logPath, log.ToString()); } catch { }
            }
        }

        void Say(string s)
        {
            log.AppendLine(DateTime.Now.ToString("HH:mm:ss") + "  " + s);
            try { IVGame.Console.Print(s); } catch { }
        }

        void Run()
        {
            gameDir = Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            string sdkDir = Path.Combine(gameDir, "IVSDKDotNet");
            logPath = Path.Combine(sdkDir, "IVSDKScriptCompiler.log");
            Say("IVSDKScriptCompiler 0.3: looking for source scripts (.cs / .vb)");

            string shdn = Path.Combine(Path.Combine(sdkDir, "bin"), "ScriptHookDotNet.dll");
            if (!File.Exists(shdn)) { Say("IVSDKScriptCompiler: IVSDKDotNet\\bin\\ScriptHookDotNet.dll not found - nothing compiled"); return; }

            // IV-SDK loads scripts from memory, so Assembly.Location is empty: find this tool's own file by name
            DateTime toolTime = DateTime.MinValue;
            try
            {
                string loc = Assembly.GetExecutingAssembly().Location;
                if (string.IsNullOrEmpty(loc)) loc = Path.Combine(Path.Combine(sdkDir, "scripts"), "IVSDKScriptCompiler.ivsdk.dll");
                if (File.Exists(loc)) toolTime = File.GetLastWriteTimeUtc(loc);
            }
            catch { }
            int found = 0, built = 0, upToDate = 0, failed = 0, removed = 0;

            // the game's scripts folder: every *.cs / *.vb, as ScriptHookDotNet 1.7.1.9 did.
            // IVSDKDotNet\scripts also holds IV-SDK's own files, so there only *.net.cs / *.net.vb.
            string[][] places = { new[] { Path.Combine(gameDir, "scripts"), "any" }, new[] { Path.Combine(sdkDir, "scripts"), "net" } };
            foreach (string[] place in places)
            {
                string dir = place[0];
                bool anySource = place[1] == "any";
                if (!Directory.Exists(dir)) continue;

                // compiled files whose source is gone are removed, so a deleted script really goes away
                foreach (string dll in Directory.GetFiles(dir, "*" + OutSuffix))
                {
                    string stem = dll.Substring(0, dll.Length - OutSuffix.Length);
                    bool has = File.Exists(stem + ".net.cs") || File.Exists(stem + ".net.vb") ||
                               (anySource && (File.Exists(stem + ".cs") || File.Exists(stem + ".vb")));
                    if (!has)
                    {
                        try { File.Delete(dll); removed++; Say("  removed " + Path.GetFileName(dll) + " (its source is gone)"); } catch { }
                    }
                }

                foreach (string src in Directory.GetFiles(dir))
                {
                    string lower = src.ToLowerInvariant();
                    bool cs = lower.EndsWith(".cs"), vb = lower.EndsWith(".vb");
                    if (!cs && !vb) continue;
                    bool netNamed = lower.EndsWith(".net.cs") || lower.EndsWith(".net.vb");
                    if (!anySource && !netNamed) continue;
                    found++;
                    string stemPath = src.Substring(0, src.Length - (netNamed ? 7 : 3));
                    string outPath = stemPath + OutSuffix;
                    if (File.Exists(outPath))
                    {
                        DateTime o = File.GetLastWriteTimeUtc(outPath);
                        if (o >= File.GetLastWriteTimeUtc(src) && o >= toolTime) { upToDate++; continue; }
                    }
                    if (Compile(src, outPath, cs, shdn)) built++; else failed++;
                }
            }
            Say("IVSDKScriptCompiler: " + found + " source script(s): " + built + " compiled, " + upToDate + " up to date, " + failed + " failed");

            // IV-SDK loads ScriptHookDotNet scripts from a list of files it made when the game started, kept up to date by a
            // background file watcher. That watcher can be too late for a file written a moment ago, so a brand-new script
            // would only load on the next start. Refresh the list now so new scripts load on this start.
            if (built > 0 || removed > 0) RefreshManagerFileList(sdkDir);
        }

        // Updates IV-SDK .NET's FileCacheManager the same way its own file watcher does (IV-SDK .NET 1.9.x).
        void RefreshManagerFileList(string sdkDir)
        {
            try
            {
                Type fcm = null;
                foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
                {
                    Type t;
                    try { t = a.GetType("Manager.Managers.FileCacheManager", false); } catch { continue; }
                    if (t != null) { fcm = t; break; }
                }
                if (fcm == null) { Say("IVSDKScriptCompiler: could not find IV-SDK's file list; new scripts load on the next start"); return; }

                FieldInfo f = fcm.GetField("cachedFiles", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                IDictionary cache = f != null ? f.GetValue(null) as IDictionary : null;
                Type[] kv = f != null && f.FieldType.IsGenericType ? f.FieldType.GetGenericArguments() : null;
                if (cache == null || kv == null || kv.Length != 2 || !kv[0].IsEnum || kv[1] != typeof(string[]))
                {
                    Say("IVSDKScriptCompiler: IV-SDK's file list looks different in this version; new scripts load on the next start");
                    return;
                }

                const string patterns = "*.ivsdk.dll|*.net.dll|*.cs|*.vb";
                // keys used by IV-SDK: 2 = <game>\scripts, 6 = <game>\IVSDKDotNet\scripts
                SetList(cache, kv[0], 2, Path.Combine(gameDir, "scripts"), patterns);
                SetList(cache, kv[0], 6, Path.Combine(sdkDir, "scripts"), patterns);
                Say("IVSDKScriptCompiler: refreshed IV-SDK's script file list, so new scripts load now");
            }
            catch (Exception ex)
            {
                Say("IVSDKScriptCompiler: could not refresh IV-SDK's file list (" + ex.Message + "); new scripts load on the next start");
            }
        }

        static void SetList(IDictionary cache, Type keyType, int key, string dir, string patterns)
        {
            if (!Directory.Exists(dir)) return;
            var files = new List<string>();
            foreach (string p in patterns.Split('|')) files.AddRange(Directory.GetFiles(dir, p, SearchOption.TopDirectoryOnly));
            cache[Enum.ToObject(keyType, key)] = files.ToArray();
        }

        bool Compile(string src, string outPath, bool cs, string shdn)
        {
            string name = Path.GetFileName(src);
            string code;
            try { code = File.ReadAllText(src); }
            catch (Exception ex) { Say("  " + name + ": can't read it: " + ex.Message); return false; }

            var opts = new Dictionary<string, string>();
            opts["CompilerVersion"] = "v4.0";
            CodeDomProvider provider = cs ? (CodeDomProvider)new Microsoft.CSharp.CSharpCodeProvider(opts)
                                          : new Microsoft.VisualBasic.VBCodeProvider(opts);

            var p = new CompilerParameters();
            p.GenerateExecutable = false;
            p.GenerateInMemory = false;
            p.IncludeDebugInformation = false;
            p.TreatWarningsAsErrors = false;
            p.OutputAssembly = outPath;
            p.CompilerOptions = "/platform:x86 /optimize+";
            foreach (string r in DefaultRefs) p.ReferencedAssemblies.Add(r);
            p.ReferencedAssemblies.Add(shdn);

            // extra references:  //#reference X.dll   (C#)  or  '#reference X.dll  (VB)
            foreach (string raw in code.Split('\n'))
            {
                string line = raw.Trim();
                string tag = cs ? "//#reference" : "'#reference";
                if (!line.StartsWith(tag, StringComparison.OrdinalIgnoreCase)) continue;
                string r = line.Substring(tag.Length).Trim().Trim('"');
                if (r.Length == 0) continue;
                string resolved = ResolveRef(r, Path.GetDirectoryName(src));
                p.ReferencedAssemblies.Add(resolved);
            }

            try
            {
                if (File.Exists(outPath)) File.Delete(outPath);
            }
            catch (Exception ex)
            {
                Say("  " + name + ": can't replace " + Path.GetFileName(outPath) + " (" + ex.Message + "). Restart the game to recompile it.");
                return false;
            }

            CompilerResults res;
            try { res = provider.CompileAssemblyFromSource(p, code); }
            catch (Exception ex) { Say("  " + name + ": the compiler could not run: " + ex.Message); return false; }

            int errors = 0;
            foreach (CompilerError e in res.Errors)
            {
                if (e.IsWarning) continue;
                errors++;
                if (errors <= 20) Say("  " + name + "(" + e.Line + "," + e.Column + "): error " + e.ErrorNumber + ": " + e.ErrorText);
            }
            if (errors > 0)
            {
                if (errors > 20) Say("  " + name + ": ... " + (errors - 20) + " more error(s)");
                Say("  " + name + ": NOT compiled (" + errors + " error(s)); this script will not load");
                try { if (File.Exists(outPath)) File.Delete(outPath); } catch { }
                return false;
            }
            Say("  " + name + " -> " + Path.GetFileName(outPath));
            return true;
        }

        string ResolveRef(string r, string scriptDir)
        {
            if (Path.IsPathRooted(r)) return r;
            string fw = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
            foreach (string d in new[] { scriptDir, gameDir, Path.Combine(Path.Combine(gameDir, "IVSDKDotNet"), "bin"), fw })
            {
                string c = Path.Combine(d, r);
                if (File.Exists(c)) return c;
            }
            return r;   // let the compiler look for it (works for framework assemblies by name)
        }
    }
}
