using System.ComponentModel;

namespace LauncherTwo
{
    public class PlayerInfo : INotifyPropertyChanged
    {
        private string _name;
        public string Name { get { return _name; } set { _name = value; NotifyPropertyChanged("Name"); } }

        public PlayerInfo()
        {
            Name = string.Empty;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
