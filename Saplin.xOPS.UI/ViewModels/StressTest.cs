﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Saplin.xOPS.UI.Misc;
using Xamarin.Forms;

namespace Saplin.xOPS.UI.ViewModels
{
    public class StressTest : BaseViewModel
    {
        public StressTest()
        {
        }

        public Command Retry => new Command(StartTest);

        private volatile bool stopTest = false;

        Saplin.xOPS.StressTest stressTest;
        Stopwatch sw = new Stopwatch();

        const int samplingMs = 500;
        const int smoothing = 2;
        const int warmUpSample = 7;

        public void StartTest()
        {
            VmLocator.OnlineDb.SendPageHit("stressStart");
            ScreenOn.Enable();
            TestNotStarted = false;
            UpdateCounter = 0;

            stressTest = new Saplin.xOPS.StressTest(samplingMs, smoothing, warmUpSample, Environment.ProcessorCount, true, true);

            Gflops = stressTest.GflopsResults?.SmoothResults;
            Ginops = stressTest.GinopsResults?.SmoothResults;
            Temp = null;

            var di = DependencyService.Get<IDeviceInfo>();

            try
            {
                di.GetCpuTemp();
                Temp = new List<double>();
                RaisePropertyChanged(nameof(Temp));
            }
            catch { }

            var tempText = string.Empty;

            if (Temp == null)
            {
                tempText = TempLabel = VmLocator.L11n.TempNotAvailable + "\n";
            }

            RaisePropertyChanged(nameof(Gflops));
            RaisePropertyChanged(nameof(Ginops));

            var label1 = VmLocator.L11n.Start + ": {1:0.00} {0}\n" + VmLocator.L11n.Now + ": {2:0.00} {0}";
            var label2 = "{1:0.00} {0}\n{2:0.00}%";

            var prevCount = 0;

            stressTest.ResultsUpdated += (e) =>
            {
                if (stressTest.WarmpingUp)
                {
                    GflopsLabel = GinopsLabel = Environment.ProcessorCount + " "+VmLocator.L11n.threads+" \n" + VmLocator.L11n.WarmingUp + "...";
                    TempLabel = VmLocator.L11n.WarmingUp + "...";
                }
                else
                {
                    var count = Gflops != null ? Gflops.Count
                        : Ginops != null ? Ginops.Count : 0;

                    if (prevCount != count)
                    {
                        prevCount = count;

                        if (stressTest.GflopsResults != null)
                        {
                            GflopsLabel = UpdateCounter < 10 ?
                                string.Format(label1,
                                    "GFLOPS",
                                    stressTest.GflopsResults.StartSmooth,
                                    stressTest.GflopsResults.CurrentSmooth) :
                                string.Format(label2,
                                    "GFLOPS",
                                    stressTest.GflopsResults.CurrentSmooth,
                                    ((stressTest.GflopsResults.CurrentSmooth - stressTest.GflopsResults.StartSmooth) / stressTest.GflopsResults.StartSmooth * 100));
                        }

                        if (stressTest.GinopsResults != null)
                        {
                            GinopsLabel = UpdateCounter < 14 ?
                                string.Format(label1,
                                    "GINOPS",
                                    stressTest.GinopsResults.StartSmooth,
                                    stressTest.GinopsResults.CurrentSmooth) :
                                string.Format(label2,
                                    "GINOPS",
                                    stressTest.GinopsResults.CurrentSmooth,
                                    ((stressTest.GinopsResults.CurrentSmooth - stressTest.GinopsResults.StartSmooth) / stressTest.GinopsResults.StartSmooth * 100));
                        }

                        if (Temp != null)
                        {
                            try
                            {
                                var temp = di.GetCpuTemp();
                                Temp.Add(temp);
                                tempText = "CPU " + temp.ToString("0.0") + "°C "
                                 + (temp > Temp[0] ? "↑" : "↓")
                                 + (temp - Temp[0]).ToString("0.0") + "°C";

                            }
                            catch { };
                        }                            

                        Update();
                    }

                    TempLabel = tempText + "\n" + sw.Elapsed.Minutes + (UpdateCounter % 2 == 0 ? ":" : ".") + sw.Elapsed.Seconds.ToString("00"); ;
                }

                RaisePropertyChanged(nameof(GflopsLabel));
                RaisePropertyChanged(nameof(GinopsLabel));
                RaisePropertyChanged(nameof(TempLabel));
            };

            stressTest.Start();
            sw.Restart();
        }

        public int UpdateCounter { get; private set; } = 0;
        void Update() { UpdateCounter++; RaisePropertyChanged(nameof(UpdateCounter)); }

        public Command Stop => new Command(StopTest);

        public void StopTest()
        {
            stressTest?.Stop();
            sw.Stop();
            TestNotStarted = true;
            VmLocator.OnlineDb.SendPageHit("breakStress");

            var label1 = string.Empty;

            if (stressTest.GflopsResults?.SmoothResults?.Count > 10 || stressTest.GflopsResults?.SmoothResults?.Count > 10)
                label1 = VmLocator.L11n.First5Secs + ": {1:0.00} {0}\n" + VmLocator.L11n.Last5Secs + ": {2:0.00} {0}\n{3} {4:0.00}%";
            else label1 = VmLocator.L11n.Start + ": {1:0.00} {0}\n" + VmLocator.L11n.End + ": {2:0.00} {0}\n{3} {4:0.00}%";

            if (stressTest.GflopsResults != null)
            {
                GflopsLabel = GetResultLabel(stressTest.GflopsResults, label1, "GFLOPS");
            }

            if (stressTest.GinopsResults != null)
            {
                GinopsLabel = GetResultLabel(stressTest.GinopsResults, label1, "GINOPS");
            }

            RaisePropertyChanged(nameof(GflopsLabel));
            RaisePropertyChanged(nameof(GinopsLabel));
            RaisePropertyChanged(nameof(TempLabel));

            ScreenOn.Disable();
            VmLocator.OnlineDb.SendPageHit("stressStop");
        }

        private string GetResultLabel(TimeSeries ts, string label1, string unit)
        {
            double start = -1, end = -1;

            GetStartEnd(ts, out start, out end);

            var diff = (end - start) / start * 100;

            var result =
                string.Format(label1,
                    unit,
                    start,
                    end,
                    (diff < -5 ? "↓" : diff > 5 ? "↑" : "⇆"),
                    diff
                   );
            return result;
        }

        private void GetStartEnd(TimeSeries ts, out double start, out double end)
        {
            if (sw.Elapsed.Seconds < 10)
            {
                start = ts.StartSmooth;
                end = ts.CurrentSmooth;
            }
            else
            {
                start = ts.SmoothResults.Take(5).Average();
                end = ts.SmoothResults.Skip(ts.SmoothResults.Count - 5).Take(5).Average();
            }
        }

        bool testNotStarted = true;

        public bool TestNotStarted
        {
            get { return testNotStarted; }
            set
            {
                testNotStarted = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(TestStarted));
            }
        }

        public bool TestStarted => !TestNotStarted;

        public IList<double> Gflops { get; set; }
        public IList<double> Ginops { get; set; }
        public IList<double> Temp { get; set; }

        public string GflopsLabel { get; set; }
        public string GinopsLabel { get; set; }
        public string TempLabel { get; set; }
    }
}