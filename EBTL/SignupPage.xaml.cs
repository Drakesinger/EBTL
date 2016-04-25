using System;
using System.IO;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace EBTL
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SignupPage : Page
    {
        private string debugPath = "\\bin\\Debug\\";
        private string releasePath = "\\bin\\Debug\\";
        private string baseDirPath = Directory.GetCurrentDirectory();

        private string _Delimiter = ",";

        private string AssetsFilePath;
        private Windows.Data.Xml.Dom.XmlDocument _DocCitiesCH;

        public SignupPage()
        {
            this.InitializeComponent();

            InitializeSignUpPage();
        }

        private async void InitializeSignUpPage()
        {
            try
            {
                Windows.Data.Xml.Dom.XmlDocument _DocCountries = await LoadXmlFile("Assets", "countries.xml");
                _DocCitiesCH = await LoadXmlFile("Assets", "cities_ch.xml");
                var _DocBloodTypes = await LoadXmlFile("Assets", "bloodtypes.xml"); // http://www.redcrossblood.org/learn-about-blood/blood-types
                //Scenario.RichEditBoxSetMsg(scenario2OriginalData, _DocCountries.GetXml(), true);

                Windows.Data.Xml.Dom.XmlNodeList nodes = _DocCountries.SelectNodes("//country");
                foreach (var item in nodes)
                {
                    comboBox_Country.Items.Add(item.InnerText);
                }

                foreach (var item in _DocBloodTypes.SelectNodes("//bloodtype"))
                {
                    comboBox_BloodType.Items.Add(item.InnerText);
                }
            }
            catch (Exception)
            {
                // Do nothing for now.
                //Scenario.RichEditBoxSetError(scenario2Result, exp.Message);
                //this.NotifyUser("Exception occured while loading xml file!", NotifyType.ErrorMessage);
            }
        }

        public static async Task<Windows.Data.Xml.Dom.XmlDocument> LoadXmlFile(String folder, String file)
        {
            Windows.Storage.StorageFolder storageFolder = await Windows.ApplicationModel.Package.Current.InstalledLocation.GetFolderAsync(folder);
            Windows.Storage.StorageFile storageFile = await storageFolder.GetFileAsync(file);
            Windows.Data.Xml.Dom.XmlLoadSettings loadSettings = new Windows.Data.Xml.Dom.XmlLoadSettings();
            loadSettings.ProhibitDtd = false;
            loadSettings.ResolveExternals = false;
            return await Windows.Data.Xml.Dom.XmlDocument.LoadFromFileAsync(storageFile, loadSettings);
        }

        // http://www.dotnetperls.com/combobox-wpf
        private void comboBox_Country_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Once this is changed.
            var _CountryComboBox = sender as ComboBox;
            var _SelectedCountry = _CountryComboBox.SelectedValue.ToString();
            if (_SelectedCountry.Contains("Switzerland"))
            {
                // Load the cities in Switzerland.
                if (_DocCitiesCH.HasChildNodes())
                {
                    var _List = _DocCitiesCH.SelectNodes("//Data[@Type='CityName']");
                    if (_List.Length != 0)
                    {
                        foreach (var city in _List)
                        {
                            comboBox_City.Items.Add(city.InnerText);
                        }
                    }
                }
            }
            else
            {
                textBox_Address_City.Visibility = Windows.UI.Xaml.Visibility.Visible;
                comboBox_City.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }
        }

        private System.Xml.XmlDocument loadXML(String FileName)
        {
            // Find out how to open files.
            FileStream XMLFile = File.OpenRead(AssetsFilePath + FileName);
            System.Xml.XmlDocument XMLDocument = new System.Xml.XmlDocument();
            XMLDocument.Load(XMLFile);
            return XMLDocument;
        }

        private void button_signUp_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            string _DonorSurName = textBox_Surname.Text;
            string _DonorName = textBox_Name.Text;
            string _DonorContactNumber = textBox_Number.Text;
            string _DonorAddressStreet = textBox_Address_Street.Text;
            string _DonorAddressNumber = textBox_Address_Nr.Text;
            string _DonorAddressCity;
            if (textBox_Address_City.Visibility == Windows.UI.Xaml.Visibility.Visible)
            {
                _DonorAddressCity = textBox_Address_City.Text;
            }
            else
            {
                _DonorAddressCity = comboBox_City.SelectedValue.ToString();
            }

            string _DonorBloodType = comboBox_BloodType.SelectedValue.ToString();
            string _DonorAddressCountry = comboBox_Country.SelectedValue.ToString();

            string _DonorAddress = _DonorAddressStreet + _Delimiter + _DonorAddressNumber + _Delimiter + _DonorAddressCity + _Delimiter + _DonorAddressCountry;

            var _Donor = new Donor(_DonorSurName, _DonorName, _DonorContactNumber, _DonorAddress, _DonorBloodType);

            // Load the geo-locating page and send data from this page over.
            this.Frame.Navigate((typeof(GeoLocationPage)), _Donor);
        }
    }
}