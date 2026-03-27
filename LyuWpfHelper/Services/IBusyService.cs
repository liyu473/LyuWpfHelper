using System.Windows;

namespace LyuWpfHelper.Services;

public interface IBusyService
{
    void SetOwnerWindow(Window owner);

    void Show(BusyDisplayOptions options);

    void Hide();
}
