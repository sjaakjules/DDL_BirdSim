using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using UnityEngine;

namespace DeepDesignLab.Base {
    public class CSVWriter : BackgroundFileWriter {

        public List<List<string>> TableToWrite;
        public List<List<string>> HeaderToWrite;
        int nHeaderLines = -1;

        private int currentRow = -1;
        private int rowsPerLoop = -1;

        public CSVWriter(List<List<string>> Table) : this(Table,0) {

        }
        public CSVWriter(List<List<string>> Table, int headerLines) : base() {
            TableToWrite = Table;
            nHeaderLines = headerLines;
        }

        public CSVWriter(List<List<string>> Header, List<List<string>> data) {
            TableToWrite = data;
            HeaderToWrite = Header;
        }

        protected override void ProcessHeaderLine(StreamWriter writer, ref int nLines, BackgroundWorker worker, DoWorkEventArgs e) {
            if (HeaderToWrite == null) {
                base.ErrorMessages += "no header table loaded\n";
                for (int i = 0; i < nHeaderLines; i++) {
                    currentRow++;
                    base.ErrorMessages += string.Format("Header: {0}: {1}\nTable rows: {2}\n",i, currentRow, TableToWrite.Count);
                    writer.WriteLine(string.Join(",", TableToWrite[currentRow]));
                }
            }
            else {

                base.ErrorMessages += "writing from header table\n";
                for (int i = 0; i < HeaderToWrite.Count; i++) {
                    base.ErrorMessages += string.Format("Header: {0}: {1}\n", i, currentRow);
                    writer.WriteLine(string.Join(",", HeaderToWrite[i]));
                }
            }
            rowsPerLoop = (int)(TableToWrite.Count / 99);
            nLines = TableToWrite.Count;
            base.ProcessHeaderLine(writer,ref nLines, worker, e);
        }

        protected override void ProcessLine(StreamWriter writer, int loopIndex, BackgroundWorker worker, DoWorkEventArgs e) {

            base.ErrorMessages += string.Format("body current row: {0}\n", currentRow);
            for (int i = 0; i < rowsPerLoop; i++) {
                currentRow++;
                if (TableToWrite.Count > currentRow) {
                    writer.WriteLine(string.Join(",", TableToWrite[currentRow]));
                }
            }

            base.ProcessLine(writer, loopIndex, worker, e);
        }

    }
}