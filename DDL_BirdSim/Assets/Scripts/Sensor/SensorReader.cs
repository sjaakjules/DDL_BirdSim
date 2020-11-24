using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DeepDesignLab.Base;
using DeepDesignLab.Debug;
using Unity.Collections;
using System;
using System.Linq;

namespace DeepDesignLab.Sensors {
    public enum ReadingType { Temp, Humid, Pressure,Lux, IR, Visible, UV, Total_Light, eCO2, VOCs, Min, Sec};

    public class SensorReader : MonoBehaviour {

        Dictionary<ReadingType, string> HeaderName = new Dictionary<ReadingType, string>();

        CSVreader FileReader = new CSVreader(1);
        public string filePath = null;

        public bool readFile = false;

        public string[] HeaderValues;
        public int nDataPoins;

        public int entry = 9;
       // [ReadOnly] public double[] values;

        [ReadOnly] public bool hasLoadedValues = false;
        bool startedJob = false;
        
        string[] rows;
        List<double[]> NumberData = new List<double[]>();
        public int nDatapointErrors;

        Dictionary<ReadingType, List<double>> Data = new Dictionary<ReadingType, List<double>>();
        List<DateTime> Dates = new List<DateTime>();

        Dictionary<ReadingType, List<double>> AverageValues = new Dictionary<ReadingType, List<double>>();
        List<DateTime> AverageDates = new List<DateTime>();

        int Counter = 0;

        // Use this for initialization
        void Start() {
            FileReader.readFile(filePath);
            readFile = false;
            startedJob = true;
            hasLoadedValues = false;

            HeaderName.Add(ReadingType.Lux, "TSL_Lux");             // 2147483.75 is false recording
            HeaderName.Add(ReadingType.IR, "TSL_IR");               // 1 can be false or true recording
            HeaderName.Add(ReadingType.Visible, "TSL_Vis");         // -1 is false recording <------------------------this to validate TSL sensor
            HeaderName.Add(ReadingType.Total_Light, "TSL_Full");    // 0 can be false or true recording
            HeaderName.Add(ReadingType.Temp, "BME_Temp");
            HeaderName.Add(ReadingType.Humid, "BME_Hum");
            HeaderName.Add(ReadingType.Pressure, "BME_Pres");
            HeaderName.Add(ReadingType.UV, "VEML_UV");              // 65535 is NaN, false recording<-----------------this to validate VEML sensor
            HeaderName.Add(ReadingType.eCO2, "SGP_CO2");            // 400 is uncalibrated<-----------/---------------Both to validate SGP sensor
            HeaderName.Add(ReadingType.VOCs, "SGP_VOCs");           // 0 is uncalibrated<------------/
            HeaderName.Add(ReadingType.Min, "Minute");
            HeaderName.Add(ReadingType.Sec, "Second");

            for (int i = 0; i < Enum.GetNames(typeof(ReadingType)).Length; i++) {
                AverageValues.Add((ReadingType)i, new List<double>());
                Data.Add((ReadingType)i, new List<double>());
            }


            // Set up graph properties using our graph keys
            DebugGUI.SetGraphProperties("Lux", "Lux", 0, 2700, -1, new Color(0, 1, 1), false);
            
        }

