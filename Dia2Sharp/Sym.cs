﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Dia2Lib;
using static System.Console;
using System.Diagnostics;
using System.ComponentModel;
using System.Runtime.InteropServices;


namespace Dia2Sharp
{
    public class Sym
    {
        public static IntPtr hCurrentProcess = Process.GetCurrentProcess().Handle;
        IDiaSession Session = null;
        static string SymPath;

        public static Sym Initalize()
        {
            DebugHelp.SymSetOptions(DebugHelp.SymOptions.SYMOPT_DEBUG);

            SymPath = Environment.GetEnvironmentVariable("_NT_SYMBOL_PATH");
            if (string.IsNullOrWhiteSpace(SymPath))
                SymPath = "SRV*http://msdl.microsoft.com/download/symbols";

            bool symStatus = DebugHelp.SymInitialize(hCurrentProcess, SymPath, false);
            if (!symStatus)
                WriteLine($"symbol status  {symStatus}:  {new Win32Exception(Marshal.GetLastWin32Error()).Message }");

            DebugHelp.SymSetOptions(DebugHelp.SymOptions.SYMOPT_DEBUG);

            return new Sym();
        }

        void CollectCompileDetails(IDiaSymbol detail, String ModName, String BlockName)
        {
            string Language = string.Empty, Platform = string.Empty;
            var lang = detail.language;
            var plat = detail.platform;

            switch (lang)
            {
                case 0: Language = "C"; break;
                case 1: Language = "C++"; break;
                case 2: Language = "Linked DLL/Import"; break;
                case 3: Language = "Fortran"; break;
                case 4: Language = "MASM"; break;
                case 5: Language = "Pascal"; break;
                case 6: Language = "ILASM"; break;
                case 7: Language = "MSIL"; break;
                case 8: Language = "HLSL"; break;
                case 9: Language = "Resource Data"; break;
                case 10: Language = "PGO Data (performance guided opt)"; break;
                case 11: Language = "Managed C#"; break;
                default: Language = "Other / Not hookable"; break;
            }

            if (plat > 2 && plat < 8)
                Platform = "x86";
            if (plat == 0xD0)
                Platform = "x64";
            else
                Platform = "Unsupported";

            WriteLine($"Language: {Language} / {Platform}");
        }

        void FuncCollectSym(IDiaSymbol Detail, uint tag, String ModName, String BlockName)
        {
            IDiaEnumSymbols EnumSymbols = null;
            IDiaSymbol Symbol = null;
            List<string> Args = new List<string>();
            uint childrenFetched = 0;

            ForegroundColor = ConsoleColor.Green;

            if (Detail == null || string.IsNullOrWhiteSpace(Detail.name))
                return;

            WriteLine($"{Detail.undecoratedName} ({Detail.name}) Length: {Detail.length} RVA: {Detail.targetRelativeVirtualAddress} VA: {Detail.targetVirtualAddress}");

            Detail.findChildren(SymTagEnum.SymTagNull, null, 0, out EnumSymbols);
            do
            {
                //EnumSymbols.Next(1, out Symbol, out childrenFetched);
                //if (Symbol == null || string.IsNullOrEmpty(Symbol.name))
                //    continue;

                Symbol = Detail;

                if (Symbol.type != null)
                    Args.Add(Symbol.type.name);
                else
                    WriteLine($"{Symbol.undecoratedName} ({Symbol.name}) @ {Symbol.virtualAddress:X} Length: {Symbol.length} ");

            } while (childrenFetched == 1);
        }

        void ClassCollectSym(IDiaSymbol Detail)
        {
            IDiaEnumSymbols EnumSymbols = null;
            IDiaSymbol Symbol = null;
            List<string> Args = new List<string>();
            uint childrenFetched = 0;

            ForegroundColor = ConsoleColor.Yellow;

            if (Detail == null || string.IsNullOrWhiteSpace(Detail.name))
                return;

            WriteLine($"{Detail.undecoratedName} ({Detail.name}) Length: {Detail.length} RVA: {Detail.targetRelativeVirtualAddress} VA: {Detail.targetVirtualAddress}");

            Detail.findChildren(SymTagEnum.SymTagNull, null, 0, out EnumSymbols);
            do
            {
                EnumSymbols.Next(1, out Symbol, out childrenFetched);
                if (Symbol == null || string.IsNullOrEmpty(Symbol.name))
                    continue;

                if (Symbol.type != null)
                    Args.Add(Symbol.type.name);
                else
                    WriteLine($"{Symbol.undecoratedName} ({Symbol.name}) @ {Symbol.virtualAddress:X} Length: {Symbol.length} ");

            } while (childrenFetched == 1);
        }

