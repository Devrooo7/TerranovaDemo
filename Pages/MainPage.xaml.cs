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

    // Cambia la IP de tu ESP32 aquí
    private const string ESP32_IP = "192.168.1.16";

    public MainPage()
    {
        InitializeComponent();

        // Inicializar datos de ejemplo
        for (int i = 0; i < 12; i++)
        {
            _humidityData.Add(30);
            _temperatureData.Add(25);
        }

        // Timer para refrescar la gráfica
        _timer = new System.Timers.Timer(1000);
        _timer.Elapsed += (s, e) => MainThread.BeginInvokeOnMainThread(() =>
        {
            SensorsCanvas.InvalidateSurface();
        });
        _timer.Start();
    }

    // Botón de la bomba
    private async void BombaButton_Clicked(object sender, EventArgs e)
    {
        _bombaEncendida = !_bombaEncendida;
        UpdateBombaButton();

        string action = _bombaEncendida ? "on" : "off";
        string url = $"http://{ESP32_IP}/bomba/{action}";

        try
        {
            using var http = new HttpClient();
            var response = await http.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                await DisplayAlert("Error", "No se pudo enviar la señal a la bomba", "OK");
        }
        catch
        {
            await DisplayAlert("Error", "No se pudo conectar al ESP32", "OK");
        }
    }

    private void UpdateBombaButton()
    {
        BombaButton.Text = _bombaEncendida ? "ON" : "OFF";
        BombaButton.BackgroundColor = _bombaEncendida ? Colors.Green : Colors.Red;
    }

    // Método para recibir datos del ESP32
    public void UpdateSensorData(float humidity, float temperature)
    {
        if (_humidityData.Count >= 12) _humidityData.RemoveAt(0);
        if (_temperatureData.Count >= 12) _temperatureData.RemoveAt(0);

        _humidityData.Add(humidity);
        _temperatureData.Add(temperature);
    }

    // Pintado de la gráfica
    private void OnCanvasViewPaintSurface(object sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        float width = e.Info.Width;
        float height = e.Info.Height;

        canvas.Clear();

        // Fondo degradado verde
        var bgTop = new SKColor(144, 238, 144);
        var bgBottom = new SKColor(0, 100, 0);
        using (var paintBg = new SKPaint
        {
            Shader = SKShader.CreateLinearGradient(
                new SKPoint(0, 0),
                new SKPoint(0, height),
                new[] { bgTop, bgBottom },
                null,
                SKShaderTileMode.Clamp)
        })
        {
            canvas.DrawRect(new SKRect(0, 0, width, height), paintBg);
        }

        float marginLeft = 60, marginBottom = 50, marginTop = 60, marginRight = 10;
        float graphWidth = width - marginLeft - marginRight;
        float graphHeight = height - marginBottom - marginTop;

        // Líneas de la cuadrícula
        var paintGrid = new SKPaint { Color = new SKColor(255, 255, 255, 35), StrokeWidth = 1, IsAntialias = true };
        for (int y = 0; y <= 60; y += 5)
        {
            float py = marginTop + graphHeight - (y / 60f * graphHeight);
            canvas.DrawLine(marginLeft, py, marginLeft + graphWidth, py, paintGrid);
        }
        for (int i = 0; i <= 12; i++)
        {
            float px = marginLeft + i * (graphWidth / 12f);
            canvas.DrawLine(px, marginTop, px, marginTop + graphHeight, paintGrid);
        }

        // Ejes
        var paintAxes = new SKPaint { Color = new SKColor(255, 255, 255, 70), StrokeWidth = 2, IsAntialias = true };
        canvas.DrawLine(marginLeft, marginTop, marginLeft, marginTop + graphHeight, paintAxes);
        canvas.DrawLine(marginLeft, marginTop + graphHeight, marginLeft + graphWidth, marginTop + graphHeight, paintAxes);

        // Dibujar líneas de humedad y temperatura
        var neonGreen = new SKColor(57, 255, 20);
        var cyan = new SKColor(0, 220, 255);

        for (int i = 0; i < _humidityData.Count - 1; i++)
        {
            float x1 = marginLeft + i * (graphWidth / 12f);
            float y1H = marginTop + graphHeight - (_humidityData[i] / 100f * graphHeight);
            float y2H = marginTop + graphHeight - (_humidityData[i + 1] / 100f * graphHeight);
            canvas.DrawLine(x1, y1H, x1 + (graphWidth / 12f), y2H, new SKPaint { Color = neonGreen, StrokeWidth = 3.5f, IsAntialias = true });

            float y1T = marginTop + graphHeight - (_temperatureData[i] / 60f * graphHeight);
            float y2T = marginTop + graphHeight - (_temperatureData[i + 1] / 60f * graphHeight);
            canvas.DrawLine(x1, y1T, x1 + (graphWidth / 12f), y2T, new SKPaint { Color = cyan, StrokeWidth = 3.5f, IsAntialias = true });
        }

        // Dibujar puntos
        for (int i = 0; i < _humidityData.Count; i++)
        {
            float px = marginLeft + i * (graphWidth / 12f);
            float pyH = marginTop + graphHeight - (_humidityData[i] / 100f * graphHeight);
            float pyT = marginTop + graphHeight - (_temperatureData[i] / 60f * graphHeight);
            canvas.DrawCircle(px, pyH, 3.8f, new SKPaint { Color = neonGreen, IsAntialias = true });
            canvas.DrawCircle(px, pyT, 3.8f, new SKPaint { Color = cyan, IsAntialias = true });
        }

        // Mostrar valores actuales en cápsulas
        var fontLegend = new SKFont(SKTypeface.FromFamilyName(null, SKFontStyle.Bold), 30);
        float padding = 14;

        string textHumidity = $"Humedad: {_humidityData[^1]:F1}%";
        string textTemperature = $"Temperatura: {_temperatureData[^1]:F1}°C";

        var paintH = new SKPaint { Typeface = fontLegend.Typeface, TextSize = fontLegend.Size };
        var paintT = new SKPaint { Typeface = fontLegend.Typeface, TextSize = fontLegend.Size };

        float widthH = paintH.MeasureText(textHumidity);
        float widthT = paintT.MeasureText(textTemperature);

        var metricsH = paintH.FontMetrics;
        float heightH = metricsH.Descent - metricsH.Ascent;
        var metricsT = paintT.FontMetrics;
        float heightT = metricsT.Descent - metricsT.Ascent;

        float xStart = 60;
        float yStart = 10;

        var capsuleBg = new SKPaint { Color = new SKColor(10, 50, 10, 180), IsAntialias = true, Style = SKPaintStyle.Fill };
        var capsuleStroke = new SKPaint { Color = new SKColor(80, 200, 80), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1.5f };

        var rectH = new SKRect(xStart, yStart, xStart + widthH + 2 * padding, yStart + heightH + 2 * padding);
        canvas.DrawRoundRect(new SKRoundRect(rectH, 16, 16), capsuleBg);
        canvas.DrawRoundRect(new SKRoundRect(rectH, 16, 16), capsuleStroke);
        canvas.DrawText(textHumidity, rectH.MidX, rectH.MidY + heightH / 2.8f, new SKPaint
        {
            Color = neonGreen,
            IsAntialias = true,
            TextAlign = SKTextAlign.Center,
            TextSize = fontLegend.Size
        });

        float gap = 22;
        var rectT = new SKRect(rectH.Right + gap, yStart, rectH.Right + gap + widthT + 2 * padding, yStart + heightT + 2 * padding);
        canvas.DrawRoundRect(new SKRoundRect(rectT, 16, 16), capsuleBg);
        canvas.DrawRoundRect(new SKRoundRect(rectT, 16, 16), capsuleStroke);
        canvas.DrawText(textTemperature, rectT.MidX, rectT.MidY + heightT / 2.8f, new SKPaint
        {
            Color = cyan,
            IsAntialias = true,
            TextAlign = SKTextAlign.Center,
            TextSize = fontLegend.Size
        });
    }
}
