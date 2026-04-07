using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace testSoulChat;

public static class ClipboardHelper
{
    public static void SetText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        RunInSta(() => Clipboard.SetText(text));
    }

    public static void SetImage(Image image)
    {
        ArgumentNullException.ThrowIfNull(image);

        using var clonedImage = new Bitmap(image);
        RunInSta(() => Clipboard.SetImage(clonedImage));
    }

    private static void RunInSta(Action action)
    {
        Exception? threadException = null;

        var staThread = new Thread(() =>
        {
            for (var attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    action();
                    return;
                }
                catch (ExternalException) when (attempt < 2)
                {
                    Thread.Sleep(120);
                }
                catch (Exception ex)
                {
                    threadException = ex;
                    return;
                }
            }
        });

        staThread.SetApartmentState(ApartmentState.STA);
        staThread.Start();
        staThread.Join();

        if (threadException is not null)
        {
            throw new InvalidOperationException("Failed to access clipboard.", threadException);
        }
    }
}
