using System.ComponentModel;

namespace LauncherTwo
{
    public class LevelInfo : INotifyPropertyChanged
    {
        private string _name;
        public string Name { get { return _name; } set { _name = value; NotifyPropertyChanged("Name"); } }

        private string _guid;
        public string GUID { get { return _guid; } set { _guid = value; NotifyPropertyChanged("GUID"); } }

        public LevelInfo()
        {
            Name = string.Empty;
            GUID = string.Empty;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
