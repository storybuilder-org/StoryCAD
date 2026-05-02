using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.Messaging;
using StoryCADLib.Services.Messages;
using static StoryCADLib.Services.MacInterop.ObjCRuntime;

namespace StoryCADLib.ViewModels.Tools;

public partial class PrintReportDialogVM
{
    private async void StartPrintMenu()
    {
        if (_isPrinting)
        {
            return;
        }

        _isPrinting = true;

        try
        {
            if (!OperatingSystem.IsMacOS())
            {
                // Non-macOS desktop (e.g. Linux): fall back to PDF export
                await ExportReportsToPdfAsync();
                return;
            }

            var pdfBytes = await GeneratePdfBytesAsync();
            if (pdfBytes.Length == 0)
            {
                Messenger.Send(new StatusChangedMessage(
                    new StatusMessage("Unable to generate report for printing.", LogLevel.Error)));
                return;
            }

            ShowMacPrintDialog(pdfBytes);
        }
        catch (Exception ex)
        {
            _logService.LogException(LogLevel.Error, ex, "Failed to print on macOS.");
            Messenger.Send(new StatusChangedMessage(
                new StatusMessage("Printing failed: " + ex.Message, LogLevel.Error)));
            await Window.ShowContentDialog(new ContentDialog
            {
                Title = "Printing error",
                Content = "The following error occurred when trying to print:\n\n" + ex.Message,
                PrimaryButtonText = "OK"
            }, true);
        }
        finally
        {
            _isPrinting = false;
        }
    }

    private void ShowMacPrintDialog(byte[] pdfBytes)
    {
        // Ensure Quartz.framework is loaded (provides PDFKit / PDFDocument)
        var quartzHandle = dlopen("/System/Library/Frameworks/Quartz.framework/Quartz", RTLD_LAZY);
        if (quartzHandle == IntPtr.Zero)
        {
            _logService.Log(LogLevel.Error, "Failed to load Quartz.framework for printing");
            throw new InvalidOperationException("Could not load Quartz.framework required for printing.");
        }

        // Create NSData from PDF bytes
        var nsDataClass = objc_getClass("NSData");
        var dataWithBytesSel = sel_registerName("dataWithBytes:length:");
        var pinnedBytes = GCHandle.Alloc(pdfBytes, GCHandleType.Pinned);
        IntPtr nsData;
        try
        {
            nsData = objc_msgSend(nsDataClass, dataWithBytesSel,
                pinnedBytes.AddrOfPinnedObject(), (nuint)pdfBytes.Length);
        }
        finally
        {
            pinnedBytes.Free();
        }

        if (nsData == IntPtr.Zero)
        {
            throw new InvalidOperationException("Failed to create NSData from PDF bytes.");
        }

        // Create PDFDocument from NSData
        var pdfDocClass = objc_getClass("PDFDocument");
        if (pdfDocClass == IntPtr.Zero)
        {
            throw new InvalidOperationException("PDFDocument class not found. Quartz.framework may not be loaded.");
        }

        var alloc = objc_msgSend(pdfDocClass, sel_registerName("alloc"));
        var pdfDoc = objc_msgSend(alloc, sel_registerName("initWithData:"), nsData);
        if (pdfDoc == IntPtr.Zero)
        {
            throw new InvalidOperationException("Failed to create PDFDocument from PDF data.");
        }

        // Get shared NSPrintInfo
        var printInfoClass = objc_getClass("NSPrintInfo");
        var printInfo = objc_msgSend(printInfoClass, sel_registerName("sharedPrintInfo"));

        // Create NSPrintOperation from PDFDocument
        // kPDFPrintPageScaleNone = 0
        var printOp = objc_msgSend(pdfDoc,
            sel_registerName("printOperationForPrintInfo:scalingMode:autoRotate:"),
            printInfo, 0L, true);

        if (printOp == IntPtr.Zero)
        {
            throw new InvalidOperationException("Failed to create NSPrintOperation.");
        }

        // Show the native macOS print dialog
        var success = objc_msgSend_bool(printOp, sel_registerName("runOperation"));

        if (success != 0)
        {
            Messenger.Send(new StatusChangedMessage(
                new StatusMessage("Report sent to printer.", LogLevel.Info)));
        }
        else
        {
            Messenger.Send(new StatusChangedMessage(
                new StatusMessage("Printing was cancelled.", LogLevel.Info)));
        }
    }
}
