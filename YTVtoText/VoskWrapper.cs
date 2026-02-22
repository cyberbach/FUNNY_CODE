using System;
using System.IO;
using System.Runtime.InteropServices;

namespace YTVtoText.VoskWrapper;

public static class VoskLib
{
    static VoskLib()
    {
        // Пытаемся загрузить библиотеку явно с полным путём
        string appDir = AppDomain.CurrentDomain.BaseDirectory;
        
        // Пробуем разные имена DLL
        string[] possibleNames = { "vosk.dll", "libvosk.dll" };
        string? voskPath = null;
        
        foreach (var name in possibleNames)
        {
            string path = Path.Combine(appDir, name);
            if (File.Exists(path))
            {
                voskPath = path;
                break;
            }
        }
        
        if (voskPath != null)
        {
            try
            {
                // Загружаем библиотеку с указанием полного пути
                NativeLibrary.Load(voskPath);
            }
            catch (Exception ex)
            {
                throw new Exception($"Не удалось загрузить vosk.dll: {ex.Message}. Убедитесь, что все зависимости (libgcc_s_seh-1.dll, libstdc++-6.dll, libwinpthread-1.dll) находятся в той же папке.");
            }
        }
        else
        {
            throw new Exception($"vosk.dll не найден в папке: {appDir}. Доступные имена: vosk.dll, libvosk.dll");
        }
    }

    private const string DllName = "vosk";

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void vosk_set_log_level(int level);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr vosk_model_new(string path);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void vosk_model_free(IntPtr model);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr vosk_recognizer_new(IntPtr model, float sample_rate);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void vosk_recognizer_free(IntPtr recognizer);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int vosk_recognizer_accept_waveform(IntPtr recognizer, byte[] data, int len);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr vosk_recognizer_result(IntPtr recognizer);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr vosk_recognizer_final_result(IntPtr recognizer);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr vosk_recognizer_partial_result(IntPtr recognizer);

    public static string GetStringResult(IntPtr ptr)
    {
        // Vosk возвращает UTF-8 строку
        return Marshal.PtrToStringUTF8(ptr) ?? "";
    }
}

public class VoskModel : IDisposable
{
    private IntPtr _model;
    private bool _disposed;

    public VoskModel(string path)
    {
        _model = VoskLib.vosk_model_new(path);
        if (_model == IntPtr.Zero)
            throw new Exception($"Не удалось загрузить модель Vosk из: {path}");
    }

    public IntPtr Handle => _model;

    public void Dispose()
    {
        if (!_disposed)
        {
            VoskLib.vosk_model_free(_model);
            _disposed = true;
        }
    }
}

public class VoskRecognizer : IDisposable
{
    private IntPtr _recognizer;
    private bool _disposed;

    public VoskRecognizer(VoskModel model, float sampleRate)
    {
        _recognizer = VoskLib.vosk_recognizer_new(model.Handle, sampleRate);
        if (_recognizer == IntPtr.Zero)
            throw new Exception("Не удалось создать распознаватель Vosk");
    }

    public bool AcceptWaveform(byte[] data, int length)
    {
        return VoskLib.vosk_recognizer_accept_waveform(_recognizer, data, length) != 0;
    }

    public string Result()
    {
        return VoskLib.GetStringResult(VoskLib.vosk_recognizer_result(_recognizer));
    }

    public string FinalResult()
    {
        return VoskLib.GetStringResult(VoskLib.vosk_recognizer_final_result(_recognizer));
    }

    public string PartialResult()
    {
        return VoskLib.GetStringResult(VoskLib.vosk_recognizer_partial_result(_recognizer));
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            VoskLib.vosk_recognizer_free(_recognizer);
            _disposed = true;
        }
    }
}
