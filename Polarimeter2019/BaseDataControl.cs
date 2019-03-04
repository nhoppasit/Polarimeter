﻿using System;
using System.Data;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace Polarimeter2019
{
    public class BaseDataControl
    {
        #region Members

        public struct strucCurveData
        {
            public double X;
            public double Y;
            public double Xm;
            public double Ym;
            public double AngleOfRotation;
        }

        private string mSampleName;
        private double mSpecificRotation;
        public strucCurveData Reference;
        public strucCurveData Data;

        #endregion

        #region Properties

        public string SampleName
        {
            get
            {
                return SampleName;
            }
            set
            {
                SampleName = value;
            }
        }

        #endregion

        #region Functions/Method

        public void PatchReference(int PointID, double X, double Y)
        {
            // !!!!!
            double[] a;
            a = new double[2345];
        }

        public void PatchData(int RepeatID, int PointID, double X, double Y)
        {
            //    if (Data == null)
            //    {
            //        System.Data(RepeatID).Ym = 999999;
            //    }
            //    else if (System.Data.Length - 1 < RepeatID)
            //    {
            //    Input: Data(0 To RepeatID);

            //        System.Data(RepeatID).Ym = 999999;
            //    }
            //Input: Data(RepeatID).X(0 To PointID);
            //Input: Data(RepeatID).Y(0 To PointID);

            Data.X = X;
            Data.Y = Y;
            if (Y < Data.Ym)
            {
                Data.Ym = Y;
                Data.Xm = X;
                AnalyzeData(RepeatID);
            }
        }
        
        public void SaveFile()
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "Text File (*.txt)|*.txt|CSV File (*.csv)|*.csv|Polarimeter File (*.pom)|*.pom|All File (*.*)|*.*";
            DialogResult redlg = dlg.ShowDialog();
            if (redlg != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }


            // ----------Save---------------------

            string path = dlg.FileName;

            // Delete the file if it exists.
            if (File.Exists(path))
                File.Delete(path);

            // Create the file.
            FileStream fs = File.Create(path);

            // Start header
            AddText(fs, "Polarimeter Data File"); // Intro
            AddText(fs, "(Copy Right)2011, CIID, KMITL" + Environment.NewLine);
            AddText(fs, DateTime.Now.ToString() + Environment.NewLine); // Date-time

            // Start body
            AddText(fs, "[Sample Name]" + Environment.NewLine); // Sample name
            AddText(fs, mSampleName);
            AddText(fs, "[Specific Rotation]" + Environment.NewLine); // Specific rotation
            AddText(fs, "[Samples]" + Environment.NewLine); // Number of samples
            //AddText(fs, Data.Length.ToString);
            AddText(fs, Data.ToString());

            // Reference
            AddText(fs, "[Reference]");
            //for (int i = 0; i <= Reference.X.Length - 1; i++)
            for (int i = 0; i <= Reference.X - 1; i++)
                //AddText(fs, Reference.X(i).ToString + "," + Reference.Y(i).ToString + Environment.NewLine);
                AddText(fs, Reference.X.ToString() + "," + Reference.Y.ToString() + Environment.NewLine);
            // Data
            for (int k = 0; k <= Data.Length - 1; k++)
            {
                AddText(fs, "[Sample " + (k + 1).ToString() + "]");
                for (int i = 0; i <= Data(k).X.Length - 1; i++)
                    AddText(fs, Data(k).X(i).ToString + "," + Data(k).Y(i).ToString + Environment.NewLine);
            }

            // Ending
            fs.Close();
        }

        private void AddText(FileStream fs, string value)
        {
            byte[] info = new UTF8Encoding(true).GetBytes(value);
            fs.Write(info, 0, info.Length);
        }
        
        public void OpenFile()
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Text File (*.txt)|*.txt";
            DialogResult redlg = dlg.ShowDialog();
            if (redlg != System.Windows.Forms.DialogResult.OK)
                return;
        }

        private void AnalyzeData(int RepeatID)
        {
            try
            {
                if (Reference.X == null)
                    return;
                if (Data.X == null)
                    return;
                Data.AngleOfRotation = Math.Abs(Data.Xm - Reference.Xm);
            }
            catch (Exception ex)
            {
            }
        }
        
        private void AnalyzeAllData()
        {
            int i = 0;
            foreach (strucCurveData d in Data)
            {
                if (d.X != null)
                    AnalyzeData(i);
                i += 1;
            }
        }

        #endregion

        #region New

        public void New()
        {
            //Reference.Ym = 9999999;
        }

        #endregion
    }
}