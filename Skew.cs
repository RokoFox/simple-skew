// Name: Skew
// Submenu: Distort
// Author: Roko Lisica
// Title:
// Version:
// Desc:
// Keywords:
// URL:
// Help:
#region UICode
IntSliderControl Amount1 = 0; // [0,100] Top Left Offset (%)
IntSliderControl Amount2 = 0; // [0,100] Bottom Left Offset (%)
IntSliderControl Amount3 = 0; // [0,100] Top Right Offset (%)
IntSliderControl Amount4 = 0; // [0,100] Bottom Right Offset (%)
#endregion

void Render(Surface dst, Surface src, Rectangle rect)
{
    Rectangle selection = EnvironmentParameters.SelectionBounds;

    int topLeftOffset = Amount1 * (selection.Bottom - selection.Top) / 100;
    int bottomLeftOffset = Amount2 * (selection.Bottom - selection.Top) / 100;
    int topRightOffset = Amount3 * (selection.Bottom - selection.Top) / 100;
    int bottomRightOffset = Amount4 * (selection.Bottom - selection.Top) / 100;

    int origHeight = selection.Bottom - selection.Top;
    int origWidth = selection.Right - selection.Left;

    if (topLeftOffset > origHeight - bottomLeftOffset)
    {
        topLeftOffset = origHeight - bottomLeftOffset - 1;
    }

    if (topRightOffset > origHeight - bottomRightOffset)
    {
        topRightOffset = origHeight - bottomRightOffset - 1;
    }

    ColorBgra currentPixel;
    for (int x = rect.Left; x < rect.Right; x++)
    {
        //int percentLeft = (rect.Right - x) / (double) origWidth;
        int currStart = selection.Top + (selection.Right - x) * topLeftOffset / origWidth
            + (x - selection.Left) * topRightOffset / origWidth;
        int currEnd = selection.Bottom - (selection.Right - x) * bottomLeftOffset / origWidth
            - (x - selection.Left) * bottomRightOffset / origWidth;
        int currHeight = currEnd - currStart;

        if (IsCancelRequested) return;
        for (int y = rect.Top; y < rect.Bottom; y++)
        {
            int yOld = (y - currStart) * origHeight / currHeight + selection.Top;
            if (yOld < 0 || yOld >= src.Size.Height)
            {
                dst[x,y] = ColorBgra.Transparent;
            }
            else {
                dst[x,y] = src[x,yOld];
            }
            
        }
    }
}
