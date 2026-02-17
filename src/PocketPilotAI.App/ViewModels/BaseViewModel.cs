using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PocketPilotAI.App.ViewModels;

public abstract class BaseViewModel : INotifyPropertyChanged
{
  public event PropertyChangedEventHandler? PropertyChanged;

  private bool isBusy;

  public bool IsBusy
  {
    get => isBusy;
    set => SetProperty(ref isBusy, value);
  }

  protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
  {
    if (EqualityComparer<T>.Default.Equals(storage, value))
    {
      return false;
    }

    storage = value;
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    return true;
  }
}
