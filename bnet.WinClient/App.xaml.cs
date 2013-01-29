
namespace BESharp.WinClient
{
    using System.IO;
    using System.Windows;
    using log4net.Config;


    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            XmlConfigurator.ConfigureAndWatch(new FileInfo("log4net.config"));
            base.OnStartup(e);
        }
    }
}
