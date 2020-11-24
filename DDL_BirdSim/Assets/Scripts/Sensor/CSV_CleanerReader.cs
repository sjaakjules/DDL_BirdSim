using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System;
using System.Text.RegularExpressions;

namespace DeepDesignLab.Base {
    public class CSV_CleanerReader : BackgroundFileReader {


        // Object files, once read this will store the information.
        private List<List<string>> TableText = new List<List<string>>();
        private List<List<string>> Headers = new List<List<string>>();
        // private List<double[]> NumberData = new List<double[]>(57967);
        //private List<string> rows = new List<string>();

        public List<List<string>> GetDataTable { get { if (base.hasFinished) return TableText; else return null; } }
        public List<List<string>> GetHeaderTable { get { if (base.hasFinished) return Headers; else return null; } }


        public string[] getHeader {
            get {
                if (base.hasFinished && TableText.Count >= 1) {
                    return TableText[0].ToArray();
                }
                return null;
            }
        }

        public int getNumberRows { get { if (base.hasFinished) return TableText.Count; else return -1; } }

        // The characters for CSV file
        private string ret = "\r";
        private char columnChar = ',';

        // ==> how to join to CSV,      var Row = string.Join(",", words);
        //                              var table = string.Join(";\r", Row);

        // Temp variables for each line read.
        private double ReadValue;
        private int nColumns;

        public CSV_CleanerReader() : this(1) {
        }

        public CSV_CleanerReader(int nHeaderLines) {
            base.headerFinish = nHeaderLines.ToString();
        }


        protected override void ProcessHeaderLine(string line, int lineNumber, BackgroundWorker worker, DoWorkEventArgs e) {
            string[] headerNames = line.Split(columnChar);
            for (int i = 0; i < headerNames.Length; i++) {
                headerNames[i] = headerNames[i].Replace(ret, "");
            }
            Headers.Add(new List<string>(headerNames));
            nColumns = headerNames.Length;
            worker.ReportProgress(1);

            //TableText = new List<List<string>>(nColumns);
            base.ProcessHeaderLine(line, lineNumber, worker, e); // Call the base function. This is a blank function.
        }


        protected override void ProcessLine(string line, int lineNumber, BackgroundWorker worker, DoWorkEventArgs e) {

            string[] rowTexts = line.Split(columnChar);
            for (int i = 0; i < rowTexts.Length; i++) {
                rowTexts[i] = Regex.Replace(rowTexts[i], @"\t|\n|\r", "");
            }
            if (rowTexts.Length == nColumns) {

                double[] rowValues = new double[rowTexts.Length];

                for (int k = 0; k < rowTexts.Length; k++) {
                    if (double.TryParse(rowTexts[k], out ReadValue)) {
                        rowValues[k] = ReadValue;
                    }
                    else {
                        rowValues[k] = double.NaN;
                    }
                }
                //NumberData.Add(rowValues);
                if (rowValues[3] == -1) {
                    rowValues[1] = 0;
                    rowValues[2] = 0;
                    rowValues[3] = 0;
                    for (int i = 0; i < rowTexts.Length; i++) {
                        rowTexts[i] = double.IsNaN(rowValues[i]) ? rowTexts[i] : rowValues[i].ToString();
                    }
                }


                if (rowTexts[16].Contains("_")) {
                    // save as PIR movements
                }
                else if (rowValues[10] == 400) {
                    // save row
                    TableText.Add(new List<string>(rowTexts));
                }else {
                    base.ErrorMessages += string.Format("Line removed: {0}\n", line);
                }

                ProcessLineValues(rowValues, line, lineNumber, worker, e);
                ProcessLineTexts(rowTexts, line, lineNumber, worker, e);
            }
            else {
                base.ErrorMessages += string.Format("\nValues in row {0} is not the same as header row.\n\tRow: {1}\n\tHead: {2}", lineNumber, rowTexts.Length, nColumns);
                base.ErrorMessages += "\nLine: " + line;
            }
            base.ProcessLine(line, lineNumber, worker, e); // This is a blank function.
        }

        protected override void ProcessLineOverride(string line, int lineNumber, BackgroundWorker worker, DoWorkEventArgs e) {

            if (lineNumber == 0) {
                base.ErrorMessages += string.Format("Running override, expected number of lines not read.");
                TableText.Clear();
                ProcessLine(line, lineNumber, worker, e);
            }
            else {
                ProcessLine(line, lineNumber, worker, e);
            }

            base.ProcessLineOverride(line, lineNumber, worker, e);
        }

        protected virtual void ProcessLineValues(double[] rowValues, string line, int lineNumber, BackgroundWorker worker, DoWorkEventArgs e) {

        }

        protected virtual void ProcessLineTexts(string[] rowTexts, string line, int lineNumber, BackgroundWorker worker, DoWorkEventArgs e) {

        }

    }
}