        public int Enum(string arg)
        {
            IDiaEnumSymbols EnumSymbols = null;
            IDiaSymbol Master = null;
            IDiaEnumTables tables = null;
            int level = 2;
            uint compileFetched = 0;

            //DebugHelp.SymFindDebugInfoFile(hCurrentProcess, SymPath, arg, )

            var foo = new Dia2Lib.DiaSource();
            foo.loadDataForExe(arg, SymPath, null);
            //foo.loadDataFromPdb(arg);
            foo.openSession(out Session);

            if (Session == null)
                return -1;

            Session.loadAddress = 0;
            uint rva = 0;

            //do
            //{
            //    try
            //    {
            //        Session.findChildren(Session.globalScope, SymTagEnum.SymTagNull, null, 0, out EnumSymbols);
            //        var tot1 = EnumSymbols.count;
            //        uint curr1 = 0;
            //        do
            //        {
            //            EnumSymbols.Next(1, out Master, out compileFetched);
            //            if (Master == null)
            //                continue;

            //            ForegroundColor = ConsoleColor.White;

            //            foreach (var pr in typeof(IDiaSymbol).LinqPublicProperties())
            //            {
            //                WriteLine($"{pr.Name} = {pr.GetValue(Master)}");
            //            }

            //            //foreach (var fn in typeof(IDiaSymbol).LinqPublicFunctions())
            //            //{
            //            //    //if (fn.Name.Contains("get"))
            //            //        WriteLine($"{fn.Name} = {fn.Invoke(Master, null)}");
            //            //}
            //            // DumpSymbol<IDiaSymbol>(Master, ref level);

            //        } while (curr1++ < tot1);



            //        //foreach (var pr in typeof(IDiaSymbol).LinqPublicProperties())
            //        //    WriteLine($"{pr.Name} = {pr.GetValue(Master)}");
            //    }
            //    catch { }
            //    finally
            //    {
            //        if (Master != null)
            //            rva += (uint)(Master.length == 0 ? 1 : Master.length);
            //    }

            //} while (Master != null);





            var GlobalScope = Session.globalScope;

            IDiaEnumDebugStreams DebugStreams;

            Session.getEnumDebugStreams(out DebugStreams);
            for (int i = 0; i < DebugStreams.count; i++)
            {
                var ds = DebugStreams.Item(i);
                foreach (var pr in typeof(IDiaEnumDebugStreamData).LinqPublicProperties())
                    WriteLine($"{pr.Name} = {pr.GetValue(ds)}");
            }

            Session.getEnumTables(out tables);
            for (int i = 0; i < tables.count; i++)
            {
                var ds = tables.Item(i);
                foreach (var pr in typeof(IDiaTable).LinqPublicProperties())
                    WriteLine($"{pr.Name} = {pr.GetValue(ds)}");
            }
            GlobalScope.findChildren(SymTagEnum.SymTagNull, null, 0, out EnumSymbols);
            var tot = EnumSymbols.count;
            var curr = 0;
            do
            {
                EnumSymbols.Next(1, out Master, out compileFetched);
                if (Master == null)
                    continue;

                ForegroundColor = ConsoleColor.White;

                //foreach (var pr in typeof(IDiaSymbol).LinqPublicProperties())
                    //WriteLine($"{pr.Name} = {pr.GetValue(Master)}");

                DumpSymbol<IDiaSymbol>(Master, level);

            } while (curr++ < tot);

            return 0;
        }

        void DumpSymbol<T>(T Symbol, int level)
        {
            IDiaEnumSymbols EnumInner; 
            IDiaSymbol IsInner;
            var curr = 0;
            if (Symbol == null)
                return;
            uint tmp = 0;

            var ds = Symbol;
            foreach (var pr in typeof(T).LinqPublicProperties())
            {
                WriteLine($"{pr.Name} = {pr.GetValue(ds)}");
                if (ds is IDiaSymbol)
                {
                    (ds as IDiaSymbol).findChildren(SymTagEnum.SymTagNull, null, 0, out EnumInner);
                    if (EnumInner != null)
                    {
                        var tot = EnumInner.count;
                        level++;
                        do
                        {
                            EnumInner.Next(1, out IsInner, out tmp);
                            if (IsInner == null)
                                continue;

                            DumpSymbol(IsInner, level);


                            //foreach (var prx in typeof(IDiaSymbol).LinqPublicProperties())
                                //WriteLine($"{pr.Name} = {pr.GetValue(IsInner)}");
                                
                        } while (curr++ < tot);
                    }
                }
            }


            //if (Symbol is IDiaSymbol)
            //{
            //    (Symbol as IDiaSymbol).findChildren(SymTagEnum.SymTagNull, null, 0, out Enum);
            //    if (Enum == null)
            //        return;

            //    for (uint ui = 0; ui < Enum.count; ui++)
            //    {
            //        var _ds = Enum.Item(ui);
            //        foreach (var pr in typeof(T).LinqPublicProperties())
            //        {
            //            WriteLine($"{pr.Name} = {pr.GetValue(_ds)}");
            //            if (pr.GetType() == typeof(IDiaSymbol))
            //            {
            //                _ds.findChildren(SymTagEnum.SymTagNull, null, 0, out EnumInner);
            //                if (EnumInner != null)
            //                    DumpSymbol(_ds, ref level);
            //            }
            //        }
            //    }
            //}
        }
    }
}



//if (Master.symTag >=2u && Master.symTag <= 4)
//    WriteLine($"{Master.name} @ {Master.virtualAddress:X} Length {Master.length:X} SectionAddr = {Master.addressSection}");

//Master.findChildren(SymTagEnum.SymTagNull, null, 0, out childrenEnumSymbols);
//do
//{//if (childrenEnumSymbols != null)
//{
//if (Symbol == null || string.IsNullOrEmpty(Symbol.name))
//continue;

//WriteLine($"{Symbol.name} @ {Symbol.virtualAddress:X} Length {Symbol.length:X} Offset {Symbol.addressOffset:X} Section {Symbol.addressSection} ");
//WriteLine($"{Symbol.name} @ {Symbol.virtualAddress:X} Length {Symbol.length:X} Offset {Symbol.addressOffset:X} Section {Symbol.addressSection} ");
/*
var tag = Symbol.symTag;

if (tag == 3)
    CollectCompileDetails(Symbol, Path.GetFileNameWithoutExtension(arg), Master.name);
else if (tag == 5 || tag == 10 || tag == 27 || tag == 31)
        FuncCollectSym(Symbol, tag, Path.GetFileNameWithoutExtension(arg), Master.name);

if (Symbol.udtKind == 1)
    ClassCollectSym(Symbol);
    */
//childrenEnumSymbols.Next(1, out Symbol, out childrenFetched);
//}
//} while (childrenFetched == 1u);