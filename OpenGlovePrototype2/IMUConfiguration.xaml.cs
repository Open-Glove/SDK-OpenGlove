using Microsoft.Win32;
using OpenGlovePrototype2;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using OpenGlove_API_C_Sharp_HL;
using OpenGlove_API_C_Sharp_HL.ServiceReference1;
using System.ServiceModel;

namespace OpenGlovePrototype2
{
    /// <summary>
    /// Lógica de interacción para IMUConfiguration.xaml
    /// </summary>
    public partial class IMUConfiguration : Window
    {
        private OGServiceClient serviceClient;
        private OpenGloveAPI gloves = OpenGloveAPI.GetInstance();
        private Glove selectedGlove;

        private bool testing;
        private bool showingTiming;
        private bool _suppressImuComboEvent;

        private const int MaxTimingSamples = 300;
        private readonly Stopwatch _sampleArrivalStopwatch = new Stopwatch();
        private readonly List<double> _processingTimesMs = new List<double>();
        private readonly List<double> _updateIntervalsMs = new List<double>();
        private long _lastSampleTimestampMs;
        private bool _hasPreviousSampleTimestamp;
        private int _accumulatedSampleCount;

        // Estado para calibración y modelo segmentario (cuaterniones en orden w,x,y,z)
        private ArmTrackingMath.Quat _qBrazoActual;
        private ArmTrackingMath.Quat _qAntebrazoActual;
        private ArmTrackingMath.Quat _qBrazo0;
        private ArmTrackingMath.Quat _qAntebrazo0;
        private ArmTrackingMath.Vec3 _pManoAtCalibration;
        private bool _isCalibrated;

        // Últimos valores mostrados (para exportar fila CSV)
        private ArmTrackingMath.Quat _lastQBrazoRel;
        private ArmTrackingMath.Quat _lastQAntebrazoRel;
        private double _lastCoordX, _lastCoordY, _lastCoordZ;

        public IMUConfiguration(Glove selectedGlove)
        {
            BasicHttpBinding binding = new BasicHttpBinding();
            EndpointAddress address = new EndpointAddress("http://localhost:8733/Design_Time_Addresses/OpenGloveWCF/OGService/");
            serviceClient = new OGServiceClient(binding, address);

            this.selectedGlove = selectedGlove;
            InitializeComponent();
            this.initDataComboBox();

            // Asegurar perfil y defaults
            if (this.selectedGlove.GloveConfiguration.GloveProfile == null)
            {
                this.selectedGlove.GloveConfiguration.GloveProfile = new Glove.Configuration.Profile();
            }

            if (string.IsNullOrWhiteSpace(this.selectedGlove.GloveConfiguration.GloveProfile.imuModel)
                || string.Equals(this.selectedGlove.GloveConfiguration.GloveProfile.imuModel, "Default", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(this.selectedGlove.GloveConfiguration.GloveProfile.imuModel))
                {
                    this.selectedGlove.GloveConfiguration.GloveProfile.imuStatus = false;
                    this.selectedGlove.GloveConfiguration.GloveProfile.rawData = false;
                    this.selectedGlove.GloveConfiguration.GloveProfile.imuCalibrationStatus = false;
                    this.selectedGlove.GloveConfiguration.GloveProfile.imuModel = "BNO055";
                    labelIMUStatus.Content = "Off";
                }
                else
                {
                    this.selectedGlove.GloveConfiguration.GloveProfile.imuModel = "LSM9DS1";
                }
            }

            _suppressImuComboEvent = true;
            comboBoxIMU.Items.Clear();
            comboBoxIMU.Items.Add("BNO055");
            comboBoxIMU.Items.Add("LSM9DS1");
            string currentModel = this.selectedGlove.GloveConfiguration.GloveProfile.imuModel;
            comboBoxIMU.SelectedItem = string.Equals(currentModel, "LSM9DS1", StringComparison.OrdinalIgnoreCase)
                ? "LSM9DS1"
                : "BNO055";
            _suppressImuComboEvent = false;

            updateView();
            ApplyImuModeUi();
            testing = false;
            showingTiming = false;
            UpdateDataPanelVisibility();
            _isCalibrated = false;

            labelPManoX.Content = "—"; labelPManoY.Content = "—"; labelPManoZ.Content = "—";

            _sampleArrivalStopwatch.Start();
            ResetTimingMetrics();
        }

