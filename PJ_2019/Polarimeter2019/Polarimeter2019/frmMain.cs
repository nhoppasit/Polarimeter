﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Polarimeter2019
{
    public partial class frmMain : Form
    {
        #region DECRALATION
            //Constants
            const var StepFactor = 0.013325; //Deg /Step 
            ////var คือ value??

            //Scaning & Data
            BaseDataControl TheData;
            bool IsScanning = false;
            bool IsContinuing = false;
            int CurrentPointIndex = 0;
            double SpecificRotation;
            int NumberOfRepeatation;
            int SelectedIndex;

            //ColorTable
            public Color ReferenceColor = Color.Red;
            public Color[] ColorTable;
        #endregion
        
        #region Devices

            private Ivi.Visa.Interop.IFormattedIO488 DMM;
            private Ivi.Visa.Interop.IFormattedIO488 MMC;

            void DisconnectDevices()
            {
                try
                {
                    DMM.IO.Close();
                    DMM.IO = null;
                    MMC.IO.Close();
                    MMC.IO = null;
                }
                catch (Exception ex)
                {
                    string message = "IO Error: ";
                    string caption = "Error";
                    //Interaction.MsgBox("IO Error: " + ex.Message, MsgBoxStyle.Critical); //// กล่องข้อความ??
                    MessageBox.Show(message + ex.Message);
                    lblDMM.Text = "Disconnected";
                    lblDMM.BackColor = Color.Red;
                    lblMMC.Text = "Disconncected";
                    lblMMC.BackColor = Color.Red;

                }
            }
            void ConnectedDevices()
            {
                try
                {
                    //CONNECT DMM
                    Ivi.Visa.Interop.ResourceManager mgr1;
                    string DMMAddress;
                    DMMAddress = txtDMMAddress.Text;
                    mgr1 = new Ivi.Visa.Interop.ResourceManager();
                    DMM = new Ivi.Visa.Interop.FormattedIO488();
                    DMM.IO() = mgr1.Open(DMMAddress);
                    DMM.IO.Timeout = 7000;
                    DMM.WriteString("CONF:VOLT:DC " + txtVoltageRange.Text + ", " + txtVoltageResolution.Text);
                    DMM.WriteString("TRIG:SOUR TMM");
                    DMM.WriteString("TRIG:DEL 1.5E-3");
                    DMM.WriteString("TRIG:COUNT 1");
                    DMM.WriteString("SAMP:COUNT 1");

                    //CONNECT MMC
                    Ivi.Visa.Interop.ResourceManager mgr2;
                    string MMCAddress;
                    MMCAddress = txtMMCAddress.Text;
                    mgr2 = new Ivi.Visa.Interop.ResourceManager();
                    MMC = new Ivi.Visa.Interop.FormattedIO488();
                    MMC.IO() = mgr2.Open(MMCAddress);
                    MMC.IO.Timeout = 7000;

                    // MsgBox("Connect devices are successful.")
                    lblDMM.Text = "Connected";
                    lblDMM.BackColor = Color.Lime;
                    lblMMC.Text = "Conncected";
                    lblMMC.BackColor = Color.Lime;
                }
                catch (Exception ex)
                {
                    string message = "InitIO Error";
                    string caption = "Error";
                    //Interaction.MsgBox("InitIO Error:" + Constants.vbCrLf + ex.Message); //// กล่องข้อความ
                    MessageBox.Show(message, caption, MessageBoxButtons.RetryCancel, MessageBoxIcon.Warning); ////ขึ้นข้อความเตือน
                    lblDMM.Text = "Disconnected";
                    lblDMM.BackColor = Color.Red;
                    lblMMC.Text = "Disconncected";
                    lblMMC.BackColor = Color.Red;
                }
            }

        #endregion
                
        #region Control Panel

            private void DoStart()
            {
                // Add curve
                double[] x = new double[1];
                double[] y = new double[1];

                ResetDynaplot();

                PlotReferenceCurve();

                // ----------------------------------------
                // 1. Update buttons
                // ----------------------------------------
                btnStart.Enabled = false;
                btnStop.Enabled = true;
                btnPause.Enabled = true;

                // ----------------------------------------
                // disable box
                // ----------------------------------------
                gbStartMea.Enabled = false;
                gbSample.Enabled = false;
                gbScanCondition.Enabled = false;

                // ----------------------------------------
                // 2. start Test loop of reading light intensity
                // ----------------------------------------
                CurrentPointIndex = 0;
                IsScanning = true;
                lblMainStatus.Text = "Measuring...";

                DoScanLightIntensity();

                lblMainStatus.Text = "Ready";
            }

            private void DoStop()
            {
                //----------------------------------------
                //1. stop Test loop of reading light intensity
                //----------------------------------------
                StopScanning();

                //----------------------------------------
                //2. Update buttons
                //----------------------------------------
                btnStart.Enabled = true;
                btnStop.Enabled = false;
                btnPause.Enabled = false;
                btnPause.Text = "PAUSE";
            }

            private void DoPause()
            {
                // ----------------------------------------
                // 1. pause/continue Test loop of reading light intensity
                // ----------------------------------------
                if (btnPause.Text == "PAUSE")
                {
                    DoPasuseScanning();
                }

                else
                {
                    DoContinueScanning();
                }
                // ----------------------------------------
                // 2. Update buttons
                // ----------------------------------------
                btnStart.Enabled = false;
                btnStop.Enabled = true;
                btnPause.Enabled = true;
                if (btnPause.Text == "PAUSE")
                    btnPause.Text = "CONTINUE";
                else
                {
                    btnPause.Text = "PAUSE";
                    DoScanLightIntensity();
                }
            }

            private void btnStart_Click(System.Object sender, System.EventArgs e)
            {
                DoStart();
            }

            private void btnStop_Click(System.Object sender, System.EventArgs e)
            {
                DoStop();
            }

            private void btnPause_Click(System.Object sender, System.EventArgs e)
            {
                DoPause();
            }

        #endregion

        #region Scanning Procedure

            private void DoScanLightIntensity()
            {
                // --------------------------------------------
                // validate selected index of repeats
                // --------------------------------------------
                if (lsvData.SelectedItems.Count <= 0)
                {
                    Interaction.MsgBox("Please select item in samples list view that you want to measure!", MsgBoxStyle.Exclamation + MsgBoxStyle.OkOnly);
                    btnStart.Enabled = true;
                    btnStop.Enabled = false;
                    btnPause.Enabled = false;
                    btnPause.Text = "PAUSE";
                    btnNew.Enabled = true;
                    gbSample.Enabled = true;
                    gbScanCondition.Enabled = true;
                    return;
                }

                // --------------------------------------------
                // Confirmation
                // --------------------------------------------
                // If Not IsContinuing Then
                // Dim trt As String
                // If SelectedIndex = 0 Then
                // trt = "Reference data"
                // Else
                // trt = "Sample " & SelectedIndex
                // End If
                // If MsgBox("Are you sure to measure " & trt & "?", MsgBoxStyle.YesNo) <> MsgBoxResult.Yes Then
                // btnStart.Enabled = True
                // btnStop.Enabled = False
                // btnPause.Enabled = False
                // btnPause.Text = "PAUSE"
                // btnNewMeas.Enabled = True
                // gbSample.Enabled = True
                // gbScanCondition.Enabled = True
                // Exit Sub
                // End If
                // End If

                if (!mnuOptionsDemomode.Checked)
                    ConnectDevices();

                try
                {
                    // --------------------------------------------
                    // get read conditions
                    // --------------------------------------------
                    double ThetaA = System.Convert.ToDouble(txtStart.Text);
                    double ThetaB = System.Convert.ToDouble(txtStop.Text);
                    double Delta = System.Convert.ToDouble(txtResolution.Text);

                    // --------------------------------------------
                    // initialize minimum finder
                    // --------------------------------------------
                    if (!IsContinuing)
                    {
                        if (SelectedIndex == 0)
                            TheData.Reference.Ym = 99999999;
                        else if (TheData.Data != null)
                        {
                            if (SelectedIndex <= TheData.Data.Length)
                                TheData.Data(SelectedIndex - 1).Ym = 99999999;
                        }
                    }

                    // ----------------------------------------------------------------
                    // REAL INTERFACE YES OR NOT (Theta,I)
                    // ----------------------------------------------------------------
                    double CurrentLightIntensity;
                    int StepNumber;
                    string MSG;
                    double CurrentTheta;
                    if (ThetaA < ThetaB)
                    {
                        CurrentTheta = ThetaA + CurrentPointIndex * Delta;
                    }
                    else if (ThetaA > ThetaB)
                    {
                        CurrentTheta = ThetaA - CurrentPointIndex * Delta;
                    }
                    // check demo mode
                    if (mnuOptionsDemomode.Checked == false)
                    {
                        // 0.4 GOTO Theta A
                        StepNumber = -1 * System.Convert.ToInt32(CurrentTheta / StepFactor); // step
                        MSG = "A:WP" + StepNumber.ToString() + "P" + StepNumber.ToString();
                        MMC.WriteString(MSG);

                        // 0.5 Read first
                        int nAvg = numRepeatNumber.Value;
                        CurrentLightIntensity = 0;
                        for (int tt = 0; tt <= nAvg - 1; tt++)
                        {
                            DMM.WriteString("READ?");
                            CurrentLightIntensity = CurrentLightIntensity + DMM.ReadNumber;
                        }
                        CurrentLightIntensity = CurrentLightIntensity / nAvg;
                    }
                    else
                        CurrentLightIntensity = VBMath.Rnd() * 0.1 + Math.Cos((CurrentTheta - VBMath.Rnd() * 50) * Math.PI / 180) + 2;

                    // ----------------------------------------------------------------
                    // STORE DATA AND PLOT
                    // ----------------------------------------------------------------
                    // Save to memory
                    if (SelectedIndex == 0)
                        TheData.PatchReference(CurrentPointIndex, CurrentTheta, CurrentLightIntensity);
                    else
                        TheData.PatchData(SelectedIndex - 1, CurrentPointIndex, CurrentTheta, CurrentLightIntensity);
                    DefineAngleOfRotation();
                    PlotReferenceCurve();
                    PlotTreatmentsCurve();
                    PlotSelectedTRTMarker();

                    // auto scale
                    // AxDynaPlot1.Axes.Autoscale()

                    // --------------------------------------------
                    // MAIN READING LOOP (^0^)
                    // --------------------------------------------
                    while (IsScanning)
                    {
                        Application.DoEvents();

                        // Update current THETA
                        if (ThetaA < ThetaB)
                            CurrentTheta = ThetaA + CurrentPointIndex * Delta;
                        else if (ThetaA > ThetaB)
                            CurrentTheta = ThetaA - CurrentPointIndex * Delta;

                        // --------------------------------------------
                        // CHECK DEMO MODE
                        // --------------------------------------------
                        if (mnuOptionsDemomode.Checked == false)
                        {

                            // --------------------------------------------
                            // REAL INTERFACING
                            // --------------------------------------------
                            // 1. Move polarizer 
                            StepNumber = -1 * System.Convert.ToInt32(CurrentTheta / StepFactor); // step
                            MSG = "A:WP" + StepNumber.ToString() + "P" + StepNumber.ToString();
                            MMC.WriteString(MSG);

                            // 3. Read light intensity
                            int nAvg = numRepeatNumber.Value;
                            CurrentLightIntensity = 0;
                            for (int tt = 0; tt <= nAvg - 1; tt++)
                            {
                                DMM.WriteString("READ?");
                                CurrentLightIntensity = CurrentLightIntensity + DMM.ReadNumber;
                            }
                            CurrentLightIntensity = CurrentLightIntensity / nAvg;
                        }
                        else

                            // --------------------------------------------
                            // DEMO MODE
                            // --------------------------------------------
                            // Delay.
                            // Dim sw As New Stopwatch
                            // sw.Start()
                            // Do
                            // 'do nothing
                            // Loop Until sw.ElapsedMilliseconds > 50 'ms
                            // 'Simulation
                            CurrentLightIntensity = VBMath.Rnd() * 0.1 + Math.Cos((CurrentTheta - VBMath.Rnd() * 50) * Math.PI / 180) + 2;

                        // Save to memory and update curve
                        if (SelectedIndex == 0)
                            TheData.PatchReference(CurrentPointIndex, CurrentTheta, CurrentLightIntensity);
                        else
                            TheData.PatchData(SelectedIndex - 1, CurrentPointIndex, CurrentTheta, CurrentLightIntensity);
                        DefineAngleOfRotation();
                        PlotReferenceCurve();
                        PlotTreatmentsCurve();
                        PlotSelectedTRTMarker();

                        // auto scale
                        // AxDynaPlot1.Axes.Autoscale()

                        // check stop condition!!!
                        if (ThetaA < ThetaB)
                        {
                            if (ThetaB < CurrentTheta)
                                IsScanning = false;
                        }
                        else if (ThetaA > ThetaB)
                        {
                            if (CurrentTheta < ThetaB)
                                IsScanning = false;
                        }
                        // --------------------------------------------

                        // next point
                        CurrentPointIndex += 1;
                    }
                    // --------------------------------------------(^0^)

                    // if stop update buttons to a new start
                    if (btnPause.Text != "CONTINUE")
                    {
                        if (!mnuOptionsDemomode.Checked)
                        {
                            MSG = "A:WP" + System.Convert.ToInt32(-1 * ThetaA / StepFactor).ToString + "P" + System.Convert.ToInt32(-1 * ThetaA / StepFactor).ToString;
                            MMC.WriteString(MSG);
                            DisconnectDevices();
                        }
                        btnStart.Enabled = true;
                        btnStop.Enabled = false;
                        btnPause.Enabled = false;
                        btnPause.Text = "PAUSE";
                        btnNew.Enabled = true;
                        gbSample.Enabled = true;
                        gbScanCondition.Enabled = true;
                    }
                    else
                    {
                        if (!mnuOptionsDemomode.Checked)
                            DisconnectDevices();
                        btnStart.Enabled = false;
                        btnStop.Enabled = true;
                        btnPause.Enabled = true;
                    }
                }
                catch (Exception ex)
                {
                    Interaction.MsgBox(ex.Message);

                    // ----------------------------------------
                    // 1. stop Test loop of reading light intensity
                    // ----------------------------------------
                    StopScanning();

                    // ----------------------------------------
                    // 2. Update buttons
                    // ----------------------------------------
                    btnStart.Enabled = true;
                    btnStop.Enabled = false;
                    btnPause.Enabled = false;
                    btnPause.Text = "PAUSE";
                    btnNew.Enabled = true;
                    gbSample.Enabled = true;
                    gbScanCondition.Enabled = true;
                }
            }

            private void StopScanning()
            {
                IsScanning = false;
                IsContinuing = false;
            }

            private void DoPasuseScanning()
            {
                IsScanning = false;
                IsContinuing = false;
            }

            private void DoContinueScanning()
            {
                IsScanning = true;
                IsContinuing = true;
            }

        #endregion

        #region Menu

            private void ConnectToolStripMenuItem_Click(System.Object sender, System.EventArgs e)
            {
                ConnectedDevices();
            }

            private void DisconnectToolStripMenuItem_Click(System.Object sender, System.EventArgs e)
            {
                DisconnectDevices();
            }

        #endregion

        #region Form Event

            private void frmMain_Load(object sender, EventArgs e)
            {
                this.WindowState = FormWindowState.Maximized;
                LoadSetting();  //// loadSetting มาจากไหน??
            }

            private void Form1_FormClosing(object sender, System.Windows.Forms.FormClosingEventArgs e)
            {
                //if (Interaction.MsgBox("Do you want to quit program?", MsgBoxStyle.YesNo + MsgBoxStyle.Question, "Quit") == MsgBoxResult.Yes) 
                //{
                //    IsScanning = false;
                //    SaveSetting();  //// SaveSetting มาจากไหน??
                //}
                //else
                //{
                //    e.Cancel = true;
                //}
                string message = "Do you want to quit program?";
                string caption = "Polarimeter2019";
                if (MessageBox.Show(message, caption, MessageBoxButtons.YesNo, MessageBoxIcon.Question))
                {
                    IsScanning = false;
                }
                else
                {
                    e.Cancel = true;
                }
            }

        #endregion

        #region PLOT when select curve

            private void ResetDynaplot()
            {
                try
                {
                    double[] x = new double[1];
                    double[] y = new double[1];

                    // AxDynaPlot1.DataCurves.RemoveAll()
                    // AxDynaPlot1.Markers.RemoveAll()

                    // ReferenceCurve = AxDynaPlot1.DataCurves.Add("REF", x, y, 0, False).Curve
                    // ReferenceCurve.Penstyle.MaxWidth = 2
                    // ReferenceCurve.Penstyle.Color = RGB(255, 0, 0)
                    // ReferenceMinMarker = AxDynaPlot1.Markers.Add(0.0, 0.0, 0, DYNAPLOT3Lib.dpsMARKERTYPE.dpsMARKER_CIRCLE)

                    for (int i = 0; i <= NumberOfRepeatation - 1; i++)
                    {
                    }
                }
                // TreatmentMinMarker = AxDynaPlot1.Markers.Add(0.0, 0.0, 0, DYNAPLOT3Lib.dpsMARKERTYPE.dpsMARKER_SQUARE)
                catch (Exception ex)
                {
                    Information.Err.Clear();  //// กล่องข้อความ Error ?
                }
            }

            private bool PlotReferenceCurve()
            {
                bool e = false;
                try
                {
                    if (lsvData.Items[0].Checked == true)
                    {
                        if (TheData.Reference.X != null)
                        {
                            // ReferenceCurve.UpdateData(TheData.Reference.X, TheData.Reference.Y, TheData.Reference.X.Length)
                            // ReferenceCurve.Penstyle.Color = RGB(ReferenceColor.R, ReferenceColor.G, ReferenceColor.B)
                            // ReferenceMinMarker.PositionX = TheData.Reference.Xm
                            // ReferenceMinMarker.PositionY = TheData.Reference.Ym
                            lblNullPoint.Text = TheData.Reference.Xm.ToString("0.0000") + " deg";
                            e = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                }
                return e;
            }

            private bool PlotTreatmentsCurve()
            {
                if (TheData == null)
                    return false;
                if (TheData.Data == null)
                    return false;
                for (int i = 0; i <= NumberOfRepeatation - 1; i++)
                {
                    try
                    {
                        if (lsvData.Items[i + 1].Checked)
                        {
                        }
                    }
                    catch (Exception ex)
                    {
                        Information.Err.Clear();  ////กล่องข้อความ Error ??
                    }
                }
                return true;
            }

            private void PlotSelectedTRTMarker()
            {
                try
                {
                    if (lsvData.Items[SelectedIndex].Checked)
                        // TreatmentMinMarker.PositionX = TheData.Data(SelectedIndex - 1).Xm
                        // TreatmentMinMarker.PositionY = TheData.Data(SelectedIndex - 1).Ym
                        lblNullPoint.Text = TheData.Data(SelectedIndex - 1).Xm.ToString("0.0000") + " deg";
                }
                catch (Exception ex)
                {
                    Information.Err.Clear(); //// กล่องข้อความ Error ??
                }
            }

        #endregion

        #region Sub-Routine

            private void NewMeasurement()
            {
                // verify user
                if (lsvData.Items.Count > 0)
                {
                    //if (Interaction.MsgBox("Data will be deleted. Do you want to new measurement?", MsgBoxStyle.YesNo) != MsgBoxResult.Yes)
                    if (MessageBox.Show("Data will be deleted. Do you want to new measurement?", MessageBoxButtons.YesNo))
                        return;
                }

                // load dialog
                frmNewMeasurement f = new frmNewMeasurement();
                f.StartPosition = FormStartPosition.CenterScreen;
                f.ShowDialog();

                // do update the job
                if (f.Verify() == true)
                {
                    try
                    {
                        // get information
                        txtSampleName.Text = f.SampleName;
                        NumberOfRepeatation = f.OfRepeatation;
                        numRepeatNumber.Value = f.RepeatNumber;

                        // initialize the data object
                        TheData = new BaseDataControl();    ////สร้าง pubic BaseDataControl แล้ว แต่ใส่ข้อมูลข้างในไม่ได้ ???
                        TheData.SampleName = txtSampleName.Text;

                        // clear
                        lsvData.Items.Clear();
                        ListViewItem lvi;

                        // add ref.
                        lvi = new ListViewItem();
                        lvi.Text = "Reference";
                        lvi.SubItems.Add("-");
                        lvi.SubItems.Add("-");
                        lvi.SubItems.Add("-");
                        lvi.Checked = true;
                        lvi.BackColor = ReferenceColor;
                        lvi.UseItemStyleForSubItems = false;
                        lsvData.Items.Add(lvi);

                        // add repeats
                        for (int i = 1; i <= NumberOfRepeatation; i++)
                        {
                            lvi = new ListViewItem();
                            lvi.Text = "Sample " + i.ToString();
                            lvi.SubItems.Add("-");
                            lvi.SubItems.Add("-");
                            lvi.SubItems.Add("-");
                            lvi.Checked = true;
                            lvi.BackColor = ColorTable((i - 1) % ColorTable.Length);
                            lvi.UseItemStyleForSubItems = false;
                            lsvData.Items.Add(lvi);
                        }

                        // clear treatment curve
                        // ReDim TreatmentCurve(0 To NumberOfRepeatation - 1)

                        gbMeasurement.Enabled = true;
                        btnNew.Enabled = true;
                        gbSample.Enabled = true;
                        gbScanCondition.Enabled = true;

                        lsvData.Items[0].Selected = true;
                        lsvData.Focus();
                    }
                    catch (Exception ex)
                    {
                    }
                }
            }

            private void DefineAngleOfRotation()
            {
                ListViewItem lvi;
                lvi = lsvData.Items[SelectedIndex];
                if (SelectedIndex == 0)
                {
                    lvi.SubItems[1].Text = "(" + TheData.Reference.Xm.ToString("0.00") + ", " + TheData.Reference.Ym.ToString("0.0000") + ")";
                }
                else
                {
                    lvi = lsvData.Items[SelectedIndex];
                    lvi.SubItems[1].Text = "(" + TheData.Data(SelectedIndex - 1).Xm.ToString("0.00") + ", " + TheData.Data(SelectedIndex - 1).Ym.ToString("0.0000") + ")";
                    lvi.SubItems[2].Text = TheData.Data(SelectedIndex - 1).AngleOfRotation.ToString("0.00");
                }
            }

            private void LoadSetting()
            {
                txtVoltageRange.Text = My.Settings.VoltageRange.ToString;
                txtVoltageResolution.Text = My.Settings.VoltageResolution.ToString;
                mnuOptionsDemomode.Checked = My.Settings.IsDemo;
                ReferenceColor = My.Settings.ReferenceColor;
                ColorTable(0) = My.Settings.Color1;
                ColorTable(1) = My.Settings.Color2;
                ColorTable(2) = My.Settings.Color3;
                ColorTable(3) = My.Settings.Color4;
                ColorTable(4) = My.Settings.Color5;
                ColorTable(5) = My.Settings.Color6;
                ColorTable(6) = My.Settings.Color7;
                ColorTable(7) = My.Settings.Color8;
                ColorTable(8) = My.Settings.Color9;
                ColorTable(9) = My.Settings.Color10;
                ColorTable(10) = My.Settings.Color11;
                ColorTable(11) = My.Settings.Color12;
                ColorTable(12) = My.Settings.Color13;
                ColorTable(13) = My.Settings.Color14;
                ColorTable(14) = My.Settings.Color15;
                ColorTable(15) = My.Settings.Color16;
                ColorTable(16) = My.Settings.Color17;
                ColorTable(17) = My.Settings.Color18;
                ColorTable(18) = My.Settings.Color19;
                ColorTable(19) = My.Settings.Color20;
            }   //// My.Setting คือ ???

            private void SaveSetting()
            {
                My.Settings.IsDemo = mnuOptionsDemomode.Checked;
                My.Settings.Save();
            }

            public void ApplyColorTableToSamples()
            {
                try
                {
                    lsvData.Items[0].BackColor = ReferenceColor;
                    for (int i = 1; i <= lsvData.Items.Count - 1; i++)
                        lsvData.Items[i].BackColor = ColorTable((i - 1) % ColorTable.Length);
                }
                catch (Exception ex)
                {
                }
            }

        #endregion

        #region Summary

            private void lvSummary_ItemSelectionChanged(object sender, System.Windows.Forms.ListViewItemSelectionChangedEventArgs e)
        {
            if (lsvData.SelectedIndices == null)
                return;
            if (lsvData.SelectedIndices.Count <= 0)
                return;
            SelectedIndex = lsvData.SelectedIndices[0];
            try
            {
                if (SelectedIndex == 0)
                {
                    lblSample.Text = "Reference";
                }
                else
                {
                    lblSample.Text = "Sample " + SelectedIndex.ToString();
                }
                ResetDynaplot();
                PlotReferenceCurve();
                PlotTreatmentsCurve();
                PlotSelectedTRTMarker();
            }
            catch (Exception ex)
            {
            }
        }

        #endregion

        private void btnRun_Click(object sender, EventArgs e)   ////ยังไม่เสร็จ
        {
            try
            {
                ConnectedDevices();
                //Dim MSG As String = "A:WP" & CInt(-1 * Val(txtStart.Text) / StepFactor).ToString & "P" & CInt(-1 * Val(txtStart.Text) / StepFactor).ToString
                //// คำสั่งให้มอเตอร์หมุน ??
                string MSG = "A:WP";
                MMC.WriteString(MSG);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnNew_Click(object sender, EventArgs e)
        {
            NewMeasurement();
        }

        private void txtAvageNumber_KeyPress(System.Object sender, System.Windows.Forms.KeyPressEventArgs e)
        {
            if (e.KeyChar == Chr(Keys.Enter))   //// Chr=Char ??
            {
                btnStart.Focus();
            }
        }

        private void lsvData_KeyPress(System.Object sender, System.Windows.Forms.KeyPressEventArgs e)
        {
            if (e.KeyChar == Chr(Keys.Enter))
                SendKeys.Send("{TAB}");
        }

        private void lvSummary_ItemChecked(System.Object sender, System.Windows.Forms.ItemCheckedEventArgs e)
        {
            ResetDynaplot();
            PlotReferenceCurve();
            PlotTreatmentsCurve();
            PlotSelectedTRTMarker();
        }

        private void AboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Polarimeter2019 program. (c)2019, Physics, KMITL. Design by S. Saejia.");  //// แก้เป็นปี 2019
        }

        private void colorTableToolStripMenuItem_Click(object sender, EventArgs e)
        {
            frmColorTable f = new frmColorTable();
            DialogResult result = f.ShowDialog();
            ResetDynaplot();
            PlotReferenceCurve();
            PlotTreatmentsCurve();
            PlotSelectedTRTMarker();
        }

        private void mnuStartToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DoStart();
        }

        private void mnuStopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DoStop();
        }

        private void mnuPauseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DoPause();
        }

        private void mnuCoutinewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DoPause();
        }

        private void txtVoltageRange_TextChanged(object sender, System.EventArgs e)
        {
            try
            {
                My.Settings.VoltageRange = Val(txtVoltageRange.Text);
                My.Settings.Save();
            }
            catch (Exception ex)
            {
            }
        }

        private void txtVoltageResolution_TextChanged(object sender, System.EventArgs e)
        {
            try
            {
                My.Settings.VoltageResolution = Val(txtVoltageResolution.Text);
                My.Settings.Save();
            }
            catch (Exception ex)
            {
            }
        }

        private void mnuDevicesClearDMM_Click(System.Object sender, System.EventArgs e)
        {
            try
            {
                ConnectedDevices();
                DMM.WriteString("*CLS");
                DisconnectDevices();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void mnuDevicesResetDMM_Click(System.Object sender, System.EventArgs e)
        {
            try
            {
                ConnectedDevices();
                DMM.WriteString("*RST");
                DisconnectDevices();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void mnuExportToImageFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                SaveFileDialog dlg = new SaveFileDialog();
                dlg.Filter = "Bit map (*.bmp)|*.bmp|All File (*.*)|*.*";
                DialogResult redlg = dlg.ShowDialog();
                if (redlg != Windows.Forms.DialogResult.OK);    ////คำสั่งเปิดรูป ???
                {
                    return;
                }
                string path = dlg.FileName;

                //AxDynaPlot1.ToFile(path)
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private Bitmap GetFormImage(bool include_boders)
        {
            int wid = this.Width;
            int hgt = this.Height;
            Bitmap bm = new Bitmap(wid, hgt);

            this.DrawToBitmap(bm, new Rectangle(0, 0, wid, hgt));

            if (include_boders)
                return bm;

            wid = this.ClientSize.Width;
            hgt = this.ClientSize.Height;
            Bitmap bm2 = new Bitmap(wid, hgt);

            Point pt = new Point(0, 0);
            pt = PointToScreen(pt);
            int dx = pt.X - this.Left;
            int dy = pt.Y - this.Top;

            Graphics g = Graphics.FromImage(bm2);
            g.DrawImage(bm, 0, 0, new Rectangle(dx, dy, wid, hgt), GraphicsUnit.Pixel);
            return bm2;
        }
    }
}
