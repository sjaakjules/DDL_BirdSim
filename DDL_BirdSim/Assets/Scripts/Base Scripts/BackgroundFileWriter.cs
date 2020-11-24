using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System;
using System.ComponentModel;
using System.Diagnostics;
using UnityEngine;
using System.Text;
using Easy.Common.Extensions;

namespace DeepDesignLab.Base {
    public abstract class BackgroundFileWriter {

        private BackgroundWorker backgroundWriter = new BackgroundWorker();
        private readonly bool writeBinary;
        protected int highestPercentageReached = 0;
        protected int nLinesToWrite = -1;
        protected string progress = "0%";
        protected string ErrorMessages = "";
        protected string messageOut = "";
        protected bool isFinished = false;
        protected bool finishedWithOutError = false;

        protected string headerFinish;

        protected string lastWrittenPath;

        //   private float lastX=float.NaN, lastY = float.NaN;
        //  protected string outputPath, folderName, newFilePath;


        protected Stopwatch timer = new Stopwatch();

        public string getProgress { get { return progress.ToString(); } }
        public string getMessages { get { return messageOut.ToString(); } }
        public string getErrors { get { return ErrorMessages.ToString(); } }
        public float getProgressValue { get { return (float)highestPercentageReached / 100; } }
        public string getLastWrittenPath { get { return lastWrittenPath; } }

        public bool hasFinished { get { return finishedWithOutError; } }
        public bool isActive { get { return backgroundWriter.IsBusy; } }

        public bool isCanceled { get { return backgroundWriter.CancellationPending; } }

        // protected Dictionary<string, fileHeaderInfo> files = new Dictionary<string, fileHeaderInfo>();

        public TimeSpan timeRemaining {
            get {
                if (isActive) {
                    double time = double.IsNaN((double)timer.ElapsedMilliseconds / (highestPercentageReached/100)) ? 0 : (double)timer.ElapsedMilliseconds / (highestPercentageReached / 100) - (double)timer.ElapsedMilliseconds;
                    return TimeSpan.FromMilliseconds(time);
                }
                else return TimeSpan.Zero;
            }
        }

        public TimeSpan elapsedTime {
            get {
                return TimeSpan.FromMilliseconds(timer.ElapsedMilliseconds);
            }
        }

        public BackgroundFileWriter(bool WriteBinary) {
            writeBinary = WriteBinary;
            backgroundWriter.WorkerReportsProgress = true;
            backgroundWriter.WorkerSupportsCancellation = true;
            InitializeBackgroundWorker();
        }

        public BackgroundFileWriter():this(false) {
        }

        /// <summary>
        /// Sets up the background worker. This adds the stuff to do, compleation and progress events to the event handlers
        /// </summary>
        private void InitializeBackgroundWorker() {
            backgroundWriter.DoWork += new DoWorkEventHandler(BackgroundWriter_DoWork);
            backgroundWriter.RunWorkerCompleted += new RunWorkerCompletedEventHandler(AsyncWrite_RunWorkerCompleted);
            backgroundWriter.ProgressChanged += new ProgressChangedEventHandler(AsyncWrite_ProgressChanged);

        }

        /// <summary>
        /// This will read a file given a path directory. Expected to be a full path from C:\
        /// </summary>
        /// <param name="path"></param> The path to read the file.
        public void WriteFile(string path) {
            if (backgroundWriter.IsBusy != true) {
                // this.outputPath = outputPath;
                //   this.folderName = folderName;
                // reset the results....
                // set any bools and settings to keep track... this might be flag files.
                highestPercentageReached = 0;
                // nLinesRead = 1;


                ErrorMessages += "Writing to file: " + path;
                messageOut = "Writing to file: " + path;
                lastWrittenPath = path;
                // Start the asynchronous operation.
                backgroundWriter.RunWorkerAsync(path);
            }
        }

        public void cancel() {
            if (!backgroundWriter.CancellationPending) {
                backgroundWriter.CancelAsync();
            }
        }


        /// <summary>
        /// Start reading a file. This will check if it is busy, as in it has started reading already.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void startAsyncWrite(object sender, EventArgs e) {

            if (backgroundWriter.IsBusy != true) {
                // Start the asynchronous operation.
                backgroundWriter.RunWorkerAsync();
            }
        }

