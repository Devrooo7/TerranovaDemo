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
                HumidityLabel.FormattedText = new FormattedString
                {
                    Spans =
                     {
                       new Span { Text = "Humedad: ", FontAttributes = FontAttributes.Bold, TextColor = Colors.Black },
                       new Span { Text = $"{data.humedadSuelo}%", TextColor = Colors.Black }
                     }
                };

                TemperatureLabel.FormattedText = new FormattedString
                {
                    Spans =
                     {
                       new Span { Text = "Temp: ", FontAttributes = FontAttributes.Bold, TextColor = Colors.Black },
                       new Span { Text = $"{data.temperatura:F1}°C", TextColor = Colors.Black }
                     }
                };


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
            canvas.Clear(SKColors.Transparent);

            float marginLeft = 55, marginBottom = 70, marginTop = 70, marginRight = 25;
            float graphWidth = width - marginLeft - marginRight;
            float graphHeight = height - marginBottom - marginTop;

            // --- Sombra global detrás ---
            var shadowRect = new SKRect(10, 10, width - 10, height - 10);
            using (var shadowPaint = new SKPaint
            {
                Color = new SKColor(0, 0, 0, 90),
                IsAntialias = true,
                ImageFilter = SKImageFilter.CreateBlur(18, 18)
            })
            {
                canvas.DrawRoundRect(shadowRect, 14, 14, shadowPaint);
            }

            // --- Fondo degradado ---
            using var bg = new SKPaint
            {
                Shader = SKShader.CreateLinearGradient(
                    new SKPoint(0, 0),
                    new SKPoint(0, height),
                    new[]
                    {
                new SKColor(245, 245, 245),
                new SKColor(230, 240, 236)
                    },
                    null,
                    SKShaderTileMode.Clamp)
            };
            canvas.DrawRect(0, 0, width, height, bg);

            // -----------------------------  
            // 📌 Definir colores de líneas  
            // -----------------------------
            var humedadColor = new SKColor(0, 140, 255); // azul
            var tempColor = new SKColor(255, 180, 0);    // amarillo

            // 📌 Cuadrícula
            using var grid = new SKPaint
            {
                Color = new SKColor(150, 150, 150, 55),
                StrokeWidth = 1,
                IsAntialias = true
            };

            using var axis = new SKPaint
            {
                Color = new SKColor(40, 40, 40),
                StrokeWidth = 3,
                IsAntialias = true
            };

            using var textY = new SKPaint { Color = new SKColor(60, 60, 60), TextSize = 18, IsAntialias = true, TextAlign = SKTextAlign.Right };
            using var textX = new SKPaint { Color = new SKColor(60, 60, 60), TextSize = 18, IsAntialias = true, TextAlign = SKTextAlign.Center };

            // Líneas horizontales
            for (int y = 0; y <= 100; y += 10)
            {
                float py = marginTop + graphHeight - (y / 100f * graphHeight);
                canvas.DrawLine(marginLeft, py, marginLeft + graphWidth, py, grid);
                canvas.DrawText(y.ToString(), marginLeft - 12, py + 8, textY);
            }

            // Líneas verticales
            for (int i = 0; i <= 12; i++)
            {
                float px = marginLeft + i * (graphWidth / 12f);
                canvas.DrawLine(px, marginTop, px, marginTop + graphHeight, grid);
                canvas.DrawText(i.ToString(), px, marginTop + graphHeight + 30, textX);
            }

            // Ejes
            canvas.DrawLine(marginLeft, marginTop, marginLeft, marginTop + graphHeight, axis);
            canvas.DrawLine(marginLeft, marginTop + graphHeight, marginLeft + graphWidth, marginTop + graphHeight, axis);

            // -----------------------------  
            // 📈 Líneas con manto  
            // -----------------------------
            DrawGraphLine(canvas, _humidityData, graphWidth, graphHeight, marginLeft, marginTop, humedadColor, 100f);
            DrawGraphLine(canvas, _temperatureData, graphWidth, graphHeight, marginLeft, marginTop, tempColor, 100f);

            // ===================================================================
            // 🏷️ ETIQUETAS DE HUMEDAD Y TEMPERATURA DENTRO DE LA GRÁFICA
            // ===================================================================
            // ===================================================================
            // 🏷️ ETIQUETAS COMPACTAS Y PROPORCIONALES
            // ===================================================================
            SKPaint bgLabelPaint = new SKPaint
            {
                Color = new SKColor(255, 255, 255, 160),
                IsAntialias = true,
                MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 2)
            };

            SKPaint labelPaint = new SKPaint
            {
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold),
                TextSize = 18,  // 🔥 más pequeño
            };

            // --- HUMEDAD ---
            string textH = "Humedad";
            labelPaint.Color = humedadColor;
            float wH = labelPaint.MeasureText(textH);

            // caja compacta
            var rectH = new SKRect(
                marginLeft + 10,               // más adentro
                marginTop + 5,                 // más bajo
                marginLeft + wH + 30,
                marginTop + 40                 // altura compacta
            );

            canvas.DrawRoundRect(rectH, 10, 10, bgLabelPaint);
            canvas.DrawText(textH, rectH.Left + 15, rectH.Bottom - 10, labelPaint);

            // --- TEMPERATURA ---
            string textT = "Temperatura";
            labelPaint.Color = tempColor;
            float wT = labelPaint.MeasureText(textT);

            var rectT = new SKRect(
                rectH.Right + 20,              // separación pequeña
                marginTop + 5,
                rectH.Right + 20 + wT + 30,
                marginTop + 40
            );

            canvas.DrawRoundRect(rectT, 10, 10, bgLabelPaint);
            canvas.DrawText(textT, rectT.Left + 15, rectT.Bottom - 10, labelPaint);

        }




        // Se modifica para recibir la escala máxima
        private void DrawGraphLine(SKCanvas canvas, List<float> data,
    float graphWidth, float graphHeight, float marginLeft, float marginTop,
    SKColor color, float scaleMax)
        {
            if (data == null || data.Count < 2) return;

            float stepX = graphWidth / 12f;

            List<SKPoint> points = new();
            for (int i = 0; i < data.Count; i++)
            {
                float x = marginLeft + i * stepX;
                float y = marginTop + graphHeight - (Math.Min(data[i], scaleMax) / scaleMax * graphHeight);
                points.Add(new SKPoint(x, y));
            }

            // --- Curva suave (Bezier cuadrática) ---
            SKPath path = new SKPath();
            path.MoveTo(points[0]);

            for (int i = 1; i < points.Count; i++)
            {
                var p0 = points[i - 1];
                var p1 = points[i];

                float cx = (p0.X + p1.X) / 2;
                float cy = (p0.Y + p1.Y) / 2;

                path.QuadTo(p0.X, p0.Y, cx, cy);
            }

            // --- 🌫 Manto debajo de la línea (relleno sombreado) ---
            SKPath area = new SKPath(path);
            area.LineTo(points[^1].X, marginTop + graphHeight);
            area.LineTo(points[0].X, marginTop + graphHeight);
            area.Close();

            using var fill = new SKPaint
            {
                Shader = SKShader.CreateLinearGradient(
                    points[0],
                    new SKPoint(points[0].X, marginTop + graphHeight),
                    new[]
                    {
                new SKColor(color.Red, color.Green, color.Blue, 90),
                new SKColor(color.Red, color.Green, color.Blue, 0)
                    },
                    null,
                    SKShaderTileMode.Clamp),
                Style = SKPaintStyle.Fill
            };

            canvas.DrawPath(area, fill);

            // --- 🌫 Glow exterior ---
            using var glow = new SKPaint
            {
                Color = new SKColor(color.Red, color.Green, color.Blue, 120),
                StrokeWidth = 12,
                Style = SKPaintStyle.Stroke,
                IsAntialias = true,
                ImageFilter = SKImageFilter.CreateBlur(8, 8)
            };
            canvas.DrawPath(path, glow);

            // --- Línea principal ---
            using var paint = new SKPaint
            {
                Color = color,
                StrokeWidth = 5,
                Style = SKPaintStyle.Stroke,
                IsAntialias = true
            };
            canvas.DrawPath(path, paint);

            // --- Punto final destacado ---
            using var dot = new SKPaint
            {
                Color = color,
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };
            canvas.DrawCircle(points[^1], 10, dot);
        }


    }
}