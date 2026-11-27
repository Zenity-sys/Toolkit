using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using Windows.Foundation;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;

namespace KavrosClueHelper
{
    // Called only by the analysis worker. All WinRT operations have a deadline
    // and honour cancellation; no network service or game input is involved.
    public sealed class NativeClueOcr
    {
        private OcrEngine engine;
        private bool initialized;
        public string Unavailable;
        private Stopwatch budget;
        private int budgetMs;
        private bool OverBudget()
        {
            return budget!=null && budget.ElapsedMilliseconds>budgetMs;
        }
        private T Wait<T>(IAsyncOperation<T> operation)
        {
            try
            {
                // The UI tick must not abort this wait. The whole Read call
                // shares one deadline and a failure becomes no text.
                while(operation.Status==AsyncStatus.Started)
                {
                    if(OverBudget())throw new TimeoutException("Windows text recognition timed out.");
                    Thread.Sleep(2);
                }
                if(OverBudget())throw new TimeoutException("Windows text recognition timed out.");
                return operation.GetResults();
            }
            catch { try{operation.Cancel();}catch(Exception){} throw; }
            finally{operation.Close();}
        }
        public string Read(Bitmap image)
        {
            return Read(image,2500);
        }
        public string Read(Bitmap image, int budgetMilliseconds)
        {
            try
            {
                if(image==null||budgetMilliseconds<1)return null;
                budget=Stopwatch.StartNew();
                budgetMs=budgetMilliseconds;
                if(Thread.CurrentThread.GetApartmentState()==ApartmentState.STA)
                    return System.Threading.Tasks.Task.Run(delegate{return ReadCore(image);}).GetAwaiter().GetResult();
                return ReadCore(image);
            }
            catch(Exception ex)
            {
                CrashLog.Write(ex);
                return null;
            }
        }
        private string ReadCore(Bitmap image)
        {
            try{return Recognize(image);}
            catch(Exception e)
            {
                TimeoutException timeout=e as TimeoutException;
                Unavailable=timeout!=null?e.Message:"Windows English text recognition is unavailable. Enable the English OCR language in Windows Settings.";
                return null;
            }
        }
        private string Recognize(Bitmap image)
        {
            if(!initialized)
            {
                initialized=true;
                engine=OcrEngine.TryCreateFromLanguage(new Language("en-US"));
                if(engine==null)engine=OcrEngine.TryCreateFromLanguage(new Language("en-GB"));
                if(engine==null)try{engine=OcrEngine.TryCreateFromUserProfileLanguages();}catch(Exception){engine=null;}
                if(engine==null){Unavailable="Enable English OCR in Windows Settings to read text clues.";return null;}
            }
            if(engine==null)return null;
            using(var bytes=new MemoryStream())
            using(var stream=new InMemoryRandomAccessStream())
            {
                image.Save(bytes,ImageFormat.Png);
                using(var writer=new DataWriter(stream.GetOutputStreamAt(0)))
                {writer.WriteBytes(bytes.ToArray());Wait<uint>(writer.StoreAsync());Wait<bool>(writer.FlushAsync());writer.DetachStream();}
                stream.Seek(0);BitmapDecoder decoder=Wait<BitmapDecoder>(BitmapDecoder.CreateAsync(stream));
                using(SoftwareBitmap bitmap=Wait<SoftwareBitmap>(decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8,BitmapAlphaMode.Premultiplied)))
                {return Wait<OcrResult>(engine.RecognizeAsync(bitmap)).Text;}
            }
        }
    }
}
