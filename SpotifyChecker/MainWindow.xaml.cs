using Leaf.xNet;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SpotifyChecker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static string ResultsPath = Environment.CurrentDirectory + @"\\Results\\";
        private static string CurrentTime = DateTime.Now.ToString("dd-MM-yyyy hh-mm-ss");

        #region Vars
        private int _goodCount;
        private int _badCount;

        private int _checkedCount;
        private int _proxyErrors;

        private string[]? _combos;
        private string[]? _proxies;
        #endregion

        public MainWindow()
        {
            InitializeComponent();
            
            if (!Directory.Exists(ResultsPath))
                Directory.CreateDirectory(ResultsPath);
        }

        private void DragWindow(object sender, MouseButtonEventArgs e)
        {
            try
            {
                DragMove();
            }
            catch (Exception)
            {
                // throw
            }
        }

        private void WriteToFile(string email)
        {
            string fileName = ResultsPath = "[Good] " + CurrentTime + ".txt";
            File.AppendAllText(fileName, email + Environment.NewLine);
        }

        // Load combos elements
        private void Btn_LoadCombos_Click(object sender, RoutedEventArgs e)
        {
            _combos = LoadInFileDialog("Browse Combos", "Combos|*.txt");
            Lbl_TotalCombos.Content = _combos?.Length.ToString();
        }

        // Load proxies
        private void Btn_LoadProxy_Click(object sender, RoutedEventArgs e)
        {
            _proxies = LoadInFileDialog("Browse Combos", "Combos|*.txt");
            Lbl_TotalProxy.Content = _proxies?.Length.ToString();
        }

        private string? RandomProxy()
        {
            Random rand = new Random();
            if (_proxies != null)
                return _proxies[rand.Next(_proxies.Length)];
            else
                return null;
        }

        private void Checker(string email)
        {
            string url = $"https://spclient.wg.spotify.com/signup/public/v1/account?validate=1&email={email}";

            HttpRequest httpRequest = new HttpRequest();
            httpRequest.UserAgent = Http.ChromeUserAgent();

            while (true)
            {
                if (Cmb_ProxyType.SelectedItem is ComboBoxItem item)
                {
                    switch (item.Content)
                    {
                        case "HTTPS":
                            httpRequest.Proxy = HttpProxyClient.Parse(RandomProxy());
                            break;
                        case "SOCKS 4":
                            httpRequest.Proxy = Socks4ProxyClient.Parse(RandomProxy());
                            break;
                        case "SOCKS 5":
                            httpRequest.Proxy = Socks5ProxyClient.Parse(RandomProxy());
                            break;
                    }

                    try
                    {
                        string response = httpRequest.Get(url).ToString();

                        // OLD CHECK: That email is already registered to an account.
                        if (response.Contains("{\"status\":20"))
                        {
                            _goodCount++;
                            _checkedCount++;

                            string[] row = { email };
                            var listItem = new ListViewItem();
                            listItem.Content = row;
                            Listv_Results.Items.Add(listItem);

                            WriteToFile(email);
                            break;
                        }
                        else
                        {
                            _badCount++;
                            _checkedCount++;
                            break;
                        }
                    }
                    catch (HttpException ex)
                    {
                        if (ex.Status.Equals(HttpExceptionStatus.ConnectFailure))
                            _proxyErrors++;
                    }
                }
            }

            UpdateStatUI();
        }

        private void UpdateStatUI()
        {
            Lbl_Bad.Content = _badCount.ToString();
            Lbl_Good.Content = _goodCount.ToString();
            Lbl_Checked.Content = _checkedCount.ToString();
            Lbl_Errors.Content = _proxyErrors.ToString();
        }

        private static string[]? LoadInFileDialog(string title, string filter)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = title;
            openFileDialog.Filter = filter;

            if (openFileDialog.ShowDialog() == true)
            {
                string[] values = File.ReadAllLines(openFileDialog.FileName);
                return values;                              
            }
            else
                return null;
        }

        private void SeparateThread()
        {
            if (_combos != null)
            {
                Parallel.ForEach(
                    _combos,
                    new ParallelOptions
                    {
                        MaxDegreeOfParallelism = int.Parse(Txtb_Threads.Text)
                    },
                    combo =>
                    {
                        Checker(combo);
                    }
                    );
            }
            else
                throw new ArgumentNullException("Combos is Null!");
        }

        private void Btn_Start_Click(object sender, RoutedEventArgs e)
        {
            Thread thread = new Thread(SeparateThread);
            thread.Start();
            thread.IsBackground = true;
        }
    }
}
