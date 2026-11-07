using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Numerics;

Raylib.InitWindow(1200, 900, "Circulitos");
Raylib.SetTargetFPS(60);


const int canvasX = 260;
const int canvasY = 20;
const int canvasW = 920;
const int canvasH = 860;
Rectangle canvasRect = new Rectangle(canvasX, canvasY, canvasW, canvasH);

Color[] colors = new Color[]
{
    new Color(255, 255,   0, 255),
    new Color( 26,  64, 133, 255),
    new Color( 90,  78,  46, 255),
    new Color(225, 252, 200, 255),
    new Color(240,  96,  29, 255)
};
int indexA = 0, indexB = -1;
Color colorSeleccionado = colors[0];

const int palW = 48, palH = 48, palX = 20, palY0 = 30, palGap = 60;
const byte alphaByte = 128;


Herramienta herramientaActual = Herramienta.Linea;
float grosorActual = 4f;

Image baseImage = Raylib.GenImageColor(canvasW, canvasH, Color.RayWhite);
Texture2D baseTex = Raylib.LoadTextureFromImage(baseImage);
Raylib.SetTextureFilter(baseTex, TextureFilter.Point);


List<Trazo> trazos = new();
List<TrazoCirculo> circulos = new();

RenderTexture2D scratch = Raylib.LoadRenderTexture(canvasW, canvasH);


Vector2 mousePrev = Vector2.Zero;
bool dibujando = false;
Vector2 lineStart = Vector2.Zero;
bool drawingLine = false;

Vector2 circleCenter = Vector2.Zero;
bool drawingCircle = false;

string[] toolNames = { "Linea", "Pincel", "Cubeta", "Circulo", "Borrador" };


