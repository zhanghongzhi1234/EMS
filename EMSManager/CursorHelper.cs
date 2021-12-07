using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.Security.Permissions;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.IO;

namespace TemplateProject
{
    public class CursorHelper
    {
        private static class NativeMethods
        {
            public struct IconInfo
            {
                public bool fIcon;
                public int xHotspot;
                public int yHotspot;
                public IntPtr hbmMask;
                public IntPtr hbmColor;
            }

            [DllImport("user32.dll")]
            public static extern SafeIconHandle CreateIconIndirect(ref IconInfo icon);

            [DllImport("user32.dll")]
            public static extern bool DestroyIcon(IntPtr hIcon);

            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool GetIconInfo(IntPtr hIcon, ref IconInfo pIconInfo);
        }

        [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
        private class SafeIconHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            public SafeIconHandle()
                : base(true)
            {
            }

            override protected bool ReleaseHandle()
            {
                return NativeMethods.DestroyIcon(handle);
            }
        }

        private static Cursor InternalCreateCursor(System.Drawing.Bitmap bmp)
        {
            var iconInfo = new NativeMethods.IconInfo();
            NativeMethods.GetIconInfo(bmp.GetHicon(), ref iconInfo);

            iconInfo.xHotspot = 0;
            iconInfo.yHotspot = 0;
            iconInfo.fIcon = false;

            SafeIconHandle cursorHandle = NativeMethods.CreateIconIndirect(ref iconInfo);
            return CursorInteropHelper.Create(cursorHandle);
        }

        public static Cursor CreateCursor(UIElement element)
        {
            DrawingVisual dv = new DrawingVisual();
            using (DrawingContext dc = dv.RenderOpen())
            {
                VisualBrush vb = new VisualBrush(element);
                dc.DrawRectangle(vb, null, new Rect(new Point(), element.DesiredSize));
            }

            // The BitmapSource that is rendered with a Visual.
            RenderTargetBitmap rtb = new RenderTargetBitmap(
            (int)element.DesiredSize.Width, (int)element.DesiredSize.Height,
            96, 96, PixelFormats.Pbgra32);
            rtb.Render(dv);

            // Encoding the RenderBitmapTarget into a bitmap (as PNG)
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(rtb));

            using (var ms = new MemoryStream())
            {
                encoder.Save(ms);
                using (var bmp = new System.Drawing.Bitmap(ms))
                {
                    return InternalCreateCursor(bmp);
                }
            }
        }
    }
}
