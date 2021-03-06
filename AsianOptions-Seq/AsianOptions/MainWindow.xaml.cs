﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Threading.Tasks;
using System.Threading;

namespace AsianOptions
{

	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
        private int count = 0;


		//
		// Methods:
		//
		public MainWindow()
		{
			InitializeComponent();
		}

		/// <summary>
		/// Exit the app.
		/// </summary>
		private void mnuFileExit_Click(object sender, RoutedEventArgs e)
		{
			this.Close();  // trigger "closed" event as if user had hit "X" on window:
		}

		/// <summary>
		/// Saves the contents of the list box.
		/// </summary>
		private void mnuFileSave_Click(object sender, RoutedEventArgs e)
		{
			using (StreamWriter file = new StreamWriter("results.txt"))
			{
				foreach (string item in this.lstPrices.Items)
					file.WriteLine(item);
			}
		}

        /// <summary>
		/// Saves the contents of the list box.
		/// </summary>
		private void mnuFileCancel_Click(object sender, RoutedEventArgs e)
        {
            
        }

        /// <summary>
        /// Main button to run the simulation.
        /// </summary>
        private void cmdPriceOption_Click(object sender, RoutedEventArgs e)
		{
            count++;
            this.lblCount.Content = count.ToString();

            //ButtonToggle();

            this.spinnerWait.Visibility = System.Windows.Visibility.Visible;
            this.spinnerWait.Spin = true;

            //Config params
            double initial = Convert.ToDouble(txtInitialPrice.Text);
			double exercise = Convert.ToDouble(txtExercisePrice.Text);
			double up = Convert.ToDouble(txtUpGrowth.Text);
			double down = Convert.ToDouble(txtDownGrowth.Text);
			double interest = Convert.ToDouble(txtInterestRate.Text);
			long periods = Convert.ToInt64(txtPeriods.Text);
			long sims = Convert.ToInt64(txtSimulations.Text);

            //
            // Run simulation to price option:
            //

            string result = null;
            
            //Following approach is for .Net v4.0. The new .Net versin has much better approach

            //Create a separate task
            Task T = new Task(() =>
            {
                result = CalculationTask(initial, exercise, up, down, interest, periods, sims);
            });

            //Task for UI thread
            Task T2 = T.ContinueWith((t) => {

                //
                // Display the results (UI thread):
                //
                lstPrices.Items.Insert(0, result);

                count--;
                this.lblCount.Content = count.ToString();

                if (count == 0)
                {
                    //ButtonToggle();
                    this.spinnerWait.Visibility = System.Windows.Visibility.Collapsed;
                    this.cmdPriceOption.IsEnabled = true;
                }
            },
            TaskScheduler.FromCurrentSynchronizationContext() //Run on the UI thread
            );

            //Start the task
            T.Start();
        }

        /// <summary>
        /// Calculation method
        /// </summary>
        /// <param name="initial"></param>
        /// <param name="exercise"></param>
        /// <param name="up"></param>
        /// <param name="down"></param>
        /// <param name="interest"></param>
        /// <param name="periods"></param>
        /// <param name="sims"></param>
        /// <param name="result"></param>
        private string CalculationTask(double initial, 
                                    double exercise, 
                                    double up, 
                                    double down, 
                                    double interest, 
                                    long periods, 
                                    long sims) {

            //Cancellation token
            CancellationTokenSource cts = new CancellationTokenSource();
            CancellationToken token = cts.Token;

            Task<string> T = Task.Factory.StartNew(() =>
            {
                Random rand = new Random();
                int start = System.Environment.TickCount;

                //The most heavy function
                double price = AsianOptionsPricing.Simulation(token, rand, initial, exercise, up, down, interest, periods, sims);

                int stop = System.Environment.TickCount;

                double elapsedTimeInSecs = (stop - start) / 1000.0;

                string result = string.Format("{0:C}  [{1:#,##0.00} secs]",
                    price, elapsedTimeInSecs);
                return result;
            },
            token
            );

            return T.Result;
        }

        /// <summary>
        /// Disable/Enable calculation button.
        /// Show/Hide spinning icon
        /// </summary>
        private void ButtonToggle()
        {
            if (this.cmdPriceOption.IsEnabled)
            {
                this.cmdPriceOption.IsEnabled = false;
                this.spinnerWait.Visibility = System.Windows.Visibility.Visible;
                this.spinnerWait.Spin = true;
            }
            else
            {
                this.spinnerWait.Spin = false;
                this.spinnerWait.Visibility = System.Windows.Visibility.Collapsed;
                this.cmdPriceOption.IsEnabled = true;
            }
        }

	}//class
}//namespace
