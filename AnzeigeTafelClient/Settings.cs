using AnzeigeTafelClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AnzeigeTafelClient
{
    public partial class Settings : Form
    {
        public string IP;
        public int PORT;

        public bool IR;
        public Settings(bool ir, string ip, int port)
        {

            IR = ir;
            IP = ip;
            PORT = port;
            

            InitializeComponent();
        }

        private void save_settings_to_disc()
        {
            if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"/AnzeigeTafel"))
            {
                Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"/AnzeigeTafel");
            }

            if (File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"/AnzeigeTafel/settings.conf"))
            {
                using (StreamWriter sw = new StreamWriter(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"/AnzeigeTafel/settings.conf", false))
                {
                    sw.WriteLine("IPADDRESS:" + IP);
                    sw.WriteLine("PORT:" + PORT);
                    sw.Close();
                }
            }
            else
            {
                File.Create(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"/AnzeigeTafel/settings.conf");
                save_settings_to_disc();
            }
        }

        public static bool IsValidIP(string Address)
        {
            IPAddress ip;
            if (IPAddress.TryParse(Address, out ip))
            {
                switch (ip.AddressFamily)
                {
                    case System.Net.Sockets.AddressFamily.InterNetwork:
                        if (Address.Length > 6 && Address.Contains("."))
                        {
                            string[] s = Address.Split('.');
                            if (s.Length == 4 && s[0].Length > 0 && s[1].Length > 0 && s[2].Length > 0 && s[3].Length > 0)
                                return true;
                        }
                        break;
                    case System.Net.Sockets.AddressFamily.InterNetworkV6:
                        if (Address.Contains(":") && Address.Length > 15)
                            return true;
                        break;
                    default:
                        break;
                }
            }

            return false;
        }


        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                int newport = Convert.ToInt32(tb_port.Text);

                if (newport > 1 && newport < 65535)
                {
                    PORT = newport;
                }
                else
                {
                    throw new Exception("Der Port muss eine Zahl zwischen 1 und 65535 sein.");
                }



                if (IsValidIP(tb_address.Text))
                {
                    IP = tb_address.Text;

                }
                else
                {
                    throw new Exception("Die IP-Adresse hat das falsche Format.");

                }


                save_settings_to_disc();

                this.Close();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }





        }

        private void Settings_Load(object sender, EventArgs e)
        {
            tb_address.Text = IP;
            tb_port.Text = Convert.ToString(PORT);

            if(IR)
            {
                tb_address.Enabled = false;
                tb_port.Enabled = false;
            }

        }
    }
}