        /// <summary>
        /// This will cancel the reading of a file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cancelAsyncWrite(object sender, EventArgs e) {
            if (backgroundWriter.WorkerSupportsCancellation == true) {
                // Cancel the asynchronous operation.

                /*
                foreach (KeyValuePair<string, fileHeaderInfo> item in files) {
                    item.Value.closeStreams();
                }
                */
                backgroundWriter.CancelAsync();

            }
        }

        // This event handler is where the time-consuming work is done.
        private void BackgroundWriter_DoWork(object sender, DoWorkEventArgs e) {
            BackgroundWorker worker = sender as BackgroundWorker;

            if (worker.CancellationPending == true) {
                e.Cancel = true;
            }
            else {
                WriteDataFile((string)e.Argument, worker, e);
            }
        }

        private void AsyncWrite_ProgressChanged(object sender, ProgressChangedEventArgs e) {
            progress = (e.ProgressPercentage.ToString() + "%");
            //messageOut = string.Format("{0} Lines read.\n", nLinesRead) + ErrorMessages;
        }

        private void AsyncWrite_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            if (e.Cancelled == true) {
                ErrorMessages += progress = "Job was canceled!\n";
                messageOut = "Job was canceled!\n" + ErrorMessages;
                // ErrorMessages = "";
                isFinished = true;
                finishedWithOutError = false;
            }
            else if (e.Error != null) {
                ErrorMessages += progress = "Error: \n" + e.Error.Message + "\n";
                ErrorMessages += "Stack: \n" + e.Error.StackTrace + "\n";
                messageOut = ErrorMessages;
                // ErrorMessages = "";
                isFinished = true;
                finishedWithOutError = false;
            }
            else {
                ErrorMessages += progress = "100%\n";
                messageOut = "Job Finished!\n" + ErrorMessages;
                //  ErrorMessages = "";
                isFinished = true;
                finishedWithOutError = true;
            }

            // Reset any bools as it is complete. This might be deleting flag files.
        }




        private void WriteDataFile(string path, BackgroundWorker worker, DoWorkEventArgs e) {

            isFinished = false;
            ErrorMessages += "Log:\nAttempting to write\n";

            if (worker.CancellationPending) {
                e.Cancel = true;
            }
            else {
                // var data = new DataHeader();
                try {
                    // begin
                    timer.Reset();
                    timer.Start();
                    if (writeBinary) {
                        using (FileStream fs = File.Open(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite))
                        using (BufferedStream bs = new BufferedStream(fs, 4 * 1024 * 1024))
                        using (BinaryWriter writer = new BinaryWriter(bs)) {

                            ErrorMessages += string.Format("No header lines.\n");
                            // Wrote header lines, now to write body.
                            int percentage = 0;
                            ProcessData(writer, worker, e);

                            percentage = 100;
                            highestPercentageReached = percentage;
                            ErrorMessages += messageOut = string.Format("{0}% finished.\n", percentage);
                            worker.ReportProgress(highestPercentageReached);

                            if (worker.CancellationPending) {
                                worker.CancelAsync();
                                e.Cancel = true;
                            }
                        }
                        ErrorMessages += messageOut = string.Format("\nRead within {0} seconds\nJob Finished.\n", (float)timer.ElapsedMilliseconds / 1000f);
                    }
                    else {
                        using (FileStream fs = File.Open(path, FileMode.Open, FileAccess.Write, FileShare.ReadWrite))
                        using (BufferedStream bs = new BufferedStream(fs, 4 * 1024 * 1024))
                        using (StreamWriter writer = new StreamWriter(bs)) {

                            ProcessHeaderLine(writer, ref nLinesToWrite, worker, e);

                            ErrorMessages += string.Format("Wrote header lines.\n");


                            // Wrote header lines, now to write body.
                            int percentage = 0;
                            for (int i = 0; i < 100; i++) {

                                ProcessLine(writer, i, worker, e);

                                percentage++;
                                highestPercentageReached = percentage;
                                ErrorMessages += messageOut = string.Format("{0}% finished.\n", percentage);
                                worker.ReportProgress(highestPercentageReached);

                                if (worker.CancellationPending) {
                                    worker.CancelAsync();
                                    e.Cancel = true;
                                    break;
                                }
                            }
                            ErrorMessages += messageOut = string.Format("\nRead within {0} seconds\nJob Finished.\n", (float)timer.ElapsedMilliseconds / 1000f);
                        }
                        timer.Stop();
                    }
                }
                catch (Exception er) {
                    ErrorMessages += er.Message;
                    //throw;
                }
                timer.Stop();
                //timer.Reset();
            }
            // isFinished = true;
        }

        protected virtual void ProcessHeaderLine(StreamWriter writer, ref int nLines, BackgroundWorker worker, DoWorkEventArgs e) {

        }

        /// <summary>
        /// This is called 100 times! Make sure to batch processing into 100 tasks for percentage notification.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="worker"></param>
        /// <param name="e"></param>
        protected virtual void ProcessLine(StreamWriter writer, int loopIndex,  BackgroundWorker worker, DoWorkEventArgs e) {

        }

        protected virtual void ProcessData(BinaryWriter writer, BackgroundWorker worker, DoWorkEventArgs e) {

        }

    }


}