while (!Raylib.WindowShouldClose())
{
    Vector2 mouse = Raylib.GetMousePosition();

   
    if (Raylib.IsKeyPressed(KeyboardKey.M))
    {
        herramientaActual = (Herramienta)(((int)herramientaActual + 1) % 5);
        drawingLine = false;
        drawingCircle = false;
        dibujando = false;
    }


    if (Raylib.IsKeyPressed(KeyboardKey.Up))   grosorActual = MathF.Min(30f, grosorActual + 1f);
    if (Raylib.IsKeyPressed(KeyboardKey.Down)) grosorActual = MathF.Max(1f,  grosorActual - 1f);


    if (Raylib.IsKeyPressed(KeyboardKey.C))
    {
        trazos.Clear();
        circulos.Clear();
        Raylib.ImageClearBackground(ref baseImage, Color.RayWhite);
        Raylib.UnloadTexture(baseTex);
        baseTex = Raylib.LoadTextureFromImage(baseImage);
        Raylib.SetTextureFilter(baseTex, TextureFilter.Point);
    }

  
    for (int i = 0; i < colors.Length; i++)
    {
        Rectangle rec = new Rectangle(palX, palY0 + i * palGap, palW, palH);
        if (Raylib.CheckCollisionPointRec(mouse, rec))
        {
            if (Raylib.IsMouseButtonPressed(MouseButton.Left))
            {
                indexA = i;
                colorSeleccionado = colors[i];
            }
            if (Raylib.IsMouseButtonPressed(MouseButton.Right))
                indexB = i;
        }
    }

  
    Vector2 canvasPos = new Vector2(mouse.X - canvasX, mouse.Y - canvasY);
    bool onCanvas = Raylib.CheckCollisionPointRec(mouse, canvasRect);

    switch (herramientaActual)
    {
        case Herramienta.Linea:
            if (Raylib.IsMouseButtonPressed(MouseButton.Left) && onCanvas)
            {
                lineStart = ClampToCanvas(canvasPos);
                drawingLine = true;
            }
            if (Raylib.IsMouseButtonReleased(MouseButton.Left) && drawingLine)
            {
                Vector2 end = ClampToCanvas(canvasPos);
                trazos.Add(new Trazo(lineStart, end, colorSeleccionado, grosorActual));
                drawingLine = false;
            }
            break;

        case Herramienta.Pincel:
        case Herramienta.Borrador:
            if (Raylib.IsMouseButtonPressed(MouseButton.Left) && onCanvas)
            {
                dibujando = true;
                mousePrev = ClampToCanvas(canvasPos);
            }
            if (dibujando && Raylib.IsMouseButtonDown(MouseButton.Left))
            {
                Vector2 cur = ClampToCanvas(canvasPos);
                Color col = herramientaActual == Herramienta.Borrador ? Color.RayWhite : colorSeleccionado;
                if (Vector2.Distance(mousePrev, cur) > 0f)
                    trazos.Add(new Trazo(mousePrev, cur, col, grosorActual));
                mousePrev = cur;
            }
            if (Raylib.IsMouseButtonReleased(MouseButton.Left))
                dibujando = false;
            break;

        case Herramienta.Circulo:
            if (Raylib.IsMouseButtonPressed(MouseButton.Left) && onCanvas)
            {
                circleCenter = ClampToCanvas(canvasPos);
                drawingCircle = true;
            }
            if (Raylib.IsMouseButtonReleased(MouseButton.Left) && drawingCircle)
            {
                Vector2 end = ClampToCanvas(canvasPos);
                float radio = Vector2.Distance(circleCenter, end);
                circulos.Add(new TrazoCirculo(circleCenter, radio, colorSeleccionado));
                drawingCircle = false;
            }
            break;

        case Herramienta.Cubeta:
            if (Raylib.IsMouseButtonPressed(MouseButton.Left) && onCanvas)
                DoBucketFill(ClampToCanvas(canvasPos), colorSeleccionado);
            break;
    }

  
    Raylib.BeginDrawing();
    Raylib.ClearBackground(Color.RayWhite);

    Raylib.DrawRectangleRec(canvasRect, Color.RayWhite);
    Raylib.DrawTexture(baseTex, canvasX, canvasY, Color.White);

    Raylib.BeginScissorMode(canvasX, canvasY, canvasW, canvasH);
    Vector2 offset = new Vector2(canvasX, canvasY);
    foreach (var t in trazos)
        Raylib.DrawLineEx(t.Desde + offset, t.Hasta + offset, t.Grosor, t.Color);


    foreach (var ci in circulos)
        DibujarCirculoBresenham(
            (int)(ci.Centro.X + canvasX),
            (int)(ci.Centro.Y + canvasY),
            (int)ci.Radio,
            ci.Color);

    // PREVIEW 
    if (herramientaActual == Herramienta.Circulo && drawingCircle)
    {
        Vector2 cur = ClampToCanvas(canvasPos);
        float previewRadio = Vector2.Distance(circleCenter, cur);
        DibujarCirculoBresenham(
            (int)(circleCenter.X + canvasX),
            (int)(circleCenter.Y + canvasY),
            (int)previewRadio,
            colorSeleccionado);
    }
    // PREVIEW

    Raylib.EndScissorMode();
    Raylib.DrawRectangleLinesEx(canvasRect, 2, Color.Black);

  
    for (int i = 0; i < colors.Length; i++)
    {
        int x = palX;
        int y = palY0 + i * palGap;
        Rectangle rec = new Rectangle(x, y, palW, palH);
        Raylib.DrawRectangleRec(rec, colors[i]);
        bool hover = Raylib.CheckCollisionPointRec(mouse, rec);

        if (i == indexA)
            Raylib.DrawRectangleLinesEx(rec, 4, Color.Black);
        else
            Raylib.DrawRectangleLinesEx(rec, hover ? 3 : 1, hover ? Color.Black : Color.Gray);

        string hex = $"#{colors[i].R:X2}{colors[i].G:X2}{colors[i].B:X2}";
        Raylib.DrawText(hex, x + palW + 8, y + 16, 14, Color.Black);

        if (i == indexA && i == indexB)
        {
            Raylib.DrawText("A", x + 3,  y + 2, 22, Color.Black);
            Raylib.DrawText("B", x + 26, y + 2, 22, Color.Black);
        }
        else if (i == indexA) Raylib.DrawText("A", x + 3, y + 2, 22, Color.Black);
        else if (i == indexB) Raylib.DrawText("B", x + 3, y + 2, 22, Color.Black);
    }


    Color cA = indexA >= 0 ? colors[indexA] : Color.RayWhite;
    Color cB = indexB >= 0 ? colors[indexB] : Color.RayWhite;

    float af = alphaByte / 255f;
    byte mr = (byte)Math.Round(cA.R * af + cB.R * (1f - af));
    byte mg = (byte)Math.Round(cA.G * af + cB.G * (1f - af));
    byte mb = (byte)Math.Round(cA.B * af + cB.B * (1f - af));
    Color blended = new Color(mr, mg, mb, (byte)255);
    string blendedHex = $"#{blended.R:X2}{blended.G:X2}{blended.B:X2}";

    const int indSize = 36;
    const int indGap  = 10;
    int blendY = palY0 + colors.Length * palGap + 15;

    Raylib.DrawRectangleRec(new Rectangle(palX, blendY, indSize, indSize), cA);
    Raylib.DrawRectangleLinesEx(new Rectangle(palX, blendY, indSize, indSize), 1, Color.Gray);
    Raylib.DrawText("A", palX + 4, blendY + 4, 20, Color.Black);

    int midX = palX + indSize + indGap;
    Raylib.DrawRectangleRec(new Rectangle(midX, blendY, indSize, indSize), blended);
    Raylib.DrawRectangleLinesEx(new Rectangle(midX, blendY, indSize, indSize), 1, Color.Gray);

    int endX = midX + indSize + indGap;
    Raylib.DrawRectangleRec(new Rectangle(endX, blendY, indSize, indSize), cB);
    Raylib.DrawRectangleLinesEx(new Rectangle(endX, blendY, indSize, indSize), 1, Color.Gray);
    Raylib.DrawText("B", endX + 4, blendY + 4, 20, Color.Black);

    Raylib.DrawText(blendedHex, palX, blendY + indSize + 6, 14, Color.Black);

    
    int infoY = blendY + indSize + 40;
    Raylib.DrawText($"Herramienta: {toolNames[(int)herramientaActual]}", palX, infoY, 18, Color.Black);
    Raylib.DrawText($"Grosor: {grosorActual:F0}", palX, infoY + 26, 18, Color.Black);
    Raylib.DrawText($"Trazos: {trazos.Count}",  palX, infoY + 52, 18, Color.DarkGray);


    int toolsY = infoY + 96;
    Raylib.DrawText("Herramientas [M]:", palX, toolsY, 18, Color.Black);

    for (int i = 0; i < toolNames.Length; i++)
    {
        int ty = toolsY + 32 + i * 30;
        bool active = herramientaActual == (Herramienta)i;

        if (active)
            Raylib.DrawRectangleRec(new Rectangle(palX - 4, ty - 2, 210, 28),
                                    new Color(80, 140, 220, 80));

        Color c = active ? Color.Black : Color.DarkGray;
        string prefix = active ? "> " : "   ";
        Raylib.DrawText(prefix + toolNames[i], palX + 4, ty, 20, c);
    }


    int keysY = toolsY + 32 + toolNames.Length * 30 + 20;
    Raylib.DrawText("[M] cambiar herramienta", palX, keysY,      14, Color.DarkGray);
    Raylib.DrawText("[C] limpiar lienzo",      palX, keysY + 18, 14, Color.DarkGray);
    Raylib.DrawText("[Up/Down] grosor",        palX, keysY + 36, 14, Color.DarkGray);

    Raylib.EndDrawing();
}


