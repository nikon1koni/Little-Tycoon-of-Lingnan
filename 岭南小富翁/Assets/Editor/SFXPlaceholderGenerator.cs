using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class SFXPlaceholderGenerator
{
    private const int SampleRate = 44100;
    private const int BitsPerSample = 16;
    private const int Channels = 1;

    [MenuItem("Tools/Generate SFX Placeholders")]
    public static void Generate()
    {
        string basePath = Path.Combine(Application.dataPath, "Music", "SFX");

        GenerateUISFX(Path.Combine(basePath, "UI"));
        GenerateCharacterSFX(Path.Combine(basePath, "Character"));
        GenerateEventSFX(Path.Combine(basePath, "Event"));
        GenerateDiceSFX(Path.Combine(basePath, "Dice"));

        AssetDatabase.Refresh();
        Debug.Log("SFX placeholder generation complete.");
    }

    private static void GenerateUISFX(string dir)
    {
        WriteWav(Path.Combine(dir, "click.wav"), 0.05f, (t, f) => Sine(t, 1200f));
        WriteWav(Path.Combine(dir, "hover.wav"), 0.07f, (t, f) => Sine(t, 900f));
        WriteWav(Path.Combine(dir, "open.wav"), 0.1f, (t, f) => Sine(t, 800f));
        WriteWav(Path.Combine(dir, "close.wav"), 0.08f, (t, f) => Sine(t, 1000f));
    }

    private static void GenerateCharacterSFX(string dir)
    {
        WriteWav(Path.Combine(dir, "jump.wav"), 0.2f, (t, f) => Sine(t, 400f + t * 600f) * Decay(t, 0.2f));
        WriteWav(Path.Combine(dir, "land.wav"), 0.15f, (t, f) => Sine(t, 300f) * Decay(t, 0.15f));
        WriteWav(Path.Combine(dir, "move.wav"), 0.1f, (t, f) => Sine(t, 500f) * Decay(t, 0.1f));
    }

    private static void GenerateEventSFX(string dir)
    {
        WriteWav(Path.Combine(dir, "gain_money.wav"), 0.3f, (t, f) => Sine(t, 400f + t * 400f) * Decay(t, 0.3f));
        WriteWav(Path.Combine(dir, "lose_money.wav"), 0.35f, (t, f) => Sine(t, 600f - t * 400f) * Decay(t, 0.35f));
        WriteWav(Path.Combine(dir, "property_bought.wav"), 0.25f, (t, f) => Sine(t, 350f + t * 250f) * Decay(t, 0.25f));
        WriteWav(Path.Combine(dir, "building_placed.wav"), 0.3f, (t, f) => Sine(t, 300f + t * 300f) * Decay(t, 0.3f));
        WriteWav(Path.Combine(dir, "building_upgraded.wav"), 0.4f, (t, f) => Sine(t, 250f + t * 350f) * Decay(t, 0.4f));
        WriteWav(Path.Combine(dir, "go_to_jail.wav"), 0.5f, (t, f) => Sine(t, 500f - t * 300f) * Decay(t, 0.5f));
        WriteWav(Path.Combine(dir, "tax_paid.wav"), 0.25f, (t, f) => Sine(t, 450f - t * 200f) * Decay(t, 0.25f));
        WriteWav(Path.Combine(dir, "buff_activated.wav"), 0.35f, (t, f) => Sine(t, 200f + t * 400f) * Decay(t, 0.35f));
    }

    private static void GenerateDiceSFX(string dir)
    {
        WriteWav(Path.Combine(dir, "dice_roll.wav"), 0.5f, (t, f) => Noise() * Decay(t, 0.5f));
        WriteWav(Path.Combine(dir, "dice_stop.wav"), 0.3f, (t, f) => (Noise() * 0.6f + Sine(t, 250f) * 0.4f) * Decay(t, 0.3f));
    }

    private static float Sine(float t, float freq)
    {
        return Mathf.Sin(2f * Mathf.PI * freq * t);
    }

    private static float Decay(float t, float duration)
    {
        return 1f - t / duration;
    }

    private static float Noise()
    {
        return UnityEngine.Random.Range(-1f, 1f);
    }

    private static void WriteWav(string path, float duration, Func<float, float, float> sampleFunc)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));

        int sampleCount = Mathf.CeilToInt(SampleRate * duration);
        byte[] data = new byte[sampleCount * 2];

        float peak = 0f;
        float[] floatSamples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SampleRate;
            float s = sampleFunc(t, duration);
            floatSamples[i] = s;
            if (Mathf.Abs(s) > peak) peak = Mathf.Abs(s);
        }

        float normalize = peak > 0.001f ? 0.9f / peak : 1f;
        for (int i = 0; i < sampleCount; i++)
        {
            short val = (short)Mathf.Clamp(floatSamples[i] * normalize * 32767f, -32768f, 32767f);
            byte[] bytes = BitConverter.GetBytes(val);
            data[i * 2] = bytes[0];
            data[i * 2 + 1] = bytes[1];
        }

        using (var fs = new FileStream(path, FileMode.Create))
        using (var bw = new BinaryWriter(fs))
        {
            int dataSize = data.Length;
            int fmtChunkSize = 16;
            int fileSize = 4 + (8 + fmtChunkSize) + (8 + dataSize);

            bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            bw.Write(fileSize);
            bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));

            bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            bw.Write(fmtChunkSize);
            bw.Write((short)1);
            bw.Write((short)Channels);
            bw.Write(SampleRate);
            bw.Write(SampleRate * Channels * BitsPerSample / 8);
            bw.Write((short)(Channels * BitsPerSample / 8));
            bw.Write((short)BitsPerSample);

            bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            bw.Write(dataSize);
            bw.Write(data);
        }
    }
}
