using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using Easy.Common.Extensions;

namespace DeepDesignLab.Base {
    public abstract class BackgroundFileReader {

        private BackgroundWorker backgroundReader = new BackgroundWorker();
        private readonly bool readBinary;
        protected int highestPercentageReached = 0;
        protected int nLinesRead = 1;
        protected int aproxLines;
        protected string progress = "0%";
        protected string ErrorMessages = "";
        protected string messageOut = "";
        protected bool isFinished = false;
        protected bool finishedWithOutError = false;
        protected bool hasReadHeader = false;

        protected string lastReadPath;

        protected string headerFinish;

        //   private float lastX=float.NaN, lastY = float.NaN;
      //  protected string outputPath, folderName, newFilePath;


        protected Stopwatch timer = new Stopwatch();

        public string getProgress { get { return progress.ToString(); } }
        public string getMessages { get { return messageOut.ToString(); } }
        public string getErrors { get { return ErrorMessages.ToString(); } }
        public float getProgressValue { get { return (float)highestPercentageReached / 100; } }
        public string getLastReadPath { get { return lastReadPath; } }


        public bool hasFinished { get { return finishedWithOutError; } }
        public bool isActive { get { return backgroundReader.IsBusy; } }

        public bool isCanceled { get { return backgroundReader.CancellationPending; } }

       // protected Dictionary<string, fileHeaderInfo> files = new Dictionary<string, fileHeaderInfo>();

        public TimeSpan timeRemaining {
            get {
                if (isActive) {
                    double time = double.IsNaN((double)timer.ElapsedMilliseconds / nLinesRead * (aproxLines - nLinesRead)) ? 0 : (double)timer.ElapsedMilliseconds / nLinesRead * (aproxLines - nLinesRead);
                    return TimeSpan.FromMilliseconds(time);
                }
                else return TimeSpan.Zero;
            }
        }

        public BackgroundFileReader(bool ReadBinary) {
            readBinary = ReadBinary;
            backgroundReader.WorkerReportsProgress = true;
            backgroundReader.WorkerSupportsCancellation = true;
            InitializeBackgroundWorker();
        }

        public BackgroundFileReader() : this(false) {
        }

        /// <summary>
        /// Sets up the background worker. This adds the stuff to do, compleation and progress events to the event handlers
        /// </summary>
        private void InitializeBackgroundWorker() {
            backgroundReader.DoWork += new DoWorkEventHandler(BackgroundReader_DoWork);
            backgroundReader.RunWorkerCompleted += new RunWorkerCompletedEventHandler(AsyncRead_RunWorkerCompleted);
            backgroundReader.ProgressChanged += new ProgressChangedEventHandler(AsyncRead_ProgressChanged);

        }

        /// <summary>
        /// This will read a file given a path directory. Expected to be a full path from C:\
        /// </summary>
        /// <param name="path"></param> The path to read the file.
        public void readFile(string path) {
            if (backgroundReader.IsBusy != true) {
               // this.outputPath = outputPath;
             //   this.folderName = folderName;
                // reset the results....
                // set any bools and settings to keep track... this might be flag files.
                highestPercentageReached = 0;
                nLinesRead = 1;

                lastReadPath = path;
                // Start the asynchronous operation.
                backgroundReader.RunWorkerAsync(path);
            }
        }

        public void cancel() {
            if (!backgroundReader.CancellationPending) {
                backgroundReader.CancelAsync();
            }
        }


        /// <summary>
        /// Start reading a file. This will check if it is busy, as in it has started reading already.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void startAsyncRead(object sender, EventArgs e) {

            if (backgroundReader.IsBusy != true) {
                // Start the asynchronous operation.
                backgroundReader.RunWorkerAsync();
            }
        }

