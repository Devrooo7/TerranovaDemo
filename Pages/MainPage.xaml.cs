using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using TerranovaDemo.Services;

namespace TerranovaDemo
{
    public partial class MainPage : ContentPage
    {
        private readonly List<float> _humidityData = new();
        private readonly List<float> _temperatureData = new();

        private bool _bombaEncendida = false;
        private bool _esp32Disponible = false;
        private readonly System.Timers.Timer _timer;

        private readonly AuthService _auth;
        private readonly FirebaseAuthClient _firebase;

        public MainPage(AuthService auth, FirebaseAuthClient firebase)
        {
            InitializeComponent();

            _auth = auth ?? throw new ArgumentNullException(nameof(auth));
            _firebase = firebase ?? throw new ArgumentNullException(nameof(firebase));

            for (int i = 0; i < 12; i++)
            {
                _humidityData.Add(30f);
                _temperatureData.Add(25f);
            }

            // Suscripción al evento que dispara SettingsPage
            AppState.OnNewEspData += OnEsp32DataReceived;

            _ = CheckESP32Connection();

            _timer = new System.Timers.Timer(1000);
            _timer.Elapsed += (s, e) =>
            {
                MainThread.BeginInvokeOnMainThread(() => SensorsCanvas.InvalidateSurface());
            };
            _timer.Start();

            UpdateBombaButton();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            // Desuscripción del evento
            AppState.OnNewEspData -= OnEsp32DataReceived;
            _timer?.Stop();
        }

        private void OnEsp32DataReceived(ESP32Data data)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                if (data is null) return;

                // 1. Actualizar los datos de la gráfica
                UpdateSensorData(data.humedadSuelo, data.temperatura);

                // 2. Actualizar las etiquetas y el estado de la bomba
                HumidityLabel.Text = $"Humedad: {data.humedadSuelo}%";
                TemperatureLabel.Text = $"Temp: {data.temperatura:F1}°C";

                _bombaEncendida = data.bombaStatus;
                UpdateBombaButton();

                // 3. Forzar el redibujado de la gráfica AHORA
                // 🟢 CORRECCIÓN APLICADA: Forzar redibujado
                SensorsCanvas.InvalidateSurface();

