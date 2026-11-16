namespace BookReviewSystem.Views;

/// <summary>
/// Base class for every window. Makes the layout scale with the Windows display
/// scaling setting (100%, 125%, 150%...) so nothing is clipped on high-DPI screens.
/// </summary>
public class BaseForm : Form
{
    public BaseForm()
    {
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = new Font("Segoe UI", 9F);
    }
}
