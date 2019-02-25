using System.ComponentModel;

namespace LauncherTwo
{
    public class PlayerInfo : INotifyPropertyChanged
    {
        public string Name;

        public PlayerInfo()
        {
            Name = string.Empty;
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