        /// <summary>
        /// This will cancel the reading of a file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cancelAsyncRead(object sender, EventArgs e) {
            if (backgroundReader.WorkerSupportsCancellation == true) {
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
        private void BackgroundReader_DoWork(object sender, DoWorkEventArgs e) {
            BackgroundWorker worker = sender as BackgroundWorker;

            if (worker.CancellationPending == true) {
                e.Cancel = true;
            }
            else {
                ReadDataFile((string)e.Argument, worker, e);
            }
        }

        private void AsyncRead_ProgressChanged(object sender, ProgressChangedEventArgs e) {
            progress = (e.ProgressPercentage.ToString() + "%");
            //messageOut = string.Format("{0} Lines read.\n", nLinesRead) + ErrorMessages;
        }

        private void AsyncRead_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            if (e.Cancelled == true) {
                ErrorMessages += progress = "Job was canceled!\n";
                messageOut = "Job was canceled!\n" + ErrorMessages;
               // ErrorMessages = "";
                isFinished = true;
                finishedWithOutError = false;
            }
            else if (e.Error != null) {
                ErrorMessages += progress = "Error: \n" + e.Error.Message + "\n";
                ErrorMessages += "Stack: \n" + e.Error.StackTrace+"\n";
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




        private void ReadDataFile(string path, BackgroundWorker worker, DoWorkEventArgs e) {
            
            isFinished = false;
            ErrorMessages += "Error Log:\nAttempting to read\n";

            if (worker.CancellationPending) {
                e.Cancel = true;
            }
            else {
                // var data = new DataHeader();
                try {
                    // begin
                    timer.Reset();
                    timer.Start();
                    if (readBinary) {
                        using (FileStream fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                        using (BufferedStream bs = new BufferedStream(fs, 4 * 1024 * 1024))
                        using (BinaryReader reader = new BinaryReader(bs)) {

                            ErrorMessages += string.Format("No header lines.\n");
                            // Wrote header lines, now to write body.
                            int percentage = 0;
                            ProcessData(reader, worker, e);

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

                        using (FileStream fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        //using (BufferedStream bs = new BufferedStream(fs, 4 * 1024))
                        using (StreamReader reader = new StreamReader(fs)) {


                            string line;
                            int nHeaderRows = -1;
                            if (int.TryParse(headerFinish, out nHeaderRows)) {
                                for (int i = 0; i < nHeaderRows; i++) {
                                    ProcessHeaderLine(reader.ReadLine(), i, worker, e);
                                }
                                ErrorMessages += string.Format("Have read {0} header lines.\n", nHeaderRows);
                            }
                            else {
                                line = reader.ReadLine();
                                nHeaderRows = 0;
                                while (line != headerFinish) {
                                    ProcessHeaderLine(line, nHeaderRows, worker, e);
                                    line = reader.ReadLine();
                                    nHeaderRows++;
                                }
                                ErrorMessages += string.Format("Read {0} header lines with header finish string: \"{1}\".\n", nHeaderRows, headerFinish);
                            }

                            // Get aprox lines if the header didn't contain info.
                            if (aproxLines == 0) {
                                long nLine = reader.BaseStream.CountLines();
                                aproxLines = nLine > int.MaxValue ? int.MaxValue : (int)nLine;
                                ErrorMessages += string.Format("From base stream, calculated lines. Reading {0} lines.\n", aproxLines);
                            }

                            hasReadHeader = true;
                            // read header lines, now to read body.
                            line = reader.ReadLine();
                            int nRows = 0;
                            int percentage = 0;
                            while (!reader.EndOfStream) {
                                ProcessLine(line, nRows, worker, e);
                                line = reader.ReadLine();

                                if (nRows % (aproxLines / 100) == 0) {
                                    percentage = (int)((float)nRows / (float)aproxLines * 100);

                                    if (percentage > highestPercentageReached) {
                                        highestPercentageReached = percentage;
                                        messageOut = string.Format("\nReading {1} body lines, {0} lines read.\n{2}% finished.\n", nRows, aproxLines, percentage) + ErrorMessages;
                                        worker.ReportProgress(highestPercentageReached);
                                    }
                                }
                                nRows++;
                                nLinesRead = nRows;

                                if (worker.CancellationPending) {
                                    worker.CancelAsync();
                                    e.Cancel = true;
                                    break;
                                }
                            }
                            ErrorMessages += messageOut = string.Format("\nRead {0} body lines within {1} seconds\nJob Finished.\n", nRows, (float)timer.ElapsedMilliseconds / 1000f);
                        }
                        if (nLinesRead + aproxLines * .1 < aproxLines) {

                            ErrorMessages += messageOut = string.Format("Did not read enough lines!\nRead {0} lines out of {1}\nWill dump and retry.\n", nLinesRead, aproxLines);

                            using (FileStream fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) 
                            using (StreamReader reader = new StreamReader(path)) 
                            {
                                reader.ReadLine();
                                string fileDump = reader.ReadToEnd();
                                string[] rows = fileDump.Split('\r');
                                ErrorMessages += messageOut = string.Format("Reading {0} rows from dumped file\n", rows.Length);
                                for (int i = 0; i < rows.Length; i++) {
                                    ProcessLineOverride(rows[i], i, worker, e);
                                }
                            }

                            ErrorMessages += messageOut = string.Format("Dumped File within {0} seconds\n", (float)timer.ElapsedMilliseconds / 1000f);
                        }
                    }
                    timer.Stop();
                }
                catch (Exception er) {
                    ErrorMessages += er.Message;
                    //throw;
                }
                timer.Stop();
                timer.Reset();
            }
           // isFinished = true;
        }

        protected virtual void ProcessHeaderLine(string line,int lineNumber, BackgroundWorker worker, DoWorkEventArgs e) {

        }

        protected virtual void ProcessLine(string line, int lineNumber, BackgroundWorker worker, DoWorkEventArgs e) {

        }
        

        protected virtual void ProcessLineOverride(string line, int lineNumber, BackgroundWorker worker, DoWorkEventArgs e) {

        }

        protected virtual void ProcessData(BinaryReader reader, BackgroundWorker worker, DoWorkEventArgs e) {

        }
    }
    

}
 