Raylib.UnloadTexture(baseTex);
Raylib.UnloadImage(baseImage);
Raylib.UnloadRenderTexture(scratch);
Raylib.CloseWindow();



Vector2 ClampToCanvas(Vector2 p) => new Vector2(
    Math.Clamp(p.X, 0, canvasW - 1),
    Math.Clamp(p.Y, 0, canvasH - 1));

void DoBucketFill(Vector2 canvasPos, Color replacement)
{
    Raylib.BeginTextureMode(scratch);
    Raylib.ClearBackground(Color.RayWhite);
    Raylib.DrawTexture(baseTex, 0, 0, Color.White);
    foreach (var t in trazos)
        Raylib.DrawLineEx(t.Desde, t.Hasta, t.Grosor, t.Color);

    foreach (var ci in circulos)
        DibujarCirculoBresenham(
            (int)ci.Centro.X,
            (int)ci.Centro.Y,
            (int)ci.Radio,
            ci.Color);

    Raylib.EndTextureMode();

    Image composite = Raylib.LoadImageFromTexture(scratch.Texture);
    Raylib.ImageFlipVertical(ref composite);

    int x = Math.Clamp((int)canvasPos.X, 0, canvasW - 1);
    int y = Math.Clamp((int)canvasPos.Y, 0, canvasH - 1);
    FloodFillImage(ref composite, x, y, replacement);

    Raylib.UnloadImage(baseImage);
    baseImage = composite;
    Raylib.UnloadTexture(baseTex);
    baseTex = Raylib.LoadTextureFromImage(baseImage);
    Raylib.SetTextureFilter(baseTex, TextureFilter.Point);

    trazos.Clear();
    circulos.Clear();
}

