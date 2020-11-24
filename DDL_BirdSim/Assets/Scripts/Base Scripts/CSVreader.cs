using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System;

namespace DeepDesignLab.Base {
    public class CSVreader : BackgroundFileReader {


        // Object files, once read this will store the information.
        private List<List<string>> HeaderRows = new List<List<string>>();
        private List<double[]> NumberDataRows = new List<double[]>(57967);
        private List<string[]> TextDataRows = new List<string[]>();


        public List<double[]> CopyData {
            get {
                if (base.hasFinished) {
                    List<double[]> DataOut = new List<double[]>();
                    for (int i = 0; i < NumberDataRows.Count; i++) {
                        DataOut.Add((double[])NumberDataRows[i].Clone());
                    }
                    return DataOut;
                }
                return null;
            }
        }
        
        public string[] getHeader {
            get {
                if (base.hasFinished && HeaderRows.Count >= 1) {
                    return HeaderRows[0].ToArray();
                }
                return null;
            }
        }

        public int getnDataPoints { get { if (base.hasFinished) return NumberDataRows.Count; else return -1; } }

        public List<string[]> GetTable { get { if (base.hasFinished) return TextDataRows; else return null; } }


        // The characters for CSV file
        private string ret = "\r";
        private char columnChar = ',';

        // Temp variables for each line read.
        private double ReadValue;
        private int nColumns;

        public CSVreader() : this(1) {
        }

        public CSVreader(int nHeaderLines) {
            base.headerFinish = nHeaderLines.ToString();
            HeaderRows = new List<List<string>>();
        }


        protected override void ProcessHeaderLine(string line, int lineNumber, BackgroundWorker worker, DoWorkEventArgs e) {
            string[] headerNames = line.Split(columnChar);
            for (int i = 0; i < headerNames.Length; i++) {
                headerNames[i] = headerNames[i].Replace(ret, "");
            }
            HeaderRows.Add(new List<string>(headerNames));
            nColumns = headerNames.Length;
            worker.ReportProgress(1);
            
            base.ProcessHeaderLine(line, lineNumber, worker, e); // Call the base function. This is a blank function.
        }


        protected override void ProcessLine(string line, int lineNumber, BackgroundWorker worker, DoWorkEventArgs e) {

            string[] rowTexts = line.Split(columnChar);

            for (int i = 0; i < rowTexts.Length; i++) {
                rowTexts[i] = rowTexts[i].Replace(ret, "");
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
                NumberDataRows.Add(rowValues);
                //TextDataRows.Add(rowTexts.Clone() as string[]);


                ProcessLineValues(rowValues, line, lineNumber, worker, e);
                ProcessLineTexts(rowTexts, line, lineNumber, worker, e);
            }
            else {
                base.ErrorMessages += string.Format("\nValues in row {0} is not the same as header row.\n\tRow: {1}\n\tHead: {2}", lineNumber, rowTexts.Length, nColumns);
                base.ErrorMessages += "\nLine: " + line;
            }
            TextDataRows.Add(rowTexts.Clone() as string[]);
            base.ProcessLine(line, lineNumber, worker, e); // This is a blank function.
        }

        protected override void ProcessLineOverride(string line, int lineNumber, BackgroundWorker worker, DoWorkEventArgs e) {
            if (lineNumber == 0) {
                NumberDataRows.Clear();
                //TextDataRows.Clear();
                ProcessLine(line, lineNumber, worker, e);
            }
            else{
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