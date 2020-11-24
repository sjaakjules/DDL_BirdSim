using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using Easy.Common.Extensions;


namespace DeepDesignLab.Base
{
    public abstract class SimpleThreadedParent
    {

        private BackgroundWorker backgroundReader = new BackgroundWorker();
        protected int highestPercentageReached = 0;
        protected string progress = "0%";
        protected string ErrorMessages = "";
        protected string messageOut = "";
        protected bool isFinished = false;
        protected bool finishedWithOutError = false;

        protected int finalValue = -1;
        protected int currentValue = -1;

        protected Stopwatch timer = new Stopwatch();

        public string getProgress { get { return progress.ToString(); } }
        public string getMessages { get { return messageOut.ToString(); } }
        public string getErrors { get { return ErrorMessages.ToString(); } }
        public float getProgressValue { get { return (float)highestPercentageReached / 100; } }


        public bool hasFinished { get { return finishedWithOutError; } }
        public bool isActive { get { return backgroundReader.IsBusy; } }

        public bool isCanceled { get { return backgroundReader.CancellationPending; } }


        public TimeSpan timeRemaining
        {
            get
            {
                if (isActive)
                {
                    double time = double.IsNaN((double)timer.ElapsedMilliseconds / currentValue * (finalValue - currentValue)) ? 0 : (double)timer.ElapsedMilliseconds / currentValue * (finalValue - currentValue);
                    return TimeSpan.FromMilliseconds(time);
                }
                else return TimeSpan.Zero;
            }
        }


        public SimpleThreadedParent() 
        {
            backgroundReader.WorkerReportsProgress = true;
            backgroundReader.WorkerSupportsCancellation = true;
            InitializeBackgroundWorker();
        }

        /// <summary>
        /// Sets up the background worker. This adds the stuff to do, compleation and progress events to the event handlers
        /// </summary>
        private void InitializeBackgroundWorker()
        {
            backgroundReader.DoWork += new DoWorkEventHandler(BackgroundReader_DoWork);
            backgroundReader.RunWorkerCompleted += new RunWorkerCompletedEventHandler(AsyncRead_RunWorkerCompleted);
            backgroundReader.ProgressChanged += new ProgressChangedEventHandler(AsyncRead_ProgressChanged);

        }

        /// <summary>
        /// This will do work on an extracted method passing in an int to schedule multiple tasks. 
        /// Only one task at a time.
        /// </summary>
        /// <param name="task"></param> Int to select task via Switch.
        public void doWork(int task)
        {
            if (backgroundReader.IsBusy != true)
            {
                // this.outputPath = outputPath;
                //   this.folderName = folderName;
                // reset the results....
                // set any bools and settings to keep track... this might be flag files.
                highestPercentageReached = 0;

                // Start the asynchronous operation.
                backgroundReader.RunWorkerAsync(task);
            }
        }


        public void cancel()
        {
            if (!backgroundReader.CancellationPending)
            {
                backgroundReader.CancelAsync();
            }
        }


        /// <summary>
        /// Start reading a file. This will check if it is busy, as in it has started reading already.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void startAsyncRead(object sender, EventArgs e)
        {

            if (backgroundReader.IsBusy != true)
            {
                // Start the asynchronous operation.
                backgroundReader.RunWorkerAsync();
            }
        }

        /// <summary>
        /// This will cancel the reading of a file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cancelAsyncRead(object sender, EventArgs e)
        {
            if (backgroundReader.WorkerSupportsCancellation == true)
            {
                // Cancel the asynchronous operation.

                /*
                foreach (KeyValuePair<string, fileHeaderInfo> item in files) {
                    item.Value.closeStreams();
                }
                */
                backgroundReader.CancelAsync();

            }
        }

        // This event handler is where the time-consuming work is done.
        private void BackgroundReader_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            if (worker.CancellationPending == true)
            {
                e.Cancel = true;
            }
            else
            {
                scheduleTask((int)e.Argument, worker, e);
            }
        }

        private void AsyncRead_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progress = (e.ProgressPercentage.ToString() + "%");
            //messageOut = string.Format("{0} Lines read.\n", nLinesRead) + ErrorMessages;
        }

        private void AsyncRead_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled == true)
            {
                ErrorMessages += progress = "Job was canceled!\n";
                messageOut = "Job was canceled!\n" + ErrorMessages;
                // ErrorMessages = "";
                isFinished = true;
                finishedWithOutError = false;
            }
            else if (e.Error != null)
            {
                ErrorMessages += progress = "Error: \n" + e.Error.Message + "\n";
                ErrorMessages += "Stack: \n" + e.Error.StackTrace + "\n";
                messageOut = ErrorMessages;
                // ErrorMessages = "";
                isFinished = true;
                finishedWithOutError = false;
            }
            else
            {
                ErrorMessages += progress = "100%\n";
                messageOut = "Job Finished!\n" + ErrorMessages;
                //  ErrorMessages = "";
                isFinished = true;
                finishedWithOutError = true;
            }

            // Reset any bools as it is complete. This might be deleting flag files.
        }



        private void scheduleTask(int taskID, BackgroundWorker worker, DoWorkEventArgs e)
        {

            isFinished = false;
            ErrorMessages += "Error Log:\nAttempting to read\n";

            if (worker.CancellationPending)
            {
                e.Cancel = true;
            }
            else
            {
                // var data = new DataHeader();
                try
                {
                    timer.Reset();
                    timer.Start();


                    ProcessData(taskID, worker, e);
                                        

                    if (worker.CancellationPending)
                    {
                        worker.CancelAsync();
                        e.Cancel = true;
                    }
                
                        ErrorMessages += messageOut = string.Format("\nJob Finished within {0} seconds", (float)timer.ElapsedMilliseconds / 1000f);

                timer.Stop();
                }
                catch (Exception er)
                {
                    ErrorMessages += er.Message;
                    //throw;
                }
                timer.Stop();
                timer.Reset();
            }
        }

        protected void updatePercentage(int final, int current, BackgroundWorker worker, DoWorkEventArgs e)
        {
            int percentage = (int)((float)current / (float)final * 100);

            if (percentage > highestPercentageReached)
            {
                highestPercentageReached = percentage;
                messageOut = string.Format("\n{0}% finished.\n", percentage) + ErrorMessages;
                worker.ReportProgress(highestPercentageReached);
            }
        }

        protected virtual void ProcessData(int taskID, BackgroundWorker worker, DoWorkEventArgs e)
        {

        }

    }
}