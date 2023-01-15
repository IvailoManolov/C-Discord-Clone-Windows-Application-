using Client.MVVM.Mediator;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Client.MVVM.Model
{
    class UserModel :INotifyPropertyChanged
    {
        public string Username { get; set; }

        public string ImageSource { get; set; }
        public ObservableCollection<MessageModel> Messages { get; set; }

        public string LastMessage => Messages.Last().Message;

        public string UID { get; set; }

        private string userColor = "Red";

        public event PropertyChangedEventHandler PropertyChanged;

        public string UserMuted
        {
            get
            {
                return userColor;
            }
            set
            {
                userColor = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("UserMuted"));
                }
            }
        }
    }
}