                // 4. Guardar datos en Firebase (Responsabilidad única de MainPage)
                string uid = AppState.CurrentUserUid;
                string deviceId = SessionStore.GetDeviceId();
                if (!string.IsNullOrEmpty(uid) && !string.IsNullOrEmpty(deviceId))
                {
                    string deviceIp = SessionStore.GetDeviceIp();
                    await _firebase.SaveSensorDataAsync(uid, deviceId, data.humedadSuelo, data.temperatura, data.bombaStatus, deviceIp);
                }
            });
        }

        private async Task CheckESP32Connection()
        {
            while (true)
            {
                try
                {
                    string ip = AppState.SavedESP32Ip;
                    if (!string.IsNullOrEmpty(ip))
                    {
                        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
                        // 🟢 CORRECCIÓN 3.1: Cambiar /ping por el endpoint raíz "/"
                        var response = await client.GetAsync($"http://{ip}/");
                        _esp32Disponible = response.IsSuccessStatusCode;
                    }
                    else _esp32Disponible = false;
                }
                catch
                {
                    _esp32Disponible = false;
                }

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    BombaButton.IsEnabled = _esp32Disponible;
                    // Mantenemos el color de la bomba en base a su estado (_bombaEncendida) y no de la disponibilidad
                    // Pero la disponibilidad del botón sí depende de _esp32Disponible
                });

                await Task.Delay(5000);
            }
        }

        private async void BombaButton_Clicked(object sender, EventArgs e)
        {
            if (!_esp32Disponible)
            {
                await DisplayAlert("Sin conexión", "El ESP32 no está disponible.", "OK");
                return;
            }

            _bombaEncendida = !_bombaEncendida;
            UpdateBombaButton();

            string action = _bombaEncendida ? "on" : "off";
            string ip = AppState.SavedESP32Ip;
            string url = $"http://{ip}/bomba/{action}";

            try
            {
                using var http = new HttpClient();
                var response = await http.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                    await DisplayAlert("Error", "No se pudo enviar la señal a la bomba.", "OK");

                string uid = AppState.CurrentUserUid;
                string deviceId = SessionStore.GetDeviceId();
                if (!string.IsNullOrEmpty(uid) && !string.IsNullOrEmpty(deviceId))
                    await _firebase.UpdatePumpStateAsync(uid, deviceId, _bombaEncendida);
            }
            catch
            {
                await DisplayAlert("Error", "No se pudo conectar al ESP32.", "OK");
            }
        }

        private void UpdateBombaButton()
        {
            BombaButton.Text = _bombaEncendida ? "ON" : "OFF";
            BombaButton.BackgroundColor = _bombaEncendida ? Microsoft.Maui.Graphics.Colors.Green : Microsoft.Maui.Graphics.Colors.Red;
        }

        private void UpdateSensorData(int humidity, float temperature)
        {
            if (_humidityData.Count >= 12) _humidityData.RemoveAt(0);
            if (_temperatureData.Count >= 12) _temperatureData.RemoveAt(0);

            _humidityData.Add(humidity);
            _temperatureData.Add(temperature);
        }

        private void OnCanvasViewPaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            var canvas = e.Surface.Canvas;
            float width = e.Info.Width;
            float height = e.Info.Height;
            canvas.Clear(SKColors.White);

            float marginLeft = 80, marginBottom = 60, marginTop = 60, marginRight = 40;
            float graphWidth = width - marginLeft - marginRight;
            float graphHeight = height - marginBottom - marginTop;

            using var paintGrid = new SKPaint { Color = new SKColor(60, 60, 60, 100), StrokeWidth = 1, IsAntialias = true };
            using var paintAxes = new SKPaint { Color = SKColors.Black, StrokeWidth = 2, IsAntialias = true };
            using var paintTextY = new SKPaint { Color = new SKColor(40, 40, 40), TextSize = 22, IsAntialias = true, TextAlign = SKTextAlign.Right };
            using var paintTextX = new SKPaint { Color = new SKColor(40, 40, 40), TextSize = 22, IsAntialias = true, TextAlign = SKTextAlign.Center };

            for (int y = 0; y <= 100; y += 10)
            {
                float py = marginTop + graphHeight - (y / 100f * graphHeight);
                canvas.DrawLine(marginLeft, py, marginLeft + graphWidth, py, paintGrid);
                canvas.DrawText(y.ToString(), marginLeft - 10, py + 6, paintTextY);
            }

            for (int i = 0; i <= 12; i++)
            {
                float px = marginLeft + i * (graphWidth / 12f);
                canvas.DrawLine(px, marginTop, px, marginTop + graphHeight, paintGrid);
                canvas.DrawText(i.ToString(), px, marginTop + graphHeight + 25, paintTextX);
            }

            canvas.DrawLine(marginLeft, marginTop, marginLeft, marginTop + graphHeight, paintAxes);
            canvas.DrawLine(marginLeft, marginTop + graphHeight, marginLeft + graphWidth, marginTop + graphHeight, paintAxes);

            // Se dibuja la humedad (azul)
            DrawGraphLine(canvas, _humidityData, graphWidth, graphHeight, marginLeft, marginTop, SKColors.Aqua, 100f);

            // Se dibuja la temperatura (oro/amarillo)
            // 💡 Nota: La temperatura se escala a 100f. Si la temperatura máxima real es 50°C, 
            // considera cambiar el divisor (ej. 50f) para que se vea mejor. 
            // Por ahora, usamos 100f para que se mantenga dentro del gráfico de 0-100.
            DrawGraphLine(canvas, _temperatureData, graphWidth, graphHeight, marginLeft, marginTop, new SKColor(255, 215, 0), 100f);
        }

        // Se modifica para recibir la escala máxima
        private void DrawGraphLine(SKCanvas canvas, List<float> data, float graphWidth, float graphHeight, float marginLeft, float marginTop, SKColor color, float scaleMax)
        {
            if (data == null || data.Count < 2) return;

            var path = new SKPath();

            // Usamos scaleMax para calcular la posición Y
            float y0 = marginTop + graphHeight - (Math.Min(data[0], scaleMax) / scaleMax * graphHeight);
            path.MoveTo(marginLeft, y0);

            for (int i = 1; i < data.Count; i++)
            {
                float x = marginLeft + i * (graphWidth / 12f);
                // Usamos scaleMax
                float y = marginTop + graphHeight - (Math.Min(data[i], scaleMax) / scaleMax * graphHeight);
                path.LineTo(x, y);
            }

            using var paint = new SKPaint { Color = color, StrokeWidth = 4, Style = SKPaintStyle.Stroke, IsAntialias = true };
            canvas.DrawPath(path, paint);
        }
    }
}