        // Update is called once per frame
        void Update() {
            if (!hasLoadedValues && FileReader.hasFinished) {
                HeaderValues = FileReader.getHeader;
                NumberData = FileReader.CopyData;
                nDataPoins = FileReader.getnDataPoints;

                double lastMin = -1;

                int TempSecond = -1;
                DateTime timestamp;

                // Instantiate lists within dictionaries
                Dictionary<ReadingType, List<double>> tempValues = new Dictionary<ReadingType, List<double>>();
                for (int i = 0; i < Enum.GetNames(typeof(ReadingType)).Length; i++) {
                    tempValues.Add((ReadingType)i,new List<double>());
                }
                double tempAverageValue = -1;


                lastMin = NumberData[0][Array.IndexOf(HeaderValues, "Minute")];
                TempSecond = 0;
                for (int i = 0; i < nDataPoins; i++) {
                    if (NumberData[i].Length != HeaderValues.Length) {
                        nDatapointErrors++;
                        UnityEngine.Debug.Log(string.Format("Error at row {0}", i));
                    }
                    else {
                        /////////////////////////////////////
                        // 1) Check if the new value is within the averaging minute. If not find the average and reset temp variables.

                        // This section averages the values per minute.
                        if (NumberData[i][Array.IndexOf(HeaderValues, "Minute")] != lastMin) {
                            lastMin = NumberData[i][Array.IndexOf(HeaderValues, "Minute")];
                            TempSecond = 0;
                            // Use the last values [i-1] for the average date.
                            timestamp = new DateTime((int)NumberData[i - 1][Array.IndexOf(HeaderValues, "Year")],
                                                    (int)NumberData[i - 1][Array.IndexOf(HeaderValues, "Month")],
                                                    (int)NumberData[i - 1][Array.IndexOf(HeaderValues, "Day")],
                                                    (int)NumberData[i - 1][Array.IndexOf(HeaderValues, "Hour")],
                                                    (int)NumberData[i - 1][Array.IndexOf(HeaderValues, "Minute")],
                                                    TempSecond);
                            AverageDates.Add(timestamp);
                            // Average the temp list, add to the global average list.
                            // Reset temp enums
                            for (int j = 0; j < Enum.GetNames(typeof(ReadingType)).Length; j++) {
                                tempAverageValue = tempValues[(ReadingType)j].ToArray().Average();
                                AverageValues[(ReadingType)j].Add(tempAverageValue);
                                tempValues[(ReadingType)j].Clear();
                            }
                        }
                        /////////////////////////////////////
                        // 2) Calculate the time where the seconds incriment by 1 for each burst value.
                        TempSecond++;
                        timestamp = new DateTime((int)NumberData[i][Array.IndexOf(HeaderValues, "Year")],
                                                    (int)NumberData[i][Array.IndexOf(HeaderValues, "Month")],
                                                    (int)NumberData[i][Array.IndexOf(HeaderValues, "Day")],
                                                    (int)NumberData[i][Array.IndexOf(HeaderValues, "Hour")],
                                                    (int)NumberData[i][Array.IndexOf(HeaderValues, "Minute")],
                                                    TempSecond);
                        Dates.Add(timestamp);

                        /////////////////////////////////////
                        // 3) Add the current value to the average list, global list and date to the global date list.
                        for (int j = 0; j < Enum.GetNames(typeof(ReadingType)).Length; j++) {
                            // Check if the Enum is within the data.
                            if (Array.IndexOf(HeaderValues, HeaderName[(ReadingType)j]) >= 0) {
                                // It is in data, now add to Total list and temp list for 

                                tempValues[(ReadingType)j].Add(NumberData[i][Array.IndexOf(HeaderValues, HeaderName[(ReadingType)j])]);
                                Data[(ReadingType)j].Add(NumberData[i][Array.IndexOf(HeaderValues, HeaderName[(ReadingType)j])]);
                            }
                        }
                    }
                    
                }
                /*
                // finished for loop for each data entry
                string debuggerString = "";
                for (int i = 0; i < 100; i++) {
                    // 1) print the date and values for each data point. make note of the index
                    debuggerString += string.Format("\n {0}:\tDate: {1}\t Lux: {2}", i, Dates[i], Data[ReadingType.Lux][i]);
                    // 2) For every 10th print the average value. Make note of the index
                    if (i>=9 && i%10 == 9) {
                        debuggerString += string.Format("\nAve{0}:\tDate: {1}\t Lux: {2}\n", (i+1)/10-1, AverageDates[(i + 1) / 10 - 1], AverageValues[ReadingType.Lux][(i + 1) / 10 - 1]);
                    }
                }
                Debug.Log(debuggerString);
                */
                hasLoadedValues = true;
            }
            if (hasLoadedValues) {
                // values = NumberData[entry];
                if (Counter < AverageValues[ReadingType.Lux].Count) {
                    DebugGUI.Graph("Lux", (float)AverageValues[ReadingType.Lux][Counter]);
                }
                Counter++;
            }
        }
    }
}