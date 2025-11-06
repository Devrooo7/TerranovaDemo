using Microsoft.Maui.Controls;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Maui.Dispatching;

namespace TerranovaDemo;

public partial class MainPage : ContentPage
{
    private readonly List<float> _humidityData = new();
    private readonly List<float> _temperatureData = new();
    private bool _bombaEncendida = false;
    private readonly System.Timers.Timer _timer;
    private bool _esp32Disponible = false;

    private const string ESP32_IP = "192.168.1.16";

    public MainPage()
    {
        InitializeComponent();

        // Inicializa datos de ejemplo
        for (int i = 0; i < 12; i++)
        {
            _humidityData.Add(30);
            _temperatureData.Add(25);
        }

        CheckESP32Connection();

        _timer = new System.Timers.Timer(1000);
        _timer.Elapsed += (s, e) => MainThread.BeginInvokeOnMainThread(() =>
        {
            SensorsCanvas.InvalidateSurface();
        });
        _timer.Start();
    }

    private async void CheckESP32Connection()
    {
        while (true)
        {
            try
            {
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
                var response = await http.GetAsync($"http://{ESP32_IP}/ping");
                _esp32Disponible = response.IsSuccessStatusCode;
            }
            catch
            {
                _esp32Disponible = false;
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                BombaButton.IsEnabled = _esp32Disponible;
                BombaButton.BackgroundColor = _esp32Disponible ? Colors.Red : Colors.Gray;
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
        string url = $"http://{ESP32_IP}/bomba/{action}";

        try
        {
            using var http = new HttpClient();
            var response = await http.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                await DisplayAlert("Error", "No se pudo enviar la señal a la bomba.", "OK");
        }
        catch
        {
            await DisplayAlert("Error", "No se pudo conectar al ESP32.", "OK");
        }
    }

    private void UpdateBombaButton()
    {
        BombaButton.Text = _bombaEncendida ? "ON" : "OFF";
        BombaButton.BackgroundColor = _bombaEncendida ? Colors.Green : Colors.Red;
    }

    public void UpdateSensorData(float humidity, float temperature)
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

        // Márgenes
        float marginLeft = 80, marginBottom = 60, marginTop = 60, marginRight = 40;
        float graphWidth = width - marginLeft - marginRight;
        float graphHeight = height - marginBottom - marginTop;

        // Cuadrícula y ejes
        using var paintGrid = new SKPaint
        {
            Color = new SKColor(60, 60, 60, 100),
            StrokeWidth = 1,
            IsAntialias = true
        };
        using var paintAxes = new SKPaint
        {
            Color = SKColors.Black,
            StrokeWidth = 2,
            IsAntialias = true
        };

        // Texto auxiliar
        using var paintTextY = new SKPaint
        {
            Color = new SKColor(40, 40, 40),
            TextSize = 22,
            IsAntialias = true,
            TextAlign = SKTextAlign.Right
        };
        using var paintTextX = new SKPaint
        {
            Color = new SKColor(40, 40, 40),
            TextSize = 22,
            IsAntialias = true,
            TextAlign = SKTextAlign.Center
        };

        // Cuadrícula horizontal + etiquetas Y (0-100)
        for (int y = 0; y <= 100; y += 10)
        {
            float py = marginTop + graphHeight - (y / 100f * graphHeight);
            canvas.DrawLine(marginLeft, py, marginLeft + graphWidth, py, paintGrid);
            canvas.DrawText(y.ToString(), marginLeft - 10, py + 6, paintTextY);
        }

        // Cuadrícula vertical + etiquetas X (12 puntos como horas)
        for (int i = 0; i <= 12; i++)
        {
            float px = marginLeft + i * (graphWidth / 12f);
            canvas.DrawLine(px, marginTop, px, marginTop + graphHeight, paintGrid);
            canvas.DrawText(i.ToString(), px, marginTop + graphHeight + 25, paintTextX);
        }

        // Ejes
        canvas.DrawLine(marginLeft, marginTop, marginLeft, marginTop + graphHeight, paintAxes);
        canvas.DrawLine(marginLeft, marginTop + graphHeight, marginLeft + graphWidth, marginTop + graphHeight, paintAxes);

        // Colores base
        var baseHumidityColor = SKColors.Aqua;
        var baseGoldenColor = new SKColor(255, 215, 0); // dorado real
        var neonYellowColor = SKColors.Yellow; // línea de temperatura neon

        int count = Math.Min(_humidityData.Count, _temperatureData.Count);
        if (count < 2) return;

        // Mantos más visibles
        // --- HUMEDAD ---
        var pathHumidity = new SKPath();
        pathHumidity.MoveTo(marginLeft, marginTop + graphHeight - (_humidityData[0] / 100f * graphHeight));
        for (int i = 1; i < _humidityData.Count; i++)
        {
            float x = marginLeft + i * (graphWidth / 12f);
            float y = marginTop + graphHeight - (_humidityData[i] / 100f * graphHeight);
            pathHumidity.LineTo(x, y);
        }
        pathHumidity.LineTo(marginLeft + graphWidth, marginTop + graphHeight);
        pathHumidity.LineTo(marginLeft, marginTop + graphHeight);
        pathHumidity.Close();

        using var paintFillH = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Shader = SKShader.CreateLinearGradient(
                new SKPoint(0, marginTop),
                new SKPoint(0, marginTop + graphHeight),
                new[] { new SKColor(0, 255, 255, 180), new SKColor(0, 255, 255, 60) },
                null,
                SKShaderTileMode.Clamp)
        };
        canvas.DrawPath(pathHumidity, paintFillH);

        // --- TEMPERATURA (manto dorado)
        var pathTemperature = new SKPath();
        pathTemperature.MoveTo(marginLeft, marginTop + graphHeight - (_temperatureData[0] / 100f * graphHeight));
        for (int i = 1; i < _temperatureData.Count; i++)
        {
            float x = marginLeft + i * (graphWidth / 12f);
            float y = marginTop + graphHeight - (_temperatureData[i] / 100f * graphHeight);
            pathTemperature.LineTo(x, y);
        }
        pathTemperature.LineTo(marginLeft + graphWidth, marginTop + graphHeight);
        pathTemperature.LineTo(marginLeft, marginTop + graphHeight);
        pathTemperature.Close();

        using var paintFillT = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Shader = SKShader.CreateLinearGradient(
                new SKPoint(0, marginTop),
                new SKPoint(0, marginTop + graphHeight),
                new[] { new SKColor(255, 215, 0, 170), new SKColor(255, 215, 0, 60) },
                null,
                SKShaderTileMode.Clamp)
        };
        canvas.DrawPath(pathTemperature, paintFillT);

