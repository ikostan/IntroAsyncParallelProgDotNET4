﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;
using System.Net;
using System.Threading.Tasks;


//
// This app takes one stock symbol, downloads historial data, and does some
// simple analysis:
//
//    min price
//    max price
//    avg price
//    standard deviation
//    standard error
//
// This version uses 3 web sites (yahoo, nasdaq, and msn) for redundancy, but 
// queries them synchronously one after another.  In reality a better approach
// would be to fire the requests in parallel, but this version is a good place
// to start from a demo perspective.
//
// Usage:  StockHistory.exe  msft
//

namespace StockHistory
{
	class Program
	{

		static void Main(string[] args)
		{
			String version, platform, symbol;
			int numYearsOfHistory;

			ProcessCmdLineArgs(args, out version, out platform, out symbol, out numYearsOfHistory);

			//
			// Process stock symbol:
			//
			ProcessStockSymbol(symbol, numYearsOfHistory);

			Console.WriteLine();
			Console.WriteLine("** Done **");
			Console.WriteLine();

			Console.Write("\n\nPress a key to exit...");
			Console.ReadKey();
		}


		/// <summary>
		/// Downloads and processes historical data for given stock symbol.
		/// </summary>
		/// <param name="symbol">stock symbol, e.g. "msft"</param>
		/// <param name="numYearsOfHistory">years of history > 0, e.g. 10</param>
		private static void ProcessStockSymbol(string symbol, int numYearsOfHistory)
		{
            try
            {
                //Create .Net exception
                //Task t_error = Task.Factory.StartNew(() =>
                //{
                //    int i = 0;
                //    int j = 1000 / i;
                //});

                StockData data = DownloadData.GetHistoricalData(symbol, numYearsOfHistory);

                int N = data.Prices.Count;

                //decimal min = 0, max = 0, avg = 0;

                //Create parallel tasks
                Task<decimal> t_Min = Task.Factory.StartNew(() => {
                    return data.Prices.Min();
                });

                //decimal min = t_Min.Result;

                Task<decimal> t_Max = Task.Factory.StartNew(() => {
                    return data.Prices.Max();
                });

                Task<decimal> t_Avg = Task.Factory.StartNew(() => {
                    return data.Prices.Average();
                });

                // Standard deviation:
                //double sum = 0.0, stddev = 0.0, stderr = 0.0;

                Task<double> t_stddev = Task.Factory.StartNew(() => {

                    //Use local calculated average value instead of waiting for the global value 
                    decimal local_avg = data.Prices.Average();
                    double sum = 0;

                    foreach (decimal value in data.Prices)
                        sum += Math.Pow(Convert.ToDouble(value - local_avg), 2.0);

                    return Math.Sqrt(sum / N);
                });

                // double stddev = t_stddev.Result;

                // Standard error:
                //Task<double> t_stderr = Task.Factory.StartNew(() => {
                Task<double> t_stderr = t_stddev.ContinueWith((t) => {
                    //t_stddev.Wait(); //Wait until task is finished
                    return t.Result / Math.Sqrt(N);
                });

                //Array of tasks
                Task[] tasks = { t_Min, t_Max, t_Avg, t_stddev, t_stderr};

                //Wait for completion of all tasks
                Task.WaitAll(tasks);

                //
                // Output:
                //
                Console.WriteLine();
                Console.WriteLine("** {0} **", symbol);
                Console.WriteLine("   Data source:  '{0}'", data.DataSource);
                Console.WriteLine("   Data points:   {0:#,##0}", N);

                //t_Min.Wait(); //Wait until task is finished
                Console.WriteLine("   Min price:    {0:C}", t_Min.Result);

                //t_Max.Wait(); //Wait until task is finished
                Console.WriteLine("   Max price:    {0:C}", t_Max.Result);

                //t_Avg.Wait(); //Wait until task is finished
                Console.WriteLine("   Avg price:    {0:C}", t_Avg.Result);

                //t_stderr.Wait(); //Wait until task is finished
                Console.WriteLine("   Std dev/err:   {0:0.000} / {1:0.000}", t_stddev.Result, t_stderr.Result);
            }
            catch (AggregateException ae)
            {
                Console.WriteLine();
                //Console.WriteLine("Tasking Error: {0}", ae.InnerException.Message);

                //Flatten all exceptions since there could be many of them
                ae = ae.Flatten();

                foreach (var err in ae.InnerExceptions)
                {
                    Console.WriteLine("Tasking Error: {0}", err.Message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("** {0} **", symbol);
                Console.WriteLine("Error: {0}", ex.Message);
            }
		}


		/// <summary>
		/// Processes command-line arguments, and outputs to the user.
		/// </summary>
		/// 
		static void ProcessCmdLineArgs(string[] args, out string version, out string platform, out string symbol, out int numYearsOfHistory)
		{
#if DEBUG
			version = "debug";
#else
			version = "release";
#endif

#if _WIN64
	platform = "64-bit";
#elif _WIN32
	platform = "32-bit";
#else
			platform = "any-cpu";
#endif

			symbol = "";  // in case user does not supply:
			numYearsOfHistory = 10;

			string usage = "Usage: StockHistory.exe [-? /? symbol ]";

			if (args.Length > 1)
			{
				Console.WriteLine("** Error: incorrect number of arguments (found {0}, expecting 1)", args.Length);
				Console.WriteLine(usage);
				System.Environment.Exit(-1);
			}

			for (int i = 0; i < args.Length; i++)
			{
				string arg = args[i];

				if (arg == "-?" || arg == "/?")
				{
					Console.WriteLine(usage);
					System.Environment.Exit(-1);
				}
				else  // assume arg is stock symbol:
				{
					symbol = arg;
				}
			}//for

			if (symbol == "")
			{
				Console.WriteLine();
				Console.Write("Please enter stock symbol (e.g. 'msft'): ");
				symbol = Console.ReadLine();
			}

			symbol = symbol.Trim();  // delete any leading/trailing spaces:
			if (symbol == "")
			{
				Console.WriteLine();
				Console.WriteLine("** Error: you must enter a stock symbol, e.g. 'msft'");
				Console.WriteLine(usage);
				Console.WriteLine();
				System.Environment.Exit(-1);
			}

			Console.WriteLine();
			Console.WriteLine("** Sequential Stock History App [{0}, {1}] **", platform, version);
			Console.WriteLine("   Stock symbol:     {0}", symbol);
			Console.WriteLine("   Time period:      last {0} years", numYearsOfHistory);
			Console.WriteLine("   Internet access?  {0}", DownloadData.IsConnectedToInternet());
		}

	}//class
}//namespace