        private bool IsBno055Mode
        {
            get
            {
                string model = selectedGlove?.GloveConfiguration?.GloveProfile?.imuModel;
                return !string.Equals(model, "LSM9DS1", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(model, "Default", StringComparison.OrdinalIgnoreCase);
            }
        }

        private void ApplyImuModeUi()
        {
            bool bno = IsBno055Mode;

            buttonTiming.Visibility = bno ? Visibility.Visible : Visibility.Collapsed;
            label_ValueData.Visibility = bno ? Visibility.Collapsed : Visibility.Visible;
            choosenDataComboBox.Visibility = bno ? Visibility.Collapsed : Visibility.Visible;
            buttonSetRawData.Visibility = bno ? Visibility.Collapsed : Visibility.Visible;

            if (!bno && showingTiming)
            {
                showingTiming = false;
                buttonTiming.Content = "Timing";
            }

            UpdateDataPanelVisibility();
        }

        private void comboBoxIMU_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressImuComboEvent || comboBoxIMU.SelectedItem == null)
                return;

            string model = comboBoxIMU.SelectedItem.ToString();
            if (this.selectedGlove.GloveConfiguration.GloveProfile == null)
                this.selectedGlove.GloveConfiguration.GloveProfile = new Glove.Configuration.Profile();

            this.selectedGlove.GloveConfiguration.GloveProfile.imuModel = model;

            bool wasReceiving = IsReceivingData;
            if (wasReceiving)
                UpdateReceiverSubscription(false);

            ApplyImuModeUi();

            if (wasReceiving)
                UpdateReceiverSubscription(true);

            // Propaga al servicio → Communication.SetImuModel
            try
            {
                serviceClient.SaveGlove(this.selectedGlove);
            }
            catch { }
        }

        public class ComboboxItem
        {
            public string Text { get; set; }
            public object Value { get; set; }
            public override string ToString() => Text;
        }

        private void initDataComboBox()
        {
            ComboboxItem item = new ComboboxItem { Text = "All Data", Value = "z" };
            choosenDataComboBox.Items.Add(item);

            item = new ComboboxItem { Text = "Accelerometer", Value = "a" };
            choosenDataComboBox.Items.Add(item);

            item = new ComboboxItem { Text = "Gyroscope", Value = "g" };
            choosenDataComboBox.Items.Add(item);

            item = new ComboboxItem { Text = "magnetometer", Value = "m" };
            choosenDataComboBox.Items.Add(item);

            item = new ComboboxItem { Text = "Attitude Data", Value = "r" };
            choosenDataComboBox.Items.Add(item);

            choosenDataComboBox.SelectedIndex = 0;
        }

        private void updateView()
        {
            if (this.selectedGlove.GloveConfiguration.GloveProfile.imuStatus == true)
            {
                button.Content = "Deactivate data";
                labelIMUStatus.Content = "On";
            }
            else
            {
                button.Content = "Activate data";
                labelIMUStatus.Content = "Off";
            }

            if (this.selectedGlove.GloveConfiguration.GloveProfile.rawData == true)
            {
                buttonSetRawData.Content = "Processed data";
            }
            else
            {
                buttonSetRawData.Content = "Raw Data";
            }

            serviceClient.SaveGlove(this.selectedGlove);
        }

        // Activate / Deactivate: delega al SDK (servicio). Nada de SerialPort aquí.
        private void button_Click(object sender, RoutedEventArgs e)
        {
            if (this.selectedGlove.GloveConfiguration.GloveProfile.imuStatus == false)
            {
                gloves.startIMU(selectedGlove);
                this.selectedGlove.GloveConfiguration.GloveProfile.imuStatus = true;
            }
            else
            {
                gloves.setIMUStatus(selectedGlove, false);
                this.selectedGlove.GloveConfiguration.GloveProfile.imuStatus = false;
            }

            updateView();
        }