        // Líneas principales
        for (int i = 0; i < _humidityData.Count - 1; i++)
        {
            float x1 = marginLeft + i * (graphWidth / 12f);
            float x2 = x1 + (graphWidth / 12f);
            float y1H = marginTop + graphHeight - (_humidityData[i] / 100f * graphHeight);
            float y2H = marginTop + graphHeight - (_humidityData[i + 1] / 100f * graphHeight);
            float y1T = marginTop + graphHeight - (_temperatureData[i] / 100f * graphHeight);
            float y2T = marginTop + graphHeight - (_temperatureData[i + 1] / 100f * graphHeight);

            float alphaFactor = 0.25f + (0.75f * (i / (float)(_humidityData.Count - 1)));

            using var paintH = new SKPaint
            {
                Color = new SKColor(0, 255, 255, (byte)(255 * alphaFactor)),
                StrokeWidth = 4,
                IsAntialias = true,
                StrokeCap = SKStrokeCap.Round
            };
            using var paintT = new SKPaint
            {
                Color = neonYellowColor, // línea neon
                StrokeWidth = 4,
                IsAntialias = true,
                StrokeCap = SKStrokeCap.Round
            };

            canvas.DrawLine(x1, y1H, x2, y2H, paintH);
            canvas.DrawLine(x1, y1T, x2, y2T, paintT);
        }

        // Puntos finales destacados
        float finalX = marginLeft + (count - 1) * (graphWidth / 12f);
        float pyH = marginTop + graphHeight - (_humidityData[^1] / 100f * graphHeight);
        float pyT = marginTop + graphHeight - (_temperatureData[^1] / 100f * graphHeight);

        canvas.DrawCircle(finalX, pyH, 6, new SKPaint { Color = baseHumidityColor, IsAntialias = true });
        canvas.DrawCircle(finalX, pyT, 6, new SKPaint { Color = baseGoldenColor, IsAntialias = true });

        // Leyendas
        string textHumidity = $"Humedad: {_humidityData[^1]:F1}%";
        string textTemperature = $"Temperatura: {_temperatureData[^1]:F1}°C";
        using var paintLegend = new SKPaint { IsAntialias = true, TextSize = 30 };

        paintLegend.Color = baseHumidityColor;
        canvas.DrawText(textHumidity, marginLeft, marginTop - 20, paintLegend);
        paintLegend.Color = baseGoldenColor;
        canvas.DrawText(textTemperature, marginLeft + 300, marginTop - 20, paintLegend);
    }
}
