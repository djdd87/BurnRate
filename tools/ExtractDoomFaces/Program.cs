// Extracts Doom face sprites from doom1.wad and saves them as PNGs.
// Usage: dotnet run <path-to-doom1.wad> <output-dir>
using System.Drawing;
using System.Drawing.Imaging;

if (args.Length < 2)
{
    Console.WriteLine("Usage: ExtractDoomFaces <doom1.wad> <output-dir>");
    return 1;
}

string wadPath = args[0];
string outDir  = args[1];
Directory.CreateDirectory(outDir);

byte[] wad = File.ReadAllBytes(wadPath);

// --- WAD header ---
// 0..3  = "IWAD"
// 4..7  = number of lumps (int32 LE)
// 8..11 = offset to directory (int32 LE)
int numLumps  = BitConverter.ToInt32(wad, 4);
int dirOffset = BitConverter.ToInt32(wad, 8);

// --- Build lump dictionary ---
var lumps = new Dictionary<string, (int offset, int size)>(StringComparer.OrdinalIgnoreCase);
for (int i = 0; i < numLumps; i++)
{
    int entryBase = dirOffset + i * 16;
    int offset = BitConverter.ToInt32(wad, entryBase);
    int size   = BitConverter.ToInt32(wad, entryBase + 4);
    // Name: 8 bytes, null-padded
    string name = System.Text.Encoding.ASCII.GetString(wad, entryBase + 8, 8).TrimEnd('\0');
    lumps[name] = (offset, size);
}

// --- Extract palette from PLAYPAL (first 256 RGB triplets) ---
if (!lumps.TryGetValue("PLAYPAL", out var palEntry))
    throw new Exception("PLAYPAL lump not found");

var palette = new Color[256];
for (int i = 0; i < 256; i++)
{
    int b = palEntry.offset + i * 3;
    palette[i] = Color.FromArgb(wad[b], wad[b + 1], wad[b + 2]);
}

// Face sprites to extract: (lumpName, outputFileName)
// Inverted health → usage mapping:
//   STFST00 = healthy (0-20% usage)
//   STFST10 = slight hurt (21-40%)
//   STFST20 = hurt (41-60%)
//   STFST30 = badly hurt (61-80%)
//   STFST40 = near death (81-99%)
//   STFDEAD0 = dead (100% / no data)
var sprites = new[]
{
    ("STFST00", "face_0.png"),
    ("STFST10", "face_1.png"),
    ("STFST20", "face_2.png"),
    ("STFST30", "face_3.png"),
    ("STFST40", "face_4.png"),
    ("STFDEAD0", "face_dead.png"),
};

foreach (var (lumpName, fileName) in sprites)
{
    if (!lumps.TryGetValue(lumpName, out var lump))
    {
        Console.WriteLine($"WARNING: lump {lumpName} not found, skipping");
        continue;
    }

    var bmp = DecodeDoomPicture(wad, lump.offset, palette);

    // Scale up to 32x32 using nearest-neighbour for pixel-art crispness
    var scaled = new Bitmap(32, 32, PixelFormat.Format32bppArgb);
    using (var g = Graphics.FromImage(scaled))
    {
        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
        g.PixelOffsetMode   = System.Drawing.Drawing2D.PixelOffsetMode.Half;
        g.Clear(Color.Transparent);
        // Centre the sprite in the 32x32 canvas
        float scale = Math.Min(32f / bmp.Width, 32f / bmp.Height);
        int   dw    = (int)(bmp.Width  * scale);
        int   dh    = (int)(bmp.Height * scale);
        int   dx    = (32 - dw) / 2;
        int   dy    = (32 - dh) / 2;
        g.DrawImage(bmp, dx, dy, dw, dh);
    }

    string outPath = Path.Combine(outDir, fileName);
    scaled.Save(outPath, ImageFormat.Png);
    Console.WriteLine($"Saved {outPath}  ({bmp.Width}x{bmp.Height} → 32x32)");
    bmp.Dispose();
    scaled.Dispose();
}

Console.WriteLine("Done.");
return 0;

// --- Doom picture format decoder ---
static Bitmap DecodeDoomPicture(byte[] data, int offset, Color[] palette)
{
    int width      = BitConverter.ToUInt16(data, offset);
    int height     = BitConverter.ToUInt16(data, offset + 2);
    // leftoffset = BitConverter.ToInt16(data, offset + 4);  // not needed
    // topoffset  = BitConverter.ToInt16(data, offset + 6);  // not needed

    var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);

    // Fill transparent
    using (var g = Graphics.FromImage(bmp))
        g.Clear(Color.Transparent);

    // Column offsets start at byte 8 (relative to start of picture data)
    for (int col = 0; col < width; col++)
    {
        int colOffset = BitConverter.ToInt32(data, offset + 8 + col * 4);
        int ptr = offset + colOffset;

        while (true)
        {
            byte topdelta = data[ptr++];
            if (topdelta == 0xFF) break;   // end of column

            byte length  = data[ptr++];
            ptr++;  // unused padding byte

            for (int row = 0; row < length; row++)
            {
                byte palIdx = data[ptr++];
                Color c = palette[palIdx];
                if (topdelta + row < height)
                    bmp.SetPixel(col, topdelta + row, c);
            }

            ptr++;  // unused padding byte
        }
    }

    return bmp;
}