        private bool IsReceivingData => testing || showingTiming;

        private void UpdateDataPanelVisibility()
        {
            if (!testing && !showingTiming)
            {
                GridDataPanel.Visibility = Visibility.Hidden;
                GridTestData.Visibility = Visibility.Collapsed;
                GridLegacyTest.Visibility = Visibility.Collapsed;
                GridTimingMetrics.Visibility = Visibility.Collapsed;
                return;
            }

            GridDataPanel.Visibility = Visibility.Visible;
            bool bno = IsBno055Mode;

            if (testing)
            {
                GridTestData.Visibility = bno ? Visibility.Visible : Visibility.Collapsed;
                GridLegacyTest.Visibility = bno ? Visibility.Collapsed : Visibility.Visible;
            }
            else
            {
                GridTestData.Visibility = Visibility.Collapsed;
                GridLegacyTest.Visibility = Visibility.Collapsed;
            }

            GridTimingMetrics.Visibility = (showingTiming && bno) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void UpdateReceiverSubscription(bool subscribe)
        {
            try
            {
                var receiver = gloves.getDataReceiver(selectedGlove);
                if (subscribe)
                {
                    if (IsBno055Mode)
                    {
                        receiver.dualQuaternionFunction += dualQuaternionHandler;
                    }
                    else
                    {
                        receiver.imu_ValuesFunction += allIMUValues;
                    }
                }
                else
                {
                    receiver.dualQuaternionFunction -= dualQuaternionHandler;
                    receiver.imu_ValuesFunction -= allIMUValues;
                    receiver.quaternionFunction -= quaternionHandler;
                }
            }
            catch
            {
            }
        }

        // Test / Stop: muestra solo cuaterniones y posición
        private void buttonTest_Click(object sender, RoutedEventArgs e)
        {
            bool wasReceiving = IsReceivingData;

            if (testing)
            {
                testing = false;
                buttonTest.Content = "Test";
            }
            else
            {
                if (showingTiming)
                {
                    showingTiming = false;
                    buttonTiming.Content = "Timing";
                }
                testing = true;
                buttonTest.Content = "Stop";
            }

            UpdateDataPanelVisibility();

            bool nowReceiving = IsReceivingData;
            if (wasReceiving && !nowReceiving)
                UpdateReceiverSubscription(false);
            else if (!wasReceiving && nowReceiving)
                UpdateReceiverSubscription(true);
        }

        // Timing / Stop: solo BNO055 (métricas del pipeline de cuaterniones)
        private void buttonTiming_Click(object sender, RoutedEventArgs e)
        {
            if (!IsBno055Mode)
                return;

            bool wasReceiving = IsReceivingData;

            if (showingTiming)
            {
                showingTiming = false;
                buttonTiming.Content = "Timing";
            }
            else
            {
                if (testing)
                {
                    testing = false;
                    buttonTest.Content = "Test";
                }
                showingTiming = true;
                buttonTiming.Content = "Stop";
                UpdateTimingLabels(_processingTimesMs.Count > 0 ? _processingTimesMs[_processingTimesMs.Count - 1] : (double?)null);
            }

            UpdateDataPanelVisibility();

            bool nowReceiving = IsReceivingData;
            if (wasReceiving && !nowReceiving)
                UpdateReceiverSubscription(false);
            else if (!wasReceiving && nowReceiving)
                UpdateReceiverSubscription(true);
        }

        /// <summary>Formato legacy (q,qx,qy,qz). Se mantiene por compatibilidad con el evento.</summary>
        public void quaternionHandler(float qx, float qy, float qz)
        {
        }

        /// <summary>LSM9DS1: accel/gyro/mag vía mensaje "z,..." (imu_ValuesFunction).</summary>
        public void allIMUValues(float ax, float ay, float az, float gx, float gy, float gz, float mx, float my, float mz)
        {
            if (!testing || IsBno055Mode)
                return;

            this.Dispatcher.Invoke((Action)(() =>
            {
                this.labelAx.Content = ax.ToString();
                this.labelAy.Content = ay.ToString();
                this.labelAz.Content = az.ToString();
                this.labelGx.Content = gx.ToString();
                this.labelGy.Content = gy.ToString();
                this.labelGz.Content = gz.ToString();
                this.labelMx.Content = mx.ToString();
                this.labelMy.Content = my.ToString();
                this.labelMz.Content = mz.ToString();
            }));
        }

        /// <summary>Cuaterniones ESP32/BNO055: brazo + antebrazo. Actualiza UI y, si está calibrado, calcula pMano.</summary>
        public void dualQuaternionHandler(float w1, float x1, float y1, float z1, float w2, float x2, float y2, float z2)
        {
            if (!IsBno055Mode)
                return;

            long currentTimestampMs = _sampleArrivalStopwatch.ElapsedMilliseconds;
            double? intervalMs = null;
            if (_hasPreviousSampleTimestamp)
                intervalMs = currentTimestampMs - _lastSampleTimestampMs;
            _lastSampleTimestampMs = currentTimestampMs;
            _hasPreviousSampleTimestamp = true;

            var processingStopwatch = Stopwatch.StartNew();

            this.Dispatcher.Invoke((Action)(() =>
            {
                _qBrazoActual = new ArmTrackingMath.Quat(w1, x1, y1, z1);
                _qAntebrazoActual = new ArmTrackingMath.Quat(w2, x2, y2, z2);

                // Cuaterniones relativos: si está calibrado usamos q0 guardado; si no, referencia identidad (rel = actual)
                ArmTrackingMath.Quat qBrazoRel = _isCalibrated
                    ? ArmTrackingMath.Multiply(ArmTrackingMath.Conjugate(_qBrazo0), _qBrazoActual)
                    : _qBrazoActual;
                ArmTrackingMath.Quat qAntebrazoRel = _isCalibrated
                    ? ArmTrackingMath.Multiply(ArmTrackingMath.Conjugate(_qAntebrazo0), _qAntebrazoActual)
                    : _qAntebrazoActual;

                // Posición: L1/L2 por defecto 10 cuando no hay valor válido
                float L1 = ParseLength(textBoxL1.Text, 10f);
                float L2 = ParseLength(textBoxL2.Text, 10f);
                ArmTrackingMath.Vec3 l1 = new ArmTrackingMath.Vec3(0, 0, L1);
                ArmTrackingMath.Vec3 l2 = new ArmTrackingMath.Vec3(0, 0, L2);
                ArmTrackingMath.Vec3 v1 = ArmTrackingMath.Rotate(qBrazoRel, l1);
                ArmTrackingMath.Vec3 v2 = ArmTrackingMath.Rotate(qAntebrazoRel, l2);
                ArmTrackingMath.Vec3 pMano = ArmTrackingMath.Add(v1, v2);

                if (_isCalibrated)
                {
                    ArmTrackingMath.Vec3 pFinal = ArmTrackingMath.Subtract(pMano, _pManoAtCalibration);
                    _lastCoordX = pFinal.X; _lastCoordY = pFinal.Y; _lastCoordZ = pFinal.Z;
                    if (testing)
                    {
                        labelPManoX.Content = pFinal.X.ToString("F2");
                        labelPManoY.Content = pFinal.Y.ToString("F2");
                        labelPManoZ.Content = pFinal.Z.ToString("F2");
                    }
                }
                else
                {
                    _lastCoordX = pMano.X; _lastCoordY = pMano.Y; _lastCoordZ = pMano.Z;
                    if (testing)
                    {
                        labelPManoX.Content = pMano.X.ToString("F2");
                        labelPManoY.Content = pMano.Y.ToString("F2");
                        labelPManoZ.Content = pMano.Z.ToString("F2");
                    }
                }
                _lastQBrazoRel = qBrazoRel;
                _lastQAntebrazoRel = qAntebrazoRel;

                if (testing)
                {
                    labelBrazoW.Content = w1.ToString();
                    labelBrazoX.Content = x1.ToString();
                    labelBrazoY.Content = y1.ToString();
                    labelBrazoZ.Content = z1.ToString();
                    labelAntebrazoW.Content = w2.ToString();
                    labelAntebrazoX.Content = x2.ToString();
                    labelAntebrazoY.Content = y2.ToString();
                    labelAntebrazoZ.Content = z2.ToString();
                    labelBrazoRelW.Content = qBrazoRel.W.ToString();
                    labelBrazoRelX.Content = qBrazoRel.X.ToString();
                    labelBrazoRelY.Content = qBrazoRel.Y.ToString();
                    labelBrazoRelZ.Content = qBrazoRel.Z.ToString();
                    labelAntebrazoRelW.Content = qAntebrazoRel.W.ToString();
                    labelAntebrazoRelX.Content = qAntebrazoRel.X.ToString();
                    labelAntebrazoRelY.Content = qAntebrazoRel.Y.ToString();
                    labelAntebrazoRelZ.Content = qAntebrazoRel.Z.ToString();
                }

                processingStopwatch.Stop();
                double processingMs = processingStopwatch.Elapsed.TotalMilliseconds;
                AddLimitedSample(_processingTimesMs, processingMs, MaxTimingSamples);
                if (intervalMs.HasValue)
                    AddLimitedSample(_updateIntervalsMs, intervalMs.Value, MaxTimingSamples);
                _accumulatedSampleCount++;

                if (showingTiming)
                    UpdateTimingLabels(processingMs);
            }));
        }

        private static void AddLimitedSample(List<double> buffer, double value, int maxSize)
        {
            buffer.Add(value);
            if (buffer.Count > maxSize)
                buffer.RemoveAt(0);
        }

        private static double? Average(IReadOnlyList<double> values)
        {
            if (values == null || values.Count == 0)
                return null;
            return values.Average();
        }

        private static double? Max(IReadOnlyList<double> values)
        {
            if (values == null || values.Count == 0)
                return null;
            return values.Max();
        }

        private struct TimingSummary
        {
            public double? ProcActualMs;
            public double? ProcPromedioMs;
            public double? ProcMaximoMs;
            public double? IntervaloPromedioMs;
            public double? FrecuenciaHz;
            public int Muestras;
        }

        private TimingSummary GetTimingSummary(double? currentProcessingMs = null)
        {
            double? procActual = currentProcessingMs;
            if (!procActual.HasValue && _processingTimesMs.Count > 0)
                procActual = _processingTimesMs[_processingTimesMs.Count - 1];

            double? avgInterval = Average(_updateIntervalsMs);
            double? freqHz = null;
            if (avgInterval.HasValue && avgInterval.Value > 0)
                freqHz = 1000.0 / avgInterval.Value;

            return new TimingSummary
            {
                ProcActualMs = procActual,
                ProcPromedioMs = Average(_processingTimesMs),
                ProcMaximoMs = Max(_processingTimesMs),
                IntervaloPromedioMs = avgInterval,
                FrecuenciaHz = freqHz,
                Muestras = _accumulatedSampleCount
            };
        }

        private static string FormatTimingMs(double? value)
        {
            return value.HasValue
                ? value.Value.ToString("F3", CultureInfo.InvariantCulture)
                : "--";
        }

        private static string FormatTimingHz(double? value)
        {
            return value.HasValue
                ? value.Value.ToString("F2", CultureInfo.InvariantCulture)
                : "--";
        }

        private void UpdateTimingLabels(double? currentProcessingMs)
        {
            TimingSummary summary = GetTimingSummary(currentProcessingMs);

            labelProcActual.Content = FormatTimingMs(summary.ProcActualMs);
            labelProcPromedio.Content = FormatTimingMs(summary.ProcPromedioMs);
            labelProcMaximo.Content = FormatTimingMs(summary.ProcMaximoMs);
            labelIntervaloPromedio.Content = FormatTimingMs(summary.IntervaloPromedioMs);
            labelFrecuenciaHz.Content = FormatTimingHz(summary.FrecuenciaHz);
            labelMuestrasTiming.Content = summary.Muestras.ToString(CultureInfo.InvariantCulture);
        }

        private void ResetTimingMetrics()
        {
            _processingTimesMs.Clear();
            _updateIntervalsMs.Clear();
            _accumulatedSampleCount = 0;
            _hasPreviousSampleTimestamp = false;
            _lastSampleTimestampMs = 0;

            if (showingTiming)
                UpdateTimingLabels(null);
        }

        private static string FormatTimingCsvNumber(double? value, string format, CultureInfo culture)
        {
            return value.HasValue ? value.Value.ToString(format, culture) : "";
        }

        private void ExportTimingMetrics()
        {
            const string colSep = ";";
            var dec = CultureInfo.GetCultureInfo("es-ES");
            TimingSummary summary = GetTimingSummary();

            string header = string.Join(colSep,
                "tiempo_procesamiento_actual_ms",
                "tiempo_procesamiento_promedio_ms",
                "tiempo_procesamiento_maximo_ms",
                "intervalo_promedio_ms",
                "frecuencia_efectiva_hz",
                "muestras_consideradas");

            string row = string.Join(colSep,
                FormatTimingCsvNumber(summary.ProcActualMs, "F3", dec),
                FormatTimingCsvNumber(summary.ProcPromedioMs, "F3", dec),
                FormatTimingCsvNumber(summary.ProcMaximoMs, "F3", dec),
                FormatTimingCsvNumber(summary.IntervaloPromedioMs, "F3", dec),
                FormatTimingCsvNumber(summary.FrecuenciaHz, "F2", dec),
                summary.Muestras.ToString(CultureInfo.InvariantCulture));

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string projectDir = Path.GetDirectoryName(Path.GetDirectoryName(baseDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)));
            if (string.IsNullOrEmpty(projectDir)) projectDir = baseDir;
            string path = Path.Combine(projectDir, "OpenGlove_IMU_timing_export.csv");

            if (!File.Exists(path))
                File.WriteAllText(path, header + Environment.NewLine + row + Environment.NewLine, Encoding.UTF8);
            else
                File.AppendAllText(path, row + Environment.NewLine, Encoding.UTF8);

            MessageBox.Show("Resumen exportado en:\n" + path, "Exportar timing", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void buttonResetTiming_Click(object sender, RoutedEventArgs e)
        {
            ResetTimingMetrics();
        }

        private void buttonExportTiming_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ExportTimingMetrics();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al exportar timing: " + ex.Message, "Exportar timing", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private static float ParseLength(string text, float defaultValue)
        {
            float v;
            if (float.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out v) && v > 0)
                return v;
            return defaultValue;
        }

        /// <summary>Guarda orientaciones y posición de la mano en este instante como referencia. pFinal = pMano - pManoCalibration (en calibración = 0,0,0).</summary>
        private void buttonCalibrar_Click(object sender, RoutedEventArgs e)
        {
            _qBrazo0 = _qBrazoActual;
            _qAntebrazo0 = _qAntebrazoActual;

            float L1 = ParseLength(textBoxL1.Text, 10f);
            float L2 = ParseLength(textBoxL2.Text, 10f);
            ArmTrackingMath.Vec3 l1 = new ArmTrackingMath.Vec3(0, 0, L1);
            ArmTrackingMath.Vec3 l2 = new ArmTrackingMath.Vec3(0, 0, L2);
            // En el instante de calibración qBrazoRel = Inverse(qBrazo0)*qBrazoActual = identidad (qBrazo0 = qBrazoActual)
            ArmTrackingMath.Quat qBrazoRelCal = ArmTrackingMath.Multiply(ArmTrackingMath.Conjugate(_qBrazo0), _qBrazoActual);
            ArmTrackingMath.Quat qAntebrazoRelCal = ArmTrackingMath.Multiply(ArmTrackingMath.Conjugate(_qAntebrazo0), _qAntebrazoActual);
            _pManoAtCalibration = ArmTrackingMath.Add(
                ArmTrackingMath.Rotate(qBrazoRelCal, l1),
                ArmTrackingMath.Rotate(qAntebrazoRelCal, l2)
            );

            _isCalibrated = true;
            labelCalibrado.Content = "Calibrado: Sí";
        }

        /// <summary>Append una fila al CSV. Separador de columnas: / . Separador decimal: coma. Cuaterniones en orden w,x,y,z. Sin encabezado.</summary>
        private void buttonExportarFila_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                const string colSep = "/";
                var dec = CultureInfo.GetCultureInfo("es-ES"); // coma como separador decimal

                string nombre = textBoxTituloFila?.Text ?? "";
                if (nombre.IndexOf(colSep, StringComparison.Ordinal) >= 0 || nombre.IndexOf('"') >= 0)
                {
                    nombre = "\"" + nombre.Replace("\"", "\"\"") + "\"";
                }

                string line = nombre + colSep
                    + _qBrazoActual.W.ToString("F4", dec) + colSep + _qBrazoActual.X.ToString("F4", dec) + colSep + _qBrazoActual.Y.ToString("F4", dec) + colSep + _qBrazoActual.Z.ToString("F4", dec) + colSep
                    + _qAntebrazoActual.W.ToString("F4", dec) + colSep + _qAntebrazoActual.X.ToString("F4", dec) + colSep + _qAntebrazoActual.Y.ToString("F4", dec) + colSep + _qAntebrazoActual.Z.ToString("F4", dec) + colSep
                    + _lastQBrazoRel.W.ToString("F4", dec) + colSep + _lastQBrazoRel.X.ToString("F4", dec) + colSep + _lastQBrazoRel.Y.ToString("F4", dec) + colSep + _lastQBrazoRel.Z.ToString("F4", dec) + colSep
                    + _lastQAntebrazoRel.W.ToString("F4", dec) + colSep + _lastQAntebrazoRel.X.ToString("F4", dec) + colSep + _lastQAntebrazoRel.Y.ToString("F4", dec) + colSep + _lastQAntebrazoRel.Z.ToString("F4", dec) + colSep
                    + _lastCoordX.ToString("F4", dec) + colSep + _lastCoordY.ToString("F4", dec) + colSep + _lastCoordZ.ToString("F4", dec);

                // Dos carpetas arriba del .exe → OpenGlovePrototype2 (p. ej. bin\Debug → bin → OpenGlovePrototype2)
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string projectDir = Path.GetDirectoryName(Path.GetDirectoryName(baseDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)));
                if (string.IsNullOrEmpty(projectDir)) projectDir = baseDir;
                string path = Path.Combine(projectDir, "OpenGlove_IMU_export.csv");
                File.AppendAllText(path, line + Environment.NewLine);
                System.Windows.MessageBox.Show("Fila exportada en:\n" + path, "Exportar fila", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Error al exportar: " + ex.Message, "Exportar fila", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void buttonSetRawData_Click(object sender, RoutedEventArgs e)
        {
            if (IsBno055Mode)
                return;

            if (selectedGlove.GloveConfiguration.GloveProfile.rawData == true)
            {
                gloves.setRawData(selectedGlove, false);
                selectedGlove.GloveConfiguration.GloveProfile.rawData = false;
            }
            else
            {
                gloves.setRawData(selectedGlove, true);
                selectedGlove.GloveConfiguration.GloveProfile.rawData = true;
            }
            updateView();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (IsReceivingData)
            {
                UpdateReceiverSubscription(false);
                testing = false;
                showingTiming = false;
            }
        }

        private void choosenDataComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsBno055Mode || choosenDataComboBox.SelectedItem == null)
                return;

            string selectedData = (choosenDataComboBox.SelectedItem as ComboboxItem).Value.ToString();
            switch (selectedData)
            {
                case "a":
                    gloves.setChoosingData(this.selectedGlove, 0);
                    break;
                case "g":
                    gloves.setChoosingData(this.selectedGlove, 1);
                    break;
                case "m":
                    gloves.setChoosingData(this.selectedGlove, 2);
                    break;
                case "r":
                    gloves.setChoosingData(this.selectedGlove, 3);
                    break;
                case "z":
                    gloves.setChoosingData(this.selectedGlove, 4);
                    break;
            }
        }
    }
}
