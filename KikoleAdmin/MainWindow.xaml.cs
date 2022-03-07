using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace KikoleAdmin
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ApiProvider _apiProvider;

        public MainWindow()
        {
            InitializeComponent();
            _apiProvider = new ApiProvider();
            CbbCountries.ItemsSource = _apiProvider
                .GetCountriesAsync()
                .GetAwaiter()
                .GetResult()
                .OrderBy(x => x.Name);
        }

        private void AddClubButton_Click(object sender, RoutedEventArgs e)
        {
            var altNames = new List<string>();
            for (var i = 0; i <= 9; i++)
            {
                var txt = (FindName($"AltClubName{i}") as TextBox).Text;
                if (!string.IsNullOrWhiteSpace(txt))
                    altNames.Add(txt);
            }

            try
            {
                _apiProvider
                    .AddClubAsync(new Club
                    {
                        Name = MainClubName.Text,
                        AllowedNames = altNames
                    })
                    .GetAwaiter()
                    .GetResult();
                var result = MessageBox.Show("Ajouté ! Clear ?", "Ok", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    MainClubName.Text = string.Empty;
                    for (var i = 0; i <= 9; i++)
                    {
                        (FindName($"AltClubName{i}") as TextBox).Text = string.Empty;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void AddPlayer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var req = new PlayerRequest
                {
                    ProposalDate = string.IsNullOrWhiteSpace(TxtProposalDate.Text)
                        ? default(DateTime?)
                        : DateTime.Parse(TxtProposalDate.Text),
                    Clubs = ClubsPanel.Children.Cast<ComboBox>().Select(cbb => (cbb.SelectedItem as Club).Id).ToList(),
                    Clue = TxtClue.Text,
                    Country = (CbbCountries.SelectedItem as Country).Code,
                    AllowedNames = TxtAllowedNames.Text.Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries),
                    Name = TxtPlayerName.Text,
                    YearOfBirth = ushort.Parse(TxtPlayerYear.Text)
                };

                _apiProvider.AddPlayerAsync(req).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void AddClubBtn_Click(object sender, RoutedEventArgs e)
        {
            var cbb = new ComboBox
            {
                ItemsSource = _apiProvider
                    .GetClubsAsync()
                    .GetAwaiter()
                    .GetResult()
                    .OrderBy(x => x.Name),
                DisplayMemberPath = "Name",
                Width = 250
            };
            ClubsPanel.Children.Add(cbb);
        }
    }
}
