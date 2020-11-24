using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System;
using DeepDesignLab.Base;

namespace DeepDesignLab.PointCloud {
    public class CCReader : BackgroundFileReader {


        // Object files, once read this will store the information.
        private List<string> Columns = new List<string>();

        public string[] headerNames { get { if (hasReadHeader) return Columns.ToArray() ; return null; } }

        // The characters for CSV file
        private string ret = "\r";
        private char columnChar = ',';

        // Temp variables for each line read.
        private double ReadValue;
        private int nColumns;        
        private string[] rowTexts;

        public CCReader() : this(2) {
        }

        public CCReader(int nHeaderLines) : base() {
            base.headerFinish = nHeaderLines.ToString();
            Columns = new List<string>();
        }


        protected override void ProcessHeaderLine(string line, int lineNumber, BackgroundWorker worker, DoWorkEventArgs e) {

            switch (lineNumber) {
                case 0:
                    string[] headerNames = line.Split(columnChar);
                    for (int i = 0; i < headerNames.Length; i++) {
                        headerNames[i] = headerNames[i].Replace(ret, "");
                    }
                    Columns = new List<string>(headerNames);
                    nColumns = Columns.Count;
                    break;
                case 1:
                    if (int.TryParse(line, out base.aproxLines)) {
                        worker.ReportProgress(1);
                    }
                    break;
                default:
                    break;
            }

            base.ProcessHeaderLine(line, lineNumber, worker, e); // This is a blank function.
        }


        protected override void ProcessLine(string line, int lineNumber, BackgroundWorker worker, DoWorkEventArgs e) {

            rowTexts = line.Split(columnChar);
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
                ProcessLineValues(rowValues, line, lineNumber, worker, e);
                ProcessLineTexts(rowTexts, line, lineNumber, worker, e);
            }
            base.ProcessLine(line, lineNumber, worker, e); // This is a blank function.
        }

        protected virtual void ProcessLineValues(double[] rowValues, string line, int lineNumber, BackgroundWorker worker, DoWorkEventArgs e) {

        }

        protected virtual void ProcessLineTexts(string[] rowTexts, string line, int lineNumber, BackgroundWorker worker, DoWorkEventArgs e) {

        }
    }
}