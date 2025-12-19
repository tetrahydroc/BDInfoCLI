//============================================================================
// BDInfo - Blu-ray Video and Audio Analysis Tool
// Copyright © 2010 Cinema Squid
//
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
//=============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Mono.Options;

namespace BDInfo
{
    static class Program
    {
        public static void ShowHelp(OptionSet optionSet, string msg = null)
        {
            if (msg != null)
                Console.Error.WriteLine(msg);
            Console.Error.WriteLine("Usage: BDInfo <BD_PATH> [REPORT_DEST]");
            Console.Error.WriteLine("BD_PATH may be a directory containing a BDMV folder or a BluRay ISO file.");
            Console.Error.WriteLine("REPORT_DEST is the folder the BDInfo report is to be written to. If not");
            Console.Error.WriteLine("given, the report will be written to BD_PATH. REPORT_DEST is required if");
            Console.Error.WriteLine("BD_PATH is an ISO file.\n");
            optionSet.WriteOptionDescriptions(Console.Error);
            Environment.Exit(-1);
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            bool help = false;
            bool version = false;
            bool whole = false;
            bool list = false;
            string mpls = null;

            OptionSet optionSet = new OptionSet()
                .Add("h|help", "Print out the options.", option => help = option != null)
                .Add("l|list", "Print the list of playlists.", option => list = option != null)
                .Add("m=|mpls=", "Comma separated list of playlists to scan.", option => mpls = option)
                .Add("w|whole", "Scan whole disc - every playlist.", option => whole = option != null)
                .Add("v|version", "Print the version.", option => version = option != null)
            ;

            List<string> nsargs = new List<string>();
            try
            {
                nsargs = optionSet.Parse(args);
            }
            catch (OptionException)
            {
                ShowHelp(optionSet, "Error - usage is:");
            }

            if (help)
                ShowHelp(optionSet);

            if (version)
            {
                Console.WriteLine(Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.8.0");
                Environment.Exit(0);
            }

            if (list)
                whole = true;

            if (nsargs.Count == 0)
            {
                ShowHelp(optionSet, "Error: insufficient args - usage is:");
                Environment.Exit(-1);
            }

            string bdPath = nsargs[0];
            if (!File.Exists(bdPath) && !Directory.Exists(bdPath))
            {
                Console.Error.WriteLine($"error: {bdPath} does not exist");
                Environment.Exit(-1);
            }

            string reportPath = bdPath;
            if (nsargs.Count == 1 && !Directory.Exists(bdPath))
            {
                Console.Error.WriteLine($"error: REPORT_DEST must be given if BD_PATH is an ISO.");
                Environment.Exit(-1);
            }
            if (nsargs.Count == 2)
                reportPath = nsargs[1];
            if (!Directory.Exists(reportPath))
            {
                Console.Error.WriteLine($"error: {reportPath} does not exist or is not a directory");
                Environment.Exit(-1);
            }

            // Create CLI handler and run scan
            BDInfoCLI cli = new BDInfoCLI();

            Console.WriteLine("Please wait while we scan the disc...");
            cli.InitBDROM(bdPath);

            if (mpls != null)
            {
                Console.WriteLine(mpls);
                cli.LoadPlaylists(mpls.Split(',').ToList());
            }
            else if (whole)
            {
                cli.LoadPlaylists(true);
            }
            else
            {
                cli.LoadPlaylists();
            }

            if (list)
                Environment.Exit(0);

            cli.ScanBDROM();

            // Generate report
            ScanBDROMResult scanResult = cli.GetScanResult();
            if (scanResult.ScanException != null)
            {
                Console.WriteLine($"{scanResult.ScanException.Message}");
            }
            else
            {
                if (scanResult.FileExceptions.Count > 0)
                {
                    Console.WriteLine("Scan completed with errors (see report).");
                }
                else
                {
                    Console.WriteLine("Scan completed successfully.");
                }

                Console.WriteLine("Please wait while we generate the report...");
                try
                {
                    ReportGenerator report = new ReportGenerator();
                    report.Generate(cli.GetBDROM(), cli.GetSelectedPlaylists(), scanResult, reportPath);
                    Console.WriteLine($"Report saved to: {reportPath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error generating report: {ex.Message}");
                }
            }
        }
    }
}