static void FloodFillImage(ref Image img, int x, int y, Color replacement)
{
    int w = img.Width;
    int h = img.Height;
    if (x < 0 || x >= w || y < 0 || y >= h) return;

    Color[] pixels = new Color[w * h];
    for (int py = 0; py < h; py++)
        for (int px = 0; px < w; px++)
            pixels[py * w + px] = Raylib.GetImageColor(img, px, py);

    Color target = pixels[y * w + x];
    if (target.R == replacement.R && target.G == replacement.G
        && target.B == replacement.B && target.A == replacement.A) return;

    var stack = new Stack<int>();
    var changed = new List<int>();
    stack.Push(y * w + x);

    while (stack.Count > 0)
    {
        int idx = stack.Pop();
        Color p = pixels[idx];
        if (p.R != target.R || p.G != target.G
            || p.B != target.B || p.A != target.A) continue;

        pixels[idx] = replacement;
        changed.Add(idx);

        int cx = idx % w;
        int cy = idx / w;
        if (cx > 0)     stack.Push(idx - 1);
        if (cx < w - 1) stack.Push(idx + 1);
        if (cy > 0)     stack.Push(idx - w);
        if (cy < h - 1) stack.Push(idx + w);
    }

    foreach (int idx in changed)
    {
        int cx = idx % w;
        int cy = idx / w;
        Raylib.ImageDrawPixel(ref img, cx, cy, replacement);
    }
}


static void DibujarOchoPuntos(int xc, int yc, int x, int y, Color c)
{
    Raylib.DrawPixel(xc + x, yc + y, c);
    Raylib.DrawPixel(xc - x, yc + y, c);
    Raylib.DrawPixel(xc + x, yc - y, c);
    Raylib.DrawPixel(xc - x, yc - y, c);
    Raylib.DrawPixel(xc + y, yc + x, c);
    Raylib.DrawPixel(xc - y, yc + x, c);
    Raylib.DrawPixel(xc + y, yc - x, c);
    Raylib.DrawPixel(xc - y, yc - x, c);
}

static void DibujarCirculoBresenham(int xc, int yc, int r, Color c)
{
    int x = 0;
    int y = r;
    int e = 3 - 2 * r;

    while (x <= y)
    {
        DibujarOchoPuntos(xc, yc, x, y, c);

        if (e < 0)
            e += 4 * x + 6;
        else
        {
            e += 4 * (x - y) + 10;
            y--;
        }
        x++;
    }
}

enum Herramienta { Linea, Pincel, Cubeta, Circulo, Borrador }

struct Trazo
{
    public Vector2 Desde, Hasta;
    public Color Color;
    public float Grosor;
    public Trazo(Vector2 desde, Vector2 hasta, Color color, float grosor)
    {
        Desde = desde; Hasta = hasta; Color = color; Grosor = grosor;
    }
}

struct TrazoCirculo
{
    public Vector2 Centro;
    public float Radio;
    public Color Color;
    public TrazoCirculo(Vector2 centro, float radio, Color color)
    {
        Centro = centro; Radio = radio; Color = color;
    }
}